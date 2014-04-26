using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FalconSoft.ReactiveWorksheets.Common.Security
{
    public class User
    {
        public string Id { get; set; }

        public string LoginName { get; set; }

        public string FirstName { get; set; }

        public string LastName { get; set; }

        public string Department { get; set; }

        public string CorporateTitle { get; set; }

        public string EMail { get; set; }

        public List<Permission> Permissions { get; set; }

        public UserGroup UserGroupId { get; set; }
    }

    public class UserGroup
    {
        public string UserGroupId { get; set; }

        public string GroupName { get; set; }

        public List<User> Users { get; set; }
    }

    public class Permission
    {
        public bool FullAccess { get; set; }

        public bool Delete { get; set; }

        public bool Modify { get; set; }

        public bool ReadOnly { get; set; }

        public PermissionType PermissionType { get; set; }

    }

    public enum PermissionType
    {
        None,
        DesignerPermission,
        WorksheetPermission,
        DataSourcePermission,
        ServiceSourcePermission        
    }

}
