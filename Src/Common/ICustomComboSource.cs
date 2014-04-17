using System.Collections;
using System.Collections.Generic;

namespace FalconSoft.ReactiveWorksheets.Common
{
    public interface ICustomComboSource
    {
        IList GetItemsSource(IList<FilterRule> filterRules = null, object context = null);
    }
}
