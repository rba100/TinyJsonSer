using System;
using System.Runtime.Serialization;

namespace /***$rootnamespace$.***/TinyJsonSer
{
    internal class JsonValueFormatterConverter : IFormatterConverter
    {
        private readonly JsonDeserializer m_Deserializer;

        public JsonValueFormatterConverter(JsonDeserializer deserializer)
        {
            m_Deserializer = deserializer;
        }

        public object Convert(object value, Type type)
        {
            return m_Deserializer.Deserialize(type, value as JsonValue);
        }

        public object Convert(object value, TypeCode typeCode)
        {
            switch (typeCode)
            {
                case TypeCode.Empty:
                case TypeCode.DBNull:
                    return null;
                case TypeCode.Object:
                    return value;
                case TypeCode.Boolean:
                    return ToBoolean(value);
                case TypeCode.Char:
                    return ToChar(value);
                case TypeCode.SByte:
                    return ToSByte(value);
                case TypeCode.Byte:
                    return ToByte(value);
                case TypeCode.Int16:
                    return ToInt16(value);
                case TypeCode.UInt16:
                    return ToUInt16(value);
                case TypeCode.Int32:
                    return ToInt32(value);
                case TypeCode.UInt32:
                    return ToUInt32(value);
                case TypeCode.Int64:
                    return ToInt64(value);
                case TypeCode.UInt64:
                    return ToUInt64(value);
                case TypeCode.Single:
                    return ToSingle(value);
                case TypeCode.Double:
                    return ToDouble(value);
                case TypeCode.Decimal:
                    return ToDecimal(value);
                case TypeCode.DateTime:
                    return ToDateTime(value);
                case TypeCode.String:
                    return ToString(value);
                default:
                    throw new ArgumentOutOfRangeException(nameof(typeCode), typeCode, null);
            }
        }

        public bool ToBoolean(object value)
        {
            return (bool)Convert(value, typeof(bool));
        }

        public char ToChar(object value)
        {
            return (char)Convert(value, typeof(char));
        }

        public sbyte ToSByte(object value)
        {
            return (sbyte)Convert(value, typeof(sbyte));
        }

        public byte ToByte(object value)
        {
            return (byte)Convert(value, typeof(byte));
        }

        public short ToInt16(object value)
        {
            return (short)Convert(value, typeof(short));
        }

        public ushort ToUInt16(object value)
        {
            return (ushort)Convert(value, typeof(ushort));
        }

        public int ToInt32(object value)
        {
            return (int)Convert(value, typeof(int));
        }

        public uint ToUInt32(object value)
        {
            return (uint)Convert(value, typeof(uint));
        }

        public long ToInt64(object value)
        {
            return (long)Convert(value, typeof(long));
        }

        public ulong ToUInt64(object value)
        {
            return (ulong)Convert(value, typeof(ulong));
        }

        public float ToSingle(object value)
        {
            return (float)Convert(value, typeof(float));
        }

        public double ToDouble(object value)
        {
            return (double)Convert(value, typeof(double));
        }

        public decimal ToDecimal(object value)
        {
            return (decimal)Convert(value, typeof(decimal));
        }

        public DateTime ToDateTime(object value)
        {
            return (DateTime)Convert(value, typeof(DateTime));
        }

        public string ToString(object value)
        {
            return (string)Convert(value, typeof(string));
        }
    }
}