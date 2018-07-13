using System.Collections.Generic;

namespace Spectero.daemon.CLI.Libraries.I18N
{
    public class I18NHandler
    {
        private IDictionary<Locale, IDictionary<string, string>> linguisticsMultiplexer;

        public I18NHandler()
        {
            linguisticsMultiplexer = new Dictionary<Locale, IDictionary<string, string>>();
        }
        
        private IDictionary<string, string> resolveDictionary()
        {
            return null;
        }
        
        public string get(string key, Locale locale = Locale.EN)
        {
            return null;
        }
    }
}