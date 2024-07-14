//
// Cuboid (http://github.com/arjonagelhout/cuboid)
// Copyright (c) 2023 Arjo Nagelhout
//

using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace Cuboid
{
    /// <summary>
    /// Represents a collection of user data that can be saved to and loaded from a JSON file.
    ///
    /// Contains static methods so that you don't need a reference to the instance.
    /// </summary>
    public sealed class UserData : MonoBehaviour
    {
        private static UserData _instance;

        private const string k_UserDataFileName = "userdata.json";
        private static string _userDataFilePath => Path.Combine(Application.persistentDataPath, k_UserDataFileName);

        private Dictionary<string, object> _userdata;

        private void Awake()
        {
            // Singleton implementation
            if (_instance != null && _instance != this) { Destroy(this); } else { _instance = this; }

            _userdata = Load();
        }

        /// <summary>
        /// Store a preference with a given key of type T
        /// </summary>
        public static void Set<T>(string key, T value)
        {
            if (_instance == null)
            {
                return;
            }
            _instance.SetInternal<T>(key, value);
        }

        private void SetInternal<T>(string key, T value)
        {
            _userdata[key] = value;
        }

        /// <summary>
        /// Get a stored preference with a given key of type T
        /// </summary>
        public static bool TryGetValue<T>(string key, out T value)
        {
            if (_instance == null)
            {
                value = default(T);
                return false;
            }
            return _instance.TryGetValueInternal<T>(key, out value);
        }

        private static T GetValueInternal<T>(object obj)
        {
            if (obj is T)
            {
                return (T)obj;
            }

            if (typeof(T).IsEnum)
            {
                // make sure to cast the obj (which could be a long etc.)
                // to the type of the enum
                Enum referenceT = default(T) as Enum;
                return (T)Convert.ChangeType(obj, referenceT.GetTypeCode());
            }
            // HACK
            // the following additions are because we can't change the Vector2, Vector3 and Quaternion types
            // but, we do want them to be serializable using the UserData and StoredBinding.
            //
            // However, the user data types are stored as objects, and the type annotations will
            // thus be changed to their serialized counterparts. In the case of Vector2 and Vector3 this is an array
            // of floats (or System.Single).
            //
            // Unfortunately, we can't add an implicit or explicit cast operator to these types, because we don't own
            // either the System.Single[] type or the Vector2, Vector3 and Quaternion types.
            //
            // And even the fucking array doesn't get serialized properly to a System.Single[]. Because
            // it's a JArray...
            else if (typeof(T) == typeof(Vector2) && obj is JArray)
            {
                float[] values = (obj as JArray).ToObject<float[]>();
                obj = new Vector2(values[0], values[1]);
            }
            else if (typeof(T) == typeof(Vector3) && obj is JArray)
            {
                float[] values = (obj as JArray).ToObject<float[]>();
                obj = new Vector3(values[0], values[1], values[2]);
            }
            else if (typeof(T) == typeof(Quaternion) && obj is JArray)
            {
                float[] values = (obj as JArray).ToObject<float[]>();
                obj = new Quaternion(values[0], values[1], values[2], values[3]);
            }

            try
            {
                return (T)Convert.ChangeType(obj, typeof(T));
            }
            catch (InvalidCastException)
            {
                return default(T);
            }
        }

        private bool TryGetValueInternal<T>(string key, out T value)
        {
            bool hasValue = (_userdata.TryGetValue(key, out object obj));

            value = hasValue ? GetValueInternal<T>(obj) : default(T);

            return hasValue;
        }

        public static void Save() => _instance.Save(_userDataFilePath);

        /// <summary>
        /// Saves the user data to the specified file in JSON format.
        /// </summary>
        /// <param name="filePath">The path of the file to save the user data to.</param>
        private void Save(string filePath)
        {
            string json = JsonConvert.SerializeObject(_userdata, Formatting.Indented, JsonSerialization.UserDataSerializerSettings);
            
            File.WriteAllText(filePath, json);
        }

        private Dictionary<string, object> Load() => Load(_userDataFilePath);

        /// <summary>
        /// Loads the user data from the specified file in JSON format.
        /// </summary>
        private Dictionary<string, object> Load(string filePath)
        {
            if (File.Exists(filePath))
            {
                string json = File.ReadAllText(filePath);
                Dictionary<string, object> deserializedPreferences = JsonConvert.DeserializeObject<Dictionary<string, object>>(json, JsonSerialization.UserDataSerializerSettings);

                if (deserializedPreferences != null)
                {
                    return deserializedPreferences;
                }
            }
            return new Dictionary<string, object>();
        }
    }
}
