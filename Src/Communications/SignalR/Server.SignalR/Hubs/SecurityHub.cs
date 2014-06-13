using System.Collections.Generic;
using FalconSoft.Data.Management.Common.Facades;
using FalconSoft.Data.Management.Common.Security;
using Microsoft.AspNet.SignalR;
using Microsoft.AspNet.SignalR.Hubs;

namespace FalconSoft.Data.Management.Server.SignalR.Hubs
{
    [HubName("ISecurityFacade")]
    public class SecurityHub:Hub
    {
        private readonly ISecurityFacade _securityFacade;

        public SecurityHub(ISecurityFacade securityFacade)
        {
            _securityFacade = securityFacade;
        }

        public List<User> GetUsers()
        {
            return  _securityFacade.GetUsers();
        }

        public void SaveNewUser(User user, string userToken)
        {
            _securityFacade.SaveNewUser(user, userToken);
            Clients.Caller.OnComplete();
        }

        public void UpdateUser(User user, string userToken)
        {
            _securityFacade.UpdateUser(user, userToken);
            Clients.Caller.OnComplete();
        }

        public void RemoveUser(User user, string userToken)
        {
            _securityFacade.UpdateUser(user, userToken);
            Clients.Caller.OnComplete();
        }
    }
}
