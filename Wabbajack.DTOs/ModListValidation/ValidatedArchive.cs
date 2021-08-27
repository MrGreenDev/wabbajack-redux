using Wabbajack.DTOs.ServerResponses;

namespace Wabbajack.DTOs.ModListValidation
{
    public class ValidatedArchive
    {
        public ArchiveStatus Status { get; set; }
        public Archive Original { get; set; }
        public Archive? PatchedFrom { get; set; }
    }
}