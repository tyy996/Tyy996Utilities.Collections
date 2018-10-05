using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Tyy996Utilities.Collections
{
    public partial class TransparentDictionary<TKey, TValue>
    {
        [Serializable]
        public class OpaqueDictionary : IDictionary<TKey, TValue>
        {
            internal Dictionary<TKey, LayerBucket> buckets;

            public ICollection<TKey> Keys { get { return buckets.Keys; } }
            public ICollection<TValue> Values { get { return (ICollection<TValue>)buckets.Values.Cast<TValue>(); } }
            public int Count { get { return buckets.Count; } }
            public bool IsReadOnly { get { return false; } }

            public event CollectionChanged<TKey> OnAdd;
            public event CollectionChanged<TKey> OnRemove;
            public event CollectionChanged<TKey> OnChange;

            public OpaqueDictionary()
            {
                buckets = new Dictionary<TKey, LayerBucket>();
            }

            public TValue this[TKey key]
            {
                get
                {
                    return GetValue(key);
                }

                set
                {
                    if (!buckets.ContainsKey(key))
                        Add(key, value);
                    else
                        buckets[key].BaseValue = value;
                }
            }

            #region Add, Remove, and Clear
            public void Add(KeyValuePair<TKey, TValue> item)
            {
                Add(item.Key, item.Value);
            }

            public void Add(TKey key, TValue value)
            {
                buckets.Add(key, new LayerBucket(value));
                fireOnAdd(key);
            }

            public bool Remove(KeyValuePair<TKey, TValue> item)
            {
                return Remove(item.Key);
            }

            public virtual bool Remove(TKey key)
            {
                if (buckets.Remove(key))
                {
                    fireOnRemove(key);
                    return true;
                }

                return false;
            }

            public void Clear()
            {
                buckets.Clear();
            }
            #endregion

            #region Get and Contains
            public TValue GetValue(TKey key)
            {
                TValue value;
                TryGetValue(key, out value);

                return value;
            }

            public bool TryGetValue(TKey key, out TValue baseValue)
            {
                LayerBucket bucket;
                if (buckets.TryGetValue(key, out bucket))
                {
                    baseValue = bucket.BaseValue;
                    return true;
                }

                baseValue = default(TValue);
                return false;
            }

            public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
            {
                return new Enumerator(this);
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return new Enumerator(this);
            }

            public bool Contains(KeyValuePair<TKey, TValue> item)
            {
                return buckets.ContainsKey(item.Key);
            }

            public virtual bool ContainsKey(TKey key)
            {
                return buckets.ContainsKey(key);
            }
            #endregion

            #region Copy
            public void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex)
            {
                if (array == null)
                    throw new ArgumentNullException("Array was null.");

                if (arrayIndex < 0)
                    throw new ArgumentOutOfRangeException("arrayIndex must be greater than zero.");
                else if (arrayIndex > array.Length)
                    throw new ArgumentOutOfRangeException("ArrayIndex must be less than array's length.");
                else if (Count < array.Length - arrayIndex)
                    throw new ArgumentOutOfRangeException("Array, with given ArrayIndex, is not large enough to copy dictionary too.");

                foreach (var pair in this)
                    array[arrayIndex++] = pair;
            }
            #endregion

            //public void SetValue(TKey key, TValue value)
            //{

            //}

            #region Fire Events
            private void fireOnAdd(TKey key)
            {

            }

            private void fireOnRemove(TKey key)
            {
                if (OnRemove != null)
                    OnRemove.Invoke(key);
            }

            private void fireOnChange(TKey key)
            {

            }
            #endregion

            internal LayerBucket LayerAdd(TKey key, TValue opaqueValue)
            {
                var bucket = new LayerBucket(opaqueValue);
                buckets.Add(key, bucket);

                return bucket;
            }

            internal void ClearLayer(Guid layerID)
            {
                //queue remove instead of looping
            }

            #region Convert
            public ICollection<KeyValuePair<TKey, TValue>> ToList()
            {
                KeyValuePair<TKey, TValue>[] pairs = new KeyValuePair<TKey, TValue>[Count];

                CopyTo(pairs, 0);
                return pairs;
            }
            //public List<KeyValuePair<TKey, TValue>> ConvertToLists(TransparentDictionary<TKey, TValue>[] dictionaries)
            //{
            //    List<KeyValuePair<TKey, TValue>> lists = new List<KeyValuePair<TKey, TValue>>(dictionaries.Length + 1);

            //    for (var index = 0; index < buckets.Count + 1; index++)
            //    {

            //    }
            //}
            #endregion

            #region Enumerator
            public struct Enumerator : IEnumerator<KeyValuePair<TKey, TValue>>, System.Collections.IEnumerator
            {
                private Dictionary<TKey, LayerBucket>.Enumerator enumerator;

                internal Enumerator(OpaqueDictionary opaque)
                {
                    enumerator = opaque.buckets.GetEnumerator();
                }

                public KeyValuePair<TKey, TValue> Current
                {
                    get
                    {
                        var current = enumerator.Current;
                        return new KeyValuePair<TKey, TValue>(current.Key, current.Value.BaseValue);
                    }
                }

                object System.Collections.IEnumerator.Current
                {
                    get
                    {
                        return enumerator.Current;
                    }
                }

                public bool MoveNext()
                {
                    return enumerator.MoveNext();
                }

                void System.Collections.IEnumerator.Reset()
                {
                    ((IEnumerator)enumerator).Reset();
                }

                public void Dispose()
                {
                    enumerator.Dispose();
                }
            }
            #endregion
        }
    }
}
