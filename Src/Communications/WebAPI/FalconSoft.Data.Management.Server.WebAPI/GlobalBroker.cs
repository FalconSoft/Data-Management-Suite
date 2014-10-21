using System;
using System.Collections.Generic;
using System.Linq;
using FalconSoft.Data.Management.Common.Facades;
using FalconSoft.Data.Management.Common.Metadata;

namespace FalconSoft.Data.Management.Server.WebAPI
{
    public class GlobalBroker : IFalconSoftBroker
    {
        private readonly IRabbitMQBroker _rabbitMQBroker;
        private readonly IReactiveDataQueryFacade _reactiveDataQueryFacade;
        private readonly IMetaDataAdminFacade _metaDataAdminFacade;
        private readonly IPermissionSecurityFacade _permissionSecurityFacade;
        private readonly ISecurityFacade _securityFacade;

        private const string MetadataExchangeName = "MetaDataFacadeExchange";
        private const string MetadataExceptionsExchangeName = "MetaDataFacadeExceptionsExchangeName";
        private const string PermissionSecurityFacadeExchangeName = "PermissionSecurityFacadeExchange";
        private const string ExceptionsExchangeName = "SecurityFacadeExceptionsExchangeName";
        private const string ReactiveDataQueryTopic = "GetDataChangesTopic";

        private readonly Dictionary<string, DispoceItems> _getDataChangesDispocebles = new Dictionary<string, DispoceItems>();
        private readonly Dictionary<string, IDisposable> _getPermissionChangesDisposables = new Dictionary<string, IDisposable>();

        public GlobalBroker(IRabbitMQBroker rabbitMQBroker, 
            IReactiveDataQueryFacade reactiveDataQueryFacade, 
            IMetaDataAdminFacade metaDataAdminFacade,
            IPermissionSecurityFacade permissionSecurityFacade, 
            ISecurityFacade securityFacade)
        {
            _rabbitMQBroker = rabbitMQBroker;
            _reactiveDataQueryFacade = reactiveDataQueryFacade;
            _metaDataAdminFacade = metaDataAdminFacade;
            _permissionSecurityFacade = permissionSecurityFacade;
            _securityFacade = securityFacade;

            // meta data fasade exchanges
            _rabbitMQBroker.CreateExchange(MetadataExchangeName, "fanout");
            _rabbitMQBroker.CreateExchange(MetadataExceptionsExchangeName, "fanout");

            _metaDataAdminFacade.ObjectInfoChanged += MetaDataAdminFacadeOnObjectInfoChanged;
            _metaDataAdminFacade.ObjectInfoChanged += DispoceSubscribitionOnObjectInfoChanged;
            _metaDataAdminFacade.ErrorMessageHandledAction = ErrorMessageHandledAction;

            // security exchange
            _rabbitMQBroker.CreateExchange(ExceptionsExchangeName, "fanout");
            _securityFacade.ErrorMessageHandledAction = SecurityErrorMessageHandledAction;

            // permission exchange
            _rabbitMQBroker.CreateExchange(PermissionSecurityFacadeExchangeName, "direct");

            // reactive data query exchange for get data changes
            _rabbitMQBroker.CreateExchange(ReactiveDataQueryTopic, "topic");
        }

        private void DispoceSubscribitionOnObjectInfoChanged(object sender, SourceObjectChangedEventArgs sourceObjectChangedEventArgs)
        {

            if (sourceObjectChangedEventArgs.ChangedActionType == ChangedActionType.Delete &&
                sourceObjectChangedEventArgs.ChangedObjectType == ChangedObjectType.DataSourceInfo)
            {
                var dataSource = (DataSourceInfo)sourceObjectChangedEventArgs.SourceObjectInfo;

                var keys = _getDataChangesDispocebles.Keys.Where(k => k.Contains(dataSource.DataSourcePath));
                var array = keys as string[] ?? keys.ToArray();
                if (array.Any())
                {
                    foreach (string key in array)
                    {
                        _getDataChangesDispocebles[key].Disposable.Dispose();
                        _getDataChangesDispocebles.Remove(key);
                    }
                }
            }

        }

        public void SubscribeOnGetDataChanges(string userToken, string dataSourcePath, string[] fields = null)
        {
            var routingKey = fields != null ? string.Format("{0}.{1}.", dataSourcePath, userToken) + fields.Aggregate("",
              (cur, next) => string.Format("{0}.{1}", cur, next)).GetHashCode() : string.Format("{0}.{1}", dataSourcePath, userToken);

            if (_getDataChangesDispocebles.ContainsKey(routingKey))
            {
                _getDataChangesDispocebles[routingKey].Count++;
                return;
            }

            var disposer = _reactiveDataQueryFacade.GetDataChanges(userToken, dataSourcePath, fields).Subscribe(
                rcpArgs =>
                {
                    var messageBytes = _rabbitMQBroker.CastToBytes(rcpArgs);

                    _rabbitMQBroker.SendMessage(messageBytes, ReactiveDataQueryTopic, "topic", routingKey);
                });

            _getDataChangesDispocebles.Add(routingKey, new DispoceItems { Count = 1, Disposable = disposer });
        }

        public void SubscribeOnGetPermissionChanges(string userToken)
        {
            if (_getPermissionChangesDisposables.ContainsKey(userToken)) return;

            var disposer = _permissionSecurityFacade.GetPermissionChanged(userToken).Subscribe(data =>
            {
                var messageBytes = _rabbitMQBroker.CastToBytes(data);

                _rabbitMQBroker.SendMessage(messageBytes, PermissionSecurityFacadeExchangeName, "direct", userToken);
            });

            _getPermissionChangesDisposables.Add(userToken, disposer);
        }

        private void MetaDataAdminFacadeOnObjectInfoChanged(object sender, SourceObjectChangedEventArgs sourceObjectChangedEventArgs)
        {
            var messageBytes = _rabbitMQBroker.CastToBytes(sourceObjectChangedEventArgs);
            _rabbitMQBroker.SendMessage(messageBytes, MetadataExchangeName, "fanout", "");
        }
        
        private void ErrorMessageHandledAction(string arg1, string arg2)
        {
            var typle = string.Format("{0}[#]{1}", arg1, arg2);
            var messageBytes = _rabbitMQBroker.CastToBytes(typle);
            _rabbitMQBroker.SendMessage(messageBytes, MetadataExceptionsExchangeName, "fanout", "");
        }

        private void SecurityErrorMessageHandledAction(string arg1, string arg2)
        {
            var typle = string.Format("{0}#{1}", arg1, arg2);
            var messageBytes = _rabbitMQBroker.CastToBytes(typle);
            _rabbitMQBroker.SendMessage(messageBytes, ExceptionsExchangeName, "fanout", "");
        }

        private sealed class DispoceItems
        {
            public int Count { get; set; }

            public IDisposable Disposable { get; set; }
        }

        public void Dispose()
        {
            _metaDataAdminFacade.ObjectInfoChanged -= MetaDataAdminFacadeOnObjectInfoChanged;
            _metaDataAdminFacade.ObjectInfoChanged -= DispoceSubscribitionOnObjectInfoChanged;

            _metaDataAdminFacade.ErrorMessageHandledAction = null;
            _securityFacade.ErrorMessageHandledAction = null;

            _reactiveDataQueryFacade.Dispose();
        }
    }

    public interface IFalconSoftBroker :IDisposable
    {
        void SubscribeOnGetDataChanges(string userToken, string dataSourcePath, string[] fields = null);

        void SubscribeOnGetPermissionChanges(string userToken);
    }
}
