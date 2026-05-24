using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace StartAsAnyone
{
    internal static class ReflectionHelper
    {
        private static MethodInfo GetMethodInfo(Type type, string methodName, Type[] parameterTypes)
        {
            MethodInfo methodInfo = null;
            do
            {
                methodInfo = type.GetMethod(methodName,
                       BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic,
                       null, parameterTypes, null);
                type = type.BaseType;
            }
            while (methodInfo == null && type != null);
            return methodInfo;
        }

        public static object CallMethod(this object obj, string methodName, params object[] parameters)
        {
            if (obj == null)
            {
                throw new ArgumentNullException(nameof(obj));
            }

            Type objType = obj.GetType();
            Type[] paramTypes = parameters.Select(p => p.GetType()).ToArray();
            MethodInfo methodInfo = GetMethodInfo(objType, methodName, paramTypes);

            if (methodInfo == null)
            {
                throw new ArgumentOutOfRangeException(nameof(methodName),
                  string.Format("Couldn't find method {0} in type {1}", methodName, objType.FullName));
            }

            return methodInfo.Invoke(obj, parameters);
        }

        public static object CallMethod(Type type, object obj, string methodName, params object[] parameters)
        {
            if (type == null)
            {
                throw new ArgumentNullException(nameof(type));
            }

            Type[] paramTypes = parameters.Select(p => p.GetType()).ToArray();
            MethodInfo methodInfo = GetMethodInfo(type, methodName, paramTypes);

            if (methodInfo == null)
            {
                throw new ArgumentOutOfRangeException(nameof(methodName),
                  string.Format("Couldn't find method {0} in type {1}", methodName, type.FullName));
            }

            return methodInfo.Invoke(obj, parameters);
        }

        private static PropertyInfo GetPropertyInfo(Type type, string propertyName)
        {
            PropertyInfo propInfo = null;
            do
            {
                propInfo = type.GetProperty(propertyName,
                       BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
                type = type.BaseType;
            }
            while (propInfo == null && type != null);
            return propInfo;
        }

        public static object GetPropertyValue(this object obj, string propertyName)
        {
            if (obj == null)
            {
                throw new ArgumentNullException("obj");
            }

            Type objType = obj.GetType();
            PropertyInfo propInfo = GetPropertyInfo(objType, propertyName);
            if (propInfo == null)
            {
                throw new ArgumentOutOfRangeException("propertyName",
                  string.Format("Couldn't find property {0} in type {1}", propertyName, objType.FullName));
            }

            return propInfo.GetValue(obj, null);
        }

        public static void SetPropertyValue(this object obj, string propertyName, object val)
        {
            if (obj == null)
            {
                throw new ArgumentNullException("obj");
            }

            Type objType = obj.GetType();
            PropertyInfo propInfo = GetPropertyInfo(objType, propertyName);
            if (propInfo == null)
            {
                throw new ArgumentOutOfRangeException("propertyName",
                  string.Format("Couldn't find property {0} in type {1}", propertyName, objType.FullName));
            }

            propInfo.SetValue(obj, val, null);
        }

        public static object GetPropertyValue(Type type, object obj, string propertyName)
        {
            if (type == null)
            {
                throw new ArgumentNullException(nameof(type));
            }

            PropertyInfo propInfo = GetPropertyInfo(type, propertyName);
            return propInfo.GetValue(obj);
        }

        public static void SetPropertyValue(Type type, object obj, string propertyName, object value)
        {
            if (type == null)
            {
                throw new ArgumentNullException(nameof(type));
            }

            PropertyInfo propInfo = GetPropertyInfo(type, propertyName);
            propInfo.SetValue(obj, value);
        }
    }
}