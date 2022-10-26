using VaultCore.CoreAPI;

namespace Vault;

//Simple global feature resolver that can be used across the application
public static class GlobalFeatures
{
    private static readonly FeatureResolver _globalFeatureResolver = new FeatureResolver();
    
    public static IFeatureResolver Resolver => _globalFeatureResolver;
    
    public static void RegisterFeature(IFeature featureToRegister)
    {
        _globalFeatureResolver.RegisterFeature(featureToRegister);
    }
}