using System.Collections.Generic;

namespace FalconSoft.ReactiveWorksheets.Common.Metadata
{

    public class ParamInfo : HeaderInfo
    {
        public DataTypes DataType { get; set; }

        public object Value { get; set; }
    }

    public class ServiceSourceInfo : BaseSourceInfo
    {
        public IList<ParamInfo> InParams { get; set; }

        public IList<ParamInfo> OutParams { get; set; }

        public string Script { get; set; }

        public ScriptType ScriptType { get; set; }
    }

    public enum ScriptType
    {
        Python, CSharp
    }

    public enum InOutType
    {
        In, Out
    }
}