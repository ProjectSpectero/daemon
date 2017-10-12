using System.Threading.Tasks;

namespace Spectero.daemon.Libraries.Core.Statistics
{
    public interface IStatistician
    {
        Task<bool> Update<T> (long bytes, DataFlowDirections direction) where T : new();
    }
}