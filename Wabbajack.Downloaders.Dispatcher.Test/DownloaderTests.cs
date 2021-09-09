using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Wabbajack.Downloaders.Interfaces;
using Wabbajack.DTOs;
using Wabbajack.DTOs.DownloadStates;
using Wabbajack.Hashing.xxHash64;
using Wabbajack.Installer;
using Wabbajack.Paths.IO;
using Wabbajack.Services.OSIntegrated;
using Xunit;

namespace Wabbajack.Downloaders.Dispatcher.Test
{
    public class DownloaderTests
    {
        private readonly DownloadDispatcher _dispatcher;
        private readonly TemporaryFileManager _temp;

        public DownloaderTests(DownloadDispatcher dispatcher, TemporaryFileManager temp, LoggingRateLimiterReporter _)
        {
            _temp = temp;
            _dispatcher = dispatcher;
        }

        private bool AutoPassTest(Archive archive)
        {
            return false;
        }

        /// <summary>
        ///     Pairs of archives for each downloader. The first archive is considered valid,
        ///     the second should be invalid.
        /// </summary>
        public static IEnumerable<object[]> TestStates =>
            new List<object[]>
            {
                // Nexus Data
                new object[]
                {
                    new Archive
                    {
                        Hash = Hash.FromBase64("U9NkoW0w21k="),
                        State = new Nexus
                        {
                            Game = Game.SkyrimSpecialEdition,
                            ModID = 51939,
                            FileID = 212497
                        }
                    },
                    new Archive
                    {
                        State = new Nexus
                        {
                            Game = Game.SkyrimSpecialEdition,
                            ModID = 51939,
                            FileID = 212497 + 1
                        }
                    }
                },
                // Google Drive Data
                new object[]
                {
                    new Archive
                    {
                        Hash = Hash.FromBase64("eSIyd+KOG3s="),
                        State = new DTOs.DownloadStates.GoogleDrive { Id = "1grLRTrpHxlg7VPxATTFNfq2OkU_Plvh_" }
                    },
                    new Archive
                    {
                        State = new DTOs.DownloadStates.GoogleDrive { Id = "2grLRTrpHxlg7VPxATTFNfq2OkU_Plvh_" }
                    }
                },
                // LoversLab Data
                new object[]
                {
                    new Archive
                    {
                        Hash = Hash.FromBase64("eSIyd+KOG3s="),
                        State = new LoversLab { IPS4Mod = 11116, IPS4File = "WABBAJACK_TEST_FILE.zip" }
                    },
                    new Archive
                    {
                        State = new LoversLab { IPS4Mod = 11116, IPS4File = "WABBAJACK_TEST_FILE_bad.zip" }
                    }
                },
                // LoversLab Attachment Data
                new object[]
                {
                    new Archive
                    {
                        Hash = Hash.FromBase64("gLJDxGDaeQ0="),
                        State = new LoversLab { IsAttachment = true, IPS4Mod = 853295 }
                    },
                    new Archive
                    {
                        State = new LoversLab { IsAttachment = true, IPS4Mod = 85329599 }
                    }
                },
                // MediaFire Data
                new object[]
                {
                    new Archive
                    {
                        Hash = Hash.FromBase64("eSIyd+KOG3s="),
                        State = new DTOs.DownloadStates.MediaFire
                        {
                            Url = new Uri("http://www.mediafire.com/file/agiqzm1xwebczpx/WABBAJACK_TEST_FILE.txt")
                        }
                    },
                    new Archive
                    {
                        State = new DTOs.DownloadStates.MediaFire
                        {
                            Url = new Uri("http://www.mediafire.com/file/agiqzm1xwebcz42/WABBAJACK_TEST_FILE.txt")
                        }
                    }
                },
                // Mega Data
                new object[]
                {
                    new Archive
                    {
                        Hash = Hash.FromBase64("eSIyd+KOG3s="),
                        State = new Mega
                        {
                            Url = new Uri("https://mega.nz/file/CsMSFaaJ#-uziC4mbJPRy2e4pPk8Gjb3oDT_38Be9fzZ6Ld4NL-k")
                        }
                    },
                    new Archive
                    {
                        State = new Mega
                        {
                            Url = new Uri("https://mega.nz/file/zz42FaaJ#-uziC4mbJPRy2e4pPk8Gjb3oDT_38Be9fzZ6L42NL-k")
                        }
                    }
                },
                // ModDB Data
                new object[]
                {
                    new Archive
                    {
                        Hash = Hash.FromBase64("V3ejL5oUeQI="),
                        State = new DTOs.DownloadStates.ModDB
                            { Url = new Uri("https://www.moddb.com/downloads/start/199178") }
                    },
                    new Archive
                    {
                        State = new DTOs.DownloadStates.ModDB
                            { Url = new Uri("https://www.moddb.com/downloads/start/199178000000") }
                    }
                },
                // VectorPlexus Data
                new object[]
                {
                    new Archive
                    {
                        Hash = Hash.FromBase64("eSIyd+KOG3s="),
                        State = new VectorPlexus { IPS4Mod = 290, IPS4File = "WABBAJACK_TEST_FILE.zip" }
                    },
                    new Archive
                    {
                        State = new VectorPlexus { IPS4Mod = 290, IPS4File = "WABBAJACK_TEST_FILE_bad.zip" }
                    }
                },
                // Wabbajack CDN Data
                new object[]
                {
                    new Archive
                    {
                        Hash = Hash.FromBase64("u7aZhqgDA6Y="),
                        State = new WabbajackCDN
                        {
                            Url = new Uri(
                                "https://authored-files.wabbajack.org/Tonal%20Architect_WJ_TEST_FILES.zip_9cb97a01-3354-4077-9e4a-7e808d47794f")
                        }
                    },
                    new Archive
                    {
                        State = new WabbajackCDN
                        {
                            Url = new Uri(
                                "https://authored-files.wabbajack.org/Tonal%20Architect_WJ_TEST_FILES.zip_9cb97a01-3354-4077-9e4a-7e808d47794fFFOOO")
                        }
                    }
                }
            };

