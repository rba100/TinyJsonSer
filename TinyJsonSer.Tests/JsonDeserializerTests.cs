using System;
using System.Net.Configuration;
using Newtonsoft.Json;
using NUnit.Framework;
using TinyJsonSer.Tests.TestClasses;

namespace TinyJsonSer.Tests
{
    [TestFixture]
    public class JsonDeserializerTests
    {
        [Test]
        public void Deserialization()
        {
            var deserialiser = new JsonDeserializer();
            var testClass = deserialiser.Deserialize<TestClass>(TestClass.Json());

            Assert.NotNull(testClass);
            Assert.AreEqual("TheValue", testClass.StringProperty);
            Assert.AreEqual(false, testClass.BoolF);
            Assert.AreEqual(true, testClass.BoolT);
            Assert.AreEqual("ChildStringProperty", testClass.Child.StringProperty);
            CollectionAssert.AreEqual(new[] { 1, 2, 3, 4, -5 }, testClass.Int32s);
            CollectionAssert.AreEqual(new[] { "One", "Two" }, testClass.VariousStrings);
            CollectionAssert.AreEqual(new[] { "Three", "Four" }, testClass.VariousStrings2);
            var apples = testClass.StringCounts["Apples"];
            var bananas = testClass.StringCounts["Bananas"];
            Assert.AreEqual(2, apples);
            Assert.AreEqual(3, bananas);
        }

        [TestCase(@"{""BoolT"" : null }")]
        [TestCase(@"{""Int32"" : null }")]
        public void CannotDeserializeNullToValueType(string nullToValueTypeJson)
        {
            var deserialiser = new JsonDeserializer();
            Assert.That(() =>
                deserialiser.Deserialize<TestClass>(nullToValueTypeJson),
                Throws.Exception.TypeOf<JsonException>().With.Message.Contain("value type"));
        }

        public enum TestEnum { Red, Green, Blue }
        [TestCase("\"Red\"", TestEnum.Red)]
        [TestCase("\"green\"", TestEnum.Green)]
        [TestCase("\"BLUE\"", TestEnum.Blue)]
        [TestCase("\"0\"", TestEnum.Red)]
        [TestCase("\"1\"", TestEnum.Green)]
        [TestCase("\"2\"", TestEnum.Blue)]
        [TestCase("0", TestEnum.Red)]
        [TestCase("1", TestEnum.Green)]
        [TestCase("2", TestEnum.Blue)]
        public void EnumDeserialization(string json, TestEnum expectedValue)
        {
            var deserialiser = new JsonDeserializer();
            var val = deserialiser.Deserialize<TestEnum>(json);
            Assert.AreEqual(expectedValue, val);
        }

        [Test]
        public void CanDeserialiseIso8601()
        {
            var deserialiser = new JsonDeserializer();
            var val = deserialiser.Deserialize<DateTime>("\"2017-06-30T14:15:57Z\"");
            Assert.AreEqual(2017, val.Year);
            Assert.AreEqual(6, val.Month);
            Assert.AreEqual(30, val.Day);
            Assert.AreEqual(14, val.Hour);
            Assert.AreEqual(15, val.Minute);
            Assert.AreEqual(57, val.Second);
            Assert.AreEqual(DateTimeKind.Utc, val.Kind);
        }

        [Test]
        public void MostSpecificConstructorCalled()
        {
            var deserialiser = new JsonDeserializer();
            var obj = deserialiser.Deserialize<ImmutableClass>(ImmutableClass.Json());
            Assert.AreEqual(4, obj.ConstructorId);
            Assert.AreEqual(10, obj.IntValue);
            Assert.AreEqual("Hello", obj.StrValue);
        }

        [Test]
        public void CanDeserialiseMixedMutabilityClass()
        {
            var deserialiser = new JsonDeserializer();
            var obj = deserialiser.Deserialize<MixedMutabilityClass>(MixedMutabilityClass.Json());
            Assert.AreEqual(10, obj.IntValue);
            Assert.AreEqual("Hello", obj.StrValue);
        }

        [Test]
        public void CanDeserialiseISerializable()
        {
            var input = new TestClassSerializable(10, "Hello", 20, true);
            var json = JsonConvert.SerializeObject(input);
            var output = new JsonDeserializer().Deserialize<TestClassSerializable>(json);
            input.AssertMatches(output);
        }
    }
}
