using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Wabbajack.Downloaders.Interfaces;
using Wabbajack.DTOs;
using Wabbajack.DTOs.DownloadStates;
using Wabbajack.Hashing.xxHash64;
using Wabbajack.Networking.WabbajackClientApi;
using Wabbajack.Paths;

namespace Wabbajack.Downloaders
{
    public class DownloadDispatcher
    {
        private readonly IEnumerable<IDownloader> _downloaders;
        private readonly ILogger<DownloadDispatcher> _logger;
        private readonly Client _wjClient;

        public DownloadDispatcher(ILogger<DownloadDispatcher> logger, IEnumerable<IDownloader> downloaders,
            Client wjClient)
        {
            _downloaders = downloaders;
            _logger = logger;
            _wjClient = wjClient;
        }

        public async Task<Hash> Download(Archive a, AbsolutePath dest, CancellationToken token)
        {
            using var downloadScope = _logger.BeginScope("Downloading {primaryKeyString}", a.State.PrimaryKeyString);

            var hash = await Downloader(a).Download(a, dest, token);
            _logger.BeginScope("Completed {hash}", hash);
            return hash;
        }

        public async Task<IDownloadState?> ResolveArchive(IReadOnlyDictionary<string, string> ini)
        {
            throw new NotImplementedException();
        }

        public async Task<bool> Verify(Archive a, CancellationToken token)
        {
            foreach (var downloader in _downloaders)
                if (downloader.CanDownload(a))
                    return await downloader.Verify(a, token);

            throw new NotImplementedException();
        }

        public async Task<(DownloadResult, Hash)> DownloadWithPossibleUpgrade(Archive archive, AbsolutePath destination,
            CancellationToken token)
        {
            var downloadedHash = await Download(archive, destination, token);
            if (downloadedHash != default && (downloadedHash == archive.Hash || archive.Hash == default))
                return (DownloadResult.Success, downloadedHash);

            downloadedHash = await DownloadFromMirror(archive, destination, token);
            if (downloadedHash != default) return (DownloadResult.Mirror, downloadedHash);

            return (DownloadResult.Failure, downloadedHash);

            // TODO: implement patching
            /*
            if (!(archive.State is IUpgradingState))
            {
                _logger.LogInformation("Download failed for {name} and no upgrade from this download source is possible", archive.Name);
                return DownloadResult.Failure;
            }

            _logger.LogInformation("Trying to find solution to broken download for {name}", archive.Name);
            
            var result = await FindUpgrade(archive);
            if (result == default )
            {
                result = await AbstractDownloadState.ServerFindUpgrade(archive);
                if (result == default)
                {
                    _logger.LogInformation(
                        "No solution for broken download {name} {primaryKeyString} could be found", archive.Name, archive.State.PrimaryKeyString);
                    return DownloadResult.Failure;
                }
            }

            _logger.LogInformation($"Looking for patch for {archive.Name} ({(long)archive.Hash} {archive.Hash.ToHex()} -> {(long)result.Archive!.Hash} {result.Archive!.Hash.ToHex()})");
            var patchResult = await ClientAPI.GetModUpgrade(archive, result.Archive!);

            _logger.LogInformation($"Downloading patch for {archive.Name} from {patchResult}");
            
            var tempFile = new TempFile();

            if (WabbajackCDNDownloader.DomainRemaps.TryGetValue(patchResult.Host, out var remap))
            {
                var builder = new UriBuilder(patchResult) {Host = remap};
                patchResult = builder.Uri;
            }

            using var response = await (await ClientAPI.GetClient()).GetAsync(patchResult);

            await tempFile.Path.WriteAllAsync(await response.Content.ReadAsStreamAsync());
            response.Dispose();

            _logger.LogInformation($"Applying patch to {archive.Name}");
            await using(var src = await result.NewFile.Path.OpenShared())
            await using (var final = await destination.Create())
            {
                Utils.ApplyPatch(src, () => tempFile.Path.OpenShared().Result, final);
            }

            var hash = await destination.FileHashCachedAsync();
            if (hash != archive.Hash && archive.Hash != default)
            {
                _logger.LogInformation("Archive hash didn't match after patching");
                return DownloadResult.Failure;
            }

            return DownloadResult.Update;
            */
        }

        private async Task<Hash> DownloadFromMirror(Archive archive, AbsolutePath destination, CancellationToken token)
        {
            try
            {
                var url = await _wjClient.GetMirrorUrl(archive.Hash);
                if (url == null) return default;

                var newArchive =
                    new Archive
                    {
                        Hash = archive.Hash,
                        Size = archive.Size,
                        Name = archive.Name,
                        State = new WabbajackCDN { Url = url }
                    };

                return await Download(newArchive, destination, token);
            }
            catch (Exception ex)
            {
                _logger.LogCritical(ex, "While finding mirror for {hash}", archive.Hash);
                return default;
            }
        }

        public IDownloader Downloader(Archive archive)
        {
            var result = _downloaders.FirstOrDefault(d => d.CanDownload(archive));
            if (result != null) return result!;
            _logger.LogError("No downloader found for {type}", archive.State.GetType());
            throw new NotImplementedException($"No downloader for {archive.State.GetType()}");
        }

        public async Task<Archive> FillInMetadata(Archive a)
        {
            var downloader = Downloader(a);
            if (downloader is IMetaStateDownloader msd)
                return await msd.FillInMetadata(a);
            return a;
        }

        public IDownloadState? Parse(Uri url)
        {
            return _downloaders.OfType<IUrlDownloader>()
                .Select(downloader => downloader.Parse(url))
                .FirstOrDefault(parsed => parsed != null);
        }

        public IEnumerable<string> MetaIni(Archive archive)
        {
            return Downloader(archive).MetaIni(archive);
        }

        public string MetaIniSection(Archive archive)
        {
            return string.Join("\n", new[] { "[General]" }.Concat(MetaIni(archive)));
        }
    }
}