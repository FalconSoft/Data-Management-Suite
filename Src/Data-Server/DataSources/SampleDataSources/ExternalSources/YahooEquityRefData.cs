using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FalconSoft.Data.Server.Common;
using HtmlAgilityPack;

namespace ReactiveWorksheets.ExternalDataSources.ExternalSources
{
    public class YahooEquityRefDataProvider : IDataProvider
    {
        /*
Profile Page -> http://uk.finance.yahoo.com/q/pr?s=UBS
Index Membership -> /html[1]/body[1]/div[4]/div[4]/table[2]/tr[2]/td[1]/table[2]/tr[1]/td[1]/table[1]/tr[1]/td[2]/#text[1]
Sector -> 		    /html[1]/body[1]/div[4]/div[4]/table[2]/tr[2]/td[1]/table[2]/tr[1]/td[1]/table[1]/tr[2]/td[2]/#text[1]
Industry -> 	    /html[1]/body[1]/div[4]/div[4]/table[2]/tr[2]/td[1]/table[2]/tr[1]/td[1]/table[1]/tr[3]/td[2]/#text[1]
BusinessSummary ->  /html[1]/body[1]/div[4]/div[4]/table[2]/tr[2]/td[1]/p[1]/#text[1]

Summary Page -> http://uk.finance.yahoo.com/q?s=GOOG
Title              -> /html[1]/body[1]/div[4]/div[1]/div[3]/div[2]/div[1]/div[1]/div[1]/div[1]/h2[1]
Exchange           -> /html[1]/body[1]/div[4]/div[1]/div[3]/div[2]/div[1]/div[1]/div[1]/div[1]/span[1]
1y Target Est:     -> /html[1]/body[1]/div[4]/div[1]/div[3]/div[3]/div[1]/div[1]/table[1]/tr[5]/td[1]
Beta               -> /html[1]/body[1]/div[4]/div[1]/div[3]/div[3]/div[1]/div[1]/table[1]/tr[6]/td[1]
Next Earnings Dt:  -> /html[1]/body[1]/div[4]/div[1]/div[3]/div[3]/div[1]/div[1]/table[1]/tr[7]/td[1]
Avg. Vol(3m)       -> /html[1]/body[1]/div[4]/div[1]/div[3]/div[3]/div[1]/div[1]/table[2]/tr[4]/td[1]
Market Cap         -> /html[1]/body[1]/div[4]/div[1]/div[3]/div[3]/div[1]/div[1]/table[2]/tr[5]/td[1]
P/E                -> /html[1]/body[1]/div[4]/div[1]/div[3]/div[3]/div[1]/div[1]/table[2]/tr[6]/td[1]
EPS                -> /html[1]/body[1]/div[4]/div[1]/div[3]/div[3]/div[1]/div[1]/table[2]/tr[7]/td[1]
Div & Yield        -> /html[1]/body[1]/div[4]/div[1]/div[3]/div[3]/div[1]/div[1]/table[2]/tr[8]/td[1]
         
         */


        private readonly List<Dictionary<string, object>> _cache = new List<Dictionary<string, object>>();

        private YahooEquityRefData GetEquityRefData(string symbol)
        {
            var equity = new YahooEquityRefData {Symbol = symbol};
            var hw = new HtmlWeb();

            HtmlDocument profileDoc = hw.Load("http://uk.finance.yahoo.com/q/pr?s=" + symbol);

            HtmlNode sectorNode =
                profileDoc.DocumentNode.SelectSingleNode(
                    "/html[1]/body[1]/div[4]/div[4]/table[2]/tr[2]/td[1]/table[2]/tr[1]/td[1]/table[1]/tr[2]/td[2]");
            if (sectorNode != null)
                equity.Sector = sectorNode.InnerText;

            HtmlNode industryNode =
                profileDoc.DocumentNode.SelectSingleNode(
                    "/html[1]/body[1]/div[4]/div[4]/table[2]/tr[2]/td[1]/table[2]/tr[1]/td[1]/table[1]/tr[3]/td[2]");
            if (industryNode != null)
                equity.Industry = industryNode.InnerText;

            HtmlNode businessSummaryNode =
                profileDoc.DocumentNode.SelectSingleNode("/html[1]/body[1]/div[4]/div[4]/table[2]/tr[2]/td[1]/p[1]");
            if (businessSummaryNode != null)
                equity.BusinessSummary = businessSummaryNode.InnerText;

            HtmlDocument summaryDoc = hw.Load("http://uk.finance.yahoo.com/q?s=" + symbol);
            HtmlNode titleNode =
                summaryDoc.DocumentNode.SelectSingleNode(
                    "/html[1]/body[1]/div[4]/div[1]/div[3]/div[2]/div[1]/div[1]/div[1]/div[1]/h2[1]");
            if (titleNode != null)
                equity.CompanyTitle = titleNode.InnerText;

            HtmlNode exchangeNode =
                summaryDoc.DocumentNode.SelectSingleNode(
                    "/html[1]/body[1]/div[4]/div[1]/div[3]/div[2]/div[1]/div[1]/div[1]/div[1]/span[1]");
            if (exchangeNode != null)
                equity.Exchange = exchangeNode.InnerText;

            HtmlNode firstTargetEstNode =
                summaryDoc.DocumentNode.SelectSingleNode(
                    "/html[1]/body[1]/div[4]/div[1]/div[3]/div[3]/div[1]/div[1]/table[1]/tr[5]/td[1]");
            if (firstTargetEstNode != null)
                equity.FirstYearTargetEst = firstTargetEstNode.InnerText;

            HtmlNode betaNode =
                summaryDoc.DocumentNode.SelectSingleNode(
                    "/html[1]/body[1]/div[4]/div[1]/div[3]/div[3]/div[1]/div[1]/table[1]/tr[6]/td[1]");
            if (betaNode != null)
                equity.Beta = betaNode.InnerText;


            HtmlNode nextEarningsDtNode =
                summaryDoc.DocumentNode.SelectSingleNode(
                    "/html[1]/body[1]/div[4]/div[1]/div[3]/div[3]/div[1]/div[1]/table[1]/tr[7]/td[1]");
            if (nextEarningsDtNode != null)
                equity.NextEarningsDate = nextEarningsDtNode.InnerText;

            HtmlNode avgVol3MNode =
                summaryDoc.DocumentNode.SelectSingleNode(
                    "/html[1]/body[1]/div[4]/div[1]/div[3]/div[3]/div[1]/div[1]/table[2]/tr[4]/td[1]");
            if (avgVol3MNode != null)
                equity.AvgVol_3M = avgVol3MNode.InnerText;


            HtmlNode marketCapNode =
                summaryDoc.DocumentNode.SelectSingleNode(
                    "/html[1]/body[1]/div[4]/div[1]/div[3]/div[3]/div[1]/div[1]/table[2]/tr[5]/td[1]");
            if (marketCapNode != null)
                equity.MarketCap = marketCapNode.InnerText;

            HtmlNode pENode =
                summaryDoc.DocumentNode.SelectSingleNode(
                    "/html[1]/body[1]/div[4]/div[1]/div[3]/div[3]/div[1]/div[1]/table[2]/tr[6]/td[1]");
            if (pENode != null)
                equity.P_E = pENode.InnerText;

            HtmlNode epsNode =
                summaryDoc.DocumentNode.SelectSingleNode(
                    "/html[1]/body[1]/div[4]/div[1]/div[3]/div[3]/div[1]/div[1]/table[2]/tr[7]/td[1]");
            if (epsNode != null)
                equity.EPS = epsNode.InnerText;

            HtmlNode divYieldNode =
                summaryDoc.DocumentNode.SelectSingleNode(
                    "/html[1]/body[1]/div[4]/div[1]/div[3]/div[3]/div[1]/div[1]/table[2]/tr[8]/td[1]");
            if (divYieldNode != null)
                equity.Div_and_Yield = divYieldNode.InnerText;


            return equity;
        }

