using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Wabbajack.Hashing.xxHash64
{
    public static class StreamExtensions
    {
        public static async Task<Hash> HashingCopy(this Stream inputStream, Stream outputStream, CancellationToken token)
        {
            var buffer = new byte[1024 * 1024];

            var hasher = new xxHashAlgorithm();

            var running = true;
            ulong finalHash = 0;
            while (running && !token.IsCancellationRequested)
            {
                var totalRead = 0;

                while (totalRead != buffer.Length)
                {
                    var read = await inputStream.ReadAsync(buffer, totalRead, buffer.Length - totalRead, token);
                    if (read == 0)
                    {
                        running = false;
                        break;
                    }

                    totalRead += read;
                }

                var pendingWrite = outputStream.WriteAsync(buffer, 0, totalRead, token);
                if (totalRead != buffer.Length)
                {
                    finalHash = hasher.FinalizeHashValueInternal(buffer[..totalRead]);
                }
                else
                {
                    hasher.TransformByteGroupsInternal(buffer);
                }

                await pendingWrite;
            }

            return new Hash(finalHash);
        }
    }
}