namespace TinyJsonSer.Tests.TestClasses
{
    public class MixedMutabilityClass
    {
        public int IntValue { get; }

        // ReSharper disable once UnusedAutoPropertyAccessor.Global
        public string StrValue { get; set; }

        public MixedMutabilityClass(int intValue)
        {
            IntValue = intValue;
        }

        public static string Json()
        {
            return @"{""IntValue"" : 10,""StrValue"" : ""Hello""}";
        }
    }
}