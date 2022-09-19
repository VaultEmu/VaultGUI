namespace Vault;

//Use this to get access to various subsystems your emulation core may need
public static class SubsystemController
{
    private static readonly Dictionary<Type, ISubsystem> _subsystemLookup = new();

    public static void RegisterSubsystem(ISubsystem subsystemToRegister)
    {
        //Add the concrete types
        AddConcreteTypeMappingForSubsystem(subsystemToRegister.GetType(), subsystemToRegister);
        
        //And add the interfaces (except ISubsystem)
        foreach(var interfaceType in subsystemToRegister.GetType().GetInterfaces())
        {
            //ignore base type of all subsystems
            if(interfaceType == typeof(ISubsystem))
            {
                continue;
            }
            
            AddConcreteTypeMappingForSubsystem(interfaceType, subsystemToRegister);
        }
    }

    public static T GetSubsystem<T>() where T : ISubsystem
    {
        if(typeof(T) == typeof(ISubsystem))
        {
            throw new InvalidOperationException($"ISubsystem is not a valid type to use to get a subsystem, use an derived type");
        }
        
        if(_subsystemLookup.TryGetValue(typeof(T), out var subSystem) == false)
        {
            throw new InvalidOperationException($"No concrete type for subsystem type {typeof(T)} registered");
        }
        
        return (T)subSystem;
    }
    
    private static void AddConcreteTypeMappingForSubsystem(Type type, ISubsystem subsystem)
    {
        if(_subsystemLookup.ContainsKey(type))
        {
            throw new InvalidOperationException($"Type {type} already has concrete type mapped to it - " +
                                                $"Existing Concrete Type ({_subsystemLookup[type].GetType()})");
        }
            
        _subsystemLookup.Add(type, subsystem);
    }
}