using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Wabbajack.Hashing.xxHash64
{
    public static class StreamExtensions
    {
        public static async Task<Hash> Hash(this Stream stream, CancellationToken token)
        {
            return await stream.HashingCopy(Stream.Null, token);
        }
        public static async Task<Hash> HashingCopy(this Stream inputStream, Stream outputStream,
            CancellationToken token)
        {
            var buffer = new byte[1024 * 1024];

            var hasher = new xxHashAlgorithm(0);

            var running = true;
            ulong finalHash = 0;
            while (running && !token.IsCancellationRequested)
            {
                var totalRead = 0;

                while (totalRead != buffer.Length)
                {
                    var read = await inputStream.ReadAsync(buffer.AsMemory(totalRead, buffer.Length - totalRead),
                        token);
                    if (read == 0)
                    {
                        running = false;
                        break;
                    }

                    totalRead += read;
                }

                var pendingWrite = outputStream.WriteAsync(buffer, 0, totalRead, token);
                if (running)
                {
                    hasher.TransformByteGroupsInternal(buffer);
                    await pendingWrite;
                }
                else
                {
                    var preSize = (totalRead >> 5) << 5;
                    if (preSize > 0)
                    {
                        hasher.TransformByteGroupsInternal(buffer.AsSpan()[..preSize]);
                        finalHash = hasher.FinalizeHashValueInternal(buffer.AsSpan()[preSize..totalRead]);
                        await pendingWrite;
                        break;
                    }

                    finalHash = hasher.FinalizeHashValueInternal(buffer.AsSpan()[..totalRead]);
                    await pendingWrite;
                    break;
                }
            }

            await outputStream.FlushAsync(token);

            return new Hash(finalHash);
        }
    }
}