using System.Diagnostics;
using System.Text;
using NLog;
using NLog.Targets;

namespace Vault;

public class Logger : VaultCore.CoreAPI.ILogger
{
    private readonly NLog.Logger _loggerImpl;
    private readonly StringBuilder _stackTraceStringBuilder = new StringBuilder();
    public Logger()
    {
        var config = new NLog.Config.LoggingConfiguration();
        
        var layout = "${time}:" +
                     "${when:when=level!=LogLevel.Info:inner=[${level:uppercase=true:format=FirstCharacter}]} " +
                     "${message:withexception=true:exceptionSeparator= - }" +
                     "${event-properties:item=stacktrace}";

        var logFile = new FileTarget("logfile")
        {
            FileName = "VaultLog.txt",
            Layout = layout
        };
        
        var logDebugger = new DebuggerTarget("log-debugger")
        {
            Layout = layout
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
        
        LogManager.Configuration = config;
        
        _loggerImpl = LogManager.GetCurrentClassLogger();
        
        GlobalFeatures.RegisterFeature(this);
    }
    
    
    public void Log(string message)
    {
        _loggerImpl.Info(message);
    }
    
    public void LogDebug(string message, Exception? exception = null)
    {
        if(exception == null)
        {
            _loggerImpl.Debug(message);
        }
        else
        {
            _loggerImpl.Debug($"{message} - {exception.GetType().Name}: {exception.Message}");
        }
    }
    
    public void LogWarning(string message, Exception? exception = null)
    {
        if(exception == null)
        {
            _loggerImpl.Warn(message);
        }
        else
        {
            _loggerImpl.Warn($"{message} - {exception.GetType().Name}: {exception.Message}");
        }
    }
    
    public void LogError(string message, Exception? exception = null)
    {
        if(exception == null)
        {
            _loggerImpl.Error(message);
        }
        else
        {
            _loggerImpl.Error($"{message} - {exception.GetType().Name}: {exception.Message}");
        }
    }

    public void LogFatal(string message, Exception? exception = null, bool showStackTrace = true)
    {
        if(showStackTrace)
        {
            var stackTrace = GetStackTrace(exception);
        
            if(exception == null)
            {
                _loggerImpl.WithProperty("stacktrace", stackTrace).Fatal(message);
            }
            else
            {
                _loggerImpl.WithProperty("stacktrace", stackTrace).Fatal($"{message} - {exception.GetType().Name}: {exception.Message}");
            }
        }
        else
        {
            if(exception == null)
            {
                _loggerImpl.Fatal(message);
            }
            else
            {
                _loggerImpl.Fatal(exception, message);
            }
        }
        
    }
    
    private string GetStackTrace(Exception? exception)
    {
        _stackTraceStringBuilder.Clear();
        _stackTraceStringBuilder.AppendLine();
        
        //use the exception stacktrace, or generate our own (skipping the logging functions)
        var stackTrace = exception != null ?  new StackTrace(exception, true) : new StackTrace(2, true);

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
                //var fullName = string.Format("{0}.{1}({2})", method.ReflectedType.FullName, method.Name,
                //string.Join(",", method.GetParameters().Select(o => string.Format("{0} {1}", o.ParameterType, o.Name)).ToArray()));
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
}