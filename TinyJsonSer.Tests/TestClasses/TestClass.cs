using System;
using System.Collections.Generic;

namespace TinyJsonSer.Tests.TestClasses
{
    [Serializable]
    public class TestClass
    {
        public string StringProperty { get; set; }
        public Dictionary<string, int> StringCounts { get; set; }
        public IEnumerable<string> VariousStrings { get; set; }
        public ICollection<string> VariousStrings2 { get; set; }

        public int[] Int32s { get; set; }
        public bool BoolT { get; set; }
        public bool BoolF { get; set; }
        public int Int32 { get; set; }
        public TestClass Child { get; set; }

        public TestClass()
        {
        }

        public TestClass(string stringProperty)
        {
            StringProperty = stringProperty;
        }

        public static string Json()
        {
            return @"
{
    ""StringProperty"" : ""TheValue"",
    ""VariousStrings"" : [""One"", ""Two""],
    ""VariousStrings2"" : [""Three"", ""Four""],
    ""Int32s"" : [ 1 ,2, 3, 4, -5 ], 
    ""BoolT"" : true, 
    ""BoolF"" : false ,
    ""Child"" : { ""StringProperty"" : ""ChildStringProperty"" },
    ""StringCounts"" : { ""Apples"" : 2, ""Bananas"" : 3 }
}";
        }
    }
}