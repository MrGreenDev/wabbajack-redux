using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Wabbajack.TaskTracking.Interfaces;

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
        
        public static async Task CopyToWithStatusAsync(this Stream input, long maxSize, Stream output, ITrackedTask task, CancellationToken token)
        {
            var buffer = new byte[1024 * 1024];
            if (maxSize == 0) maxSize = 1;
            long totalRead = 0;
            var remain = maxSize; 
            while (true)
            {
                var toRead = Math.Min(buffer.Length, remain);
                var read = await input.ReadAsync(buffer.AsMemory(0, (int)toRead), token);
                remain -= read;
                if (read == 0) break;
                totalRead += read;
                await output.WriteAsync(buffer.AsMemory(0, read), token);
                await task.ReportProgress(Percent.FactoryPutInRange(totalRead, maxSize), totalRead);
            }

            await output.FlushAsync(token);
        }
        
    }
}