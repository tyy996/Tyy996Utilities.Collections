using System;
using System.Collections;
using System.Collections.Generic;

namespace Tyy996Utilities.Collections
{
    /// <summary>
    /// A list that will store up to its capacity, but once capacity is reached
    /// adding more values will remove the oldest.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    [Serializable]
    public class LeakingList<T> : ICollection<T>, IEnumerable<T>
    {
        private LinkedList<T> links;

        public int Count { get { return links.Count; } }
        public int Capacity { get; private set; }
        public bool IsReadOnly { get { return false; } }

        public T Previous { get { return links.First.Value; } }
        public LinkedListNode<T> PreviousNode { get { return links.Last; } }

        public LeakingList(int capacity)
        {
            Capacity = capacity;
            links = new LinkedList<T>();
        }

        public T this[int index]
        {
            get
            {
                if (index >= Count)
                    throw new IndexOutOfRangeException();

                int count = 0;
                foreach (var link in links)
                {
                    if (count == index)
                        return link;

                    count++;
                }

                throw new IndexOutOfRangeException();
            }
        }

        public void Add(T item)
        {
            links.AddFirst(item);

            if (links.Count > Capacity)
                links.RemoveLast();
        }

        public void Clear()
        {
            links.Clear();
        }

        public bool Contains(T item)
        {
            return links.Contains(item);
        }

        public bool Remove(T item)
        {
            return links.Remove(item);
        }

        public void RemoveFirst()
        {
            links.RemoveFirst();
        }

        public void RemoveLast()
        {
            links.RemoveLast();
        }

        public void CopyTo(T[] array, int arrayIndex)
        {
            links.CopyTo(array, arrayIndex);
        }

        public IEnumerator<T> GetEnumerator()
        {
            return links.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return links.GetEnumerator();
        }
    }
}
