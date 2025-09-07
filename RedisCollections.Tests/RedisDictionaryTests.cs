using System;
using System.Collections.Generic;
using System.Linq;
using Moq;
using NUnit.Framework;
using Redis.Collections.Generic;
using Redis.Collections.Generic.Serializing;
using StackExchange.Redis;

namespace RedisCollections.Tests
{
    [TestFixture]
    public class RedisDictionaryTests
    {
        private Mock<IDatabase> databaseMock;
        private Mock<IRedisSerializer> keySerializerMock;
        private Mock<IRedisSerializer> valueSerializerMock;
        private RedisCollectionOptions options;
        private RedisDictionary<string, string> redisDictionary;

        [SetUp]
        public void Setup()
        {
            databaseMock = new Mock<IDatabase>();
            keySerializerMock = new Mock<IRedisSerializer>();
            valueSerializerMock = new Mock<IRedisSerializer>();
            
            options = new RedisCollectionOptions
            {
                KeySerializer = keySerializerMock.Object,
                ValueSerializer = valueSerializerMock.Object
            };
            
            redisDictionary = new RedisDictionary<string, string>(databaseMock.Object, "test", options);
        }

        [Test]
        public void AddsValue_When_AddMethodIsCalled()
        {
            // Arrange
            var key = "testKey";
            var value = "testValue";
            var serializedKey = new RedisValue("serializedKey");
            var serializedValue = new RedisValue("serializedValue");
            
            keySerializerMock.Setup(s => s.Serialize(key)).Returns(serializedKey);
            valueSerializerMock.Setup(s => s.Serialize(value)).Returns(serializedValue);
            databaseMock.Setup(db => db.HashSet("test(test)", serializedKey, serializedValue, When.NotExists, CommandFlags.HighPriority)).Returns(true);

            // Act
            redisDictionary.Add(key, value);

            // Assert
            databaseMock.Verify(db => db.HashSet("test(test)", serializedKey, serializedValue, When.NotExists, CommandFlags.HighPriority), Times.Once);
        }

        [Test]
        public void ThrowsArgumentException_When_AddMethodIsCalledWithExistingKey()
        {
            // Arrange
            var key = "testKey";
            var value = "testValue";
            var serializedKey = new RedisValue("serializedKey");
            var serializedValue = new RedisValue("serializedValue");
            
            keySerializerMock.Setup(s => s.Serialize(key)).Returns(serializedKey);
            valueSerializerMock.Setup(s => s.Serialize(value)).Returns(serializedValue);
            databaseMock.Setup(db => db.HashSet("test(test)", serializedKey, serializedValue, When.NotExists, CommandFlags.HighPriority)).Returns(false);

            // Act & Assert
            var ex = Assert.Throws<ArgumentException>(() => redisDictionary.Add(key, value));
            Assert.That(ex.Message, Is.EqualTo("An item with the same key has already been added"));
        }

        [Test]
        public void ReturnsTrue_When_TryGetValueFindsKey()
        {
            // Arrange
            var key = "testKey";
            var value = "testValue";
            var serializedKey = new RedisValue("serializedKey");
            var serializedValue = new RedisValue("serializedValue");
            
            keySerializerMock.Setup(s => s.Serialize(key)).Returns(serializedKey);
            keySerializerMock.Setup(s => s.Deserialize<string>(serializedKey)).Returns(key);
            valueSerializerMock.Setup(s => s.Deserialize<string>(serializedValue)).Returns(value);
            databaseMock.Setup(db => db.HashGet("test(test)", serializedKey, CommandFlags.HighPriority)).Returns(serializedValue);

            // Act
            var result = redisDictionary.TryGetValue(key, out var actualValue);

            // Assert
            Assert.IsTrue(result);
            Assert.AreEqual(value, actualValue);
        }

        [Test]
        public void ReturnsFalse_When_TryGetValueDoesNotFindKey()
        {
            // Arrange
            var key = "testKey";
            var serializedKey = new RedisValue("serializedKey");
            
            keySerializerMock.Setup(s => s.Serialize(key)).Returns(serializedKey);
            databaseMock.Setup(db => db.HashGet("test(test)", serializedKey, CommandFlags.HighPriority)).Returns(RedisValue.Null);

            // Act
            var result = redisDictionary.TryGetValue(key, out var actualValue);

            // Assert
            Assert.IsFalse(result);
            Assert.IsNull(actualValue);
        }

        [Test]
        public void ClearsAllEntries_When_ClearMethodIsCalled()
        {
            // Act
            redisDictionary.Clear();

            // Assert
            databaseMock.Verify(db => db.KeyDelete("test(test)", CommandFlags.HighPriority), Times.Once);
        }

