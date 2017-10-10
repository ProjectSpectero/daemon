namespace Spectero.daemon.Libraries.Core.Statistics
{
    public interface IStatistician
    {
        bool Update<T> (double bytes, DataFlowDirections direction) where T : new();
    }
}