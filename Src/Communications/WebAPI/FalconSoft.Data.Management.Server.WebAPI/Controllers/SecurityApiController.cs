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
    public class SecurityApiController : ApiController
    {
        private readonly ISecurityFacade _securityFacade;
        private readonly ILogger _logger;

        public SecurityApiController()
        {
            _securityFacade = FacadesFactory.SecurityFacade;
            _logger = FacadesFactory.Logger;
        }

        [HttpPost]
        public HttpResponseMessage AuthenticationPost([FromBody]Tuple<string, string, string> authenticationRequest)
        {
            _logger.Debug("Call AuthenticationPost");
            try
            {
                var content = Authenticate(authenticationRequest.Item1, authenticationRequest.Item2,
                                           authenticationRequest.Item3);
                return Request.CreateResponse(HttpStatusCode.OK, content);
            }
            catch (Exception ex)
            {
                _logger.Error("AuthenticationPost failed ", ex);
                return Request.CreateErrorResponse(HttpStatusCode.BadRequest, "Authentication Failed");
            }

        }

        [HttpGet]
        public AuthenticationResult Authenticate([FromUri]string companyName, [FromUri]string userName, [FromUri]string password)
        {
            _logger.Debug("Call SecurityApiController Authenticate");
            return _securityFacade.Authenticate(companyName, userName, password ?? string.Empty);
        }

        [HttpGet]
        public User[] GetUsers([FromUri]string userToken)
        {
            _logger.Debug("Call SecurityApiController GetUsers");
            try
            {
                return _securityFacade.GetUsers(userToken);
            }
            catch (Exception ex)
            {
                _logger.Error("GetUsers failed ", ex);
                return new User[0];
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

        [HttpGet]
        public Dictionary<string, string> GetUserSettings([FromUri]string userToken)
        {
            _logger.Debug("Call GetUserSettings");
            try
            {
                return new Dictionary<string, string> { { "pushUrl", FacadesFactory.PushUrl } };
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
                return new HttpResponseMessage(HttpStatusCode.NotAcceptable);
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
