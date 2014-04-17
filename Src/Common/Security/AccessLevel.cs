using System;

namespace FalconSoft.ReactiveWorksheets.Common.Security
{
    [Flags]
    public enum AccessLevel
    {
        Read = 1, AddUpdate = 2, Delete = 4, FullAccess = 8
    }
}
