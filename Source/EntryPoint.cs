namespace Vault;

public static class EntryPoint
{
    private static void Main()
    {
        DpiAwareUtils.SetProcessDPIAware();
        
        var logger = new Logger();

        try
        {
            PrintLogHeader(logger);
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
    
    private static void PrintLogHeader(Logger logger)
    {
        logger.Log(@"+-----------------------------------------------------------------------------------+");
        logger.Log(@"|                    __     __          _ _      ____ _   _ ___                     |");
        logger.Log(@"|                    \ \   / /_ _ _   _| | |_   / ___| | | |_ _|                    |");
        logger.Log(@"|                     \ \ / / _` | | | | | __| | |  _| | | || |                     |");
        logger.Log(@"|                      \ V / (_| | |_| | | |_  | |_| | |_| || |                     |");
        logger.Log(@"|                       \_/ \__,_|\__,_|_|\__|  \____|\___/|___|                    |");
        logger.Log(@"+-----------------------------------------------------------------------------------+");
        logger.Log(@"| Vault GUI - Multi System Emulator - Chris Butler - Licensed under the MIT License |");
        logger.Log(@"+-----------------------------------------------------------------------------------+");
        logger.Log(@"");
    }
}

public class ShutdownDueToFatalErrorException : Exception
{
    public ShutdownDueToFatalErrorException() { }
    public ShutdownDueToFatalErrorException(string? message) : base(message) { }
    public ShutdownDueToFatalErrorException(string? message, Exception? innerException) : base(message, innerException) { }
}
