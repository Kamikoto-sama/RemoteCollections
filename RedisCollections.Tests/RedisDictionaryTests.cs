using NUnit.Framework;
using Moq;
using FluentAssertions;
using Redis.Collections.Generic;
using StackExchange.Redis;
using System.Collections.Generic;

namespace RedisCollections.Tests
{
    [TestFixture]
    public class RedisDictionaryTests
    {
        private Mock<IDatabase> _mockDatabase;
        private Mock<IRedisSerializer> _mockSerializer;
        private RedisDictionary<string, string> _redisDictionary;

        [SetUp]
        public void Setup()
        {
            _mockDatabase = new Mock<IDatabase>();
            _mockSerializer = new Mock<IRedisSerializer>();
            _redisDictionary = new RedisDictionary<string, string>("testKey", _mockDatabase.Object, _mockSerializer.Object);
        }

        [Test]
        public void AddsValue_When_AddMethodIsCalled()
        {
            // Arrange
            var key = "testKey";
            var value = "testValue";
            _mockSerializer.Setup(s => s.Serialize(key)).Returns((RedisValue)key);
            _mockSerializer.Setup(s => s.Serialize(value)).Returns((RedisValue)value);

            // Act
            _redisDictionary.Add(key, value);

            // Assert
            _mockDatabase.Verify(db => db.HashSetAsync("testKey", new[] { new HashEntry(key, value) }, CommandFlags.None), Times.Once);
        }

        [Test]
        public void ReturnsTrue_When_TryGetValueFindsKey()
        {
            // Arrange
            var key = "testKey";
            var value = "testValue";
            _mockSerializer.Setup(s => s.Serialize(key)).Returns((RedisValue)key);
            _mockSerializer.Setup(s => s.Deserialize<string>(It.IsAny<RedisValue>())).Returns(value);
            _mockDatabase.Setup(db => db.HashExists("testKey", key)).Returns(true);
            _mockDatabase.Setup(db => db.HashGet("testKey", key)).Returns((RedisValue)value);

            // Act
            bool result = _redisDictionary.TryGetValue(key, out string outValue);

            // Assert
            result.Should().BeTrue();
            outValue.Should().Be(value);
        }

        [Test]
        public void ReturnsFalse_When_TryGetValueDoesNotFindKey()
        {
            // Arrange
            var key = "testKey";
            _mockSerializer.Setup(s => s.Serialize(key)).Returns((RedisValue)key);
            _mockDatabase.Setup(db => db.HashExists("testKey", key)).Returns(false);

            // Act
            bool result = _redisDictionary.TryGetValue(key, out string outValue);

            // Assert
            result.Should().BeFalse();
            outValue.Should().BeNull();
        }

        [Test]
        public void ClearsAllEntries_When_ClearMethodIsCalled()
        {
            // Arrange

            // Act
            _redisDictionary.Clear();

            // Assert
            _mockDatabase.Verify(db => db.KeyDelete("testKey"), Times.Once);
        }

        [Test]
        public void ReturnsTrue_When_ContainsKeyFindsKey()
        {
            // Arrange
            var key = "testKey";
            _mockSerializer.Setup(s => s.Serialize(key)).Returns((RedisValue)key);
            _mockDatabase.Setup(db => db.HashExists("testKey", key)).Returns(true);

            // Act
            bool result = _redisDictionary.ContainsKey(key);

            // Assert
            result.Should().BeTrue();
        }

        [Test]
        public void ReturnsFalse_When_ContainsKeyDoesNotFindKey()
        {
            // Arrange
            var key = "testKey";
            _mockSerializer.Setup(s => s.Serialize(key)).Returns((RedisValue)key);
            _mockDatabase.Setup(db => db.HashExists("testKey", key)).Returns(false);

            // Act
            bool result = _redisDictionary.ContainsKey(key);

            // Assert
            result.Should().BeFalse();
        }

        [Test]
        public void ReturnsTrue_When_RemoveByKeyMethodRemovesKey()
        {
            // Arrange
            var key = "testKey";
            _mockSerializer.Setup(s => s.Serialize(key)).Returns((RedisValue)key);
            _mockDatabase.Setup(db => db.HashDelete("testKey", key)).Returns(true);

            // Act
            bool result = _redisDictionary.Remove(key);

            // Assert
            result.Should().BeTrue();
        }

        [Test]
        public void ReturnsFalse_When_RemoveByKeyMethodDoesNotRemoveKey()
        {
            // Arrange
            var key = "testKey";
            _mockSerializer.Setup(s => s.Serialize(key)).Returns((RedisValue)key);
            _mockDatabase.Setup(db => db.HashDelete("testKey", key)).Returns(false);

            // Act
            bool result = _redisDictionary.Remove(key);

            // Assert
            result.Should().BeFalse();
        }

        [Test]
        public void ReturnsTrue_When_RemoveByKeyValuePairMethodRemovesKey()
        {
            // Arrange
            var key = "testKey";
            var value = "testValue";
            _mockSerializer.Setup(s => s.Serialize(key)).Returns((RedisValue)key);
            _mockDatabase.Setup(db => db.HashDelete("testKey", key)).Returns(true);

            // Act
            bool result = _redisDictionary.Remove(new KeyValuePair<string, string>(key, value));

            // Assert
            result.Should().BeTrue();
        }

        [Test]
        public void ReturnsFalse_When_RemoveByKeyValuePairMethodDoesNotRemoveKey()
        {
            // Arrange
            var key = "testKey";
            var value = "testValue";
            _mockSerializer.Setup(s => s.Serialize(key)).Returns((RedisValue)key);
            _mockDatabase.Setup(db => db.HashDelete("testKey", key)).Returns(false);

            // Act
            bool result = _redisDictionary.Remove(new KeyValuePair<string, string>(key, value));

            // Assert
            result.Should().BeFalse();
        }
    }
}
