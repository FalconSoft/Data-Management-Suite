using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using FalconSoft.ReactiveWorksheets.Common;

namespace ReactiveWorksheets.ExternalDataSources.ExternalSources
{
    public class QuotesFeedDataProvider : IDataProvider
    {
        private readonly Timer _timer;
        private List<QuotesFeed> _quotesFeedData = new List<QuotesFeed>();

        List<int> _secId = new List<int> { 1, 2, 3, 4, 5, 6, 7, 8, 9 };
        private int _iterationId = 0;
        public QuotesFeedDataProvider()
        {
            var rand = new Random();
            foreach (var i in _secId)
            {
                _quotesFeedData.Add(new QuotesFeed
                {
                    SecID = i,
                    Quote = rand.Next(80, 150),
                    QuoteSource = "Source " + i
                });
            }

            _timer = new Timer(1000);
            _timer.Elapsed += OnElapsed;
        }

        private void OnElapsed(object sender, ElapsedEventArgs e)
        {
            var rand = new Random();
            int count = Math.Min(50, _quotesFeedData.Count);
            for (int i = 0; i < count; i++)
            {
                var index = i; //rand.Next(count);
                _quotesFeedData[index].Quote = int.Parse(DateTime.Now.ToString("HHmmss")); //rand.Next(80, 150);
                if (RecordChangedEvent != null)
                    RecordChangedEvent(this, new ValueChangedEventArgs
                    {
                        DataSourceUrn = @"ExternalDataSource\QuotesFeed",
                        Value = _quotesFeedData[index],
                        ChangedPropertyNames = new[] { "Quote" }
                    });
            }
        }

        public IEnumerable<Dictionary<string, object>> GetData(string[] fields = null, FilterRule[] filterRules = null, Action<string, string> onError = null)
        {
            _timer.Start();
            var list = new List<Dictionary<string, object>>();
            foreach (var quotesFeed in _quotesFeedData)
            {
                var dict = new Dictionary<string, object>();
                dict.Add("SecID",quotesFeed.SecID);
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
        public int SecID { get; set; }

        public double Quote { get; set; }

        public string QuoteSource { get; set; }
    }
}
