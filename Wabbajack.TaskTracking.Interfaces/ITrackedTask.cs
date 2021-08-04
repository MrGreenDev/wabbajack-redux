using System.Threading.Tasks;

namespace Wabbajack.TaskTracking.Interfaces
{
    public interface ITrackedTask
    {
        public ValueTask ReportProgress(Percent percent, long bytesProcessed);

        public static ITrackedTask None = new NullTrackedTask();
    }
}