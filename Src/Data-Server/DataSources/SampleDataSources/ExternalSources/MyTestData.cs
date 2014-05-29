using System;
using System.Collections.Generic;
using FalconSoft.Data.Server.Common;

namespace ReactiveWorksheets.ExternalDataSources.ExternalSources
{
    public class TestDataProvider : IDataProvider
    {
        private readonly List<MyTestData> _collection;
        private readonly System.Timers.Timer _timer;

        private const int Count = 150; // COUNT

        public TestDataProvider()
        {
            _collection = new List<MyTestData>();
            var rand = new Random();
            for (int i = 0; i < Count; i++)
            {
                _collection.Add(new MyTestData
                {
                    FieldId = i,
                    DoubleField = rand.Next(80, 150),
                    TimeSpanField = DateTime.Now.ToString("HHmmss")
                });
            }
            _timer = new System.Timers.Timer(5000);
            _timer.Elapsed += OnClick;
            _timer.Start();
        }

        private void OnClick(object sender, EventArgs e)
        {
            var count = _collection.Count;

            var rand = new Random();
            if (count > 0)
                for (int i = 0; i < Count; i++)
                {
                    _collection[i].DoubleField = rand.Next(80, 150);

                    _collection[i].TimeSpanField = DateTime.Now.ToString("HHmmss");

                    if (RecordChangedEvent != null)
                        RecordChangedEvent(this,
                            new ValueChangedEventArgs
                            {
                                DataSourceUrn = @"ExternalDataSource\MyTestData",
                                Value = _collection[i],
                                ChangedPropertyNames = new[] { "DoubleField", "TimeSpanField" }
                            });
                }

        }

        public IEnumerable<Dictionary<string, object>> GetData(string[] fields = null, FilterRule[] filterRules = null, Action<string, string> onError = null)
        {
            var list = new List<Dictionary<string, object>>();
            foreach (var item in _collection)
            {
                var dict = new Dictionary<string, object>();
                dict.Add("DoubleField", item.DoubleField);
                dict.Add("TimeSpanField", item.TimeSpanField);
                dict.Add("FieldId", item.FieldId);
                list.Add(dict);
            }
            return list;

        }

        public void UpdateSourceInfo(object sourceInfo)
        {
            throw new NotImplementedException();
        }

        public event EventHandler<ValueChangedEventArgs> RecordChangedEvent;

        public RevisionInfo SubmitChanges(IEnumerable<Dictionary<string, object>> recordsToChange, IEnumerable<string> recordsToDelete, string comment = null)
        {
            return new RevisionInfo();
        }
    }

    public class MyTestData
    {
        public int FieldId { get; set; }

        public double DoubleField { get; set; }

        public string TimeSpanField { get; set; }
    }
}
