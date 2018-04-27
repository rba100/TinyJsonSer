using System;

namespace TinyJsonSer.Tests.TestClasses
{
    [Serializable]
    public class ImmutableClass
    {
        public int IntValue { get; }
        public string StrValue { get; }

        public int ConstructorId { get; }

        public ImmutableClass()
        {
            IntValue = -1;
            StrValue = "bad";
            ConstructorId = 1;
        }

        public ImmutableClass(int intValue)
        {
            IntValue = intValue;
            StrValue = "bad";
            ConstructorId = 2;
        }

        public ImmutableClass(string strValue)
        {
            IntValue = -1;
            StrValue = strValue;
            ConstructorId = 3;
        }

        public ImmutableClass(int intValue, string strValue)
        {
            IntValue = intValue;
            StrValue = strValue;
            ConstructorId = 4;
        }

        public static string Json()
        {
            return @"{""IntValue"" : 10,""StrValue"" : ""Hello""}";
        }
    }
}