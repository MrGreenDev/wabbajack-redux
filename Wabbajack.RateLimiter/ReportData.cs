using System.Collections.Generic;
using System.Linq;
using Wabbajack.Common;

namespace Wabbajack.RateLimiter
{
    public class Report
    {
        public Dictionary<Resource, ResourceReport> Reports { get; set; } = new();

        public override string ToString()
        {
            return $@"RateLimiter: {Reports[Resource.Network]}, {Reports[Resource.CPU]}, {Reports[Resource.Disk]}";
        }
    }

    public class ResourceReport
    {
        public Resource Resource { get; set; }
        public Dictionary<ulong, JobReport> JobReports { get; set; } = new();
        public override string ToString()
        {
            var totalTransfer = JobReports.Values.Select(v => v.Size).Sum();
            var currentTransfer = JobReports.Values.Select(v => v.Current).Sum();
            Percent percent;
            if (totalTransfer == 0)
                percent = Percent.Zero;
            else
            {
                percent = Percent.FactoryPutInRange(currentTransfer, totalTransfer);
            }
            return $"{percent} ({currentTransfer.ToFileSizeString()}/{totalTransfer.ToFileSizeString()})";
        }
    }

    public class JobReport
    {
        public ulong Id { get; set; }
        public string Description { get; set; } = "";
        public long Current { get; set; }
        public long Size { get; set; }
        public Percent Percent => Percent.FactoryPutInRange(Current, Size);
    }
}