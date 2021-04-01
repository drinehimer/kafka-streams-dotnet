﻿using Confluent.Kafka;
using Moq;
using NUnit.Framework;
using Streamiz.Kafka.Net.Crosscutting;
using Streamiz.Kafka.Net.Errors;
using Streamiz.Kafka.Net.Processors;
using Streamiz.Kafka.Net.Processors.Internal;
using Streamiz.Kafka.Net.SerDes;
using Streamiz.Kafka.Net.State.RocksDb;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Streamiz.Kafka.Net.Tests.Stores
{
    public class RocksDbKeyValueStoreTests
    {
        private StreamConfig config = null;
        private RocksDbKeyValueStore store = null;
        private ProcessorContext context = null;
        private TaskId id = null;
        private TopicPartition partition = null;
        private ProcessorStateManager stateManager = null;
        private Mock<AbstractTask> task = null;

        [SetUp]
        public void Begin()
        {
            Random rd = new Random();
            config = new StreamConfig();
            config.ApplicationId = $"unit-test-rocksdb-kv-{rd.Next(0, 1000)}";
            config.StateDir = ".";

            id = new TaskId { Id = 0, Partition = 0 };
            partition = new TopicPartition("source", 0);
            stateManager = new ProcessorStateManager(id, new List<TopicPartition> { partition });

            task = new Mock<AbstractTask>();
            task.Setup(k => k.Id).Returns(id);

            context = new ProcessorContext(task.Object, config, stateManager);

            store = new RocksDbKeyValueStore("test-store");
            store.Init(context, store);
        }

        [TearDown]
        public void End()
        {
            store.Flush();
            stateManager.Close();
            Directory.Delete(Path.Combine(config.StateDir, config.ApplicationId), true);
        }

        [Test]
        public void TestConfig()
        {
            Assert.AreEqual($"{Path.Combine(config.StateDir, config.ApplicationId, id.ToString())}", context.StateDir);
        }

        [Test]
        public void CreateRocksDbKeyValueStore()
        {
            Assert.IsTrue(store.Persistent);
            Assert.AreEqual("test-store", store.Name);
        }

        [Test]
        public void PutKeyNotExist()
        {
            var serdes = new StringSerDes();
            byte[] key = serdes.Serialize("key", new SerializationContext()), value = serdes.Serialize("value", new SerializationContext());
            store.Put(new Bytes(key), value);
            Assert.AreEqual(1, store.ApproximateNumEntries());
        }

        [Test]
        public void PutKeyExist()
        {
            var serdes = new StringSerDes();
            byte[] key = serdes.Serialize("key", new SerializationContext()),
                value = serdes.Serialize("value", new SerializationContext()),
                value2 = serdes.Serialize("value2", new SerializationContext());

            store.Put(new Bytes(key), value);
            store.Put(new Bytes(key), value2);
            var e = store.All().ToList();
            Assert.AreEqual(1, e.Count);
            var v = store.Get(new Bytes(key));
            Assert.AreEqual("value2", serdes.Deserialize(v, new SerializationContext()));
        }

        [Test]
        public void DeletKeyNotExist()
        {
            var serdes = new StringSerDes();
            byte[] key = serdes.Serialize("key", new SerializationContext());

            var r = store.Delete(new Bytes(key));
            Assert.IsNull(r);
            Assert.AreEqual(0, store.ApproximateNumEntries());
        }

        [Test]
        public void DeleteKeyExist()
        {
            var serdes = new StringSerDes();
            byte[] key = serdes.Serialize("key", new SerializationContext()),
                value = serdes.Serialize("value", new SerializationContext());

            store.Put(new Bytes(key), value);
            Assert.AreEqual(1, store.ApproximateNumEntries());
            var v = store.Delete(new Bytes(key));
            Assert.AreEqual(0, store.ApproximateNumEntries());
            Assert.AreEqual("value", serdes.Deserialize(v, new SerializationContext()));
        }

        [Test]
        public void PutAll()
        {
            var serdes = new StringSerDes();
            byte[] key = serdes.Serialize("key", new SerializationContext()), value = serdes.Serialize("value", new SerializationContext());
            byte[] key1 = serdes.Serialize("key1", new SerializationContext()), value1 = serdes.Serialize("value1", new SerializationContext());
            byte[] key2 = serdes.Serialize("key2", new SerializationContext()), value2 = serdes.Serialize("value2", new SerializationContext());
            byte[] key3 = serdes.Serialize("key3", new SerializationContext()), value3 = serdes.Serialize("value3", new SerializationContext());

            var items = new List<KeyValuePair<Bytes, byte[]>>();
            items.Add(KeyValuePair.Create(new Bytes(key), value));
            items.Add(KeyValuePair.Create(new Bytes(key1), value1));
            items.Add(KeyValuePair.Create(new Bytes(key2), value2));
            items.Add(KeyValuePair.Create(new Bytes(key3), value3));

            store.PutAll(items);

            Assert.AreEqual(4, store.ApproximateNumEntries());
        }

        [Test]
        public void PutAllWithValueNull()
        {
            var serdes = new StringSerDes();
            byte[] key = serdes.Serialize("key", new SerializationContext()), value = serdes.Serialize("value", new SerializationContext());

            var items = new List<KeyValuePair<Bytes, byte[]>>();
            items.Add(KeyValuePair.Create(new Bytes(key), value));
            items.Add(KeyValuePair.Create(new Bytes(key), (byte[])null));

            store.PutAll(items);

            Assert.AreEqual(0, store.ApproximateNumEntries());
        }

        [Test]
        public void PutIfAbsent()
        {
            var serdes = new StringSerDes();
            byte[] key3 = serdes.Serialize("key3", new SerializationContext()), value3 = serdes.Serialize("value3", new SerializationContext());

            store.PutIfAbsent(new Bytes(key3), value3);
            store.PutIfAbsent(new Bytes(key3), value3);

            Assert.AreEqual(1, store.ApproximateNumEntries());
        }

        //RocksDbException  
        [Test]
        public void PutThrowRocksDbException()
        {

        }

        [Test]
        public void EmptyEnumerator()
        {
            var enumerator = store.All().GetEnumerator();
            Assert.Throws<NotMoreValueException>(() =>
            {
                var a = enumerator.Current;
            });
        }

        [Test]
        public void EnumeratorReset()
        {
            var serdes = new StringSerDes();
            byte[] key = serdes.Serialize("key", new SerializationContext()), value = serdes.Serialize("value", new SerializationContext());

            store.Put(new Bytes(key), value);

            var enumerator = store.All().GetEnumerator();
            Assert.IsTrue(enumerator.MoveNext());
            Assert.IsFalse(enumerator.MoveNext());
            enumerator.Reset();
            Assert.IsTrue(enumerator.MoveNext());
        }
    }
}
