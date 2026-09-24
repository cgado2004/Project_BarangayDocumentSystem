using System.Collections.Generic;

namespace BarangayDocumentSystem.Models
{
    public class BarangayStatistics
    {
        public int TotalResidents { get; internal set; }
        public int TotalRequests { get; internal set; }
        public int PendingRequests { get; internal set; }
        public int ReadyRequests { get; internal set; }
        public int FreeDocumentsReleased { get; internal set; }

    /// <summary>Pending requests that have passed the Charter's
    /// three-working-day processing standard (weekends excluded).</summary>
    public int AgedPendingRequests { get; internal set; }
        public decimal TotalCollected { get; internal set; }
        public List<KeyValuePair<string, int>> RequestsByStatus { get; internal set; }
        public List<KeyValuePair<string, int>> RequestsByDocument { get; internal set; }
        public List<KeyValuePair<string, int>> ResidentsByPurok { get; internal set; }
    }
}
