namespace Vault;

public static class EntryPoint
{ 
    
    
    private static void Main()
    {
        DpiAwareUtils.SetProcessDPIAware();
        
        var logger = new Logger();
        try
        {
            var vaultGui = new VaultGui(logger);
            vaultGui.Run();
        }
        catch (ShutdownDueToFatalErrorException e)
        {
            logger.LogFatal($"Shutting down due to Fatal Error: {e.Message}", null, false);
        }
        catch (Exception e )
        {
            logger.LogFatal("Unhandled Exception Thrown", e);
        }
        
    }
}

public class ShutdownDueToFatalErrorException : Exception
{
    public ShutdownDueToFatalErrorException() { }
    public ShutdownDueToFatalErrorException(string? message) : base(message) { }
    public ShutdownDueToFatalErrorException(string? message, Exception? innerException) : base(message, innerException) { }
}