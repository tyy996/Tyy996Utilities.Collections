using System;
using System.Collections;
using System.Collections.Generic;

namespace Tyy996Utilities.Collections
{
    /// <summary>
    /// A dictionary that will automatically sum the values of this transparent layer value
    /// with the opaque value on get.
    /// </summary>
    /// <typeparam name="TKey"></typeparam>
    /// <typeparam name="TValue"></typeparam>
    [Serializable]
    public partial class TransparentDictionary<TKey, TValue> : IDictionary<TKey, TValue>
    {
        private OpaqueDictionary opaqueLayer;
        private bool serializeOpaque;

        protected Guid layerID { get; private set; }
        protected SumLayer<TValue> sumLayer { get; private set; }
        protected Dictionary<TKey, LayerBucket> buckets { get { return opaqueLayer.buckets; } }

        /// <summary>
        /// The root layer.
        /// </summary>
        public OpaqueDictionary OpaqueLayer { get { return opaqueLayer; } }

        public int Count { get { return buckets.Count; } }
        public ICollection<TKey> Keys { get { return buckets.Keys; } }
        public ICollection<TValue> Values { get { return (ICollection<TValue>)GetValueEnumerator(); } }
        public bool IsReadOnly { get { return false; } }

        public event CollectionChanged<TKey> OnChange;

        public TransparentDictionary(SumLayer<TValue> sumLayer)
        {
            this.sumLayer = sumLayer;
            opaqueLayer = new OpaqueDictionary();
            layerID = Guid.NewGuid();
            serializeOpaque = true;
        }

        public TransparentDictionary(ICollection<KeyValuePair<TKey, TValue>> opaque, ICollection<KeyValuePair<TKey, TValue>> collection, SumLayer<TValue> sumLayer) :
            this(sumLayer)
        {

        }

        /// <summary>
        /// Makes a new Transparent layer with the same opaque layer as
        /// value.
        /// </summary>
        /// <param name="transparent"></param>
        public TransparentDictionary(TransparentDictionary<TKey, TValue> transparent)
        {
            sumLayer = transparent.sumLayer;
            opaqueLayer = transparent.opaqueLayer;
            layerID = Guid.NewGuid();
            serializeOpaque = false;
        }

        /// <summary>
        /// Get the value; Value will be default if key is not found.
        /// 
        /// Set the value for this layer, will add value if Opaque contains key
        /// but layer doesn't contain value for key.
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public TValue this[TKey key]
        {
            get
            {
                return GetValue(key);
            }
            set
            {
                LayerBucket bucket;
                if (!buckets.TryGetValue(key, out bucket))
                    throw new KeyNotFoundException("Key not found in Opaque.");

                bucket[layerID] = value;
                fireOnChange(key);
            }
        }

        #region Methods
        #region Add, Remove, and Clear
        public void Add(KeyValuePair<TKey, TValue> pair)
        {
            Add(pair.Key, pair.Value);
        }

        /// <summary>
        /// Will add a default value to opaque layer and set this transparent
        /// layer to value. Add from Transparent should only be used if the Opaque value
        /// should be default or will be set later.
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        public void Add(TKey key, TValue value)
        {
            Add(key, value, default(TValue));
        }

        public void Add(TKey key, TValue value, TValue opaqueValue)
        {
            if (buckets.ContainsKey(key))
                throw new ArgumentException("An element with the same key already exists in the Opaque");

            var bucket = opaqueLayer.LayerAdd(key, opaqueValue);
            bucket.Add(layerID, value);
        }

        public bool Remove(KeyValuePair<TKey, TValue> pair)
        {
            return Remove(pair.Key);
        }

        /// <summary>
        /// Will only remove the value of THIS layer and will not remove
        /// keys and values of other layers.
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public bool Remove(TKey key)
        {
            LayerBucket bucket;
            if (buckets.TryGetValue(key, out bucket))
                return bucket.Remove(layerID);

            return false;
        }

        public void Clear()
        {
            OpaqueLayer.ClearLayer(layerID);
        }
        #endregion

        #region Get
        public TValue GetValue(TKey key)
        {
            TValue value;
            TryGetValue(key, out value);

            return value;
        }

