using System;
using System.Threading;
using System.Threading.Tasks;
using Wabbajack.Downloaders.GoogleDrive;
using Wabbajack.DTOs;
using Wabbajack.Hashing.xxHash64;
using Wabbajack.Paths.IO;
using Xunit;

namespace Wabbajack.Downloaders.GoogleDrive.Test
{
    public class DownloaderTests
    {
        private readonly GoogleDriveDownloader _downloader;
        private readonly TemporaryFileManager _temp;

        public DownloaderTests(GoogleDriveDownloader downloader, TemporaryFileManager temp)
        {
            _downloader = downloader;
            _temp = temp;

        }
        
        [Fact]
        public async Task TestDownloadingFile()
        {
            var archive = new Archive
                { State = new DTOs.DownloadStates.GoogleDrive { Id = "1grLRTrpHxlg7VPxATTFNfq2OkU_Plvh_" } };
            using var tempFile = _temp.CreateFile();
            var hash = await _downloader.Download(archive, tempFile.Path, CancellationToken.None);
            Assert.Equal(Hash.FromBase64("eSIyd+KOG3s="), hash);
        }

        [Fact]
        public async Task TestFileVerification()
        {
            var goodArchive = new Archive
                { State = new DTOs.DownloadStates.GoogleDrive { Id = "1grLRTrpHxlg7VPxATTFNfq2OkU_Plvh_" } };
            var badArchive = new Archive
                { State = new DTOs.DownloadStates.GoogleDrive { Id = "2grLRTrpHxlg7VPxATTFNfq2OkU_Plvh_" } };
            Assert.True(await _downloader.Verify(goodArchive, CancellationToken.None));
            Assert.False(await _downloader.Verify(badArchive, CancellationToken.None));
        }
    }
}