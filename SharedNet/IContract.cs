using Microsoft.ServiceFabric.Services.Remoting;
using System.Threading.Tasks;

namespace Shared
{
    public interface IContract : IService
    {
        Task<int> WhoAreYou();
    }
}