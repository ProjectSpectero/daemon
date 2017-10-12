namespace Spectero.daemon.Migrations
{
    public interface IMigration
    {
        void Up();
        void Down();
    }
}