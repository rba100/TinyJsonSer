﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;

namespace /***$rootnamespace$.***/TinyJsonSer
{
    public class JsonDeserializer
    {
        private static readonly JsonParser _parser = new JsonParser();

        public T Deserialize<T>(string json)
        {
            return (T)Deserialize(typeof(T), json);
        }

        public object Deserialize(Type type, string json)
        {
            var jsonValue = _parser.Parse(json);
            return Deserialize(type, jsonValue);
        }

        public T Deserialize<T>(StreamReader jsonTextStream)
        {
            return (T)Deserialize(typeof(T), jsonTextStream);
        }

        public object Deserialize(Type type, StreamReader jsonTextStream)
        {
            var jsonValue = _parser.Parse(jsonTextStream);
            return Deserialize(type, jsonValue);
        }

        internal object Deserialize(Type type, JsonValue jsonValue)
        {
            if (jsonValue is JsonString str) return DeserializeString(type, str.Value);
            if (jsonValue is JsonObject obj) return DeserializeObject(type, obj);
            if (jsonValue is JsonArray array) return DeserializeArray(type, array);
            if (jsonValue is JsonNumber number) return DeserializeNumber(type, number);
            if (jsonValue is JsonNull) return DeserializeNull(type);
            if (jsonValue is JsonTrue) return DeserializeBoolean(type, true);
            if (jsonValue is JsonFalse) return DeserializeBoolean(type, false);

            throw new JsonException($"No deserializer for {jsonValue.GetType().Name}");
        }

        private object DeserializeNumber(Type type, JsonNumber jsonNumber)
        {
            try
            {
                if (type == typeof(int)) return int.Parse(jsonNumber.StringRepresentation);
                if (type == typeof(long)) return long.Parse(jsonNumber.StringRepresentation);
                if (type == typeof(decimal)) return decimal.Parse(jsonNumber.StringRepresentation);
                if (type == typeof(float)) return float.Parse(jsonNumber.StringRepresentation);
                if (type == typeof(double)) return double.Parse(jsonNumber.StringRepresentation);
                if (type == typeof(uint)) return uint.Parse(jsonNumber.StringRepresentation);
                if (type == typeof(ulong)) return ulong.Parse(jsonNumber.StringRepresentation);
                if (type == typeof(byte)) return byte.Parse(jsonNumber.StringRepresentation);
            }
            catch (FormatException)
            {
                throw new JsonException($"Malformed {type.Name}: '{jsonNumber.StringRepresentation}'");
            }

            // Fallback
            var tc = TypeDescriptor.GetConverter(type);
            if (tc.CanConvertFrom(typeof(string))) return tc.ConvertFromString(jsonNumber.StringRepresentation);

            throw new JsonException($"Could not map {jsonNumber.StringRepresentation} to {type.Name}");
        }

        private object DeserializeArray(Type type, JsonArray jsonArray)
        {
            if (type.IsArray)
            {
                return CreateArray(type.GetElementType(), jsonArray);
            }

            var genericArguments = type.GetGenericArguments();

            if (genericArguments.Length == 1
                && type.IsAssignableFrom(genericArguments.Single().MakeArrayType()))
            {
                return CreateArray(genericArguments.Single(), jsonArray);
            }

            throw new JsonException($"Could not map json array to {type.Name}");
        }

        private Array CreateArray(Type elementType, JsonArray jsonArray)
        {
            var length = jsonArray.Items.Count;
            var array = Array.CreateInstance(elementType, length);
            for (var i = 0; i < length; i++)
            {
                array.SetValue(Deserialize(elementType, jsonArray.Items[i]), i);
            }
            return array;
        }

        private object DeserializeObject(Type type, JsonObject jsonObject)
        {
            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Dictionary<,>)) return DeserializeDictionary(type, jsonObject);
            if (typeof(ISerializable).IsAssignableFrom(type)) return DeserializeISerializable(type, jsonObject);
            if (type.IsClass) return DerserializeObject(type, jsonObject);

