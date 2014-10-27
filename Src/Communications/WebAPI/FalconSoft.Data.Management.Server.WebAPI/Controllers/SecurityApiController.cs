using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
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
        public KeyValuePair<bool, string> Authenticate([FromUri]string userName, [FromUri]string password)
        {
            _logger.Debug("Call SecurityApiController Authenticate");
            try
            {
                return _securityFacade.Authenticate(userName, password ?? string.Empty);
            }
            catch (Exception ex)
            {
                _logger.Error("Authenticate failed ", ex);
                return new KeyValuePair<bool, string>(false, string.Empty);
            }
        }

        [HttpGet]
        public List<User> GetUsers([FromUri]string userToken)
        {
            _logger.Debug("Call SecurityApiController GetUsers");
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

        [HttpGet]
        public User GetUser([FromUri]string userName)
        {
            _logger.Debug("Call SecurityApiController GetUser");
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

        [HttpPost]
        public HttpResponseMessage SaveNewUser([FromBody]User user, [FromUri]UserRole userRole, [FromUri]string userToken)
        {
            _logger.Debug("Call SecurityApiController SaveNewUser");
            var responce = new HttpResponseMessage();
            try
            {
                var content = _securityFacade.SaveNewUser(user, userRole, userToken);
                responce.StatusCode = HttpStatusCode.OK;
                responce.Content = new StringContent(content);
                return responce;
            }
            catch (Exception ex)
            {
                _logger.Error("SaveNewUser failed ", ex);
                return new HttpResponseMessage(HttpStatusCode.BadRequest);
            }
        }

        [HttpPost]
        public HttpResponseMessage UpdateUser([FromBody]User user, [FromUri]UserRole userRole, [FromUri]string userToken)
        {
            _logger.Debug("Call SecurityApiController UpdateUser");
            try
            {
                _securityFacade.UpdateUser(user, userRole, userToken);
                return new HttpResponseMessage(HttpStatusCode.OK);
            }
            catch (Exception ex)
            {
                _logger.Error("UpdateUser failed ", ex);
                return new HttpResponseMessage(HttpStatusCode.BadRequest);
            }
        }

        [HttpPost]
        public HttpResponseMessage RemoveUser([FromBody]User user, [FromUri]string userToken)
        {
            _logger.Debug("Call SecurityApiController RemoveUser");
            try
            {
                _securityFacade.RemoveUser(user,  userToken);
                return new HttpResponseMessage(HttpStatusCode.OK);
            }
            catch (Exception ex)
            {
                _logger.Error("RemoveUser failed ", ex);
                return new HttpResponseMessage(HttpStatusCode.BadRequest);
            }
        }

    }
}
