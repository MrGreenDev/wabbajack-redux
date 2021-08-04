using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
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
            var reader = await BSADispatch.Open(path);
            var datas = new List<(AFile, MemoryStream)>();
            foreach (var file in reader.Files)
            {
                var ms = new MemoryStream();
                await file.CopyDataTo(ms, CancellationToken.None);
                ms.Position = 0;
                datas.Add((file.State, ms));
            }

            var oldState = reader.State;

            using var manager = new TemporaryFileManager();
            var build = BSADispatch.CreateBuilder(oldState, manager);

            foreach (var (file, memoryStream) in datas)
            {
                await build.AddFile(file, memoryStream, ITrackedTask.None, CancellationToken.None);
            }

            var rebuiltStream = new MemoryStream();
            await build.Build(rebuiltStream, ITrackedTask.None, CancellationToken.None);
            rebuiltStream.Position = 0;

            var reader2 = await BSADispatch.Open(new MemoryStreamFactory(rebuiltStream, path, path.LastModifiedUtc()));
            IReadOnlyDictionary<RelativePath, IFile> newFiles = reader2.Files.ToDictionary(f => (RelativePath)f.Path);
            foreach (var oldFile in reader.Files)
            {
                Assert.Contains(oldFile.Path, newFiles);
                var newFile = newFiles[oldFile.Path];
                Assert.Equal(oldFile.Size, newFile.Size);

                var oldData = new MemoryStream();
                var newData = new MemoryStream();
                await oldFile.CopyDataTo(oldData, CancellationToken.None);
                await newFile.CopyDataTo(newData, CancellationToken.None);
                
                Assert.Equal(oldData.ToArray(), newData.ToArray());
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