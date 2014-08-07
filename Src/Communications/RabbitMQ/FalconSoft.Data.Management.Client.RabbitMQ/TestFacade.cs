using System;
using FalconSoft.Data.Management.Common.Facades;
using RabbitMQ.Client;
using RabbitMQ.Client.Exceptions;

namespace FalconSoft.Data.Management.Client.RabbitMQ
{
    internal sealed class TestFacade : ITestFacade
    {
        public bool CheckConnection(string hostName, string login, string password, out string errorMessage)
        {
            try
            {
                var connectionFatroy = new ConnectionFactory
                {
                    HostName = hostName,
                    UserName = login,
                    Password = password
                };

                using (var connection = connectionFatroy.CreateConnection())
                {
                    if (connection.IsOpen)
                    {
                        errorMessage = null;
                        return true;
                    }
                    errorMessage = "Server do not running";
                    return false;
                }
            }
            catch (BrokerUnreachableException ex)
            {
                errorMessage = "given server is unrechable";
                return false;
            }
            catch (Exception ex)
            {
                errorMessage = "Bad credentials! Error message : " + ex.Message;
                return false;
            }
        }
    }
}
