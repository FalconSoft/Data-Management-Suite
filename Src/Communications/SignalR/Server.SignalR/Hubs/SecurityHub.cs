using System.Collections.Generic;
using System.Threading.Tasks;
using FalconSoft.ReactiveWorksheets.Common.Facade;
using FalconSoft.ReactiveWorksheets.Common.Security;
using Microsoft.AspNet.SignalR;
using Microsoft.AspNet.SignalR.Hubs;

namespace FalconSoft.ReactiveWorksheets.Server.SignalR.Hubs
{
    [HubName("ISecurityFacade")]
    public class SecurityHub:Hub
    {
        private readonly ISecurityFacade _securityFacade;

        public SecurityHub(ISecurityFacade securityFacade)
        {
            _securityFacade = securityFacade;
        }

        public async Task<List<User>> GetUsers()
        {
            return await Task.Run(() => _securityFacade.GetUsers());
        }

        public void SaveNewUser(User user)
        {
            _securityFacade.SaveNewUser(user);
        }

        public void UpdateUser(User user)
        {
            _securityFacade.UpdateUser(user);
        }

        public void RemoveUser(User user)
        {
            _securityFacade.UpdateUser(user);
        }
    }
}
