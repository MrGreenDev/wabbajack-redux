using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Wabbajack.DTOs.BSA.ArchiveStates;
using Wabbajack.DTOs.BSA.FileStates;
using Wabbajack.DTOs.DownloadStates;
using Wabbajack.DTOs.JsonConverters;
using Wabbajack.DTOs.Validation;
using Wabbajack.Paths;
using Wabbajack.Paths.IO;
using Xunit;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Wabbajack.DTOs.Test
{
    public class ModListTests
    {
        private readonly DTOSerializer _serializer;
        private readonly HttpClient _client;

        public ModListTests(DTOSerializer serializer, HttpClient client)
        {
            _serializer = serializer;
            _client = client;
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

        [Theory]
        [InlineData("https://raw.githubusercontent.com/wabbajack-tools/mod-lists/master/modlists.json")]
        [InlineData("https://raw.githubusercontent.com/wabbajack-tools/mod-lists/master/utility_modlists.json")]
        [InlineData("https://raw.githubusercontent.com/wabbajack-tools/mod-lists/master/unlisted_modlists.json")]
        public async Task CanLoadModListMetadata(string uri)
        {
            var str = await _client.GetStringAsync(uri);
            var data = _serializer.Deserialize<ModlistMetadata[]>(str);
            var s = _serializer.Serialize(data);
            Assert.True(data!.Length > 0);
        }
        
        [Theory]
        [InlineData("https://raw.githubusercontent.com/wabbajack-tools/opt-out-lists/master/ServerWhitelist.yml")]
        public async Task CanLoadAllowList(string uri)
        {
            var str = await _client.GetStringAsync(uri);
            var d = new DeserializerBuilder()
                .WithNamingConvention(PascalCaseNamingConvention.Instance)
                .Build();
            var list = d.Deserialize<ServerAllowList>(str);
            
            Assert.True(list.GoogleIDs.Length > 1);
            Assert.True(list.AllowedPrefixes.Length > 1);
        }
    }
}