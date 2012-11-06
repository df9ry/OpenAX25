using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenAX25Contracts;

namespace OpenAX25_Protocol
{
    internal struct LocalEndpoint
    {
        internal LocalEndpoint(L2Callsign callsign, string key, IL3Channel channel)
        {
            id = Guid.NewGuid();
            cs = callsign;
            ky = key;
            ch = channel;
        }

        internal readonly Guid id;
        internal readonly L2Callsign cs;
        internal readonly string ky;
        internal readonly IL3Channel ch;
    }
}
