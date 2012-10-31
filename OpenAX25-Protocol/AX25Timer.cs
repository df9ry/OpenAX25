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
        private long duration;
        private long elapsed;
        private long remainingLastStopped;
        private bool running;

        internal AX25Timer(DataLinkStateMachine dlsm, TimerCallback callback)
        {
            t = new Timer(callback, dlsm, Timeout.Infinite, Timeout.Infinite);
            start = DateTime.Now;
            duration = 0;
            elapsed = 0;
            remainingLastStopped = 0;
            running = false;
        }

        internal void Start(long _duration)
        {
            duration = _duration;
            t.Change(duration, Timeout.Infinite);
            start = DateTime.Now;
            running = true;
        }

        internal void Stop()
        {
            t.Change(Timeout.Infinite, Timeout.Infinite);
            elapsed = (DateTime.Now - start).Ticks;
            remainingLastStopped = duration - elapsed;
            running = false;
        }

        internal long Elapsed
        {
            get
            {
                if (running)
                    return (DateTime.Now - start).Ticks;
                else
                    return elapsed;
            }
        }

        internal long Remaining
        {
            get
            {
                if (running)
                    return duration - (DateTime.Now - start).Ticks;
                else
                    return duration - elapsed;
            }
        }

        internal long RemainingLastStopped
        {
            get
            {
                return remainingLastStopped;
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
