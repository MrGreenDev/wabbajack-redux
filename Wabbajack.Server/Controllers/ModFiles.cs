﻿using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Wabbajack.Common;
using Wabbajack.Server.DataLayer;

namespace Wabbajack.BuildServer.Controllers
{
    [Authorize(Roles = "User")]
    [ApiController]
    [Route("/mod_files")]
    public class ModFilesForHash : ControllerBase
    {
        private SqlService _sql;
        private ILogger<ModFilesForHash> _logger;

        public ModFilesForHash(ILogger<ModFilesForHash> logger, SqlService sql)
        {
            _logger = logger;
            _sql = sql;
        }

        [HttpGet("by_hash/{hashAsHex}")]
        public async Task<IActionResult> GetByHash(string hashAsHex)
        {
            var files = await _sql.ResolveDownloadStatesByHash(Hash.FromHex(hashAsHex));
            return Ok(files.ToJson());
        }
    }
}
