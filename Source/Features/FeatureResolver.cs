using VaultCore.CoreAPI;

namespace Vault;

public class FeatureResolver : IFeatureResolver
{
    private readonly Dictionary<Type, IFeature> _featureLookup = new();
    
    public void RegisterFeature(IFeature featureToRegister)
    {
        //Add the concrete types
        AddConcreteTypeMappingForFeature(featureToRegister.GetType(), featureToRegister);
        
        //And add the interfaces (except IFeature)
        foreach(var interfaceType in featureToRegister.GetType().GetInterfaces())
        {
            //ignore base type of all features
            if(interfaceType.IsAssignableTo(typeof(IFeature)) == false || interfaceType == typeof(IFeature))
            {
                continue;
            }
            
            AddConcreteTypeMappingForFeature(interfaceType, featureToRegister);
        }
    }
    
    public T? GetFeature<T>() where T : IFeature
    {
        if(typeof(T) == typeof(IFeature))
        {
            throw new InvalidOperationException($"IFeature is not a valid type to use to get a feature, use an derived type");
        }
        
        if(_featureLookup.TryGetValue(typeof(T), out var feature) == false)
        {
            return default;
        }
        
        return (T)feature;
    }
    
    private void AddConcreteTypeMappingForFeature(Type type, IFeature feature)
    {
        if(_featureLookup.ContainsKey(type))
        {
            throw new InvalidOperationException($"Type {type} already has concrete type mapped to it - " +
                                                $"Existing Concrete Type ({_featureLookup[type].GetType()})");
        }
            
        _featureLookup.Add(type, feature);
    }
}