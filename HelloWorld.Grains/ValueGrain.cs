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
    /// Orleans grain implementation class ValueGrain.
    /// </summary>
    public class ValueGrain : Orleans.Grain, IValue
    {
        private readonly ILogger logger;

        private string _value = "";

        public ValueGrain(ILogger<ValueGrain> logger)
        {
            this.logger = logger;
        }

        public Task<string> GetAsync()
        {
            return Task.FromResult(_value);
        }

        public void SetAsync(string value)
        {
            this._value = value;
        }
    }
}
