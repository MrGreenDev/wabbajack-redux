using System;
using System.Threading.Tasks;

namespace Wabbajack.DTOs.DownloadStates
{
    public interface IMetaState
    {
        Uri URL { get; }
        string? Name { get; set; }
        string? Author { get; set; }
        string? Version { get; set; }
        Uri? ImageURL { get; set; }
        bool IsNSFW { get; set; }
        string? Description { get; set; }
    }
}