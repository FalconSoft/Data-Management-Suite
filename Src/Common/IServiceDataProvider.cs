using System.Collections;

namespace FalconSoft.ReactiveWorksheets.Common
{
    public interface IServiceDataProvider : IBaseProvider
    {
        void RequestCalculation(IList inputParameters);
    }
}
