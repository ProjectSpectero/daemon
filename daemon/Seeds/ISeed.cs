namespace Spectero.daemon.Seeds
{
    public interface ISeed
    {
        void Up();
        void Down();
        string GetVersion();
    }
}