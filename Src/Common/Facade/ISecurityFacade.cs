using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FalconSoft.ReactiveWorksheets.Common.Security;

namespace FalconSoft.ReactiveWorksheets.Common.Facade
{
    public interface ISecurityFacade
    {
        List<User> GetUsers();

        void SaveNewUser(User user);
        
        void UpdateUser(User user);
        
        void RemoveUser(User user);
    }
}
