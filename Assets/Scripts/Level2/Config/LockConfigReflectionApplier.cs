using System;
using System.Collections.Generic;
using System.Reflection;

public static class LockConfigReflectionApplier
{
    private const BindingFlags Flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

    public static void CopySharedFields(object source, object target)
    {
        if (source == null || target == null)
            return;

        Type sourceType = source.GetType();
        Type targetType = target.GetType();

        FieldInfo[] sourceFields = sourceType.GetFields(Flags);
        FieldInfo[] targetFields = targetType.GetFields(Flags);
        Dictionary<string, FieldInfo> targetFieldMap = new Dictionary<string, FieldInfo>(targetFields.Length, StringComparer.Ordinal);

        for (int i = 0; i < targetFields.Length; i++)
        {
            FieldInfo field = targetFields[i];
            if (field.IsStatic || field.IsInitOnly)
                continue;

            targetFieldMap[field.Name] = field;
        }

        for (int i = 0; i < sourceFields.Length; i++)
        {
            FieldInfo sourceField = sourceFields[i];
            if (sourceField.IsStatic)
                continue;

            if (!targetFieldMap.TryGetValue(sourceField.Name, out FieldInfo targetField))
                continue;

            if (!targetField.FieldType.IsAssignableFrom(sourceField.FieldType))
                continue;

            object value = sourceField.GetValue(source);
            targetField.SetValue(target, value);
        }
    }
}
