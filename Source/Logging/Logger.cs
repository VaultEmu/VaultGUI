using System.Diagnostics;
using System.Text;
using NLog;
using NLog.Targets;

namespace Vault;

public class Logger : VaultCore.Features.ILogging
{
    private readonly NLog.Logger _loggerImpl;
    
    [ThreadStatic]
    private static StringBuilder? _stackTraceStringBuilder;
    
    [ThreadStatic]
    private static List<string>? _lineSplitCache;
    
    private static readonly string[] newLineCharToSplitOn = { "\r\n", "\r", "\n" };
    
    private static VaultGUIConsoleTarget? _vaultGuiConsoleTarget;
    
    public static VaultGUIConsoleTarget? VaultGuiConsoleTarget => _vaultGuiConsoleTarget;
    
    private ulong _messageIDCount;

    public Logger()
    {
        var config = new NLog.Config.LoggingConfiguration();

        var layout = "${date:format=HH\\:mm\\:ss}" +
                     "[${level:uppercase=true:format=FirstCharacter}]" +
                     ": ${message}";

        var logFile = new FileTarget("logfile")
        {
            FileName = "VaultLog.txt",
            Layout = layout
        };
        
        var logDebugger = new DebuggerTarget("log-debugger")
        {
            Layout = layout
        };
        
        _vaultGuiConsoleTarget = new VaultGUIConsoleTarget("log-vaultGuiConsole")
        {
            Layout = layout,
        };
        
        var logConsole = new ColoredConsoleTarget("log-console")
        {
            Layout = layout,
            UseDefaultRowHighlightingRules = false,
        };
        
        logConsole.RowHighlightingRules.Add(
            new ConsoleRowHighlightingRule("level == LogLevel.Fatal", ConsoleOutputColor.DarkRed, ConsoleOutputColor.NoChange));
        logConsole.RowHighlightingRules.Add(
            new ConsoleRowHighlightingRule("level == LogLevel.Error", ConsoleOutputColor.Red, ConsoleOutputColor.NoChange));
        logConsole.RowHighlightingRules.Add(
            new ConsoleRowHighlightingRule("level == LogLevel.Warn", ConsoleOutputColor.Yellow, ConsoleOutputColor.NoChange));
        logConsole.RowHighlightingRules.Add(
            new ConsoleRowHighlightingRule("level == LogLevel.Info", ConsoleOutputColor.White, ConsoleOutputColor.NoChange));
        logConsole.RowHighlightingRules.Add(
            new ConsoleRowHighlightingRule("level == LogLevel.Debug", ConsoleOutputColor.Cyan, ConsoleOutputColor.NoChange));

        config.AddRule(LogLevel.Debug, LogLevel.Fatal, logConsole);
        config.AddRule(LogLevel.Debug, LogLevel.Fatal, logFile);
        config.AddRule(LogLevel.Debug, LogLevel.Fatal, logDebugger);
        config.AddRule(LogLevel.Debug, LogLevel.Fatal, _vaultGuiConsoleTarget);
        
        LogManager.Configuration = config;
        
        _loggerImpl = LogManager.GetCurrentClassLogger();
    }
    
    public void Log(string message)
    {
        LogMessageInternal(message, null, false, LogLevel.Info);
    }
    
    public void LogDebug(string message, Exception? exception = null)
    {
        LogMessageInternal(message, exception, false, LogLevel.Debug);
    }
    
    public void LogWarning(string message, Exception? exception = null)
    {
        LogMessageInternal(message, exception, false, LogLevel.Warn);
    }
    
    public void LogError(string message, Exception? exception = null)
    {
        LogMessageInternal(message, exception, false, LogLevel.Error);
    }

    public void LogFatal(string message, Exception? exception = null, bool showStackTrace = true)
    {
        LogMessageInternal(message, exception, showStackTrace, LogLevel.Fatal);
    }
    
    private void LogMessageInternal(string message, Exception? exception, bool showStackTrace, LogLevel logLevel)
    {
        if(_lineSplitCache == null)
        {
            _lineSplitCache = new List<string>();
        }
        
        _lineSplitCache.Clear();
        _lineSplitCache.AddRange(message.Split(newLineCharToSplitOn, StringSplitOptions.None));
        
        if(exception != null)
        {
            var exceptionMessageSplit = exception.Message.Split(newLineCharToSplitOn, StringSplitOptions.None);
            
            for(var index = 0; index < exceptionMessageSplit.Length; ++index)
            {
                if(index == 0)
                {
                    _lineSplitCache.Add($"   Exception - {exception.GetType().Name}: {exceptionMessageSplit[index]}");
                }
                else
                {
                    _lineSplitCache.Add($"      {exceptionMessageSplit[index]}");
                }
            }
        }
        
        if(showStackTrace)
        {
            var stackTrace = GetStackTrace(exception);
            _lineSplitCache.AddRange(stackTrace.Split(newLineCharToSplitOn, StringSplitOptions.None));
        }
        
        var logEventInfo = new LogEventInfo(logLevel, null, "");
        logEventInfo.Properties.Add("MessageID", _messageIDCount++);
        
        foreach(var line in _lineSplitCache)
        {
            logEventInfo.Message = line;
            _loggerImpl.Log(logEventInfo);
        }
    }
    
    
    private string GetStackTrace(Exception? exception)
    {
        if(_stackTraceStringBuilder == null)
        {
            _stackTraceStringBuilder = new StringBuilder();
        }
        
        _stackTraceStringBuilder.Clear();

        //use the exception stacktrace, or generate our own (skipping the logging functions)
        var stackTrace = exception != null ?  new StackTrace(exception, true) : new StackTrace(3, true);

        var frames = stackTrace.GetFrames();
        for (var index = 0; index < frames.Length; index++)
        {
            var frame = frames[index];

            _stackTraceStringBuilder
                .Append(index == 0 ? "   at " : "      ");
                
            var method = frame.GetMethod();
            if(method == null)
            {
                _stackTraceStringBuilder.Append("?????");
            }
            else
            {
                _stackTraceStringBuilder
                    .Append(method.ReflectedType == null ? "?????" : method.ReflectedType.FullName)
                    .Append('.')
                    .Append(method.Name)
                    .Append('(');
                
                foreach(var param in method.GetParameters())
                {
                    _stackTraceStringBuilder
                        .Append(param.ParameterType)
                        .Append(' ')
                        .Append(param.Name);
                }
                
                _stackTraceStringBuilder.Append(')');
            }

            _stackTraceStringBuilder
                .Append(" [")
                .Append(frame.GetFileName())
                .Append(':')
                .Append(frame.GetFileLineNumber())
                .AppendLine("]");
        }
        
        return _stackTraceStringBuilder.ToString();
    }

    public void OnCoreAcquiresFeature(Type coreType, Type featureType, List<Type> allCoreFeaturesNeeded)
    {
        
    }

    public void OnCoreReleasesFeature(Type coreType, Type featureType, List<Type> allCoreFeaturesNeeded)
    {
       
    }
}