        [Theory]
        [MemberData(nameof(TestStates))]
        public async Task TestDownloadingFile(Archive archive, Archive badArchive)
        {
            if (AutoPassTest(archive)) return;
            await using var tempFile = _temp.CreateFile();
            var hash = await _dispatcher.Download(archive, tempFile.Path, CancellationToken.None);
            Assert.Equal(archive.Hash, hash);
        }

        [Theory]
        [MemberData(nameof(TestStates))]
        public async Task TestFileVerification(Archive goodArchive, Archive badArchive)
        {
            if (AutoPassTest(goodArchive)) return;
            Assert.True(await _dispatcher.Verify(goodArchive, CancellationToken.None));
            Assert.False(await _dispatcher.Verify(badArchive, CancellationToken.None));
        }

        [Theory]
        [MemberData(nameof(TestStates))]
        public async Task CanParseAndUnParseUrls(Archive goodArchive, Archive badArchive)
        {
            if (AutoPassTest(goodArchive)) return;
            var downloader = _dispatcher.Downloader(goodArchive);
            if (downloader is IUrlDownloader urlDownloader)
            {
                var unparsed = urlDownloader.UnParse(goodArchive.State);

                var parsed = urlDownloader.Parse(unparsed);
                Assert.NotNull(parsed);

                Assert.Equal(goodArchive.State.GetType(), parsed.GetType());
                Assert.True(await _dispatcher.Verify(new Archive { State = parsed }, CancellationToken.None));
            }
        }

        [Theory]
        [MemberData((nameof(TestStates)))]
        public async Task CanParseAndUnParseMetaInis(Archive goodArchive, Archive badArchive)
        {
            var meta = _dispatcher.MetaIniSection(goodArchive);
            var parsedIni = meta.LoadIniString()["General"];
            var newState = await _dispatcher.ResolveArchive(parsedIni.ToDictionary(d => d.KeyName, d => d.Value));
            Assert.NotNull(newState);
            Assert.Equal(meta, _dispatcher.MetaIniSection(new Archive {State = newState!}));
        }
    }
}