            throw new JsonException($"Could not map {jsonObject.GetType().Name} to {type.Name}");
        }

        private object DeserializeISerializable(Type type, JsonObject jsonObject)
        {
            var info = new SerializationInfo(type, new JsonValueFormatterConverter(this));
            foreach (var member in jsonObject.Members) info.AddValue(member.Name, member.Value);

            var ctor = type.GetConstructor(PublicInstance | BindingFlags.NonPublic,
                                           null,
                                           new[] { typeof(SerializationInfo), typeof(StreamingContext) },
                                           null);

            if (ctor == null) throw new JsonException("ISerializable did not have the conventional constructor.");

            return ctor.Invoke(new object[] { info, new StreamingContext(StreamingContextStates.All) });
        }

        private object DeserializeNull(Type type)
        {
            return type.IsValueType ? Activator.CreateInstance(type) : null;
        }

        private object DeserializeBoolean(Type type, bool value)
        {
            if (type == typeof(bool)) return value;
            throw new JsonException($"Could not map '{value}' to {type.Name}");
        }

        private object DeserializeDictionary(Type type, JsonObject jsonValue)
        {
            var keyType = type.GetGenericArguments()[0];
            var valueType = type.GetGenericArguments()[1];
            var add = type.GetMethod("Add", new[] { keyType, valueType });
            var dictionary = Activator.CreateInstance(type);
            foreach (var member in jsonValue.Members)
            {
                var key = DeserializeString(keyType, member.Name);
                var value = Deserialize(valueType, member.Value);
                add.Invoke(dictionary, new[] { key, value });
            }
            return dictionary;
        }

        private object DeserializeString(Type type, string str)
        {
            if (type == typeof(string)) return str;

            if (type == typeof(DateTime)) return DateTime.Parse(str, null, System.Globalization.DateTimeStyles.RoundtripKind);

            var tc = TypeDescriptor.GetConverter(type);
            if (tc.CanConvertFrom(typeof(string))) return tc.ConvertFromString(str);

            var ctor = type.GetConstructor(new[] { typeof(string) });
            if (ctor != null) return ctor.Invoke(new object[] { str });

            throw new JsonException($"Could not map string to {type.Name}");
        }

        private object DerserializeObject(Type type, JsonObject jsonObject)
        {
            var jsonMemberNames = jsonObject.Members.Select(m => m.Name).ToArray();

            var constructor = type.GetConstructors(PublicInstance)
                                  .Where(ctor => ctor.GetParameters().All(p => jsonMemberNames.Contains(p.Name, CaseInsensitive)))
                                  .OrderByDescending(ctor => ctor.GetParameters().Length)
                                  .FirstOrDefault();

            if (constructor == null) throw new JsonException($"Could not find a suitable constructor for {type.Name}.");

            var constructorParameterInfos = constructor.GetParameters();

            var jsonMembersForConstructor = 
                constructorParameterInfos.Select(p => jsonObject.Members.Single(m => CaseInsensitive.Equals(m.Name, p.Name))).ToArray();

            var parameters =
                constructorParameterInfos.Select((p, i) => Deserialize(p.ParameterType, jsonMembersForConstructor[i].Value)).ToArray();

            var obj = constructor.Invoke(parameters);

            foreach (var objectMember in jsonObject.Members.Except(jsonMembersForConstructor))
            {
                var property = type.GetProperty(objectMember.Name, PublicInstance | BindingFlags.IgnoreCase);
                if (property != null)
                {
                    var propertyValue = Deserialize(property.PropertyType, objectMember.Value);
                    property.SetValue(obj, propertyValue, null);
                    continue;
                }

                var field = type.GetField(objectMember.Name, PublicInstance | BindingFlags.IgnoreCase);
                if (field != null)
                {
                    var fieldValue = Deserialize(field.FieldType, objectMember.Value);
                    field.SetValue(obj, fieldValue);
                }
            }

            return obj;
        }

        private static readonly BindingFlags PublicInstance = BindingFlags.Public | BindingFlags.Instance;
        private static readonly StringComparer CaseInsensitive = StringComparer.InvariantCultureIgnoreCase;
    }
}
