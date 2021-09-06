using System;
using System.Buffers;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Wabbajack.RateLimiter
{
    public class CompositeJob : IJob
    {
        private readonly IJob[] _jobs;

        public CompositeJob(IJob[] jobs)
        {
            _jobs = jobs;
        }
        
        public void Dispose()
        {
            foreach (var job in _jobs)
                job.Dispose();
        }

        public ulong ID => throw new NotImplementedException();
        public string Description => _jobs[0].Description;
        public long Current
        {
            get => _jobs[0].Current;
            set
            {
                foreach (var job in _jobs) 
                    job.Current = value;
            }
        }

        public long Size
        {
            get => _jobs[0].Size;
            set
            {
                foreach (var job in _jobs) 
                    job.Size = value;
            }
        }
        
        public async ValueTask<IMemoryOwner<byte>> Process(int size, CancellationToken token)
        {
            var array = await _jobs[0].Process(size, token);
            foreach (var job in _jobs.Skip(1))
                await job.Report(size, token);
            return array;
        }

        public async ValueTask Report(int processedSize, CancellationToken token)
        {
            foreach (var job in _jobs)
                await job.Report(processedSize, token);
        }
    }
}