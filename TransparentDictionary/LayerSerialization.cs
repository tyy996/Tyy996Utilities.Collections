using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Tyy996Utilities.Collections
{
    public partial class TransparentDictionary<TKey, TValue> : IDictionary<TKey, TValue>, ISerializable
    {
        private const string ID_NAME = "ID";
        private const string SUM_NAME = "SUM";
        private const string OPAQUE_NAME = "OPAQUE";

        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            info.AddValue(ID_NAME, layerID);
            if (serializeOpaque)
            {
                info.AddValue(SUM_NAME, sumLayer); //bug?
                info.AddValue(OPAQUE_NAME, opaqueLayer);
            }
        }
    }
}
