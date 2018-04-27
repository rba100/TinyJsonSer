using System.Collections.Generic;
using System.IO;
using System.Text;
using NUnit.Framework;
using TinyJsonSer.Tests.TestClasses;

namespace TinyJsonSer.Tests
{
    public class IntegrationTests
    {
        [Test]
        public void RoundTrip()
        {
            var serialiser = new JsonSerializer(true);
            var deserialiser = new JsonDeserializer();

            var testClass = new TestClass
            {
                StringProperty = "allo",
                BoolF = false,
                BoolT = true,
                Child = new TestClass { Int32 = 30, Int32s = new[] { 1, -2, 3, -4, 5 } },
                StringCounts = new Dictionary<string, int> { { "bl𝒜h", 6 } }
            };

            var serialised = serialiser.Serialize(testClass);
            using (var memStream = new MemoryStream(Encoding.UTF8.GetBytes(serialised)))
            using (var reader = new StreamReader(memStream, Encoding.UTF8))
            {
                var returned = deserialiser.Deserialize<TestClass>(reader);
                Assert.AreEqual(testClass.StringProperty, returned.StringProperty);
                Assert.AreEqual(testClass.BoolF, returned.BoolF);
                Assert.AreEqual(testClass.BoolT, returned.BoolT);
                Assert.AreEqual(6, returned.StringCounts["bl𝒜h"]);
                Assert.AreEqual(30, returned.Child.Int32);
                CollectionAssert.AreEqual(testClass.Child.Int32s, returned.Child.Int32s);
            }
        }

        [Test]
        public void RoundTripISerializable()
        {
            var input = new TestClassSerializable(10, "Hello", 20, true);
            var json = new JsonSerializer(true).Serialize(input);
            var output = new JsonDeserializer().Deserialize<TestClassSerializable>(json);
            input.AssertMatches(output);
        }
    }
}
