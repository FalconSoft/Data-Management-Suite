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

        public string Authenticate( string login, string password)
        {
           return _securityFacade.Authenticate(login, password);
        }

        public List<User> GetUsers(string userToken)
        {
            return  _securityFacade.GetUsers(userToken);
        }

        public void SaveNewUser(User user,UserRole userRole, string userToken)
        {
            var userId = _securityFacade.SaveNewUser(user, userRole, userToken);
            Clients.Caller.OnComplete(userId);
        }

        public void UpdateUser(User user, UserRole userRole, string userToken)
        {
            _securityFacade.UpdateUser(user, userRole, userToken);
            Clients.Caller.OnComplete("Updated Successfull");
        }

        public void RemoveUser(User user, string userToken)
        {
            _securityFacade.RemoveUser(user, userToken);
            Clients.Caller.OnComplete("Deleted successfull");
        }
    }
}