        [Test]
        public void ReturnsTrue_When_ContainsKeyFindsKey()
        {
            // Arrange
            var key = "testKey";
            var serializedKey = new RedisValue("serializedKey");
            
            keySerializerMock.Setup(s => s.Serialize(key)).Returns(serializedKey);
            databaseMock.Setup(db => db.HashExists("test(test)", serializedKey, CommandFlags.HighPriority)).Returns(true);

            // Act
            var result = redisDictionary.ContainsKey(key);

            // Assert
            Assert.IsTrue(result);
        }

        [Test]
        public void ReturnsFalse_When_ContainsKeyDoesNotFindKey()
        {
            // Arrange
            var key = "testKey";
            var serializedKey = new RedisValue("serializedKey");
            
            keySerializerMock.Setup(s => s.Serialize(key)).Returns(serializedKey);
            databaseMock.Setup(db => db.HashExists("test(test)", serializedKey, CommandFlags.HighPriority)).Returns(false);

            // Act
            var result = redisDictionary.ContainsKey(key);

            // Assert
            Assert.IsFalse(result);
        }

        [Test]
        public void ReturnsTrue_When_ContainsFindsKeyValuePair()
        {
            // Arrange
            var key = "testKey";
            var value = "testValue";
            var pair = new KeyValuePair<string, string>(key, value);
            var serializedKey = new RedisValue("serializedKey");
            var serializedValue = new RedisValue("serializedValue");
            
            keySerializerMock.Setup(s => s.Serialize(key)).Returns(serializedKey);
            valueSerializerMock.Setup(s => s.Deserialize<string>(serializedValue)).Returns(value);
            databaseMock.Setup(db => db.HashGet("test(test)", serializedKey, CommandFlags.HighPriority)).Returns(serializedValue);

            // Act
            var result = redisDictionary.Contains(pair);

            // Assert
            Assert.IsTrue(result);
        }

        [Test]
        public void ReturnsFalse_When_ContainsDoesNotFindKeyValuePair()
        {
            // Arrange
            var key = "testKey";
            var value = "testValue";
            var pair = new KeyValuePair<string, string>(key, value);
            var serializedKey = new RedisValue("serializedKey");
            var serializedValue = new RedisValue("differentValue");
            
            keySerializerMock.Setup(s => s.Serialize(key)).Returns(serializedKey);
            valueSerializerMock.Setup(s => s.Deserialize<string>(serializedValue)).Returns("differentValue");
            databaseMock.Setup(db => db.HashGet("test(test)", serializedKey, CommandFlags.HighPriority)).Returns(serializedValue);

            // Act
            var result = redisDictionary.Contains(pair);

            // Assert
            Assert.IsFalse(result);
        }

        [Test]
        public void ReturnsTrue_When_RemoveByKeyMethodRemovesKey()
        {
            // Arrange
            var key = "testKey";
            var serializedKey = new RedisValue("serializedKey");
            
            keySerializerMock.Setup(s => s.Serialize(key)).Returns(serializedKey);
            databaseMock.Setup(db => db.HashDelete("test(test)", serializedKey, CommandFlags.HighPriority)).Returns(1);

            // Act
            var result = redisDictionary.Remove(key);

            // Assert
            Assert.IsTrue(result);
        }

        [Test]
        public void ReturnsFalse_When_RemoveByKeyMethodDoesNotRemoveKey()
        {
            // Arrange
            var key = "testKey";
            var serializedKey = new RedisValue("serializedKey");
            
            keySerializerMock.Setup(s => s.Serialize(key)).Returns(serializedKey);
            databaseMock.Setup(db => db.HashDelete("test(test)", serializedKey, CommandFlags.HighPriority)).Returns(0);

            // Act
            var result = redisDictionary.Remove(key);

            // Assert
            Assert.IsFalse(result);
        }

        [Test]
        public void ReturnsTrue_When_RemoveByKeyValuePairMethodRemovesKey()
        {
            // Arrange
            var key = "testKey";
            var value = "testValue";
            var pair = new KeyValuePair<string, string>(key, value);
            var serializedKey = new RedisValue("serializedKey");
            var serializedValue = new RedisValue("serializedValue");
            
            keySerializerMock.Setup(s => s.Serialize(key)).Returns(serializedKey);
            valueSerializerMock.Setup(s => s.Deserialize<string>(serializedValue)).Returns(value);
            databaseMock.Setup(db => db.HashGet("test(test)", serializedKey, CommandFlags.HighPriority)).Returns(serializedValue);
            databaseMock.Setup(db => db.HashDelete("test(test)", serializedKey, CommandFlags.HighPriority)).Returns(1);

            // Act
            var result = redisDictionary.Remove(pair);

            // Assert
            Assert.IsTrue(result);
        }