        public TValue GetTransparentValue(TKey key)
        {
            TValue value;
            TryGetTransparentValue(key, out value);

            return value;
        }

        public virtual bool TryGetValue(TKey key, out TValue value)
        {
            LayerBucket bucket;
            if (buckets.TryGetValue(key, out bucket))
            {
                if (bucket.TryGetValue(layerID, out value))
                    value = sumLayer(bucket.BaseValue, value);
                else
                    value = bucket.BaseValue;

                return true;
            }

            value = default(TValue);
            return false;
        }

        public virtual bool TryGetTransparentValue(TKey key, out TValue value)
        {
            LayerBucket bucket;
            if (buckets.TryGetValue(key, out bucket) && bucket.TryGetValue(layerID, out value))
                return true;

            value = default(TValue);
            return false;
        }

        public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
        {
            return new Enumerator(this);
        }

        public IEnumerator<TValue> GetValueEnumerator()
        {
            return new ValueEnumerator(this);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return new Enumerator(this);
        }
        #endregion

        #region Contains/Has
        public bool Contains(KeyValuePair<TKey, TValue> pair)
        {
            return ContainsKey(pair.Key);
        }

        /// <summary>
        /// Checks if the Opaque contains the key.
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public bool ContainsKey(TKey key)
        {
            return buckets.ContainsKey(key);
        }

        /// <summary>
        /// Only checks if THIS layer contains has a value for a given
        /// key.
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public bool HasValue(TKey key)
        {
            LayerBucket bucket;
            if (buckets.TryGetValue(key, out bucket))
                return bucket.ContainsKey(layerID);

            return false;
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

        #region Convert
        public ICollection<KeyValuePair<TKey, TValue>> ToList()
        {
            KeyValuePair<TKey, TValue>[] pairs = new KeyValuePair<TKey, TValue>[Count];

            CopyTo(pairs, 0);
            return pairs;
        }
        #endregion

        #region Fire Events
        protected void fireOnChange(TKey key)
        {
            if (OnChange != null)
                OnChange.Invoke(key);
        }
        #endregion
        #endregion

        #region Enumerator
        public struct Enumerator : IEnumerator<KeyValuePair<TKey, TValue>>, IEnumerator
        {
            private TransparentDictionary<TKey, TValue> transparent;
            private IEnumerator<TKey> keys;
            //private Dictionary<TKey, LayerBucket>.KeyCollection.Enumerator keyEnumerator;

            internal Enumerator(TransparentDictionary<TKey, TValue> transparent)
            {
                this.transparent = transparent;
                keys = transparent.Keys.GetEnumerator();
                //keyEnumerator = transparent.Keys.GetEnumerator();
            }

            public KeyValuePair<TKey, TValue> Current
            {
                get
                {
                    var currentKey = keys.Current;
                    return new KeyValuePair<TKey, TValue>(currentKey, transparent.GetValue(currentKey));
                }
            }

            object IEnumerator.Current
            {
                get
                {
                    return Current;
                }
            }

            public bool MoveNext()
            {
                return keys.MoveNext();
            }

            public void Reset()
            {
                keys = transparent.Keys.GetEnumerator();
            }

            public void Dispose()
            {
                keys.Dispose();
                transparent = null;
            }
        }

        public struct ValueEnumerator : IEnumerator<TValue>, IEnumerator
        {
            private TransparentDictionary<TKey, TValue> transparent;
            private IEnumerator<TKey> keys;
            //private Dictionary<TKey, LayerBucket>.KeyCollection.Enumerator keyEnumerator;

            internal ValueEnumerator(TransparentDictionary<TKey, TValue> transparent)
            {
                this.transparent = transparent;
                keys = transparent.Keys.GetEnumerator();
                //keyEnumerator = transparent.Keys.GetEnumerator();
            }

            public TValue Current
            {
                get
                {
                    return transparent.GetValue(keys.Current);
                }
            }

            object IEnumerator.Current
            {
                get
                {
                    return Current;
                }
            }

            public bool MoveNext()
            {
                return keys.MoveNext();
            }

            public void Reset()
            {
                keys = transparent.Keys.GetEnumerator();
            }

            public void Dispose()
            {
                keys.Dispose();
                transparent = null;
            }
        }
        #endregion
    }
}
