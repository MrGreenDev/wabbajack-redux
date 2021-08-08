using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Octodiff.Core;
using Octodiff.Diagnostics;
using Wabbajack.DTOs.Streams;

namespace Wabbajack.Installer.Utilities
{
    public class BinaryPatching
    {
        public static async ValueTask ApplyPatch(Stream input, Stream deltaStream, Stream output)
        {
            var deltaApplier = new DeltaApplier();
            deltaApplier.Apply(input, new BinaryDeltaReader(deltaStream, new NullProgressReporter()), output);
        }
    }
}