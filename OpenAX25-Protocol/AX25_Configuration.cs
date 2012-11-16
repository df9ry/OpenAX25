using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenAX25Contracts;

namespace OpenAX25_Protocol
{
    internal struct AX25_Configuration
    {
        internal AX25_Configuration(string name)
        {
            this.name = name;
            this.Initial_SAT = 300;
            this.Initial_SRT = 3000;
            this.Initial_N1 = 255;
            this.Initial_N2 = 10;
            this.Initial_relaxed = false;
            this.Initial_version = AX25Version.V2_0;
        }

        internal string name;

        internal long Initial_SAT;
        internal long Initial_SRT;
        internal long Initial_N1;
        internal long Initial_N2;
        internal bool Initial_relaxed;
        internal AX25Version Initial_version;
    }
}
