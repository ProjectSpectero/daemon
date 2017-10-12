using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace Spectero.daemon.Libraries.Core.Constants
{
    public static class Defaults
    {
        public static List<Tuple<string, int>> HTTP
        {
            get
            {
                List<Tuple<string, int>> ret = new List<Tuple<string, int>>();
                ret.Add(Tuple.Create(IPAddress.Any.ToString(), 8800));
                return ret;
            }
        }
    }
}
