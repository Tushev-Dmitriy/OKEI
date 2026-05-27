using System;

internal static class AssemblyTypeUtility
{
    public static Type ResolveGameplayType(string typeName)
    {
        Type resolvedType = Type.GetType($"{typeName}, Assembly-CSharp");
        if (resolvedType == null)
        {
            throw new TypeLoadException($"Could not resolve gameplay type '{typeName}' from Assembly-CSharp.");
        }

        return resolvedType;
    }
}
