﻿using Streamiz.Kafka.Net.SerDes;
using Streamiz.Kafka.Net.State.InMemory;
using Streamiz.Kafka.Net.State.Internal.Builder;
using Streamiz.Kafka.Net.State.RocksDb;
using Streamiz.Kafka.Net.State.Supplier;
using System;

namespace Streamiz.Kafka.Net.State
{
    /// <summary>
    /// Helper class for creating state store.
    /// </summary>
    public static class Stores
    {
        /// <summary>
        /// Create the default key/value store. The default state store is an <see cref="Streamiz.Kafka.Net.State.InMemory.InMemoryKeyValueStore"/>.
        /// </summary>
        /// <param name="name">state store name</param>
        /// <returns><see cref="InMemoryKeyValueBytesStoreSupplier"/> supplier</returns>
        public static IKeyValueBytesStoreSupplier DefaultKeyValueStore(string name)
            => InMemoryKeyValueStore(name);

        /// <summary>
        /// Create a persistent key/value store.
        /// </summary>
        /// <param name="name">state store name</param>
        /// <returns><see cref="RocksDbKeyValueBytesStoreSupplier"/> supplier</returns>
        public static IKeyValueBytesStoreSupplier PersistentKeyValueStore(string name)
            => new RocksDbKeyValueBytesStoreSupplier(name);

        /// <summary>
        /// Create a inmemory key/value store.
        /// </summary>
        /// <param name="name">state store name</param>
        /// <returns><see cref="InMemoryKeyValueBytesStoreSupplier"/> supplier</returns>
        public static IKeyValueBytesStoreSupplier InMemoryKeyValueStore(string name)
            => new InMemoryKeyValueBytesStoreSupplier(name);

        /// <summary>
        /// Create the default window store. The default state store is an <see cref="Streamiz.Kafka.Net.State.InMemory.InMemoryWindowStore"/>. 
        /// </summary>
        /// <param name="name">state store name</param>
        /// <param name="retention">retention duration</param>
        /// <param name="windowSize">window size</param>
        /// <param name="segmentInterval">segment interval</param>
        /// <returns><see cref="InMemoryWindowStoreSupplier"/> supplier</returns>
        public static IWindowBytesStoreSupplier DefaultWindowStore(string name, TimeSpan retention, TimeSpan windowSize, long segmentInterval = 3600000)
            => InMemoryWindowStore(name, retention, windowSize);

        /// <summary>
        /// Create a persistent window store. 
        /// </summary>
        /// <param name="name">state store name</param>
        /// <param name="retention">retention duration</param>
        /// <param name="windowSize">window size</param>
        /// <param name="segmentInterval">segment interval (default: 3600000)</param>
        /// <returns><see cref="RocksDbWindowBytesStoreSupplier"/> supplier</returns>
        public static IWindowBytesStoreSupplier PersistentWindowStore(string name, TimeSpan retention, TimeSpan windowSize, long segmentInterval = 3600000)
            => new RocksDbWindowBytesStoreSupplier(name, retention, segmentInterval, (long)windowSize.TotalMilliseconds);

        /// <summary>
        /// Create the inmemory window store. 
        /// </summary>
        /// <param name="name">state store name</param>
        /// <param name="retention">retention duration</param>
        /// <param name="windowSize">window size</param>
        /// <returns><see cref="InMemoryWindowStoreSupplier"/> supplier</returns>
        public static IWindowBytesStoreSupplier InMemoryWindowStore(string name, TimeSpan retention, TimeSpan windowSize)
            => new InMemoryWindowStoreSupplier(name, retention, (long)windowSize.TotalMilliseconds);

        internal static StoreBuilder<IWindowStore<K, V>> WindowStoreBuilder<K, V>(IWindowBytesStoreSupplier supplier, ISerDes<K> keySerdes, ISerDes<V> valueSerdes)
            => new WindowStoreBuilder<K, V>(supplier, keySerdes, valueSerdes);

        internal static StoreBuilder<ITimestampedKeyValueStore<K, V>> TimestampedKeyValueStoreBuilder<K, V>(IKeyValueBytesStoreSupplier supplier, ISerDes<K> keySerde, ISerDes<V> valueSerde)
            => new TimestampedKeyValueStoreBuilder<K, V>(supplier, keySerde, valueSerde);

        internal static StoreBuilder<ITimestampedWindowStore<K, V>> TimestampedWindowStoreBuilder<K, V>(IWindowBytesStoreSupplier supplier, ISerDes<K> keySerde, ISerDes<V> valueSerde)
            => new TimestampedWindowStoreBuilder<K, V>(supplier, keySerde, valueSerde);
    }
}