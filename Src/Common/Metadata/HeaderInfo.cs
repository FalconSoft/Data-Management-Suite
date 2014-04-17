namespace FalconSoft.ReactiveWorksheets.Common.Metadata
{
    public class HeaderInfo
    {
        public string Id { get; set; }

        public string Name { get; set; }

        public string Category { get; set; }

        public string Description { get; set; }

        public string ImageSource { get; set; }

        public string DataSourcePath
        {
            get
            {
                return !string.IsNullOrEmpty(Name) && !string.IsNullOrEmpty(Name)
                    ? string.Format(@"{0}\{1}", Category, Name)
                    : string.Empty;
            }
        }
    }
}
