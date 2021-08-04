using Wabbajack.Compression.BSA.Interfaces;

namespace Wabbajack.Compression.BSA.FO4Archive
{
    interface IBA2FileEntry : IFile
    {
        string FullPath { get; set; }
    }
}