# Service Fabric Management App

In this repo you can find Service Fabric management .NET Core console applications which can be used to apply Application groups Settings and placement constraints for micro services running on top of SF. This repo contains sample Service Fabric app as well. It consists of two simple stateless services, where one acts as a server and second acts as a client and calls server service thru SF Service Remoting. In this SF app you can find implemented resource governance and simple functionality, thru which you can observe resource governance behavior (whether it comes to memory or cpu resource governance)

## Service Fabric Management APP

When running console app, it always ask you for settings. These settings are serialized and stored in settings file and can be changed every time when starting application. These settings are:

- SF Cluster endpoint
- SF Cluster common name
- Client cert thumbprint
- Server cert thumbprint
- SF Application name
- SF Application app type name
- SF app version

Management app requires SF application type to be already registered in SF Cluster. It allows **following scenarios**: 

- Creation of SF application

- Creation of Stateless service with metric (during creation apps prompts for service name and metric name and default load)

- Creation of Statefull service with metric (during creation apps prompts for service name and metric name and default load)

- Limit number of nodes for app

- Set node and application metric capacity

- Set minimum number of nodes and reserve capacity for app on nodes

- Set placement constraints for service

- Show app load info

  ​

Management app is implemented to connect to your secured cluster and as stated prompts for used cert thumbprint. In this blog post you can find how to secure cluster using self signed certificate https://blog.maximerouiller.com/post/creating-a-secure-azure-service-fabric-cluster-creating-the-self-signed-certificates/

Bellows snippet shows implementation of authentication against secured SF cluster:

```c#
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
```


## Service Fabric Sample application

This application was created with aim to demonstrated service remoting communication and mainly **resource governance**. Resource governance is set for StatelessServer Service thru application manifest within ServiceManifestImport element in following way:



```xml
<ServiceManifestImport>
  <ServiceManifestRef ServiceManifestName="StatelessServerPkg" ServiceManifestVersion="1.0.14" />
  <ConfigOverrides />
  <Policies>
    <!-- Resource Governance -->
    <ServicePackageResourceGovernancePolicy CpuCores="0.1" />
    <ResourceGovernancePolicy CodePackageRef="Code" MemoryInMB="100" />
  </Policies>
</ServiceManifestImport> 
```


Memory consumption/leak is simulated within RunAsync method:

```c#
protected override async Task RunAsync(CancellationToken cancellationToken)
{
  long memoryEaten = 0;

  try
  {
    while (true)
    {
      //Simulate memory consumption
      Marshal.AllocHGlobal(10_000_000);
      memoryEaten += 10_000_000;
      ServiceEventSource.Current.ServiceMessage(this.Context, "I have eaten {0} MBytes Working-{1}", memoryEaten / 1000000, _myName);

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
```

You can validate that resource governance is working when you deploy the app on local cluster and open task manager where you can notice that service will not exceed cpu percentage usage determined by resource governance. When it comes to memory governance, there will be throw OutOfMemoryException during service execution once service exceeds memory limit.
