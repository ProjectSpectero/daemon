namespace Spectero.daemon.Seeds
{
    public abstract class BaseSeed : ISeed
    {
        public abstract void Up();
        public abstract void Down();
        public abstract string GetVersion();
    }
}