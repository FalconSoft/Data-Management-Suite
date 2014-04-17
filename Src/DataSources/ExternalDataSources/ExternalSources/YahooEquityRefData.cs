//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;
//using FalconSoft.ReactiveWorksheets.Common;

//namespace ReactiveWorksheets.ExternalDataSources.ExternalSources
//{
//    public class YahooEquityRefDataProvider : IDataProvider
//    {
//        /*
//Profile Page -> http://uk.finance.yahoo.com/q/pr?s=UBS
//Index Membership -> /html[1]/body[1]/div[4]/div[4]/table[2]/tr[2]/td[1]/table[2]/tr[1]/td[1]/table[1]/tr[1]/td[2]/#text[1]
//Sector -> 		    /html[1]/body[1]/div[4]/div[4]/table[2]/tr[2]/td[1]/table[2]/tr[1]/td[1]/table[1]/tr[2]/td[2]/#text[1]
//Industry -> 	    /html[1]/body[1]/div[4]/div[4]/table[2]/tr[2]/td[1]/table[2]/tr[1]/td[1]/table[1]/tr[3]/td[2]/#text[1]
//BusinessSummary ->  /html[1]/body[1]/div[4]/div[4]/table[2]/tr[2]/td[1]/p[1]/#text[1]

//Summary Page -> http://uk.finance.yahoo.com/q?s=GOOG
//Title              -> /html[1]/body[1]/div[4]/div[1]/div[3]/div[2]/div[1]/div[1]/div[1]/div[1]/h2[1]
//Exchange           -> /html[1]/body[1]/div[4]/div[1]/div[3]/div[2]/div[1]/div[1]/div[1]/div[1]/span[1]
//1y Target Est:     -> /html[1]/body[1]/div[4]/div[1]/div[3]/div[3]/div[1]/div[1]/table[1]/tr[5]/td[1]
//Beta               -> /html[1]/body[1]/div[4]/div[1]/div[3]/div[3]/div[1]/div[1]/table[1]/tr[6]/td[1]
//Next Earnings Dt:  -> /html[1]/body[1]/div[4]/div[1]/div[3]/div[3]/div[1]/div[1]/table[1]/tr[7]/td[1]
//Avg. Vol(3m)       -> /html[1]/body[1]/div[4]/div[1]/div[3]/div[3]/div[1]/div[1]/table[2]/tr[4]/td[1]
//Market Cap         -> /html[1]/body[1]/div[4]/div[1]/div[3]/div[3]/div[1]/div[1]/table[2]/tr[5]/td[1]
//P/E                -> /html[1]/body[1]/div[4]/div[1]/div[3]/div[3]/div[1]/div[1]/table[2]/tr[6]/td[1]
//EPS                -> /html[1]/body[1]/div[4]/div[1]/div[3]/div[3]/div[1]/div[1]/table[2]/tr[7]/td[1]
//Div & Yield        -> /html[1]/body[1]/div[4]/div[1]/div[3]/div[3]/div[1]/div[1]/table[2]/tr[8]/td[1]
         
//         */


//        public List<YahooEquityRefData> GetEquityRefData(List<string> symbols)
//        {
//            symbols = new List<string>{"A","FB","GOOG"}; // REMOVE HARDCODE TODO
//            var refDataList = new List<YahooEquityRefData>();
//            // stream result from background thread.
//            symbols.ForEach(s => refDataList.Add(GetEquityRefData(s.Substring(1, s.Length - 2))));

//            return refDataList;
//        }

//        private YahooEquityRefData GetEquityRefData(string symbol)
//        {
//            var equity = new YahooEquityRefData { Symbol = symbol }; 
//            var hw = new HtmlWeb();

//            HtmlDocument profileDoc = hw.Load("http://uk.finance.yahoo.com/q/pr?s=" + symbol);

//            HtmlNode sectorNode =
//                profileDoc.DocumentNode.SelectSingleNode(
//                    "/html[1]/body[1]/div[4]/div[4]/table[2]/tr[2]/td[1]/table[2]/tr[1]/td[1]/table[1]/tr[2]/td[2]");
//            if (sectorNode != null)
//                equity.Sector = sectorNode.InnerText;

//            HtmlNode industryNode =
//                profileDoc.DocumentNode.SelectSingleNode(
//                    "/html[1]/body[1]/div[4]/div[4]/table[2]/tr[2]/td[1]/table[2]/tr[1]/td[1]/table[1]/tr[3]/td[2]");
//            if (industryNode != null)
//                equity.Industry = industryNode.InnerText;

//            HtmlNode businessSummaryNode =
//                profileDoc.DocumentNode.SelectSingleNode("/html[1]/body[1]/div[4]/div[4]/table[2]/tr[2]/td[1]/p[1]");
//            if (businessSummaryNode != null)
//                equity.BusinessSummary = businessSummaryNode.InnerText;

//            HtmlDocument summaryDoc = hw.Load("http://uk.finance.yahoo.com/q?s=" + symbol);
//            HtmlNode titleNode =
//                summaryDoc.DocumentNode.SelectSingleNode(
//                    "/html[1]/body[1]/div[4]/div[1]/div[3]/div[2]/div[1]/div[1]/div[1]/div[1]/h2[1]");
//            if (titleNode != null)
//                equity.CompanyTitle = titleNode.InnerText;

