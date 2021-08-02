using System.Threading.Tasks;
using Wabbajack.DTOs;
using Wabbajack.DTOs.DownloadStates;
using Wabbajack.Hashing.xxHash64;
using Wabbajack.Paths;

namespace Wabbajack.Downloaders.Interfaces
{
    public abstract class ADownloader<T> : IDownloader<T>
        where T : IDownloadState
    {
        public abstract Task<Hash> Download(Archive archive, T state, AbsolutePath destination);
        public abstract Task<bool> Verify(Archive archive, T archiveState);

        public bool CanDownload(Archive a)
        {
            return a.State is T;
        }

        public Task<Hash> Download(Archive archive, AbsolutePath destination)
        {
            return Download(archive, (T) archive.State, destination);
        }

        public Task<bool> Verify(Archive archive)
        {
            return Verify(archive, (T) archive.State);
        }
    }
}