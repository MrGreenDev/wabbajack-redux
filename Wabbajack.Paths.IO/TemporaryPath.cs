using System;
using System.Threading.Tasks;

namespace Wabbajack.Paths.IO
{
    public struct TemporaryPath : IDisposable
    {
        public readonly AbsolutePath Path { get; init; }
        private readonly TemporaryFileManager _manager;

        public TemporaryPath(AbsolutePath path, TemporaryFileManager manager)
        {
            Path = path;
            _manager = manager;
        }
        public void Dispose()
        {
            Path.Delete();
        }
    }
}