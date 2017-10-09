namespace Spectero.daemon.Libraries.Config
{
    public class AppConfig
    {
        public string Key { get; set; }
        public string BlockedRedirectUri { get; set; }
        public string DatabaseFile { get; set; }
        public double AuthCacheMinutes { get; set; }
    }
}