using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FalconSoft.Data.Server.DefaultMongoDbSource
{
    public static class Utils
    {
        public static string GetCategoryPart(string dataSourceUrn)
        {
            return dataSourceUrn.Split('\\').First();
        }

        public static string GetNamePart(string dataSourceUrn)
        {
            return dataSourceUrn.Split('\\').Last();
        }

    }
}
