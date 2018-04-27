using System;
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
                // Handle IEnumerable<T> etc.
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
            if (type.IsClass) return InstatiateObject(type, jsonObject);

            throw new JsonException($"Could not map {jsonObject.GetType().Name} to {type.Name}");
        }

        private object DeserializeISerializable(Type type, JsonObject jsonObject)
        {
            var info = new SerializationInfo(type, new JsonValueFormatterConverter(this));
            foreach (var member in jsonObject.Members) info.AddValue(member.Name, member.Value);

            var ctor = type.GetConstructor(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance,
                                           null,
                                           new[] { typeof(SerializationInfo), typeof(StreamingContext) },
                                           null);

            if (ctor == null) throw new JsonException("ISerializable did not have the conventional constructor.");

            return ctor.Invoke(new object[] { info, new StreamingContext(StreamingContextStates.All) });
        }

        private object DeserializeNull(Type type)
        {
            if (!type.IsValueType) return null;
            throw new JsonException($"Could not map JsonNull to a value type '{type.Name}'");
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

        private object InstatiateObject(Type type, JsonObject jsonObject)
        {
            var objectActivationPlan = GetObjectActivationPlan(type, jsonObject);

            var parameters = objectActivationPlan.Parameters
                                                 .Select(pair => Deserialize(pair.TargetType, pair.Parameter.Value))
                                                 .ToArray();

            var obj = objectActivationPlan.Constructor.Invoke(parameters);

            foreach (var member in objectActivationPlan.Properties)
            {
                var exactProperty = type.GetProperty(member.Name, BindingFlags.Instance | BindingFlags.Public);
                if (exactProperty != null)
                {
                    var propertyValue = Deserialize(exactProperty.PropertyType, member.Value);
                    exactProperty.SetValue(obj, propertyValue, null);
                    continue;
                }

                var property = type.GetProperty(member.Name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.IgnoreCase);
                if (property != null)
                {
                    var propertyValue = Deserialize(property.PropertyType, member.Value);
                    property.SetValue(obj, propertyValue, null);
                    continue;
                }

                var exactField = type.GetField(member.Name, BindingFlags.Instance | BindingFlags.Public);
                if (exactField != null)
                {
                    var fieldValue = Deserialize(exactField.FieldType, member.Value);
                    exactField.SetValue(obj, fieldValue);
                    continue;
                }

                var field = type.GetField(member.Name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.IgnoreCase);
                if (field != null)
                {
                    var fieldValue = Deserialize(field.FieldType, member.Value);
                    field.SetValue(obj, fieldValue);
                }
            }

            return obj;
        }

        // Find a constructor whose parameter names match key names in the JsonObject
        private ObjectActivationPlan GetObjectActivationPlan(Type type, JsonObject jsonObject)
        {
            var noCase = StringComparer.InvariantCultureIgnoreCase;

            var typeMemberNames = type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                                      .Where(p => p.CanWrite).OfType<MemberInfo>()
                                      .Union(type.GetFields(BindingFlags.Public | BindingFlags.Instance))
                                      .Select(info => info.Name.ToLowerInvariant())
                                      .ToArray();

            var jsonMemberNames = jsonObject.Members.Select(m => m.Name).ToArray();

            bool CanSatisfy(ConstructorInfo ctor)
            {
                var ctorParamNames = ctor.GetParameters().Select(p => p.Name).ToArray();
                var paramsNotInCtor = jsonMemberNames.Except(ctorParamNames, noCase).ToArray();
                return ctorParamNames.All(p => jsonMemberNames.Contains(p, noCase)) &&
                       paramsNotInCtor.All(p => typeMemberNames.Contains(p, noCase));
            }

            var constructor = type.GetConstructors(BindingFlags.Public | BindingFlags.Instance)
                                  .Where(CanSatisfy)
                                  .OrderByDescending(ctor => ctor.GetParameters().Length)
                                  .FirstOrDefault();

            if (constructor == null) throw new JsonException($"Could not find a suitable constructor for {type.Name}.");

            var constructorParameterMap = constructor
                 .GetParameters()
                 .Select(p => new JsonParameterToType(jsonObject.Members
                                                              .Single(m => noCase.Equals(m.Name, p.Name)),
                                                    p.ParameterType))
                 .ToArray();

            var propertiesAndFields = jsonObject.Members
                                                .Except(constructorParameterMap.Select(p => p.Parameter))
                                                .ToArray();

            return new ObjectActivationPlan(constructor, constructorParameterMap, propertiesAndFields);
        }

        private class ObjectActivationPlan
        {
            internal ConstructorInfo Constructor { get; }
            internal JsonParameterToType[] Parameters { get; }
            internal JsonObjectMember[] Properties { get; }

            internal ObjectActivationPlan(ConstructorInfo constructor,
                                          JsonParameterToType[] parameters,
                                          JsonObjectMember[] properties)
            {
                Constructor = constructor;
                Parameters = parameters;
                Properties = properties;
            }
        }

        private class JsonParameterToType
        {
            internal JsonObjectMember Parameter { get; }
            internal Type TargetType { get; }

            internal JsonParameterToType(JsonObjectMember parameter, Type targetType)
            {
                Parameter = parameter;
                TargetType = targetType;
            }
        }
    }
}
