using System;
using System.Collections.Generic;
using System.Fabric;
using System.Fabric.Description;
using System.Fabric.Query;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace AdminAppCore
{
    class Program
    {
        static void Main(string[] args)
        {
            try
            {
                Settings.LoadSettings();

                Console.WriteLine("First we need to set few things up to connect to your SF cluster.");
                if (Settings.Instance == null)
                    Settings.Instance = new Settings();
                Settings.Instance.connection = SetSetting(Settings.Instance.connection,"Please provide connection endpoint of your SF cluster (sf.westeurope.cloudapp.azure.com:19000)");
                Settings.Instance.CommonName = SetSetting(Settings.Instance.CommonName, "Please provide common cluster name (www.sf.westeurope.azure.com)");
                Settings.Instance.clientCertThumb = SetSetting(Settings.Instance.clientCertThumb, "Please provide client cert thumbprint");
                Settings.Instance.serverCertThumb = SetSetting(Settings.Instance.serverCertThumb, "Please provide server cert thumbprint");
                Settings.Instance.appName = SetSetting(Settings.Instance.appName, "Please provide app name in format fabric:/AppName");
                Settings.Instance.appTypeName = SetSetting(Settings.Instance.appTypeName, "Please provide app type name");
                Settings.Instance.appVersion = SetSetting(Settings.Instance.serverCertThumb, "Please provide app version");

                Settings.SaveSettings();

                var fc = ConnectToSF();

                //Choose Scenario
                string option = "";
                do
                {
                    Console.WriteLine("Plese select from following options (type number and press enter):");
                    Console.WriteLine("1: Create Application");
                    Console.WriteLine("2: Create Stateless service with default metric");
                    Console.WriteLine("3: Create Statefull service with default metric");
                    Console.WriteLine("4: Limit number of nodes for app");
                    Console.WriteLine("5: Set node and application metric capacity");
                    Console.WriteLine("6: Set minimum number of nodes for app and reserve capacity for app on node");
                    Console.WriteLine("7: Set placement constraints for service");
                    Console.WriteLine("8: Show app load info");
                    Console.Write("q: Exit");
                    option = Console.ReadLine();

                    switch (option)
                    {
                        case "1":
                            CreateApp(fc);
                            break;
                        case "2":
                            CreateStatelessServiceWithMetrics(fc);
                            break;
                        case "3":
                            CreateStatefulServiceWithMetrics(fc);
                            break;
                        case "4":
                            LimitNumberOfNodesForApp(fc);
                            break;
                        case "5":
                            SetAppCapacity(fc);
                            break;
                        case "6":
                            ReserveCapacity(fc);
                            break;
                        case "7":
                            ChangePlacementConstraints(fc);
                            break;
                        case "8":
                            GetAppLoadInfo(fc);
                            break;
                    }

                } while (option != "q");
                Console.WriteLine("Press enter to exit");
            }
            catch(Exception e)
            {
                Console.WriteLine(e.Message);
            }
            Console.ReadLine();
        }

        private static string SetSetting(string setting, string message)
        {
            Console.WriteLine(message);
            if (setting != "")
                Console.WriteLine($"Default is '{setting}' Press enter to leave default, provide value to change.");
            var newSetting = Console.ReadLine();
            if (newSetting != "")
                return newSetting;
            else
                return setting;
        }

        static ApplicationDescription CreateAppDescription()
        {
            ApplicationDescription ad = new ApplicationDescription();
            ad.ApplicationName = new Uri(Settings.Instance.appName);
            ad.ApplicationTypeName = Settings.Instance.appTypeName;
            ad.ApplicationTypeVersion = Settings.Instance.appVersion;

            return ad;
        }

        static async void CreateApp(FabricClient fc)
        {
            var ad = CreateAppDescription();
            await fc.ApplicationManager.CreateApplicationAsync(ad);
        }

        static async void CreateStatelessServiceWithMetrics(FabricClient fc)
        {

            Console.WriteLine("Provide service type name");
            string serviceTypeName = Console.ReadLine();

            Console.WriteLine("Provide statless service name");
            string serviceName = Console.ReadLine();

            var serviceNameUriBuilder = new ServiceUriBuilder(Settings.Instance.appName, serviceName);

            StatelessServiceDescription statelessService = new StatelessServiceDescription()
            {
                ApplicationName = new Uri(Settings.Instance.appName),
                InstanceCount = 1,
                ServiceName = serviceNameUriBuilder.Build(),
                ServiceTypeName = serviceTypeName,
                PartitionSchemeDescription = new SingletonPartitionSchemeDescription(),


            };

            StatelessServiceLoadMetricDescription connectionMetric = new StatelessServiceLoadMetricDescription();

            Console.WriteLine("Set metric name:");
            string metricName = Console.ReadLine();

            Console.WriteLine("Default load:");
            int defaultLoad = Convert.ToInt32(Console.ReadLine());

            connectionMetric.Name = metricName;
            connectionMetric.DefaultLoad = defaultLoad;
            connectionMetric.Weight = ServiceLoadMetricWeight.High;

            statelessService.Metrics.Add(connectionMetric);

            await fc.ServiceManager.CreateServiceAsync(statelessService);
        }

        static async void CreateStatefulServiceWithMetrics(FabricClient fc)
        {

            Console.WriteLine("Provide service type name");
            string serviceTypeName = Console.ReadLine();

            Console.WriteLine("Provide stateful service name");
            string serviceName = Console.ReadLine();

            var serviceNameUriBuilder = new ServiceUriBuilder(Settings.Instance.appName, serviceName);

            StatefulServiceDescription statelessService = new StatefulServiceDescription()
            {
                ApplicationName = new Uri(Settings.Instance.appName),
                ServiceName = serviceNameUriBuilder.Build(),
                ServiceTypeName = serviceTypeName,
                //we set 1 partition by default
                PartitionSchemeDescription = new UniformInt64RangePartitionSchemeDescription(1)                ,
            };

            StatefulServiceLoadMetricDescription connectionMetric = new StatefulServiceLoadMetricDescription();

            Console.WriteLine("Set metric name:");
            string metricName = Console.ReadLine();

            Console.WriteLine("Set metric name:");
            int defaultLoad = Convert.ToInt32(Console.ReadLine());

            connectionMetric.Name = metricName;
            connectionMetric.PrimaryDefaultLoad = defaultLoad;
            connectionMetric.SecondaryDefaultLoad = 0;
            connectionMetric.Weight = ServiceLoadMetricWeight.High;

            statelessService.Metrics.Add(connectionMetric);

            await fc.ServiceManager.CreateServiceAsync(statelessService);
        }

        static async void LimitNumberOfNodesForApp(FabricClient fc)
        {
            //var ad = CreateAppDescription();
            //ad.MaximumNodes = 3;
            //await fc.ApplicationManager.CreateApplicationAsync(ad);

            ApplicationUpdateDescription adUpdate = new ApplicationUpdateDescription(new Uri(Settings.Instance.appName));
            Console.WriteLine("Please provide maximum number of nodes you want your application to run on.");
            int maxNodes = Convert.ToInt32(Console.ReadLine());
            adUpdate.MaximumNodes = maxNodes;
            await fc.ApplicationManager.UpdateApplicationAsync(adUpdate);
        }

        /// <summary>
        /// Total Application Capacity – This setting represents the total capacity of the application for a particular metric. The Cluster Resource Manager disallows the creation of any new services within this application instance that would cause total load to exceed this value. For example, let's say the application instance had a capacity of 10 and already had load of five. The creation of a service with a total default load of 10 would be disallowed.
        /// Maximum Node Capacity – This setting specifies the maximum total load for the application on a single node.If load goes over this capacity, the Cluster Resource Manager moves replicas to other nodes so that the load decreases.
        /// </summary>
        /// <param name="fc"></param>
        static async void SetAppCapacity(FabricClient fc)
        {
            ApplicationUpdateDescription adUpdate = new ApplicationUpdateDescription(new Uri(Settings.Instance.appName));

            var appMetric = new ApplicationMetricDescription();

            Console.WriteLine("Set metric name:");
            string metricName = Console.ReadLine();
            appMetric.Name = metricName;

            Console.WriteLine("Please provide total application capacity for the metric");
            int appCapacity = Convert.ToInt32(Console.ReadLine());
            appMetric.TotalApplicationCapacity = appCapacity;

            Console.WriteLine("Please provide application capacity for the metric per node");
            int nodeAppCapacity = Convert.ToInt32(Console.ReadLine());
            adUpdate.Metrics.Add(appMetric);

            await fc.ApplicationManager.UpdateApplicationAsync(adUpdate);
        }

        /// <summary>
        /// MinimumNodes - Defines the minimum number of nodes that the application instance should run on.
        ///NodeReservationCapacity - This setting is per metric for the application.The value is the amount of that metric reserved for the application on any node where that the services in that application run.
        /// </summary>
        /// <param name="fc"></param>
        static async void ReserveCapacity(FabricClient fc)
        {

            ApplicationUpdateDescription adUpdate = new ApplicationUpdateDescription(new Uri(Settings.Instance.appName));

            Console.WriteLine("Minimum number of nodes application should run on:");
            int minNodes = Convert.ToInt32(Console.ReadLine());
            adUpdate.MinimumNodes = minNodes;

            var appMetric = new ApplicationMetricDescription();

            Console.WriteLine("Set metric name:");
            string metricName = Console.ReadLine();
            appMetric.Name = metricName;

            Console.WriteLine("Metric reservation for app per node:");
            int metricReservation = Convert.ToInt32(Console.ReadLine());
            appMetric.NodeReservationCapacity = metricReservation;

            adUpdate.Metrics.Add(appMetric);

            await fc.ApplicationManager.UpdateApplicationAsync(adUpdate);
        }

        static async void ChangePlacementConstraints(FabricClient fc)
        {
            StatelessServiceUpdateDescription updateDescription = new StatelessServiceUpdateDescription();

            Console.WriteLine("Provide service name");
            string serviceName = Console.ReadLine();

            //"NodeType == n2"
            Console.WriteLine("Set placement constraint");
            string placementConstraint = Console.ReadLine();
            updateDescription.PlacementConstraints = placementConstraint ;

            var serviceNameUriBuilder2 = new ServiceUriBuilder(Settings.Instance.appName, serviceName);
            await fc.ServiceManager.UpdateServiceAsync(serviceNameUriBuilder2.Build(), updateDescription);
        }

        static async void GetAppLoadInfo(FabricClient fc)
        {
            var v = await fc.QueryManager.GetApplicationLoadInformationAsync(Settings.Instance.appName);
            var metrics = v.ApplicationLoadMetricInformation;
            foreach (ApplicationLoadMetricInformation metric in metrics)
            {
                Console.WriteLine(metric.ApplicationCapacity);  //total capacity for this metric in this application instance
                Console.WriteLine(metric.ReservationCapacity);  //reserved capacity for this metric in this application instance
                Console.WriteLine(metric.ApplicationLoad);  //current load for this metric in this application instance
            }
        }


        static FabricClient ConnectToSF()
        {
            var xc = GetCredentials(Settings.Instance.clientCertThumb, Settings.Instance.serverCertThumb, Settings.Instance.CommonName);
            var fc = new FabricClient(xc, Settings.Instance.connection);

            try
            {
                var ret = fc.ClusterManager.GetClusterManifestAsync().Result;
                Console.WriteLine(ret.ToString());
            }
            catch (Exception e)
            {
                Console.WriteLine("Connect failed: {0}", e.Message);
                return null;
            }
            return fc;
        }

        static X509Credentials GetCredentials(string clientCertThumb, string serverCertThumb, string commonName)
        {
            X509Credentials xc = new X509Credentials();
            xc.StoreLocation = StoreLocation.CurrentUser;
            xc.StoreName = "My";
            xc.FindType = X509FindType.FindByThumbprint;
            xc.FindValue = clientCertThumb;
            xc.RemoteCommonNames.Add(commonName);
            xc.RemoteCertThumbprints.Add(serverCertThumb);
            xc.ProtectionLevel = ProtectionLevel.EncryptAndSign;
            return xc;
        }
    }
}
