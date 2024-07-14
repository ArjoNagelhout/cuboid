// Copyright (c) 2023 Arjo Nagelhout

using System;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;
using Newtonsoft.Json;
using UnityEngine;

namespace Cuboid
{
    public static class JsonSerialization
    {
        /// <summary>
        /// Instead of having to define these settings each time, simply have them global here as a static variable
        /// </summary>
        public static JsonSerializerSettings GlobalSerializerSettings = new JsonSerializerSettings()
        {
            ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
            TypeNameHandling = TypeNameHandling.Auto,
            Converters = new List<JsonConverter>()
            {
                new Vector2Converter(),
                new Vector3Converter(),
                new QuaternionConverter(),
                new BindingConverter(),
                new BindingWithoutNewConverter()
            }
        };

        public static JsonSerializerSettings UserDataSerializerSettings => GlobalSerializerSettings;
    }

    public class BindingConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            if (!objectType.IsGenericType) { return false; }
            Type genericTypeDefinition = objectType.GetGenericTypeDefinition();
            return (genericTypeDefinition == typeof(Binding<>));
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            if (!objectType.IsGenericType) { throw new Exception(); }
            Type[] arguments = objectType.GetGenericArguments();
            if (arguments.Length == 0) { throw new Exception(); }
            Type valueType = arguments[0];
            if (valueType == null) { return new Exception(); }
            
            object val = serializer.Deserialize(reader, valueType);
            PropertyInfo property = objectType.GetProperty("Value");
            if (existingValue == null)
            {
                existingValue = Activator.CreateInstance(objectType);
            }
            property.SetValue(existingValue, val);
            return existingValue;
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            Type type = value.GetType();
            PropertyInfo property = type.GetProperty("Value");
            object val = property.GetValue(value);
            serializer.Serialize(writer, val);
        }
    }

    public class BindingWithoutNewConverter : BindingConverter
    {
        public override bool CanConvert(Type objectType)
        {
            if (!objectType.IsGenericType) { return false; }
            Type genericTypeDefinition = objectType.GetGenericTypeDefinition();
            return (genericTypeDefinition == typeof(BindingWithoutNew<>));
        }
    }

    public class Vector2Converter : JsonConverter<Vector2>
    {
        public override Vector2 ReadJson(JsonReader reader, Type objectType, Vector2 existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            if (existingValue == null)
            {
                existingValue = Vector2.zero;
            }

            float[] values = serializer.Deserialize<float[]>(reader);
            existingValue = new Vector2(values[0], values[1]);

            return existingValue;
        }

        public override void WriteJson(JsonWriter writer, Vector2 value, JsonSerializer serializer)
        {
            // write as an array
            float[] values = new float[2] { value.x, value.y };
            serializer.Serialize(writer, values);
        }
    }

    public class Vector3Converter : JsonConverter<Vector3>
    {
        public override Vector3 ReadJson(JsonReader reader, Type objectType, Vector3 existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            if (existingValue == null)
            {
                existingValue = Vector3.zero;
            }

            float[] values = serializer.Deserialize<float[]>(reader);
            existingValue = new Vector3(values[0], values[1], values[2]);

            return existingValue;
        }

        public override void WriteJson(JsonWriter writer, Vector3 value, JsonSerializer serializer)
        {
            // write as an array
            float[] values = new float[3] { value.x, value.y, value.z };
            serializer.Serialize(writer, values);
        }
    }

    public class QuaternionConverter : JsonConverter<Quaternion>
    {
        public override Quaternion ReadJson(JsonReader reader, Type objectType, Quaternion existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            if (existingValue == null)
            {
                existingValue = Quaternion.identity;
            }

            float[] values = serializer.Deserialize<float[]>(reader);
            existingValue = new Quaternion(values[0], values[1], values[2], values[3]);

            return existingValue;
        }

        public override void WriteJson(JsonWriter writer, Quaternion value, JsonSerializer serializer)
        {
            // write as an array
            float[] values = new float[4] { value.x, value.y, value.z, value.w };
            serializer.Serialize(writer, values);
        }
    }
}
