using NDTBundlePOC.Core.Services;

namespace NDTBundlePOC.Core.Services
{
    public interface IPrinterService
    {
        bool PrintNDTBundleTag(NDTBundlePrintData printData);
        string GetPrinterName();
    }
}

