using Wabbajack.Hashing.xxHash64;

namespace Wabbajack.DTOs.ModListValidation
{
    public class ValidatedModList
    {
        public string MachineURL { get; set; } = "";
        public Hash ModListHash { get; set; } = default;
        public ValidatedArchive[] Archives { get; set; }
        public ListStatus Status { get; set; }
    }
}