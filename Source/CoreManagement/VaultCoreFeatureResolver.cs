using VaultCore.CoreAPI;

namespace Vault;

public class VaultCoreFeatureResolver : IVaultCoreFeatureResolver
{
    private readonly Dictionary<Type, IVaultCoreFeature> _featureLookup = new();
    
    public void RegisterFeatureImplementation(IVaultCoreFeature featureToRegister)
    {
        //Add the concrete types
        AddConcreteTypeMappingForFeature(featureToRegister.GetType(), featureToRegister);
        
        //And add the interfaces (except IFeature)
        foreach(var interfaceType in featureToRegister.GetType().GetInterfaces())
        {
            //ignore base type of all features
            if(interfaceType.IsAssignableTo(typeof(IVaultCoreFeature)) == false || interfaceType == typeof(IVaultCoreFeature))
            {
                continue;
            }
            
            AddConcreteTypeMappingForFeature(interfaceType, featureToRegister);
        }
    }
    
    public IVaultCoreFeature? GetCoreFeatureImplementation(Type vaultCoreType)
    {
        if(vaultCoreType == typeof(IVaultCoreFeature))
        {
            throw new InvalidOperationException($"IVaultCoreFeature is not a valid type to use to get a feature, use an derived type");
        }
        
        if(typeof(IVaultCoreFeature).IsAssignableFrom(vaultCoreType) == false)
        {
            throw new InvalidOperationException($"Type {vaultCoreType} is not a type that derives from IVaultCoreFeature");
        }
        
        if(_featureLookup.TryGetValue(vaultCoreType, out var feature) == false)
        {
            return null;
        }
        
        return feature;
    }
    
    private void AddConcreteTypeMappingForFeature(Type type, IVaultCoreFeature feature)
    {
        if(_featureLookup.TryGetValue(type, out var value))
        {
            throw new InvalidOperationException($"Type {type} already has concrete type mapped to it - " +
                                                $"Existing Concrete Type ({value.GetType()})");
        }
            
        _featureLookup.Add(type, feature);
    }

}