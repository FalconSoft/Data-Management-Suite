using System;
using System.Collections.Generic;
using FalconSoft.ReactiveWorksheets.Common;

namespace ReactiveWorksheets.ExternalDataSources.ExternalSources
{
    public class TestDataProvider : IDataProvider
    {
        private readonly List<MyTestData> _collection;
        private readonly System.Timers.Timer _timer;

        private const int Count = 50; // COUNT

        public TestDataProvider()
        {
            _collection = new List<MyTestData>();
            var rand = new Random();
            for (int i = 0; i < Count; i++)
            {
                _collection.Add(new MyTestData
                {
                    FieldId = i,
                    FieldNames = rand.Next(500).ToString(),
                    FieldDescription = rand.Next(500).ToString(),
                    FieldData = rand.Next(500).ToString()
                });
            }
            _timer = new System.Timers.Timer(5000);
            _timer.Elapsed += OnClick;
        }

        private void OnClick(object sender, EventArgs e)
        {
            // Trace.WriteLine("Is background Timer => " + (Thread.CurrentThread.IsBackground ? "True" : "False"));
            var count = _collection.Count;


            var rand = new Random();
            if (count > 0)
                for (int i = 0; i < Count; i++)
                {
                    var itemIndex = rand.Next(Count);
                    var index = rand.Next(count);

                    _collection[itemIndex].FieldNames =
                        rand.Next(index - 100, index).ToString();

                    _collection[itemIndex].FieldDescription =
                        rand.Next(index - 100, index).ToString();

                    _collection[itemIndex].FieldData =
                        rand.Next(index - 100, index).ToString();

                    if (RecordChangedEvent != null)
                        RecordChangedEvent(this,
                            new ValueChangedEventArgs
                            {
                                DataSourceUrn = @"ExternalDataSource\MyTestData",
                                Value = _collection[itemIndex],
                                ChangedPropertyNames = new[] {"FieldNames", "FieldDescription", "FieldData"}
                            });
                }

        }

        public IEnumerable<Dictionary<string, object>> GetData(string[] fields = null, FilterRule[] filterRules = null, Action<string, string> onError = null)
        {
            _timer.Start();
            var list = new List<Dictionary<string, object>>();
            foreach (var item in _collection)
            {
                var dict = new Dictionary<string, object>();
                dict.Add("FieldData", item.FieldData);
                dict.Add("FieldDescription", item.FieldDescription);
                dict.Add("FieldId", item.FieldId);
                dict.Add("FieldNames", item.FieldNames);
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

        public string FieldNames { get; set; }

        public string FieldDescription { get; set; }

        public string FieldData { get; set; }


    }
}
