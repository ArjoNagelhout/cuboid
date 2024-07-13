using System;
using System.Collections;
using System.Collections.Generic;
using Cuboid.UI;
using System.Reflection;
using UnityEngine;

namespace Cuboid
{
    public class EnumDataAttribute : Attribute
    {
        public string Name = null;
        public string Icon = null;
    }

    public static class EnumUtils
    {
        public static void GetEnumData<T>(T value, out string text, out Sprite icon)
        {
            Type enumType = typeof(T);

            string name = Enum.GetName(enumType, value);
            FieldInfo fieldInfo = enumType.GetField(name);
            EnumDataAttribute data = fieldInfo.GetCustomAttribute<EnumDataAttribute>();

            // use overwrite name
            text = (data != null && data.Name != null) ? data.Name : name;
            icon = (data != null && data.Icon != null) ? GetEnumDataIcon(data.Icon) : null;
        }

        private static Sprite GetEnumDataIcon(string iconName)
        {
            IconsScriptableObject icons = Icons.Data;
            Type iconsType = typeof(IconsScriptableObject);
            FieldInfo field = iconsType.GetField(iconName);
            if (field == null)
            {
                Debug.LogError("Icon name does not exist in IconsScriptableObject");
                return null;
            }
            Sprite icon = (Sprite)field.GetValue(icons);
            if (icon == null)
            {
                Debug.LogError("Icon was not assigned");
                return null;
            }
            return icon;
        }
    }
}
