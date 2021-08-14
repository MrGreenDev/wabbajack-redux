﻿using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Wabbajack.Common;
using Wabbajack.Lib;
using Wabbajack.Server.DataLayer;
using Wabbajack.Server.Services;

namespace Wabbajack.BuildServer.Controllers
{
    [ApiController]
    [Route("/list_definitions")]
    public class ListDefinitions : ControllerBase
    {
        private ILogger<ListDefinitions> _logger;
        private SqlService _sql;
        private DiscordWebHook _discord;
        private readonly AppSettings _settings;

        public ListDefinitions(ILogger<ListDefinitions> logger, SqlService sql, DiscordWebHook discord, AppSettings settings)
        {
            _logger = logger;
            _sql = sql;
            _discord = discord;
            _settings = settings;
        }


        [Route("ingest")]
        [Authorize(Roles = "User")]
        [HttpPost]
        public async Task<IActionResult> PostIngest()
        {
            var user = Request.Headers[_settings.MetricsKeyHeader].First();
            var use_gzip = Request.Headers[_settings.CompressedBodyHeader].Any();
            _logger.Log(LogLevel.Information, $"Ingesting Modlist Definition for {user}");

            var modlistBytes = await Request.Body.ReadAllAsync();
            


            _logger.LogInformation("Spawning ingestion task");
            var tsk = Task.Run(async () =>
            {
                try
                {
                    if (use_gzip)
                    {
                        await using var os = new MemoryStream();
                        await using var gZipStream =
                            new GZipStream(new MemoryStream(modlistBytes), CompressionMode.Decompress);
                        await gZipStream.CopyToAsync(os);
                        modlistBytes = os.ToArray();
                    }

                    var modlist = new MemoryStream(modlistBytes).FromJson<ModList>();

                    var file = AbsolutePath.EntryPoint.Combine("mod_list_definitions")
                        .Combine($"{user}_{DateTime.UtcNow.ToFileTimeUtc()}.json");
                    file.Parent.CreateDirectory();
                    await using var stream = await file.Create();
                    modlist.ToJson(stream);
                    _logger.Log(LogLevel.Information, $"Done Ingesting Modlist Definition for {user}");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error ingesting uploaded modlist");
                }
            });
            
            return Accepted(0);
        }
        
    }
}
