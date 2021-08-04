using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Wabbajack.Common
{
    public static class StreamExtensions
    {
        public static async Task CopyToLimitAsync(this Stream frm, Stream tw, int limit, CancellationToken token)
        {
            var buff = new byte[1024 * 128];
            while (limit > 0 && !token.IsCancellationRequested)
            {
                var toRead = Math.Min(buff.Length, limit);
                var read = await frm.ReadAsync(buff.AsMemory(0, toRead), token);
                if (read == 0)
                    throw new Exception("End of stream before end of limit");
                await tw.WriteAsync(buff.AsMemory(0, read), token);
                limit -= read;
            }

            await tw.FlushAsync(token);
        }
        
    }
}