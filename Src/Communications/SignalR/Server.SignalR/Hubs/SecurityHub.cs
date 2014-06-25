using System;
using System.Collections.Generic;
using FalconSoft.Data.Management.Common.Facades;
using FalconSoft.Data.Management.Common.Security;
using Microsoft.AspNet.SignalR;
using Microsoft.AspNet.SignalR.Hubs;

namespace FalconSoft.Data.Management.Server.SignalR.Hubs
{
    [HubName("ISecurityFacade")]
    public class SecurityHub : Hub
    {
        private readonly ISecurityFacade _securityFacade;

        public SecurityHub(ISecurityFacade securityFacade)
        {
            _securityFacade = securityFacade;
        }

        public KeyValuePair<bool, string> Authenticate(string login, string password)
        {
            try
            {
                return _securityFacade.Authenticate(login, password);
            }
            catch (Exception ex)
            {
                Clients.Caller.ErrorMessageHandledAction("Authenticate", ex.Message);
                return new KeyValuePair<bool, string>(false, ex.Message);
            }
        }

        public List<User> GetUsers(string userToken)
        {
            try
            {
                return _securityFacade.GetUsers(userToken);
            }
            catch (Exception ex)
            {
                Clients.Caller.ErrorMessageHandledAction("GetUsers", ex.Message);
                return null;
            }
        }

        public User GetUser(string userName)
        {
            try
            {
                return _securityFacade.GetUser(userName);
            }
            catch (Exception ex)
            {
                Clients.Caller.ErrorMessageHandledAction("GetUser", ex.Message);
                return null;
            }
        }

        public void SaveNewUser(User user, UserRole userRole, string userToken)
        {
            try
            {
                var userId = _securityFacade.SaveNewUser(user, userRole, userToken);
                Clients.Caller.OnComplete(userId);
            }
            catch (Exception ex)
            {
                Clients.Caller.ErrorMessageHandledAction("SaveNewUser", ex.Message);
            }
        }

        public void UpdateUser(User user, UserRole userRole, string userToken)
        {
            try
            {
                _securityFacade.UpdateUser(user, userRole, userToken);
                Clients.Caller.OnComplete("Updated Successfull");
            }
            catch (Exception ex)
            {
                Clients.Caller.ErrorMessageHandledAction("UpdateUser", ex.Message);
            }
        }

        public void RemoveUser(User user, string userToken)
        {
            try
            {
                _securityFacade.RemoveUser(user, userToken);
                Clients.Caller.OnComplete("Deleted successfull");
            }
            catch (Exception ex)
            {
                Clients.Caller.ErrorMessageHandledAction("RemoveUser", ex.Message);
            }
        }
    }
}
