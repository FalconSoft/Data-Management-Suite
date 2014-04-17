using System;
using System.ComponentModel.Composition;
using FalconSoft.ReactiveWorksheets.Common.AttInterfaces;

namespace FalconSoft.ReactiveWorksheets.Common.Attributes
{
    [MetadataAttribute]
    public class ImportDataInfoAttribute : Attribute, IImportDataInfoAttribute
    {
        public ImportDataInfoAttribute(string category, string name, string[] fields)
        {
            Name = name;
            Category = category;
            Fields = fields;
        }

        public string Category { get; private set; }

        public string Name { get; private set; }

        public string Uri
        {
            get { return string.Format(@"{0}\{1}", Category, Name); }
        }

        public string[] Fields { get; private set; }
    }
}
