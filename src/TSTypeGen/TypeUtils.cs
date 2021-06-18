using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace TSTypeGen
{
    internal static class TypeUtils
    {
        public static string GetNameWithoutGenericArity(Type t)
        {
            var name = t.Name;
            var index = name.IndexOf('`');
            return index == -1 ? name : name.Substring(0, index);
        }

        public static bool Is<T>(Type type)
        {
            if (type == null)
                return false;

            return Equals(typeof(T), type);
        }

        public static bool Equals(Type type1, Type type2)
        {
            if (type1 == null && type2 == null)
                return false;

            return type1?.FullName == type2?.FullName;
        }

        public static string GetFullName(Type type)
        {
            var fullName = type.FullName;
            if ((fullName == null || fullName.Contains('`')) && type.Name != null && type.Namespace != null)
                fullName = type.Namespace + "." + GetNameWithoutGenericArity(type);

            return fullName;
        }

        public static List<PropertyInfo> GetRelevantProperties(Type type)
        {
            return type
                .GetProperties(BindingFlags.Instance | BindingFlags.GetProperty | BindingFlags.Public | BindingFlags.DeclaredOnly)
                .Where(p => p.GetIndexParameters().Length == 0)
                .ToList();
        }

        public static List<CustomAttributeData> GetCustomAttributesData(Type type)
        {
            try
            {
                return type.GetCustomAttributesData().Select(a =>
                {
                    try
                    {
                        var x = a.AttributeType;
                        return a;
                    }
                    catch
                    {
                        return null;
                    }
                }).Where(a => a != null).ToList();
            }
            catch
            {
                return new List<CustomAttributeData>();
            }
        }

        public static List<CustomAttributeData> GetCustomAttributesData(PropertyInfo propertyInfo)
        {
            try
            {
                return propertyInfo.GetCustomAttributesData().Select(a =>
                {
                    try
                    {
                        var x = a.AttributeType;
                        return a;
                    }
                    catch
                    {
                        return null;
                    }
                }).Where(a => a != null).ToList();
            }
            catch
            {
                return new List<CustomAttributeData>();
            }
        }

        public static List<CustomAttributeData> GetAssemblyCustomAttributesData(Assembly asm)
        {
            try
            {
                return asm.GetCustomAttributesData().Select(a =>
                {
                    try
                    {
                        var x = a.AttributeType;
                        return a;
                    }
                    catch
                    {
                        return null;
                    }
                }).Where(a => a != null).ToList();
            }
            catch
            {
                return new List<CustomAttributeData>();
            }
        }

        public static List<Type> GetAssemblyTypes(Assembly asm)
        {
            try
            {
                // TODO: There's a bunch of nested types with names starting with `<` that doesn't make sense.
                // Not sure what they are so we just ignore them.
                return asm.GetTypes().Where(t => !t.Name.StartsWith("<")).ToList();
            }
            catch
            {
                return new List<Type>();
            }
        }
    }
}
