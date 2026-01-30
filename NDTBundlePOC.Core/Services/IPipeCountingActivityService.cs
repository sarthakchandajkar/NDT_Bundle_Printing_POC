namespace NDTBundlePOC.Core.Services
{
    /// <summary>
    /// Interface for tracking pipe counting activity (optional service)
    /// </summary>
    public interface IPipeCountingActivityService
    {
        void LogActivity(string pipeType, int count, int totalOKCuts, int totalNDTCuts, string source = "PLC");
    }
}

