using System;
using System.Collections.Generic;
using System.Web.Http;
using FalconSoft.Data.Management.Common;
using FalconSoft.Data.Management.Common.Facades;
using FalconSoft.Data.Management.Common.Security;

namespace FalconSoft.Data.Management.Server.WebAPI.Controllers
{
    public class SecurityApiController:ApiController
    {
        private readonly ISecurityFacade _securityFacade;
        private readonly ILogger _logger;

        public SecurityApiController(ISecurityFacade securityFacade, ILogger logger)
        {
            _securityFacade = securityFacade;
            _logger = logger;
        }

        [HttpGet]
        public KeyValuePair<bool, string> Authenticate(string userName, string password)
        {
            try
            {
                return _securityFacade.Authenticate(userName, password);
            }
            catch (Exception ex)
            {
                _logger.Error("Authenticate failed ", ex);
                return new KeyValuePair<bool, string>(false, string.Empty);
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
                _logger.Error("GetUsers failed ", ex);
                return new List<User>();
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
                _logger.Error("GetUser failed ", ex);
                return null;
            }
        }

        public string SaveNewUser(User user, UserRole userRole, string userToken)
        {
            try
            {
                return _securityFacade.SaveNewUser(user, userRole, userToken);
            }
            catch (Exception ex)
            {
                _logger.Error("SaveNewUser failed ", ex);
                return null;
            }
        }

        public void UpdateUser(User user, UserRole userRole, string userToken)
        {
            try
            {
                _securityFacade.UpdateUser(user, userRole, userToken);
            }
            catch (Exception ex)
            {
                _logger.Error("UpdateUser failed ", ex);
            }
        }

        public void RemoveUser(User user, string userToken)
        {
            try
            {
                _securityFacade.RemoveUser(user,  userToken);
            }
            catch (Exception ex)
            {
                _logger.Error("RemoveUser failed ", ex);
            }
        }

    }
}
