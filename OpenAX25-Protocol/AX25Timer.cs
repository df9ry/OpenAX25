using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace OpenAX25_Protocol
{
    internal class AX25Timer
    {
        private readonly Timer t;
        private DateTime start;
        private TimeSpan elapsed;
        private bool running;

        internal AX25Timer(DataLinkStateMachine dlsm, TimerCallback callback)
        {
            t = new Timer(callback, dlsm, Timeout.Infinite, Timeout.Infinite);
            running = false;
        }

        internal void Start(long duration)
        {
            t.Change(duration, Timeout.Infinite);
            start = DateTime.Now;
            running = true;
        }

        internal void Stop()
        {
            t.Change(Timeout.Infinite, Timeout.Infinite);
            elapsed = DateTime.Now - start;
            running = false;
        }

        internal long Elapsed
        {
            get
            {
                if (running)
                    return (DateTime.Now - start).Ticks;
                else
                    return elapsed.Ticks;
            }
        }

        internal bool Running
        {
            get
            {
                return running;
            }
        }

    }
}