        [Test]
        public void ReturnsFalse_When_RemoveByKeyValuePairMethodDoesNotRemoveKey()
        {
            // Arrange
            var key = "testKey";
            var value = "testValue";
            var pair = new KeyValuePair<string, string>(key, value);
            var serializedKey = new RedisValue("serializedKey");
            var serializedValue = new RedisValue("serializedValue");
            var differentSerializedValue = new RedisValue("differentValue");
            
            keySerializerMock.Setup(s => s.Serialize(key)).Returns(serializedKey);
            valueSerializerMock.Setup(s => s.Deserialize<string>(serializedValue)).Returns(value);
            valueSerializerMock.Setup(s => s.Deserialize<string>(differentSerializedValue)).Returns("differentValue");
            databaseMock.Setup(db => db.HashGet("test(test)", serializedKey, CommandFlags.HighPriority)).Returns(differentSerializedValue);

            // Act
            var result = redisDictionary.Remove(pair);

            // Assert
            Assert.IsFalse(result);
        }

        [Test]
        public void SetsValue_When_IndexerIsUsedToSet()
        {
            // Arrange
            var key = "testKey";
            var value = "testValue";
            var serializedKey = new RedisValue("serializedKey");
            var serializedValue = new RedisValue("serializedValue");
            
            keySerializerMock.Setup(s => s.Serialize(key)).Returns(serializedKey);
            valueSerializerMock.Setup(s => s.Serialize(value)).Returns(serializedValue);
            databaseMock.Setup(db => db.HashSet("test(test)", serializedKey, serializedValue, When.Always, CommandFlags.HighPriority)).Returns(true);

            // Act
            redisDictionary[key] = value;

            // Assert
            databaseMock.Verify(db => db.HashSet("test(test)", serializedKey, serializedValue, When.Always, CommandFlags.HighPriority), Times.Once);
        }

        [Test]
        public void ReturnsValue_When_IndexerIsUsedToGet()
        {
            // Arrange
            var key = "testKey";
            var value = "testValue";
            var serializedKey = new RedisValue("serializedKey");
            var serializedValue = new RedisValue("serializedValue");
            
            keySerializerMock.Setup(s => s.Serialize(key)).Returns(serializedKey);
            valueSerializerMock.Setup(s => s.Deserialize<string>(serializedValue)).Returns(value);
            databaseMock.Setup(db => db.HashGet("test(test)", serializedKey, CommandFlags.HighPriority)).Returns(serializedValue);

            // Act
            var result = redisDictionary[key];

            // Assert
            Assert.AreEqual(value, result);
        }

        [Test]
        public void ThrowsKeyNotFoundException_When_IndexerIsUsedToGetNonExistentKey()
        {
            // Arrange
            var key = "testKey";
            var serializedKey = new RedisValue("serializedKey");
            
            keySerializerMock.Setup(s => s.Serialize(key)).Returns(serializedKey);
            databaseMock.Setup(db => db.HashGet("test(test)", serializedKey, CommandFlags.HighPriority)).Returns(RedisValue.Null);

            // Act & Assert
            var ex = Assert.Throws<KeyNotFoundException>(() => { var _ = redisDictionary[key]; });
            Assert.That(ex.Message, Does.Contain($"Key '{key}' not found in dictionary"));
        }

        [Test]
        public void ReturnsCorrectCount_When_CountPropertyIsAccessed()
        {
            // Arrange
            const int expectedCount = 5;
            databaseMock.Setup(db => db.HashLength("test(test)", CommandFlags.HighPriority)).Returns(expectedCount);

            // Act
            var count = redisDictionary.Count;

            // Assert
            Assert.AreEqual(expectedCount, count);
        }

        [Test]
        public void ReturnsFalse_When_IsReadOnlyPropertyIsAccessed()
        {
            // Act
            var isReadOnly = redisDictionary.IsReadOnly;

            // Assert
            Assert.IsFalse(isReadOnly);
        }

        [Test]
        public void ReturnsKeys_When_KeysPropertyIsAccessed()
        {
            // Arrange
            var keys = new[] { new RedisValue("key1"), new RedisValue("key2") };
            var deserializedKeys = new[] { "key1", "key2" };
            
            databaseMock.Setup(db => db.HashKeys("test(test)", CommandFlags.HighPriority)).Returns(keys);
            keySerializerMock.Setup(s => s.Deserialize<string>(keys[0])).Returns(deserializedKeys[0]);
            keySerializerMock.Setup(s => s.Deserialize<string>(keys[1])).Returns(deserializedKeys[1]);

            // Act
            var result = redisDictionary.Keys;

            // Assert
            Assert.AreEqual(deserializedKeys.Length, result.Count);
            Assert.Contains(deserializedKeys[0], result.ToList());
            Assert.Contains(deserializedKeys[1], result.ToList());
        }

