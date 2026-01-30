using System;
using System.Collections.Generic;
using System.Linq;
using NDTBundlePOC.Core.Services;

namespace NDTBundlePOC.UI.Web.Services
{
    /// <summary>
    /// Service to track pipe counting activity for UI display
    /// </summary>
    public class PipeCountingActivity
    {
        public DateTime Timestamp { get; set; }
        public string PipeType { get; set; } // "OK" or "NDT"
        public int Count { get; set; }
        public int TotalOKCuts { get; set; }
        public int TotalNDTCuts { get; set; }
        public string Source { get; set; } // "PLC" or "Manual"
    }

    public interface IPipeCountingActivityServiceExtended : IPipeCountingActivityService
    {
        List<PipeCountingActivity> GetRecentActivity(int maxCount = 100);
        void ClearActivity();
        (int okCuts, int ndtCuts) GetCurrentCounts();
    }

    public class PipeCountingActivityService : IPipeCountingActivityServiceExtended
    {
        private readonly List<PipeCountingActivity> _activities = new List<PipeCountingActivity>();
        private readonly object _lock = new object();
        private int _currentOKCuts = 0;
        private int _currentNDTCuts = 0;

        public void LogActivity(string pipeType, int count, int totalOKCuts, int totalNDTCuts, string source = "PLC")
        {
            lock (_lock)
            {
                _currentOKCuts = totalOKCuts;
                _currentNDTCuts = totalNDTCuts;

                var activity = new PipeCountingActivity
                {
                    Timestamp = DateTime.Now,
                    PipeType = pipeType,
                    Count = count,
                    TotalOKCuts = totalOKCuts,
                    TotalNDTCuts = totalNDTCuts,
                    Source = source
                };

                _activities.Insert(0, activity); // Add to beginning for newest first

                // Keep only last 1000 entries
                if (_activities.Count > 1000)
                {
                    _activities.RemoveRange(1000, _activities.Count - 1000);
                }
            }
        }

        public List<PipeCountingActivity> GetRecentActivity(int maxCount = 100)
        {
            lock (_lock)
            {
                return _activities.Take(maxCount).ToList();
            }
        }

        public void ClearActivity()
        {
            lock (_lock)
            {
                _activities.Clear();
            }
        }

        public (int okCuts, int ndtCuts) GetCurrentCounts()
        {
            lock (_lock)
            {
                return (_currentOKCuts, _currentNDTCuts);
            }
        }
    }
}

