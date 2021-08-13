using System;
using FsCheck.Xunit;
using Wabbajack.DTOs.JsonConverters;
using Wabbajack.Hashing.xxHash64;
using Xunit;
using System.Text.Json.Serialization;

namespace Wabbajack.DTOs.Test
{
    public class ValueTests
    {
        private readonly DTOSerializer _dtos;

        public ValueTests()
        {
            _dtos = new DTOSerializer(new JsonConverter[] {new HashJsonConverter(), new HashRelativePathConverter()});
        }
        public class HashData
        {
            public Hash Value { get; set; }
        }
        public class HashDataRelative
        {
            public HashRelativePath Value { get; set; }
        }
        
        [Fact]
        public void TestHash()
        {
            var a = new HashData() { Value = Hash.FromULong(int.MaxValue) };
            var b = _dtos.Deserialize<HashData>(_dtos.Serialize(a));
            Assert.Equal(a.Value, b.Value);
        }
        
        [Fact]
        public void TestHashRelative()
        {
            var a = new HashDataRelative { Value = new HashRelativePath(Hash.FromULong(int.MaxValue)) };
            var b = _dtos.Deserialize<HashDataRelative>(_dtos.Serialize(a));
            Assert.Equal(a.Value.Hash, b.Value.Hash);
        }
    }
}