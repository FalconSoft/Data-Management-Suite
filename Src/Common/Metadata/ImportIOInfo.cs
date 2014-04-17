namespace FalconSoft.ReactiveWorksheets.Common.Metadata
{
    public class ImportIOInfo : HeaderInfo
    {
        public string UserGuid { get; set; }

        public string ApiKey { get; set; }

        public string ExtractorGuid { get; set; }

        public string[] SourceUrls { get; set; }

        public string[] Fields { get; set; }

        public string[] KeyFields { get; set; }
    }
}
