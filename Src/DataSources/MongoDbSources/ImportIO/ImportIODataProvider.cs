using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using FalconSoft.ReactiveWorksheets.Common;
using FalconSoft.ReactiveWorksheets.Common.Metadata;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace FalconSoft.ReactiveWorksheets.MongoDbSources.ImportIO
{
    public class ImportIODataProvider : IDataProvider
    {

        public ImportIODataProvider(string userGuid, string apiKey, string apiGuid, IEnumerable<string> urls)
        {
            _apiGuid = apiGuid;
            _urls.Clear();
            _urls.AddRange(urls);
            _receivedData.Clear();
            _io = new ImportIOExtractor("https://query.import.io", Guid.Parse(userGuid), apiKey, OnError);
        }
        public ImportIODataProvider(ImportIOInfo importIOInfo)
        {
            _apiGuid = importIOInfo.ExtractorGuid;
            _urls.Clear();
            _urls.AddRange(importIOInfo.SourceUrls);
            _receivedData.Clear();
            _io = new ImportIOExtractor("https://query.import.io", Guid.Parse(importIOInfo.UserGuid), importIOInfo.ApiKey, OnError);
            ImportIOInfo = importIOInfo;
        }
        #region PrivateProperties
        private readonly List<Dictionary<string, object>> _receivedData = new List<Dictionary<string, object>>();
        private CountdownEvent _countdownLatch;
        private readonly string _apiGuid;
        private readonly List<string> _urls = new List<string>();
        private readonly ImportIOExtractor _io;
        private static readonly Object LockThis = new object();
        public ImportIOInfo ImportIOInfo { get; private set; }
        #endregion PrivateProperties
        public event EventHandler<ValueChangedEventArgs> RecordChangedEvent;
        public List<Dictionary<string, object>> GetData(string[] fields = null, IList<FilterRule> whereCondition = null)
        {
            lock (LockThis)
            {
                if (_io.Connect())
                {
                    foreach (var url in _urls)
                    {
                        _countdownLatch = new CountdownEvent(1);
                        var query = new Dictionary<string, object>
                        {
                            {
                                "input",
                                new Dictionary<String, String>
                                {
                                    {"webpage/url", url}
                                }
                            },
                            {"connectorGuids", new List<String> {_apiGuid}}
                        };
                        _io.DoQuery(query, HandleQuery);
                        _countdownLatch.Wait();
                    }
                    _io.Disconnect();
                }
                return _receivedData;
            }
        }

        public RevisionInfo SubmitChanges(List<Dictionary<string, object>> recordsToChange, List<string> recordsToDelete, string comment = null)
        {
            throw new NotImplementedException();
        }

        private void HandleQuery(Query query, Dictionary<String, Object> message)
        {
            if (message["type"].Equals("MESSAGE"))
            {
                //TODO
                if (JObject.Parse(JsonConvert.SerializeObject(message["data"]))["results"] == null)
                {
                    return;
                }
                foreach (var x in JObject.Parse(JsonConvert.SerializeObject(message["data"]))["results"].Select(t => t.ToString()))
                {
                    _receivedData.Add(JObject.Parse(x).ToObject<Dictionary<string, object>>());
                }
            }
            if (query.IsFinished)
            {
                try
                {
                    _countdownLatch.Signal();
                }
                catch (InvalidOperationException) { }
            }
        }
        public RevisionInfo SubmitChanges(List<Dictionary<string, object>> recordsToInsert, List<Dictionary<string, object>> recordsToUpdate, List<Dictionary<string, object>> recordsToDelete, string comment = null)
        {
            throw new NotImplementedException();
        }

        public void UpdateSourceInfo(object sourceInfo)
        {
            throw new NotImplementedException();
        }

        public string Error { get; private set; }
        private void OnError(string error)
        {
            Error = error;
        }
    }
}
