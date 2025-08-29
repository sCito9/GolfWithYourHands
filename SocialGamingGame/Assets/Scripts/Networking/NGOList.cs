using System;
using System.Collections.Generic;
using Unity.Netcode;

namespace Networking
{
    [Serializable]
    public class NGOList<T> : INetworkSerializable where T : INetworkSerializable, new()
    {
        public List<T> list = new();

        public NGOList() { }

        public NGOList(List<T> list)
        {
            this.list = list;
        }

        public void NetworkSerialize<TBuffer>(BufferSerializer<TBuffer> serializer) where TBuffer : IReaderWriter
        {
            int count = list?.Count ?? 0;
            serializer.SerializeValue(ref count);

            if (serializer.IsReader)
            {
                list = new List<T>(count);
                for (int i = 0; i < count; i++)
                {
                    T item = new T();
                    item.NetworkSerialize(serializer);
                    list.Add(item);
                }
            }
            else
            {
                for (int i = 0; i < count; i++)
                {
                    list[i].NetworkSerialize(serializer);
                }
            }
        }
    }
}
