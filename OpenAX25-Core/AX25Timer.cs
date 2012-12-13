//
// AX25Timer.cs
// 
//  Author:
//       Tania Knoebl (DF9RY) DF9RY@DARC.de
//  
//  Copyright © 2012 Tania Knoebl (DF9RY)
// 
//  This program is free software: you can redistribute it and/or modify
//  it under the terms of the GNU Lesser General Public License as published by
//  the Free Software Foundation, either version 3 of the License, or
//  (at your option) any later version.
// 
//  This program is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//  GNU Lesser General Public License for more details.
// 
//  You should have received a copy of the GNU Lesser General Public License
//  along with this program.  If not, see <http://www.gnu.org/licenses/>
//

using System;
using System.Threading;

namespace OpenAX25Contracts
{

    /// <summary>
    /// A timer with special features needed for propper AX.25 protocol
    /// implementation.
    /// </summary>
    public sealed class AX25Timer
    {
        private readonly Timer m_timer;
        private DateTime m_start;
        private long m_duration;
        private long m_elapsed;
        private long m_remainingLastStopped;
        private bool m_running;
        private long m_sequenceNumber = -1;
        private bool m_suspended;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="userData">User data object to return with every
        /// callback.</param>
        /// <param name="callback">Callback routine.</param>
        public AX25Timer(object userData, TimerCallback callback)
        {
            m_timer = new Timer(callback, userData, Timeout.Infinite, Timeout.Infinite);
            m_start = DateTime.UtcNow;
            m_duration = 0;
            m_elapsed = 0;
            m_remainingLastStopped = 0;
            m_running = false;
            m_suspended = false;
        }

        /// <summary>
        /// Start the timer.
        /// </summary>
        /// <param name="duration">How long [ms]?</param>
        /// <returns>Sequence number. This number is changed whenever the timer
        /// is started or stopped. This can be used to filter out stale timer
        /// callbacks.</returns>
        public long Start(long duration)
        {
            lock (this)
            {
                m_duration = duration;
                m_start = DateTime.UtcNow;
                if (!m_suspended)
                    m_timer.Change(duration, Timeout.Infinite);
                m_elapsed = 0;
                m_running = true;
                unchecked
                {
                    m_sequenceNumber += 1;
                }
                if (m_sequenceNumber == -1)
                    m_sequenceNumber = 0;
                return m_sequenceNumber;
            }
        }

        /// <summary>
        /// Stop the timer.
        /// </summary>
        /// <returns>Sequence number. This number is changed whenever the timer
        /// is started or stopped. This can be used to filter out stale timer
        /// callbacks.</returns>
        public long Stop()
        {
            lock(this)
            {
                if (m_running)
                {
                    m_timer.Change(Timeout.Infinite, Timeout.Infinite);
                    if (!m_suspended)
                        m_elapsed += (DateTime.UtcNow - m_start).Ticks;
                    m_remainingLastStopped = m_duration - m_elapsed;
                    m_running = false;
                }
                unchecked
                {
                    m_sequenceNumber += 1;
                }
                if (m_sequenceNumber == -1)
                    m_sequenceNumber = 0;
                return -1;
            }
        }

        /// <summary>
        /// How many ticks are elapsed since the timer were
        /// started.
        /// </summary>
        public long Elapsed
        {
            get
            {
                lock (this)
                {
                    if (m_running && (!m_suspended))
                        return m_elapsed + (DateTime.UtcNow - m_start).Ticks;
                    else
                        return m_elapsed;
                }
            }
        }

        /// <summary>
        /// How many ticks remains until the timer fires.
        /// </summary>
        public long Remaining
        {
            get
            {
                lock (this)
                {
                    if (m_running && (!m_suspended))
                        return m_duration - m_elapsed - (DateTime.UtcNow - m_start).Ticks;
                    else
                        return m_duration - m_elapsed;
                }
            }
        }

        /// <summary>
        /// How many ticks were left when the timer was stopped the
        /// last time?
        /// </summary>
        public long RemainingLastStopped
        {
            get
            {
                return m_remainingLastStopped;
            }
        }

        /// <summary>
        /// Is the timer running?
        /// </summary>
        public bool Running
        {
            get
            {
                return m_running;
            }
        }

        /// <summary>
        /// Get the current sequence number.
        /// </summary>
        public long SequenceNumber
        {
            get
            {
                return m_sequenceNumber;
            }
        }

        /// <summary>
        /// Suspend or unsuspend the timer.
        /// </summary>
        /// <param name="suspend">Suspend the timer?</param>
        public void Suspend(bool suspend)
        {
            lock (this)
            {
                if (suspend == m_suspended)
                    return;
                m_suspended = suspend;
                if (m_running)
                {
                    if (suspend)
                    {
                        m_timer.Change(Timeout.Infinite, Timeout.Infinite);
                        DateTime now = DateTime.UtcNow;
                        m_elapsed += (now - m_start).Ticks;
                        m_start = now;
                    }
                    else
                    {
                        m_timer.Change(m_duration - m_elapsed, Timeout.Infinite);
                        m_start = DateTime.UtcNow;
                    }
                }
            } // end lock //
        }

    }
}
