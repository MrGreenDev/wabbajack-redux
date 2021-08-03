using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Wabbajack.Hashing.xxHash64;
using Wabbajack.Paths;
using Wabbajack.Paths.IO;

namespace Wabbajack.CLI.Verbs
{
    [Verb("hash-file", HelpText = "Hash a file and print the result")]
    public class HashFile : AVerb<HashFile>
    {
        public HashFile(ILogger<HashFile> logger) : base(logger)
        {
        }

        [Option('i', "input", Required = true, HelpText = "Input file name")]        
        public async Task<int> Run(AbsolutePath input)
        {
            await using var istream = input.Open(FileMode.Open, FileAccess.Read, FileShare.Read);
            var hash = await istream.HashingCopy(Stream.Null, CancellationToken.None);
            Logger.LogInformation($"{input} hash: {hash} {hash.ToHex()} {(long)hash}");
            return 0;
        }

    }
}