        [Test]
        public void ReturnsValues_When_ValuesPropertyIsAccessed()
        {
            // Arrange
            var values = new[] { new RedisValue("value1"), new RedisValue("value2") };
            var deserializedValues = new[] { "value1", "value2" };
            
            databaseMock.Setup(db => db.HashValues("test(test)", CommandFlags.HighPriority)).Returns(values);
            valueSerializerMock.Setup(s => s.Deserialize<string>(values[0])).Returns(deserializedValues[0]);
            valueSerializerMock.Setup(s => s.Deserialize<string>(values[1])).Returns(deserializedValues[1]);

            // Act
            var result = redisDictionary.Values;

            // Assert
            Assert.AreEqual(deserializedValues.Length, result.Count);
            Assert.Contains(deserializedValues[0], result.ToList());
            Assert.Contains(deserializedValues[1], result.ToList());
        }

        [Test]
        public void CopiesTo_Array_When_CopyToMethodIsCalled()
        {
            // Arrange
            var entries = new[]
            {
                new HashEntry(new RedisValue("key1"), new RedisValue("value1")),
                new HashEntry(new RedisValue("key2"), new RedisValue("value2"))
            };
            var array = new KeyValuePair<string, string>[3];
            var expectedPair1 = new KeyValuePair<string, string>("key1", "value1");
            var expectedPair2 = new KeyValuePair<string, string>("key2", "value2");
            
            databaseMock.Setup(db => db.HashGetAll("test(test)", CommandFlags.HighPriority)).Returns(entries);
            keySerializerMock.Setup(s => s.Deserialize<string>(entries[0].Name)).Returns(expectedPair1.Key);
            valueSerializerMock.Setup(s => s.Deserialize<string>(entries[0].Value)).Returns(expectedPair1.Value);
            keySerializerMock.Setup(s => s.Deserialize<string>(entries[1].Name)).Returns(expectedPair2.Key);
            valueSerializerMock.Setup(s => s.Deserialize<string>(entries[1].Value)).Returns(expectedPair2.Value);

            // Act
            redisDictionary.CopyTo(array, 0);

            // Assert
            Assert.AreEqual(expectedPair1, array[0]);
            Assert.AreEqual(expectedPair2, array[1]);
            Assert.AreEqual(default(KeyValuePair<string, string>), array[2]);
        }

        [Test]
        public void ReturnsEnumerator_When_GetEnumeratorMethodIsCalled()
        {
            // Arrange
            var entries = new[]
            {
                new HashEntry(new RedisValue("key1"), new RedisValue("value1")),
                new HashEntry(new RedisValue("key2"), new RedisValue("value2"))
            };
            var expectedPair1 = new KeyValuePair<string, string>("key1", "value1");
            var expectedPair2 = new KeyValuePair<string, string>("key2", "value2");
            
            databaseMock.Setup(db => db.HashScan("test(test)", It.IsAny<RedisValue>(), It.IsAny<int>(), It.IsAny<int>(), CommandFlags.HighPriority))
                       .Returns(entries);
            keySerializerMock.Setup(s => s.Deserialize<string>(entries[0].Name)).Returns(expectedPair1.Key);
            valueSerializerMock.Setup(s => s.Deserialize<string>(entries[0].Value)).Returns(expectedPair1.Value);
            keySerializerMock.Setup(s => s.Deserialize<string>(entries[1].Name)).Returns(expectedPair2.Key);
            valueSerializerMock.Setup(s => s.Deserialize<string>(entries[1].Value)).Returns(expectedPair2.Value);

            // Act
            var enumerator = redisDictionary.GetEnumerator();

            // Assert
            Assert.IsTrue(enumerator.MoveNext());
            Assert.AreEqual(expectedPair1, enumerator.Current);
            Assert.IsTrue(enumerator.MoveNext());
            Assert.AreEqual(expectedPair2, enumerator.Current);
            Assert.IsFalse(enumerator.MoveNext());
        }

        [Test]
        public void AddsValue_When_AddKeyValuePairMethodIsCalled()
        {
            // Arrange
            var key = "testKey";
            var value = "testValue";
            var pair = new KeyValuePair<string, string>(key, value);
            var serializedKey = new RedisValue("serializedKey");
            var serializedValue = new RedisValue("serializedValue");
            
            keySerializerMock.Setup(s => s.Serialize(key)).Returns(serializedKey);
            valueSerializerMock.Setup(s => s.Serialize(value)).Returns(serializedValue);
            databaseMock.Setup(db => db.HashSet("test(test)", serializedKey, serializedValue, When.NotExists, CommandFlags.HighPriority)).Returns(true);

            // Act
            redisDictionary.Add(pair);

            // Assert
            databaseMock.Verify(db => db.HashSet("test(test)", serializedKey, serializedValue, When.NotExists, CommandFlags.HighPriority), Times.Once);
        }
    }
}
