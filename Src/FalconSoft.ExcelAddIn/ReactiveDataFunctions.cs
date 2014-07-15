using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Text;
using ExcelDna.Integration;
using FalconSoft.Data.Management.Client.SignalR;
using FalconSoft.Data.Management.Common;
using FalconSoft.Data.Management.Common.Facades;

namespace FalconSoft.ExcelAddIn
{
    public class ReactiveDataFunctions
    {
        private const string url = @"http://localhost:8081/";

        private static readonly IFacadesFactory FacadesFactory = new SignalRFacadesFactory(url);

        private static readonly string ConsoleClientToken =
            FacadesFactory.CreateSecurityFacade().Authenticate("consoleClient", "console").Value;


        [ExcelFunction]
        public static object RDP(string dataSourcePath,string primaryFieldName, string primaryKey, string outfieldName)
        {
            //var query = new []{ new FilterRule {FieldName = primaryFieldName, Operation = Operations.Equal, Value = primaryKey} };
            //var result = FacadesFactory.CreateReactiveDataQueryFacade().GetData(ConsoleClientToken, dataSourcePath, query);
            //return result.Last(f=>f.ContainsKey(outfieldName))[outfieldName];
            return
                FacadesFactory.CreateReactiveDataQueryFacade()
                    .GetData(ConsoleClientToken, dataSourcePath)
                    .First()
                    .Keys.First();
        } 


        [ExcelFunction]
        public static object rtdata()
        {
            


            return null;
        }

    }
}
