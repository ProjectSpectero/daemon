using System.Net;
using System.Threading.Tasks;

namespace Spectero.daemon.Libraries.Core.OutgoingIPResolver
{
    // ReSharper disable once InconsistentNaming
    public interface IOutgoingIPResolver
    {
        Task<IPAddress> Resolve();
        Task<IPAddress> Translate(IPAddress address);
        Task<IPAddress> Translate(string stringAddress);
    }
}