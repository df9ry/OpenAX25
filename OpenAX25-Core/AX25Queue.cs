//
// AX25Queue.cs
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
    public class AX25Queue<T> : System.Collections.IEnumerable
    {
        /// <summary>
        /// List that backups the queue elements.
        /// </summary>
        protected LinkedList<T> m_list = new LinkedList<T>();

        /// <summary>
        /// Constructor.
        /// </summary>
        public AX25Queue()
        {
        }

        /// <summary>
        /// Enqueue an element.
        /// </summary>
        /// <param name="element">The element to enqueue.</param>
        public virtual void Enqueue(T element)
        {
            m_list.AddLast(element);
        }

        /// <summary>
        /// Dequeue element from the queue.
        /// </summary>
        /// <returns>Element.</returns>
        public virtual T Dequeue()
        {
            T result = m_list.First.Value;
            m_list.RemoveFirst();
            return result;
        }

        /// <summary>
        /// Put back an element to the top of
        /// the queue.
        /// </summary>
        /// <param name="element">Element to put back.</param>
        public virtual void PutBack(T element)
        {
            m_list.AddFirst(element);
        }

        /// <summary>
        /// Remove all elements of a list.
        /// </summary>
        public virtual void Clear()
        {
            m_list.Clear();
        }

        /// <summary>
        /// Returns a enumerator for this object.
        /// </summary>
        /// <returns>Enumerator</returns>
        public System.Collections.IEnumerator GetEnumerator()
        {
            return new Enumerator<T>(m_list.GetEnumerator());
        }

        /// <summary>
        /// Size of the queue.
        /// </summary>
        public int Count
        {
            get
            {
                return m_list.Count;
            }
        }

        /// <summary>
        /// Copy all elements of a queue to another one.
        /// </summary>
        /// <param name="source">Source queue.</param>
        /// <param name="target">Target queue.</param>
        public static void Copy(AX25Queue<T> source, AX25Queue<T> target)
        {
            target.m_list = new LinkedList<T>(source.m_list);
        }

        /// <summary>
        /// Move all elements of a queue to another one.
        /// </summary>
        /// <param name="source">Source queue.</param>
        /// <param name="target">Target queue.</param>
        public static void Move(AX25Queue<T> source, AX25Queue<T> target)
        {
            target.m_list = source.m_list;
            source.m_list = new LinkedList<T>();
        }

        private sealed class Enumerator<Q> : System.Collections.IEnumerator
        {
            private System.Collections.Generic.IEnumerator<Q> m_iter;

            internal Enumerator(System.Collections.Generic.IEnumerator<Q> iter)
            {
                m_iter = iter;
            }

            public object Current
            {
                get
                {
                    return m_iter.Current;
                }
            }

            public void Reset()
            {
                m_iter.Reset();
            }

            public bool MoveNext()
            {
                return m_iter.MoveNext();
            }
        }

    } // end class //

}
