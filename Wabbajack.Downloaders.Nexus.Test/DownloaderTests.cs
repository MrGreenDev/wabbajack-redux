using System;
using System.Threading;
using System.Threading.Tasks;
using Wabbajack.DTOs;
using Wabbajack.Hashing.xxHash64;
using Wabbajack.Paths.IO;
using Xunit;

namespace Wabbajack.Downloaders.Nexus.Test
{
    public class DownloaderTests
    {
        private readonly NexusDownloader _nexusDownloader;
        private readonly TemporaryFileManager _temp;

        public DownloaderTests(NexusDownloader downloader, TemporaryFileManager temp)
        {
            _nexusDownloader = downloader;
            _temp = temp;

        }
        
        [Fact]
        public async Task TestDownloadingFile()
        {
            var archive = new Archive
            {
                State = new DTOs.DownloadStates.Nexus
                {
                    Game = Game.SkyrimSpecialEdition,
                    ModID = 51939,
                    FileID = 212497
                }
            };
            using var tempFile = _temp.CreateFile();
            var hash = await _nexusDownloader.Download(archive, tempFile.Path, CancellationToken.None);
            Assert.Equal(Hash.FromBase64("MFp65uNz/N0="), hash);
        }

        [Fact]
        public async Task TestFileVerification()
        {
            var goodArchive = new Archive
            {
                State = new DTOs.DownloadStates.Nexus
                {
                    Game = Game.SkyrimSpecialEdition,
                    ModID = 51939,
                    FileID = 212497
                }
            };
            
            var badArchive = new Archive
            {
                State = new DTOs.DownloadStates.Nexus
                {
                    Game = Game.SkyrimSpecialEdition,
                    ModID = 51939,
                    FileID = 212497 + 1
                }
            };
            Assert.True(await _nexusDownloader.Verify(goodArchive, CancellationToken.None));
            Assert.False(await _nexusDownloader.Verify(badArchive, CancellationToken.None));
        }
    }
}