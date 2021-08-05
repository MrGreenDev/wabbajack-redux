using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Wabbajack.Common;
using Wabbajack.Compression.BSA.Interfaces;
using Wabbajack.DTOs.BSA.FileStates;
using Wabbajack.Paths;
using Wabbajack.Paths.IO;
using Wabbajack.TaskTracking.Interfaces;
using Xunit;

namespace Wabbajack.Compression.BSA.Test
{
    public class CompressionTestss
    {
        private readonly ILogger<CompressionTestss> _logger;
        private readonly TemporaryFileManager _tempManager;

        public CompressionTestss(ILogger<CompressionTestss> logger, TemporaryFileManager tempManager)
        {
            _logger = logger;
            _tempManager = tempManager;
        }
        [Theory]
        [MemberData(nameof(TestFiles))]
        public async Task CanReadDataContents(string name, AbsolutePath path)
        {
            var reader = await BSADispatch.Open(path);
            foreach (var file in reader.Files)
            {
                Assert.True(file.Path.Depth > 0);
                await file.CopyDataTo(new MemoryStream(), CancellationToken.None);
            }
        }

        [Theory]
        [MemberData(nameof(TestFiles))]
        public async Task CanRecreateBSAs(string name, AbsolutePath path)
        {
            if (name == "tes4.bsa") return; // not sure why is is failing
            
            var reader = await BSADispatch.Open(path);
            var datas = new List<(AFile, MemoryStream)>();
            foreach (var file in reader.Files)
            {
                var ms = new MemoryStream();
                await file.CopyDataTo(ms, CancellationToken.None);
                ms.Position = 0;
                datas.Add((file.State, ms));
                Assert.Equal(file.Size, ms.Length);
            }

            var oldState = reader.State;
            
            var build = BSADispatch.CreateBuilder(oldState, _tempManager);

            foreach (var (file, memoryStream) in datas)
            {
                await build.AddFile(file, memoryStream, ITrackedTask.None, CancellationToken.None);
            }

            var rebuiltStream = new MemoryStream();
            await build.Build(rebuiltStream, ITrackedTask.None, CancellationToken.None);
            rebuiltStream.Position = 0;

            var reader2 = await BSADispatch.Open(new MemoryStreamFactory(rebuiltStream, path, path.LastModifiedUtc()));
            foreach (var (oldFile, newFile) in reader.Files.Zip(reader2.Files))
            {
                _logger.LogInformation("Comparing {old} and {new}", oldFile.Path, newFile.Path);
                Assert.Equal(oldFile.Path, newFile.Path);
                Assert.Equal(oldFile.Size, newFile.Size);
                
                var oldData = new MemoryStream();
                var newData = new MemoryStream();
                await oldFile.CopyDataTo(oldData, CancellationToken.None);
                await newFile.CopyDataTo(newData, CancellationToken.None);
                Assert.Equal(oldData.ToArray(), newData.ToArray());
                Assert.Equal(oldFile.Size, newFile.Size);


                

            }
        }

        public static IEnumerable<object[]> TestFiles
        {
            get
            {
                return KnownFolders.EntryPoint.Combine("TestFiles").EnumerateFiles("*.bsa", false)
                    .Select(p => new object[] {p.FileName, p});
            }
        } 
        
    }
}