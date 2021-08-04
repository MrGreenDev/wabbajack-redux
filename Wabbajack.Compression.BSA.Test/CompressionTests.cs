using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Wabbajack.Paths;
using Wabbajack.Paths.IO;
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