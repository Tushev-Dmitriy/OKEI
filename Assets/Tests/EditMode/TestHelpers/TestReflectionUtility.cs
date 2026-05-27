using System;
using System.Reflection;

internal static class TestReflectionUtility
{
    private const BindingFlags InstanceFlags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
    private const BindingFlags StaticFlags = BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;

    public static void SetPrivateField<TTarget>(TTarget target, string fieldName, object value)
    {
        FieldInfo field = typeof(TTarget).GetField(fieldName, InstanceFlags);
        if (field == null)
        {
            throw new MissingFieldException(typeof(TTarget).FullName, fieldName);
        }

        field.SetValue(target, value);
    }

    public static void SetPrivateField(object target, Type targetType, string fieldName, object value)
    {
        FieldInfo field = targetType.GetField(fieldName, InstanceFlags);
        if (field == null)
        {
            throw new MissingFieldException(targetType.FullName, fieldName);
        }

        field.SetValue(target, value);
    }

    public static object GetPrivateField<TTarget>(TTarget target, string fieldName)
    {
        FieldInfo field = typeof(TTarget).GetField(fieldName, InstanceFlags);
        if (field == null)
        {
            throw new MissingFieldException(typeof(TTarget).FullName, fieldName);
        }

        return field.GetValue(target);
    }

    public static object GetPrivateField(object target, Type targetType, string fieldName)
    {
        FieldInfo field = targetType.GetField(fieldName, InstanceFlags);
        if (field == null)
        {
            throw new MissingFieldException(targetType.FullName, fieldName);
        }

        return field.GetValue(target);
    }

    public static TResult InvokePrivateMethod<TTarget, TResult>(TTarget target, string methodName, params object[] args)
    {
        MethodInfo method = typeof(TTarget).GetMethod(methodName, InstanceFlags);
        if (method == null)
        {
            throw new MissingMethodException(typeof(TTarget).FullName, methodName);
        }

        return (TResult)method.Invoke(target, args);
    }

    public static void InvokePrivateMethod<TTarget>(TTarget target, string methodName, params object[] args)
    {
        MethodInfo method = typeof(TTarget).GetMethod(methodName, InstanceFlags);
        if (method == null)
        {
            throw new MissingMethodException(typeof(TTarget).FullName, methodName);
        }

        method.Invoke(target, args);
    }

    public static object InvokeMethod(object target, Type targetType, string methodName, params object[] args)
    {
        MethodInfo method = targetType.GetMethod(methodName, InstanceFlags);
        if (method == null)
        {
            throw new MissingMethodException(targetType.FullName, methodName);
        }

        return method.Invoke(target, args);
    }

    public static object InvokeStaticMethod(Type targetType, string methodName, params object[] args)
    {
        MethodInfo method = targetType.GetMethod(methodName, StaticFlags);
        if (method == null)
        {
            throw new MissingMethodException(targetType.FullName, methodName);
        }

        return method.Invoke(null, args);
    }

    public static void SetPrivateStaticField<TTarget>(string fieldName, object value)
    {
        FieldInfo field = typeof(TTarget).GetField(fieldName, StaticFlags);
        if (field == null)
        {
            throw new MissingFieldException(typeof(TTarget).FullName, fieldName);
        }

        field.SetValue(null, value);
    }
}
