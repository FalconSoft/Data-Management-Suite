using System.Collections;
using System.Collections.Generic;
using FalconSoft.ReactiveWorksheets.Common.Metadata;

namespace FalconSoft.ReactiveWorksheets.Common
{
    /// <summary>
    /// Is a main interface what describes a basic data provider functionality
    /// This interface has to be registered before it will consumed with ReactiveWorksheets.
    /// </summary>
    public interface IDataProvider : IBaseProvider
    {
        List<Dictionary<string,object>> GetData(string[] fields = null, IList<FilterRule> whereCondition = null);

        RevisionInfo SubmitChanges(List<Dictionary<string, object>> recordsToChange, List<string> recordsToDelete, string comment = null);

        void UpdateSourceInfo(object sourceInfo);
    }


    public class RevisionInfo
    {
        public bool IsSuccessfull { get; set; }

        public int RevisionId { get; set; }

        public string ErrorMessage { get; set; }
    }
}
