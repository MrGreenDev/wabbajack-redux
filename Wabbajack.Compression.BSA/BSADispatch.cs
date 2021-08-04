using System.IO;
using System.Threading.Tasks;
using Wabbajack.Common;
using Wabbajack.Common.FileSignatures;
using Wabbajack.Compression.BSA.Interfaces;
using Wabbajack.Paths;

namespace Wabbajack.Compression.BSA
{
    public static class BSADispatch
    {
        private static SignatureChecker BSASignatures = new SignatureChecker(FileType.BSA, FileType.BA2, FileType.TES3);

        public static async ValueTask<IReader> Open(AbsolutePath filename)
        {
            return await BSASignatures.MatchesAsync(filename) switch
            {
                FileType.TES3 => await TES3Archive.Reader.Load(new NativeFileStreamFactory(filename)),
                FileType.BSA => await TES5Archive.Reader.Load(new NativeFileStreamFactory(filename)),
                FileType.BA2 => await FO4Archive.Reader.Load(new NativeFileStreamFactory(filename)),
                _ => throw new InvalidDataException("Filename is not a .bsa or .ba2")
            };
        }
    }
}