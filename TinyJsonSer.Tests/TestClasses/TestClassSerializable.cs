using System;
using System.Runtime.Serialization;
using System.Text;

using NUnit.Framework;

namespace TinyJsonSer.Tests.TestClasses
{
    [Serializable]
    public class TestClassSerializable : ISerializable
    {
        private readonly int m_Integer;
        private readonly string m_String;
        private readonly sbyte m_Sbyte;
        private readonly bool m_Bool;
        private readonly StringBuilder m_StringBuilder;

        public void AssertMatches(TestClassSerializable other)
        {
            if (m_Integer != other.m_Integer) Assert.Fail($"Integer {m_Integer} != {other.m_Integer}");
            if (m_String != other.m_String) Assert.Fail($"String {m_String} != {other.m_String}");
            if (m_Sbyte != other.m_Sbyte) Assert.Fail($"Sbyte {m_Sbyte} != {other.m_Sbyte}");
            if (m_Bool != other.m_Bool) Assert.Fail($"Bool {m_Bool} != {other.m_Bool}");
            if (m_StringBuilder.ToString() != other.m_StringBuilder.ToString()) Assert.Fail($"StringBuilder {m_StringBuilder} != {other.m_StringBuilder}");
        }

        public TestClassSerializable()
        {
            throw new Exception("Usage of this constructor is forbidden");
        }

        public TestClassSerializable(int integer, string s, sbyte @sbyte, bool b)
        {
            m_Integer = integer;
            m_String = s;
            m_Sbyte = @sbyte;
            m_Bool = b;
            m_StringBuilder = new StringBuilder(s + s);
        }

        public TestClassSerializable(SerializationInfo info, StreamingContext context)
        {
            m_Integer = info.GetInt32("Integer");
            m_String = info.GetString("String");
            m_Sbyte = info.GetSByte("Sbyte");
            m_Bool = info.GetBoolean("Bool");
            m_StringBuilder = info.GetValue("StringBuilder", typeof(StringBuilder)) as StringBuilder;
        }

        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            info.AddValue("Integer", m_Integer);
            info.AddValue("String", m_String);
            info.AddValue("Sbyte", m_Sbyte);
            info.AddValue("Bool", m_Bool);
            info.AddValue("StringBuilder", m_StringBuilder);
        }
    }
}