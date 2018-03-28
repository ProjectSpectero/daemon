namespace Spectero.daemon.Jobs
{
    public interface IJob
    {
        string GetSchedule();
        void Perform();
        bool IsEnabled();
    }
}