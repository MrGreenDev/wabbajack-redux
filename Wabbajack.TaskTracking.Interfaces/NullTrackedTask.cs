using System.Threading.Tasks;

namespace Wabbajack.TaskTracking.Interfaces
{
    public class NullTrackedTask : ITrackedTask
    {
        public ValueTask ReportProgress(Percent percent, long bytesProcessed)
        {
            return new ValueTask();
        }
    }
}