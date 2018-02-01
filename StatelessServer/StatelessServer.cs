using System;
using System.Collections.Generic;
using System.Fabric;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.ServiceFabric.Services.Communication.Runtime;
using Microsoft.ServiceFabric.Services.Remoting.Runtime;
using Microsoft.ServiceFabric.Services.Runtime;
using Shared;

namespace StatelessServer
{
    /// <summary>
    /// An instance of this class is created for each service instance by the Service Fabric runtime.
    /// </summary>
    internal sealed class StatelessServer : StatelessService, IContract
    {
        private readonly int _myName;

        public StatelessServer(StatelessServiceContext context)
            : base(context)
        {
            _myName = new Random((int)(DateTimeOffset.Now.Ticks % 10000)).Next(10000);
        }

        public Task<int> WhoAreYou() => Task.FromResult(_myName);

        /// <summary>
        /// Optional override to create listeners (e.g., TCP, HTTP) for this service replica to handle client or user requests.
        /// </summary>
        /// <returns>A collection of listeners.</returns>
        protected override IEnumerable<ServiceInstanceListener> CreateServiceInstanceListeners()
        {
            return this.CreateServiceRemotingInstanceListeners();
        }

        /// <summary>
        /// This is the main entry point for your service instance.
        /// </summary>
        /// <param name="cancellationToken">Canceled when Service Fabric needs to shut down this service instance.</param>
        protected override async Task RunAsync(CancellationToken cancellationToken)
        {
            long memoryEaten = 0;

            try
            {
                while (true)
                {
                    //Simulate memory consumption
                    //Marshal.AllocHGlobal(10_000_000);
                    //memoryEaten += 10_000_000;
                    //ServiceEventSource.Current.ServiceMessage(this.Context, "I have eaten {0} MBytes Working-{1}", memoryEaten / 1000000, _myName);

                    cancellationToken.ThrowIfCancellationRequested();


                    await Task.Delay(TimeSpan.FromSeconds(5), cancellationToken);
                 
                }
            }
            //Will be thrown once service eats all dedicated memory by SF runtime
            catch(OutOfMemoryException)
            {
                ServiceEventSource.Current.ServiceMessage(this.Context, "I am out of memory");
            }
        }
    }
}
