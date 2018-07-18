using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Spectero.daemon.Libraries.Extensions;

namespace Spectero.daemon.CLI.Libraries.I18N
{
    public class I18NHandler
    {
        private IDictionary<Locale, IDictionary<string, string>> linguisticsMultiplexer;

        public I18NHandler()
        {
            linguisticsMultiplexer = new Dictionary<Locale, IDictionary<string, string>>();
            
            var localePath = Path.Combine(Assembly.GetExecutingAssembly().GetDirectoryPath(), "Resource", "Locale");
            var files = Directory.GetFiles(localePath);

            foreach (var fileName in files)
            {
                var parts = fileName.Split('.');
                var validLocale = Enum.TryParse(parts[1], out Locale actualLocale);
                
                // Valid filenames are like EN.lang
                if (parts.Length != 2 && validLocale && parts[1] != "lang")
                    continue;
                
                var localeDict = new Dictionary<string, string>();
                linguisticsMultiplexer.Add(actualLocale, localeDict);

                var lines = File.ReadAllLines(fileName);
                foreach (var line in lines)
                {
                    var langParts = line.Split("=");
                    
                    if (langParts.Length <= 1)
                        continue;

                    var key = langParts[0];
                    var value = string.Join("", langParts.Skip(1));
                    
                    localeDict.Add(key, value);
                }
            }

        }

        private IDictionary<string, string> resolveDictionary(Locale locale = Locale.EN)
        {
            return linguisticsMultiplexer[locale];
        }

        public string get(string key, Locale locale = Locale.EN)
        {
            return resolveDictionary(locale).GetValueOrDefault(key, key);
        }
    }
}