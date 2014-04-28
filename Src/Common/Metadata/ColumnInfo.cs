using System;
using System.Collections;

namespace FalconSoft.ReactiveWorksheets.Common.Metadata
{

    public enum ColumnFixedTo
    {
        None,
        Left,
        Right
    }

    public enum ColumnSortOrder
    {
        None = 0,
        Ascending = 1,
        Descending = 2
    }

    //todo : Refactor me, please!!! 
    public class ColumnInfo : ICloneable
    {
        public int Id { get; set; }

        public string Header { get; set; }

        public string BindableFieldName
        {
            get { return string.Format("[{0}].Value", FieldName); }
        }

        public string FieldName { get; set; }
        
        public int SortIndex { get; set; }

        public ColumnSortOrder SortOrder { get; set; }

        public ColumnFixedTo FixedTo { get; set; }

        public int GroupOrder { get; set; }

        public int ColumnIndex { get; set; }

        public int Width { get; set; }

        public bool IsVisible { get; set; }

        public bool IsReadOnly { get; set; }

        public string BackgroundColor { get; set; }

        public string ForegroundColor { get; set; }

        public string DisplayFormat { get; set; }

        public double Opacity
        {
            get { return BackgroundColor == "#00FFFFFF" ? 1 : 0.65; }
        }

        public bool IsCombo { get; set; }

        public bool AllowNew { get; set; }

        public string ItemsSourceType { get; set; }

        public string ItemsSource { get; set; }

        public string ComboDisplayNameField { get; set; }

        //todo: ComboValueField with data to datasource comboboxes is not available now

        public string ComboValueNameField { get; set; }

        public string ComboBoxWhereCondition { get; set; }

        public Type CustomComboValueType { get; set; }

        public IList CustomComboBoxValues { get; set; }

        public int GroupSummary { get; set; }

        public int TotalSummary { get; set; }

        public void Update(ColumnInfo columnInfo)
        {
            Id = columnInfo.Id;
            FieldName = columnInfo.FieldName;
            Header = columnInfo.Header;
            SortIndex = columnInfo.SortIndex;
            SortOrder = columnInfo.SortOrder;
            FixedTo = columnInfo.FixedTo;
            GroupOrder = columnInfo.GroupOrder;
            ColumnIndex = columnInfo.ColumnIndex;
            Width = columnInfo.Width;
            IsVisible = columnInfo.IsVisible;
            IsReadOnly = columnInfo.IsReadOnly;
            BackgroundColor = columnInfo.BackgroundColor;
            ForegroundColor = columnInfo.ForegroundColor;
            DisplayFormat = columnInfo.DisplayFormat;
            IsCombo = columnInfo.IsCombo;
            AllowNew = columnInfo.AllowNew;
            ItemsSource = columnInfo.ItemsSource;
            ItemsSourceType = columnInfo.ItemsSourceType;
            ComboDisplayNameField = columnInfo.ComboDisplayNameField;
            ComboValueNameField = columnInfo.ComboValueNameField;
            ComboBoxWhereCondition = columnInfo.ComboBoxWhereCondition;
            GroupSummary = columnInfo.GroupSummary;
            TotalSummary = columnInfo.TotalSummary;
        }

        public object Clone()
        {
            return new ColumnInfo
            {
                Id = Id,
                FieldName = FieldName,
                Header = Header,
                SortIndex = SortIndex,
                SortOrder = SortOrder,
                FixedTo = FixedTo,
                GroupOrder = GroupOrder,
                ColumnIndex = ColumnIndex,
                Width = Width,
                IsVisible = IsVisible,
                IsReadOnly = IsReadOnly,
                BackgroundColor = BackgroundColor,
                ForegroundColor = ForegroundColor,
                DisplayFormat = DisplayFormat,
                IsCombo = IsCombo,
                AllowNew = AllowNew,
                ItemsSource = ItemsSource,
                ItemsSourceType = ItemsSourceType,
                ComboDisplayNameField = ComboDisplayNameField,
                ComboValueNameField = ComboValueNameField,
                ComboBoxWhereCondition = ComboBoxWhereCondition,
                GroupSummary = GroupSummary,
                TotalSummary = TotalSummary
            };
        }
    }
}
