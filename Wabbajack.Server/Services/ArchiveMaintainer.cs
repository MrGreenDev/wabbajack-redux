﻿using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Alphaleonis.Win32.Filesystem;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Logging;
using Wabbajack.BuildServer;
using Wabbajack.Common;
using Wabbajack.Hashing.xxHash64;
using Wabbajack.Paths;
using Wabbajack.Paths.IO;
using File = System.IO.File;

namespace Wabbajack.Server.Services
{
    /// <summary>
    /// Maintains a concurrent cache of all the files we've downloaded, indexed by Hash. 
    /// </summary>
    public class ArchiveMaintainer
    {
        private AppSettings _settings;
        private ILogger<ArchiveMaintainer> _logger;

        public ArchiveMaintainer(ILogger<ArchiveMaintainer> logger, AppSettings settings)
        {
            _settings = settings;
            _logger = logger;
            _logger.Log(LogLevel.Information, "Creating Archive Maintainer");
        }

        public void Start()
        {
            _logger.Log(LogLevel.Information, $"Found {_settings.ArchivePath.EnumerateFiles(false).Count()} archives");
        }

        private AbsolutePath ArchivePath(Hash hash)
        {
            return _settings.ArchivePath.Combine(hash.ToHex());
        }

        public async Task Ingest(AbsolutePath file)
        {
            var hash = await file.FileHashAsync();
            if (hash == null) return;
            
            var path = ArchivePath(hash.Value);
            if (HaveArchive(hash.Value))
            {
                file.Delete();
                return;
            }
            
            var newPath = _settings.ArchivePath.Combine(hash.Value.ToHex());
            await file.MoveToAsync(newPath, true, CancellationToken.None);
        }

        public bool HaveArchive(Hash hash)
        {
            return ArchivePath(hash).FileExists();
        }

        public bool TryGetPath(Hash hash, out AbsolutePath path)
        {
            path = ArchivePath(hash);
            return path.FileExists();
        }
    }
    
    public static class ArchiveMaintainerExtensions 
    {
        public static void UseArchiveMaintainer(this IApplicationBuilder b)
        {
            var poll = (ArchiveMaintainer)b.ApplicationServices.GetService(typeof(ArchiveMaintainer));
            poll.Start();
        }
    
    }
}
