using System;
using System.Collections.Generic;

namespace Tyy996Utilities.Collections
{
    public partial class TransparentDictionary<TKey, TValue>
    {
        [Serializable]
        protected internal class LayerBucket : Dictionary<Guid, TValue>
        {
            private TValue baseValue;

            public TValue BaseValue { get { return baseValue; } set { baseValue = value; } }

            public LayerBucket() { }

            public LayerBucket(TValue value)
            {
                baseValue = value;
            }

            public static implicit operator TValue(LayerBucket value)
            {
                return value.BaseValue;
            }
        }
    }
}
