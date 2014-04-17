namespace FalconSoft.ReactiveWorksheets.Common
{
    public class FilterRule
    {
        public int? RuleNumber { get; set; }

        public CombineState? Combine { get; set; }

        public string FieldName { get; set; }

        public Operations Operation { get; set; }

        public string Value { get; set; }
  
    }

    public enum Operations
    {
        Equal,
        NotEqual,
        GreaterThan,
        LessThan,
        In,
        Like
    }

    public enum CombineState
    {
        And,
        Or
    }
}
