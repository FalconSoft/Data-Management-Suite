using System;
using System.Collections.Generic;
using System.Globalization;
using System.Timers;
using FalconSoft.Data.Management.Common;

namespace ReactiveWorksheets.ExternalDataSources.ExternalSources
{
    public class QuotesFeedDataProvider : IDataProvider
    {
        private readonly Dictionary<object, QuotesFeed> _quotesFeedData = new Dictionary<object, QuotesFeed>();

        public QuotesFeedDataProvider()
        {
            var rand = new Random();
            for (int i = 1; i < 10; i++)
            {
                _quotesFeedData.Add(i, new QuotesFeed
                {
                    SecID = i.ToString(CultureInfo.InvariantCulture),
                    Quote = rand.Next(80, 150),
                    QuoteSource = "Source " + i
                });
            }

            var timer = new Timer(2000);
            timer.Elapsed += OnElapsed;
            timer.Start();
        }

        private void OnElapsed(object sender, ElapsedEventArgs e)
        {
            foreach (var quotesFeed in _quotesFeedData.Values)
            {
                quotesFeed.Quote = int.Parse(DateTime.Now.ToString("HHmmss")); //rand.Next(80, 150);
                if (RecordChangedEvent != null)
                    RecordChangedEvent(this, new ValueChangedEventArgs
                    {
                        DataSourceUrn = @"ExternalDataSource\QuotesFeed",
                        Value = quotesFeed,
                        ChangedPropertyNames = new[] { "Quote" }
                    });
            }
        }

        public IEnumerable<Dictionary<string, object>> GetData(string[] fields = null, FilterRule[] filterRules = null, Action<string, string> onError = null)
        {
            var list = new List<Dictionary<string, object>>();
            if (filterRules != null)
            {
                foreach (var filterRule in filterRules)
                {
                    if (!_quotesFeedData.ContainsKey(filterRule.Value))
                    {
                        _quotesFeedData.Add(filterRule.Value, new QuotesFeed { SecID = filterRule.Value });

                        var dict = new Dictionary<string, object>();
                        dict.Add("SecID", _quotesFeedData[filterRule.Value].SecID);
                        dict.Add("Quote", _quotesFeedData[filterRule.Value].Quote);
                        dict.Add("QuoteSource", _quotesFeedData[filterRule.Value].QuoteSource);
                        list.Add(dict);
                    }
                    else
                    {
                        var dict = new Dictionary<string, object>();
                        dict.Add("SecID", _quotesFeedData[filterRule.Value].SecID);
                        dict.Add("Quote", _quotesFeedData[filterRule.Value].Quote);
                        dict.Add("QuoteSource", _quotesFeedData[filterRule.Value].QuoteSource);
                        list.Add(dict);
                    }
                }
                return list;
            }

            foreach (var quotesFeed in _quotesFeedData.Values)
            {
                var dict = new Dictionary<string, object>();
                dict.Add("SecID", quotesFeed.SecID);
                dict.Add("Quote", quotesFeed.Quote);
                dict.Add("QuoteSource", quotesFeed.QuoteSource);
                list.Add(dict);
            }
            return list;
        }

        public RevisionInfo SubmitChanges(IEnumerable<Dictionary<string, object>> recordsToChange, IEnumerable<string> recordsToDelete, string comment = null)
        {
            //throw new NotImplementedException();
            return null;
        }

        public void UpdateSourceInfo(object sourceInfo)
        {
            throw new NotImplementedException();
        }

        public event EventHandler<ValueChangedEventArgs> RecordChangedEvent;

    }

    public class QuotesFeed
    {
        public string SecID { get; set; }

        public double Quote { get; set; }

        public string QuoteSource { get; set; }
    }
}
