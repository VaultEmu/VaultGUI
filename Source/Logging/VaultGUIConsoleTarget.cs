using NLog;
using NLog.Targets;

namespace Vault;

public class VaultGUIConsoleTarget : TargetWithLayout
{
    public struct ConsoleLogMessage
    {
        public ulong MessageID;
        public string message;
        public LogLevel LogLevel;
    }

    private readonly List<ConsoleLogMessage> _allConsoleLogMessages = new();
    private readonly List<ConsoleLogMessage> _filteredLogMessages = new();
    private string _filterText;

    private bool _debugMessagesVisible = true;
    private bool _infoMessagesVisible = true;
    private bool _warningMessagesVisible = true;
    private bool _errorMessagesVisible = true;
    private bool _fatalMessagesVisible = true;
    

    public IReadOnlyList<ConsoleLogMessage> AllConsoleLogMessages => _allConsoleLogMessages;
    public IReadOnlyList<ConsoleLogMessage> FilteredConsoleLogMessages => _filteredLogMessages;
    
    public bool DebugMessagesVisible
    {
        get => _debugMessagesVisible;
        set
        {
            _debugMessagesVisible = value;
            UpdateFilter();
        }
    }
    
    public bool InfoMessagesVisible
    {
        get => _infoMessagesVisible;
        set
        {
            _infoMessagesVisible = value;
            UpdateFilter();
        }
    }
    
    public bool WarningMessagesVisible
    {
        get => _warningMessagesVisible;
        set
        {
            _warningMessagesVisible = value;
            UpdateFilter();
        }
    }
    
    public bool ErrorMessagesVisible
    {
        get => _errorMessagesVisible;
        set
        {
            _errorMessagesVisible = value;
            UpdateFilter();
        }
    }
    
    public bool FatalMessagesVisible
    {
        get => _fatalMessagesVisible;
        set
        {
            _fatalMessagesVisible = value;
            UpdateFilter();
        }
    }

    public string FilterText
    {
        get => _filterText;
        set
        {
            _filterText = value;
            UpdateFilter();
        }
    }

    public VaultGUIConsoleTarget(string name)
    {
        Name = name;
        _filterText = "";
    }
    
    public void ClearConsole()
    {
        _allConsoleLogMessages.Clear();
        _filteredLogMessages.Clear();
    }

    public void GetMessageCount(
        out int debugMessageCount,
        out int infoMessageCount,
        out int warningMessageCount,
        out int errorMessageCount,
        out int fatalMessageCount)
    {
        debugMessageCount = 0;
        infoMessageCount = 0;
        warningMessageCount = 0;
        errorMessageCount = 0;
        fatalMessageCount = 0;

        if(_allConsoleLogMessages.Count == 0)
        {
            return;
        }

        var currentID = _allConsoleLogMessages[0].MessageID - 1;
        foreach (var message in _allConsoleLogMessages)
        {
            if(message.MessageID != currentID)
            {
                if(message.LogLevel == LogLevel.Fatal)
                {
                    fatalMessageCount++;
                }
                else if(message.LogLevel == LogLevel.Error)
                {
                    errorMessageCount++;
                }
                else if(message.LogLevel == LogLevel.Warn)
                {
                    warningMessageCount++;
                }
                else if(message.LogLevel == LogLevel.Debug)
                {
                    debugMessageCount++;
                }
                else
                {
                    infoMessageCount++;
                }
            }

            currentID = message.MessageID;
        }
    }
    
    protected override void Write(LogEventInfo logEvent)
    {
        var logMessage = RenderLogEvent(Layout, logEvent);
        
        ulong messageID = 0;
        
        if(logEvent.Properties.TryGetValue("MessageID", out var messageIDObj))
        {
            messageID = (ulong)messageIDObj;
        }

        foreach (var line in logMessage.SplitLines())
        {
            var newMessage = new ConsoleLogMessage
            {
                MessageID = messageID,
                message = line.Line.ToString(),
                LogLevel = logEvent.Level
            };

            _allConsoleLogMessages.Add(newMessage);

            if(CheckFilter(newMessage.message, newMessage.LogLevel))
            {
                _filteredLogMessages.Add(newMessage);
            }
        }
    }

    private void UpdateFilter()
    {
        _filteredLogMessages.Clear();
        foreach (var message in _allConsoleLogMessages)
        {
            if(CheckFilter(message.message, message.LogLevel))
            {
                _filteredLogMessages.Add(message);
            }
        }
    }
    
    private bool CheckFilter(string message, LogLevel logLevel)
    {
        if(logLevel == LogLevel.Fatal && FatalMessagesVisible == false)
        {
            return false;
        }
        
        if(logLevel == LogLevel.Error && ErrorMessagesVisible == false)
        {
            return false;
        }
        
        if(logLevel == LogLevel.Warn && WarningMessagesVisible == false)
        {
            return false;
        }
        
        if(logLevel== LogLevel.Info && InfoMessagesVisible == false)
        {
            return false;
        }
        
        if(logLevel== LogLevel.Debug && DebugMessagesVisible == false)
        {
            return false;
        }
        
        if(string.IsNullOrEmpty(_filterText))
        {
            return true;
        }
        
        return message.Contains(_filterText, StringComparison.OrdinalIgnoreCase);
    }

 
}