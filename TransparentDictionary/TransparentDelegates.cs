using System.Collections.Generic;

namespace Tyy996Utilities.Collections
{
    public partial class TransparentDictionary<TKey, TValue> : IDictionary<TKey, TValue>
    {
        public delegate TValue SumLayer<TValue>(TValue opaque, TValue transparent);
    }
}
