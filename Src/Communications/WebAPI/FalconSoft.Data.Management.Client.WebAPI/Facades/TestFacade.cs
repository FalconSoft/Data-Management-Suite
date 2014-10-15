using System;
using FalconSoft.Data.Management.Common.Facades;

namespace FalconSoft.Data.Management.Client.WebAPI.Facades
{
    internal sealed class TestFacade : ITestFacade
    {
        public bool CheckConnection(string hostName, string login, string password, out string errorMessage)
        {
            throw new NotImplementedException();
        }
    }
}