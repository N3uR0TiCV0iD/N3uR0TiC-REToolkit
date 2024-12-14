using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Reflection;
using System.Diagnostics;
using System.Collections.Generic;

public static class REReflectionHelper
{
    public enum EzBindingFlags
    {
        PrivateProtected_Instance = BindingFlags.NonPublic | BindingFlags.Instance,
        PrivateProtected_Static = BindingFlags.NonPublic | BindingFlags.Static,
        PublicInternal_Instance = BindingFlags.Public | BindingFlags.Instance,
        PublicInternal_Static = BindingFlags.Public | BindingFlags.Static
    }

    public class FieldInfo<T, TField>
    {
        readonly FieldInfo fieldInfo;

        public FieldInfo(FieldInfo fieldInfo)
        {
            if (fieldInfo == null)
            {
                throw new ArgumentNullException(nameof(fieldInfo));
            }
            this.fieldInfo = fieldInfo;
        }

        public TField GetValue(T instance)
        {
            return (TField)fieldInfo.GetValue(instance);
        }

        public void SetValue(T instance, TField value)
        {
            fieldInfo.SetValue(instance, value);
        }
    }

    public class MethodInfo<T, TReturn>
    {
        readonly MethodInfo methodInfo;

        public MethodInfo(MethodInfo methodInfo)
        {
            if (methodInfo == null)
            {
                throw new ArgumentNullException(nameof(methodInfo));
            }
            this.methodInfo = methodInfo;
        }

        public TReturn Invoke(T instance, params object[] parameters)
        {
            return (TReturn)methodInfo.Invoke(instance, parameters);
        }
    }

    public static FieldInfo<T, TField> GetFieldInfo<T, TField>(string fieldName, EzBindingFlags bindingFlags)
    {
        return GetFieldInfo<T, TField>(typeof(T), fieldName, bindingFlags);
    }

    public static FieldInfo<T, TField> GetFieldInfo<T, TField>(Type type, string fieldName, EzBindingFlags bindingFlags)
    {
        var fieldInfo = type.GetField(fieldName, (BindingFlags)bindingFlags);
        if (fieldInfo == null)
        {
            var message = $"Field '{fieldName}' not found in type '{type.FullName}'.";
            throw new MissingFieldException(message);
        }
        return new FieldInfo<T, TField>(fieldInfo);
    }

    public static MethodInfo<T, TReturn> GetMethodInfo<T, TReturn>(string methodName, EzBindingFlags bindingFlags)
    {
        return GetMethodInfo<T, TReturn>(methodName, bindingFlags, 0, null);
    }

    public static MethodInfo<T, TReturn> GetMethodInfo<T, TReturn>(string methodName, EzBindingFlags bindingFlags, int genericParameterCount)
    {
        return GetMethodInfo<T, TReturn>(methodName, bindingFlags, genericParameterCount, null);
    }

#if NET5_0_OR_GREATER

    public static MethodInfo<T, TReturn> GetMethodInfo<T, TReturn>(string methodName, EzBindingFlags bindingFlags, int genericParameterCount, params Type[] argumentTypes)
    {
        var methodInfo = typeof(T).GetMethod(methodName, genericParameterCount, (BindingFlags)bindingFlags, Type.DefaultBinder, argumentTypes, null);
        ThrowIfMethodMissing<T>(methodInfo, methodName, genericParameterCount);
        return new MethodInfo<T, TReturn>(methodInfo);
    }

#else

    public static MethodInfo<T, TReturn> GetMethodInfo<T, TReturn>(string methodName, EzBindingFlags bindingFlags, int genericParameterCount, params Type[] argumentTypes)
    {
        if (genericParameterCount == 0)
        {
            var methodInfo = typeof(T).GetMethod(methodName, (BindingFlags)bindingFlags, Type.DefaultBinder, argumentTypes, null);
            ThrowIfMethodMissing<T>(methodInfo, methodName, 0);
            return new MethodInfo<T, TReturn>(methodInfo);
        }

        var genericMethodInfo = GetGenericMethod(typeof(T), methodName, genericParameterCount, argumentTypes);
        ThrowIfMethodMissing<T>(genericMethodInfo, methodName, genericParameterCount);

        return genericMethodInfo != null ? new MethodInfo<T, TReturn>(genericMethodInfo) : null;
    }

    //Generic method search for older frameworks
    private static MethodInfo GetGenericMethod(Type type, string methodName, int genericParameterCount, Type[] argumentTypes)
    {
        return type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static)
                   .Where(m => m.Name == methodName && m.IsGenericMethodDefinition && m.GetGenericArguments().Length == genericParameterCount)
                   .FirstOrDefault(m =>
                   {
                       var parameters = m.GetParameters();
                       if (parameters.Length != argumentTypes.Length) return false;

                       return !parameters.Where((t, i) => t.ParameterType != argumentTypes[i]).Any();
                   });
    }

#endif

    private static void ThrowIfMethodMissing<T>(MethodInfo methodInfo, string methodName, int genericParameterCount)
    {
        if (methodInfo == null)
        {
            var typeName = typeof(T).FullName;
            var message = $"Method '{methodName}' with {genericParameterCount} generic parameter(s) not found in type '{typeName}'.";
            throw new MissingMethodException(typeName, message);
        }
    }
}
