//
// AX25SynchronizedQueue.cs
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
using System.Collections.Generic;
using System.Threading;

namespace OpenAX25Core
{
    /// <summary>
    /// A queue with additional functionality required by the AX.25
    /// implementations.
    /// </summary>
    /// <typeparam name="T">Type of queue elements</typeparam>
    public class AX25SynchronizedQueue<T> : AX25Queue<T>
    {
        private readonly Object m_sync = new Object();

        /// <summary>
        /// Constructor.
        /// </summary>
        public AX25SynchronizedQueue() :base()
        {
        }

        /// <summary>
        /// Enqueue an element.
        /// </summary>
        /// <param name="element">The element to enqueue.</param>
        public override void Enqueue(T element)
        {
            lock (m_sync)
            {
                m_list.AddLast(element);
                if (m_list.Count == 1)
                    Monitor.Pulse(m_sync);
            } // end lock //
        }

        /// <summary>
        /// Dequeue element from the queue. Will block until an
        /// element is available.
        /// </summary>
        /// <returns>Element.</returns>
        public override T Dequeue()
        {
            lock (m_sync)
            {
                while (true)
                {
                    if (m_list.Count > 0)
                    {
                        T result = m_list.First.Value;
                        m_list.RemoveFirst();
                        return result;
                    }
                    Monitor.Wait(m_sync);
                } // end while //
            } // end lock //
        }

        /// <summary>
        /// Put back an element to the top of
        /// the queue.
        /// </summary>
        /// <param name="element">Element to put back.</param>
        public override void PutBack(T element)
        {
            lock (m_sync)
            {
                m_list.AddFirst(element);
                if (m_list.Count == 1)
                    Monitor.Pulse(m_sync);
            } // end lock //
        }

        /// <summary>
        /// Remove all elements of a list.
        /// </summary>
        public override void Clear()
        {
            lock (m_sync)
            {
                m_list.Clear();
            }
        }

        /// <summary>
        /// Copy all elements of a queue to another one.
        /// </summary>
        /// <param name="source">Source queue.</param>
        /// <param name="target">Target queue.</param>
        public static void Copy(AX25SynchronizedQueue<T> source, AX25SynchronizedQueue<T> target)
        {
            lock (source.m_sync)
            {
                lock (target.m_sync)
                {
                    target.m_list = new LinkedList<T>(source.m_list);
                }
            }
        }

        /// <summary>
        /// Move all elements of a queue to another one.
        /// </summary>
        /// <param name="source">Source queue.</param>
        /// <param name="target">Target queue.</param>
        public static void Move(AX25SynchronizedQueue<T> source, AX25SynchronizedQueue<T> target)
        {
            lock (source.m_sync)
            {
                lock (target.m_sync)
                {
                    target.m_list = source.m_list;
                    source.m_list = new LinkedList<T>();
                }
            }

        }

    } // end class //

}
