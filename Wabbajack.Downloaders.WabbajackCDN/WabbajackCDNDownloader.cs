﻿using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Toolkit.HighPerformance;
using Wabbajack.Common;
using Wabbajack.Downloaders.Interfaces;
using Wabbajack.DTOs;
using Wabbajack.DTOs.CDN;
using Wabbajack.DTOs.DownloadStates;
using Wabbajack.DTOs.JsonConverters;
using Wabbajack.DTOs.Validation;
using Wabbajack.Hashing.xxHash64;
using Wabbajack.Networking.Http;
using Wabbajack.Paths;
using Wabbajack.Paths.IO;
using Wabbajack.RateLimiter;

namespace Wabbajack.Downloaders
{
    public class WabbajackCDNDownloader : ADownloader<WabbajackCDN>, IUrlDownloader
    {
        private readonly HttpClient _client;
        private readonly ILogger<WabbajackCDNDownloader> _logger;
        private readonly DTOSerializer _dtos;
        private readonly ParallelOptions _parallelOptions;
        private readonly IRateLimiter _limiter;
        
        public static Dictionary<string, string> DomainRemaps = new()
        {
            {"wabbajack.b-cdn.net", "authored-files.wabbajack.org"},
            {"wabbajack-mirror.b-cdn.net", "mirror.wabbajack.org"},
            {"wabbajack-patches.b-cdn.net", "patches.wabbajack.org"},
            {"wabbajacktest.b-cdn.net", "test-files.wabbajack.org"}
        };
        
        public WabbajackCDNDownloader(ILogger<WabbajackCDNDownloader> logger, IRateLimiter limiter, HttpClient client, DTOSerializer dtos)
        {
            _client = client;
            _limiter = limiter;
            _logger = logger;
            _dtos = dtos;
        }
        public override async Task<Hash> Download(Archive archive, WabbajackCDN state, AbsolutePath destination, CancellationToken token)
        {
            var definition = (await GetDefinition(state, token))!;
            await using var fs = destination.Open(FileMode.Create, FileAccess.Write, FileShare.None);
            
            SemaphoreSlim slim = new(1, 1);
            await definition.Parts.PDoAll(
            async part =>
            {
                using var networkJob = await _limiter.Begin(
                    $"Downloading {definition.OriginalFileName} ({part.Index}/{definition.Parts.Length}", part.Size, token, Resource.Network);
                
                var msg = MakeMessage(new Uri(state.Url + $"/parts/{part.Index}"));
                using var response = await _client.SendAsync(msg, HttpCompletionOption.ResponseHeadersRead, token);
                if (!response.IsSuccessStatusCode)
                    throw new InvalidDataException($"Bad response for part request for part {part.Index}");
                
                using var data = await response.Content.ReadAsByteArrayAsync(token, networkJob);
                if (data.Memory.Length < part.Size)
                    throw new InvalidDataException(
                        $"Bad part size, expected {part.Size} got {data.Memory.Length} for part {part.Index}");

                await slim.WaitAsync(token);
                try
                {
                    using var diskCpuJob = await _limiter.Begin(
                        $"Hashing and saving {definition.OriginalFileName} ({part.Index}/{definition.Parts.Length})", part.Size,
                        token, Resource.Disk, Resource.CPU);
                    fs.Position = part.Offset;

                    var hash = await data.Memory[..(int)part.Size].AsStream().HashingCopy(fs, token);
                    if (hash != part.Hash)
                        throw new InvalidDataException($"Bad part hash, got {hash} expected {part.Hash} for part {part.Index}");
                    await fs.FlushAsync(token);
                }
                finally
                {
                    slim.Release();
                }
            });
            return definition.Hash;
        }

        public override Task<bool> Prepare()
        {
            return Task.FromResult(true);
        }

        public override bool IsAllowed(ServerAllowList allowList, IDownloadState state)
        {
            return true;
        }
        
        private async Task<FileDefinition?> GetDefinition(WabbajackCDN state, CancellationToken token)
        {
            var msg = MakeMessage(new Uri(state.Url + "/definition.json.gz"));
            using var data = await _client.SendAsync(msg, token);
            if (!data.IsSuccessStatusCode) return null;
            
            await using var stream = await data.Content.ReadAsStreamAsync(token);
            await using var gz = new GZipStream(stream, CompressionMode.Decompress);
            return (await _dtos.DeserializeAsync<FileDefinition>(gz, token))!;
        }

        private HttpRequestMessage MakeMessage(Uri url)
        {
            if (DomainRemaps.TryGetValue(url.Host, out var host))
            {
                url = (new UriBuilder(url) { Host = host }).Uri;
            }
            else
            {
                host = url.Host;
            }

            var msg = new HttpRequestMessage(HttpMethod.Get, url);
            msg.Headers.Add("Host", host);
            return msg;
        }

        public override IDownloadState? Resolve(IReadOnlyDictionary<string, string> iniData)
        {
            if (iniData.ContainsKey("directURL") && Uri.TryCreate(iniData["directURL"], UriKind.Absolute, out var uri))
                return Parse(uri);
            return null;
        }

        public override async Task<bool> Verify(Archive archive, WabbajackCDN archiveState, CancellationToken token)
        {
            return await GetDefinition(archiveState, token) != null;
        }

        public override IEnumerable<string> MetaIni(Archive a, WabbajackCDN state)
        {
            return new[] { $"directURL={state.Url}" };
        }

        public IDownloadState? Parse(Uri url)
        {
            if (DomainRemaps.ContainsKey(url.Host) || DomainRemaps.ContainsValue(url.Host))
            {
                return new WabbajackCDN{Url = url};
            }
            return null;
        }

        public Uri UnParse(IDownloadState state)
        {
            return ((WabbajackCDN)state).Url;
        }
        
        public override Priority Priority => Priority.Normal;
    }
}