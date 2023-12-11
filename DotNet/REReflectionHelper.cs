using System;
using System.IO;
using System.Text;
using System.Reflection;
using System.Diagnostics;
using System.Collections.Generic;

public static class REReflectionHelper
{
    public enum EzBindingFlags
    {
        PrivateProtected = BindingFlags.NonPublic | BindingFlags.Instance,
        PrivateProtectedStatic = BindingFlags.NonPublic | BindingFlags.Static,
        Internal = BindingFlags.Public | BindingFlags.Instance,
        InternalStatic =  BindingFlags.Public | BindingFlags.Static
    }

    public class FieldInfo<T, TReturn>
    {
        readonly FieldInfo fieldInfo;

        public FieldInfo(FieldInfo fieldInfo)
        {
            this.fieldInfo = fieldInfo;
        }

        public TReturn GetValue(T instance)
        {
            return (TReturn)fieldInfo.GetValue(instance);
        }
    }

    public class MethodInfo<T, TReturn>
    {
        readonly MethodInfo methodInfo;

        public MethodInfo(MethodInfo methodInfo)
        {
            this.methodInfo = methodInfo;
        }

        public TReturn Invoke(T instance, params object[] parameters)
        {
            return (TReturn)methodInfo.Invoke(instance, parameters);
        }
    }

    public static FieldInfo<T, TReturn> GetFieldInfo<T, TReturn>(string fieldName, EzBindingFlags bindingFlags)
    {
        var fieldInfo = typeof(T).GetField(fieldName, (BindingFlags)bindingFlags);
        return new FieldInfo<T, TReturn>(fieldInfo);
    }

    public static MethodInfo<T, TReturn> GetMethodInfo<T, TReturn>(string methodName, EzBindingFlags bindingFlags)
    {
        return GetMethodInfo<T, TReturn>(methodName, bindingFlags, 0, null);
    }

    public static MethodInfo<T, TReturn> GetMethodInfo<T, TReturn>(string methodName, EzBindingFlags bindingFlags, int genericParameterCount)
    {
        return GetMethodInfo<T, TReturn>(methodName, bindingFlags, genericParameterCount, null);
    }

    public static MethodInfo<T, TReturn> GetMethodInfo<T, TReturn>(string methodName, EzBindingFlags bindingFlags, int genericParameterCount, params Type[] argumentTypes)
    {
        var methodInfo = typeof(T).GetMethod(methodName, genericParameterCount, (BindingFlags)bindingFlags, Type.DefaultBinder, argumentTypes, null);
        return new MethodInfo<T, TReturn>(methodInfo);
    }
}