//            HtmlNode exchangeNode =
//                summaryDoc.DocumentNode.SelectSingleNode(
//                    "/html[1]/body[1]/div[4]/div[1]/div[3]/div[2]/div[1]/div[1]/div[1]/div[1]/span[1]");
//            if (exchangeNode != null)
//                equity.Exchange = exchangeNode.InnerText;

//            HtmlNode firstTargetEstNode =
//                summaryDoc.DocumentNode.SelectSingleNode(
//                    "/html[1]/body[1]/div[4]/div[1]/div[3]/div[3]/div[1]/div[1]/table[1]/tr[5]/td[1]");
//            if (firstTargetEstNode != null)
//                equity.FirstYearTargetEst = firstTargetEstNode.InnerText;

//            HtmlNode betaNode =
//                summaryDoc.DocumentNode.SelectSingleNode(
//                    "/html[1]/body[1]/div[4]/div[1]/div[3]/div[3]/div[1]/div[1]/table[1]/tr[6]/td[1]");
//            if (betaNode != null)
//                equity.Beta = betaNode.InnerText;


//            HtmlNode nextEarningsDtNode =
//                summaryDoc.DocumentNode.SelectSingleNode(
//                    "/html[1]/body[1]/div[4]/div[1]/div[3]/div[3]/div[1]/div[1]/table[1]/tr[7]/td[1]");
//            if (nextEarningsDtNode != null)
//                equity.NextEarningsDate = nextEarningsDtNode.InnerText;

//            HtmlNode avgVol3MNode =
//                summaryDoc.DocumentNode.SelectSingleNode(
//                    "/html[1]/body[1]/div[4]/div[1]/div[3]/div[3]/div[1]/div[1]/table[2]/tr[4]/td[1]");
//            if (avgVol3MNode != null)
//                equity.AvgVol_3M = avgVol3MNode.InnerText;


//            HtmlNode marketCapNode =
//                summaryDoc.DocumentNode.SelectSingleNode(
//                    "/html[1]/body[1]/div[4]/div[1]/div[3]/div[3]/div[1]/div[1]/table[2]/tr[5]/td[1]");
//            if (marketCapNode != null)
//                equity.MarketCap = marketCapNode.InnerText;

//            HtmlNode pENode =
//                summaryDoc.DocumentNode.SelectSingleNode(
//                    "/html[1]/body[1]/div[4]/div[1]/div[3]/div[3]/div[1]/div[1]/table[2]/tr[6]/td[1]");
//            if (pENode != null)
//                equity.P_E = pENode.InnerText;

//            HtmlNode epsNode =
//                summaryDoc.DocumentNode.SelectSingleNode(
//                    "/html[1]/body[1]/div[4]/div[1]/div[3]/div[3]/div[1]/div[1]/table[2]/tr[7]/td[1]");
//            if (epsNode != null)
//                equity.EPS = epsNode.InnerText;

//            HtmlNode divYieldNode =
//                summaryDoc.DocumentNode.SelectSingleNode(
//                    "/html[1]/body[1]/div[4]/div[1]/div[3]/div[3]/div[1]/div[1]/table[2]/tr[8]/td[1]");
//            if (divYieldNode != null)
//                equity.Div_and_Yield = divYieldNode.InnerText;


//            return equity;
//        }


//        // *******************************************************************************************************
//        #region IDataProviderImplementation
//        List<Dictionary<string, object>> IDataProvider.GetData(string[] fields, IList<FilterRule> whereCondition)
//        {
//            var list = new List<string>();
//            var result = new List<YahooEquityRefData>();
//            if (whereCondition != null)
//            {
//                foreach (var condition in whereCondition.Where(w => w.FieldName.Equals("Symbol")))
//                {
//                    switch (condition.Operation)
//                    {
//                        case Operations.Equal: list.Add(condition.Value); break;
//                        case Operations.In: list.AddRange(condition.Value.Substring(1, condition.Value.Length - 2).Split(',')); break;
//                        case Operations.NotEqual: list.Remove(condition.Value); break;
//                    }
//                }
//                var task = Task.Factory.StartNew(() => result.AddRange(GetEquityRefData(list)));
//                task.Wait();
//            }
//            return result;
//        }

//        public RevisionInfo SubmitChanges(List<Dictionary<string, object>> recordsToInsert, List<Dictionary<string, object>> recordsToUpdate, List<Dictionary<string, object>> recordsToDelete, string comment = null)
//        {
//            return new RevisionInfo();
//        }

//        public event System.EventHandler<ValueChangedEventArgs> RecordChangedEvent;

//        #endregion

//    }


//    public class YahooEquityRefData
//    {
//        //[Key]
//        public string Symbol { get; set; }
//        public string Sector { get; set; }
//        public string Industry { get; set; }
//        public string BusinessSummary { get; set; }
//        public string CompanyTitle { get; set; }
//        public string Exchange { get; set; }
//        public string FirstYearTargetEst { get; set; }
//        public string Beta { get; set; }
//        public string NextEarningsDate { get; set; }
//        public string AvgVol_3M { get; set; }
//        public string MarketCap { get; set; }
//        public string P_E { get; set; }
//        public string EPS { get; set; }
//        public string Div_and_Yield { get; set; }
//    }
//}
