using System.Collections.Generic;

namespace Tyy996Utilities.Collections
{
    public static class DictionaryExtensions
    {
        public static bool Remove<TKey, TValue>(this Dictionary<TKey, TValue> dict, TKey key, out TValue value)
        {
            if (dict.TryGetValue(key, out value))
                return dict.Remove(key);

            value = default(TValue);
            return false;
        }
    }
}