        #region IDataProviderImplementation

        public IEnumerable<Dictionary<string, object>> GetData(string[] fields = null, FilterRule[] filterRules = null,
            Action<string, string> onError = null)
        {
            var list = new List<string>();
            if (filterRules != null)
            {
                foreach (var condition in filterRules.Where(w => w.FieldName.Equals("Symbol")))
                {
                    switch (condition.Operation)
                    {
                        case Operations.Equal: list.Add(condition.Value); break;
                        case Operations.In: list.AddRange(condition.Value.Substring(1, condition.Value.Length - 2).Split(',')); break;
                        case Operations.NotEqual: list.Remove(condition.Value); break;
                    }
                }
                var dataInCacheByfilter = _cache.Join(list, j1 => j1["Symbol"].ToString(), j2 => j2, (j1, j2) =>j1).ToList();
                var symbolsForSearch = list.Except( _cache.Select(s => s).Select(s => s["Symbol"].ToString())).ToList();
                if (symbolsForSearch.Any())
                {
                    foreach (var symbol in symbolsForSearch)
                    {
                        Task.Factory.StartNew(() => GetEquityRefData(symbol)).ContinueWith((t) => SendEvent(t.Result));
                    }
                }
                return dataInCacheByfilter;
            }
            return _cache;
        }

        public RevisionInfo SubmitChanges(IEnumerable<Dictionary<string, object>> recordsToChange,
            IEnumerable<string> recordsToDelete, string comment = null)
        {
            throw new NotImplementedException();
        }


        public event EventHandler<ValueChangedEventArgs> RecordChangedEvent;

        #endregion

        private void SendEvent(YahooEquityRefData yahooObj)
        {
            var dict = new Dictionary<string, object>
                            {
                                {"Symbol", yahooObj.Symbol},
                                {"Sector", yahooObj.Sector},
                                {"Industry", yahooObj.Industry},
                                {"BusinessSummary", yahooObj.BusinessSummary},
                                {"CompanyTitle", yahooObj.CompanyTitle},
                                {"Exchange", yahooObj.Exchange},
                                {"FirstYearTargetEst", yahooObj.FirstYearTargetEst},
                                {"Beta", yahooObj.Beta},
                                {"NextEarningsDate", yahooObj.NextEarningsDate},
                                {"AvgVol_3M", yahooObj.AvgVol_3M},
                                {"MarketCap", yahooObj.MarketCap},
                                {"P_E", yahooObj.P_E},
                                {"EPS", yahooObj.EPS},
                                {"Div_and_Yield", yahooObj.Div_and_Yield}
                            };
            _cache.Add(dict);
            if (RecordChangedEvent != null)
            {
                RecordChangedEvent(this, new ValueChangedEventArgs
                {
                    DataSourceUrn = @"ExternalDataSource\YahooEquityRefData",
                    Value = yahooObj,
                    ChangedPropertyNames = dict.Keys.ToArray()
                });
            }     
        }

    }


    public class YahooEquityRefData
    {
        //[Key]
        public string Symbol { get; set; }
        public string Sector { get; set; }
        public string Industry { get; set; }
        public string BusinessSummary { get; set; }
        public string CompanyTitle { get; set; }
        public string Exchange { get; set; }
        public string FirstYearTargetEst { get; set; }
        public string Beta { get; set; }
        public string NextEarningsDate { get; set; }
        public string AvgVol_3M { get; set; }
        public string MarketCap { get; set; }
        public string P_E { get; set; }
        public string EPS { get; set; }
        public string Div_and_Yield { get; set; }
    }
}