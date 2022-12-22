using System.Reflection;

namespace Vault;

public static class TypeUtils
{
    public static Type[] GetAllTypesDerivedFromType<BaseType>(Assembly assembly, bool includeBaseTypeInOutput = false)
    {
        return assembly.GetTypes().Where(t =>
        {
            if(t == typeof(BaseType) && includeBaseTypeInOutput == false)
            {
                return false;
            }
            return typeof(BaseType).IsAssignableFrom(t);
        }).ToArray();
    }
    
    public static Type[] GetAllInterfacesDerivedFromType<BaseType>(Assembly assembly, bool includeBaseTypeInOutput = false)
    {
        return assembly.GetTypes().Where(t =>
        {
            if(t.IsInterface == false)
            {
                return false;
            }
            
            if(t == typeof(BaseType) && includeBaseTypeInOutput == false)
            {
                return false;
            }
            return typeof(BaseType).IsAssignableFrom(t);
        }).ToArray();
    }
}