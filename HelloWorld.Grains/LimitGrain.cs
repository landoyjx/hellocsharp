using System.Runtime.Serialization;
using System.Net;
using System.ComponentModel;
using System;
using System.Net.NetworkInformation;
using System.Data;
using System.Runtime.CompilerServices;
using HelloWorld.Interfaces;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Orleans;
using Orleans.Runtime;

namespace HelloWorld.Grains
{
    /// <summary>
    /// Orleans grain implementation class LimitGrain.
    /// </summary>
    public class LimitGrain : Orleans.Grain, ILimit
    {
        private readonly ILogger logger;
        private int numDropsInBucket = 0;
        private readonly int _BUCKET_SIZE_IN_DROPS = 30;
        private readonly double DROP_LEAKS_PER_MS = 0.0005; // 30 request per minutes
        private DateTime timeOfLastDropLeak = DateTime.Now;

        public LimitGrain(ILogger<LimitGrain> logger)
        {
            this.logger = logger;
        }

        public Task<bool> checkLimitOk()
        {
            DateTime now = DateTime.Now;
            double seconds = now.Subtract(timeOfLastDropLeak).TotalMilliseconds;
            long numberToLeak = (long)(seconds * DROP_LEAKS_PER_MS);
            if (numberToLeak > 0)
            {
                if (numDropsInBucket < numberToLeak)
                    numDropsInBucket = 0;
                else
                    numDropsInBucket -= (int) numberToLeak;
            }
            timeOfLastDropLeak = now;

            if (numDropsInBucket > _BUCKET_SIZE_IN_DROPS)
            {
                numDropsInBucket++;
                return Task.FromResult(false);
            }
            return Task.FromResult(true);
        }
    }
}
