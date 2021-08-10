using System.Threading.Tasks;

namespace Wabbajack.TaskTracking.Interfaces
{
    public interface ITrackedTask
    {
        public static ITrackedTask None = new NullTrackedTask();
        public ValueTask ReportProgress(Percent percent, long bytesProcessed);
    }
}