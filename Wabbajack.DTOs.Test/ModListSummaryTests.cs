using System;
using System.Linq;
using System.Text.Json;
using Wabbajack.DTOs.BSA.ArchiveStates;
using Wabbajack.DTOs.BSA.FileStates;
using Wabbajack.DTOs.DownloadStates;
using Wabbajack.DTOs.JsonConverters;
using Wabbajack.Paths;
using Wabbajack.Paths.IO;
using Xunit;

namespace Wabbajack.DTOs.Test
{
    public class UnitTest1
    {
        private readonly DTOSerializer _serializer;

        public UnitTest1(DTOSerializer serializer)
        {
            _serializer = serializer;
        }
        [Fact]
        public void CanLoadModListSummaryInfo()
        {
            var jsonPath = KnownFolders.EntryPoint.Combine(@"Resources\ModListSummarySample.json");
            var data = JsonSerializer.Deserialize<ModListSummary[]>(jsonPath.ReadAllText());
            Assert.NotNull(data);
            Assert.Equal(38, data.Length);
        }
        
        [Fact]
        public void CanLoadModList()
        {
            var jsonPath = KnownFolders.EntryPoint.Combine(@"Resources\ModListSample.json");
            var data = _serializer.Deserialize<ModList>(jsonPath.ReadAllText());
            var s = _serializer.Serialize(data);
        }
    }
}