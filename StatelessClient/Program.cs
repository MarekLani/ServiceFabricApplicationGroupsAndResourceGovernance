using System;
using System.Diagnostics;
using System.Fabric;
using System.Fabric.Description;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.ServiceFabric.Services.Client;
using Microsoft.ServiceFabric.Services.Runtime;

namespace StatelessClient
{
    internal static class Program
    {
        /// <summary>
        /// This is the entry point of the service host process.
        /// </summary>
        private static void Main()
        {
            try
            {
                // The ServiceManifest.XML file defines one or more service type names.
                // Registering a service maps a service type name to a .NET type.
                // When Service Fabric creates an instance of this service type,
                // an instance of the class is created in this host process.

                ServiceRuntime.RegisterServiceAsync("StatelessClientType",
                    context => new StatelessClient(context)).GetAwaiter().GetResult();

                ServiceEventSource.Current.ServiceTypeRegistered(Process.GetCurrentProcess().Id, typeof(StatelessClient).Name);
                //So new instance is addresable by remoting
                // This fabricclient object should be cached in your service.
                FabricClient client = new FabricClient();

                // The applicationName Uri is the name of your application that hosts your services that you are scaling out.
                ServiceNotificationFilterDescription filter = new ServiceNotificationFilterDescription(new Uri("fabric:/SF_repo1"), true, false);
                client.ServiceManager.RegisterServiceNotificationFilterAsync(filter);
                var resolver = new ServicePartitionResolver(() => client);
                ServicePartitionResolver.SetDefault(resolver);

                // Prevents this host process from terminating so services keep running.
                Thread.Sleep(Timeout.Infinite);
            }
            catch (Exception e)
            {
                ServiceEventSource.Current.ServiceHostInitializationFailed(e.ToString());
                throw;
            }
        }
    }
}
