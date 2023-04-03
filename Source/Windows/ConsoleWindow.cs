using System.Numerics;
using ImGuiNET;
using NLog;
using VaultCore.ImGuiWindowsAPI;

namespace Vault;

public class ConsoleWindow : IImGuiWindow
{
    private static readonly Vector4 DebugColor = new(0.6f, 0.9f, 0.9f, 1.0f);
    private static readonly Vector4 InfoColor = new(0.95f, 0.95f, 0.95f, 1.0f);
    private static readonly Vector4 WarningColor = new(1.0f, 0.85f, 0.45f, 1.0f);
    private static readonly Vector4 ErrorColor = new(0.9f, 0.4f, 0.4f, 1.0f);
    private static readonly Vector4 FatalColor = new(0.9f, 0.15f, 0.15f, 1.0f);

    private bool _autoScroll = true;
    private ImGuiListClipperPtr _textClipper;

    public string WindowTitle => "Console";
    public ImGuiWindowFlags WindowFlags => ImGuiWindowFlags.None;

    public ConsoleWindow()
    {
        unsafe
        {
            var textClipperPtr = ImGuiNative.ImGuiListClipper_ImGuiListClipper();
            _textClipper = new ImGuiListClipperPtr(textClipperPtr);
        }
    }

    public void OnUpdate() { }

    public void OnBeforeDrawImGuiWindow()
    {
        ImGui.PushStyleVar(ImGuiStyleVar.WindowMinSize, new Vector2(575, 300));
    }

    public void OnDrawImGuiWindowContent()
    {
        DrawToolbar();
        DrawConsoleArea();
    }

    private void DrawConsoleArea()
    {
        // Reserve enough left-over height for 1 separator + 1 input text
        if(ImGui.BeginChild("ScrollingRegion", new Vector2(0, 0), false, ImGuiWindowFlags.HorizontalScrollbar))
        {
            ImGui.PushStyleVar(ImGuiStyleVar.ItemSpacing, new Vector2(4, 1)); // Tighten spacing

            var messages = Logger.VaultGuiConsoleTarget?.FilteredConsoleLogMessages;

            if(messages != null)
            {
                _textClipper.Begin(messages.Count);
                while (_textClipper.Step())
                {
                    for (var index = _textClipper.DisplayStart; index < _textClipper.DisplayEnd; ++index)
                    {
                        var item = messages[index];
                        ImGui.PushStyleColor(ImGuiCol.Text, ImGui.GetColorU32(GetMessageColor(item.LogLevel)));
                        ImGui.TextUnformatted(item.message);
                        ImGui.PopStyleColor();
                    }
                }

                // Keep up at the bottom of the scroll region if we were already at the bottom at the beginning of the frame.
                // Using a scrollbar or mouse-wheel will take away from the bottom edge.
                if(_autoScroll && ImGui.GetScrollY() >= ImGui.GetScrollMaxY())
                {
                    ImGui.SetScrollHereY(1.0f);
                }
            }

            ImGui.PopStyleVar();
            ImGui.EndChild();
        }
    }

    private void DrawToolbar()
    {
        // Options menu
        if(ImGui.BeginPopup("Options"))
        {
            ImGui.Checkbox("Auto-scroll", ref _autoScroll);
            ImGui.EndPopup();
        }

        var consoleLoggerTarget = Logger.VaultGuiConsoleTarget;

        var debugMessageCount = 0;
        var infoMessageCount = 0;
        var warningMessageCount = 0;
        var errorMessageCount = 0;
        var fatalMessageCount = 0;

        if(consoleLoggerTarget != null)
        {
            consoleLoggerTarget.GetMessageCount(out debugMessageCount, out infoMessageCount, 
                out warningMessageCount, out errorMessageCount, out fatalMessageCount);
        }
        
        DrawLogLevelToggleButton(LogLevel.Debug, debugMessageCount);
        ImGui.SameLine();
        DrawLogLevelToggleButton(LogLevel.Info, infoMessageCount);
        ImGui.SameLine();
        DrawLogLevelToggleButton(LogLevel.Warn, warningMessageCount);
        ImGui.SameLine();
        DrawLogLevelToggleButton(LogLevel.Error, errorMessageCount);
        ImGui.SameLine();
        DrawLogLevelToggleButton(LogLevel.Fatal, fatalMessageCount);
        
        var windowWidth = ImGui.GetWindowWidth();

        if(windowWidth > 1040)
        {
            //Put on same line, on the right
            ImGui.SameLine();
            ImGui.SetCursorPosX(windowWidth - 475);
        }

        if(ImGui.Button("Clear"))
        {
            ClearConsole();
        }
        ImGui.SameLine();
        ImGui.Text($"{Fonts.FontAwesomeCodes.MagnifyingGlass}");
        ImGui.SameLine();
        
        var newFilterText = "";

        if(consoleLoggerTarget?.FilterText != null)
        {
            newFilterText = consoleLoggerTarget.FilterText;
        }

        ImGui.PushItemWidth(250);
        if(ImGui.InputText("", ref newFilterText, 1024))
        {
            if(consoleLoggerTarget != null)
            {
                consoleLoggerTarget.FilterText = newFilterText;
            }
        }

        ImGui.SameLine();
        ImGui.PopItemWidth();

        if(ImGui.Button("Options"))
        {
            ImGui.OpenPopup("Options");
        }

        ImGui.Separator();
    }

    public void OnAfterDrawImGuiWindow() { }

    public void Dispose() { }

    private void ClearConsole()
    {
        Logger.VaultGuiConsoleTarget?.ClearConsole();
    }
    
    private void DrawLogLevelToggleButton(LogLevel logLevel, int messageCount)
    {
        var toggleButtonWidth = new Vector2(100.0f, 0.0f);
        
        string messageCountText;
        
        if(messageCount > 999)
        {
            messageCountText = "999+";
        }
        else
        {
            messageCountText = messageCount.ToString();
        }
        
        string icon;
        Vector4 color;
        bool? isVisible;
        
        if(logLevel == LogLevel.Fatal)
        {
            icon = Fonts.FontAwesomeCodes.CircleXmark;
            color = FatalColor;
            isVisible = Logger.VaultGuiConsoleTarget?.FatalMessagesVisible;
        }
        else if(logLevel == LogLevel.Error)
        {
            icon = Fonts.FontAwesomeCodes.CircleExclamation;
            color = ErrorColor;
            isVisible = Logger.VaultGuiConsoleTarget?.ErrorMessagesVisible;
        }
        else if(logLevel == LogLevel.Warn)
        {
            icon = Fonts.FontAwesomeCodes.TriangleExclamation;
            color = WarningColor;
            isVisible = Logger.VaultGuiConsoleTarget?.WarningMessagesVisible;
        }
        else if(logLevel == LogLevel.Debug)
        {
            icon = Fonts.FontAwesomeCodes.Bug;
            color = DebugColor;
            isVisible = Logger.VaultGuiConsoleTarget?.DebugMessagesVisible;
        }
        else
        {
            icon = Fonts.FontAwesomeCodes.CircleInfo;
            color = InfoColor;
            isVisible = Logger.VaultGuiConsoleTarget?.InfoMessagesVisible;
        }

        Vector4 buttonColor;
        
        unsafe
        {
            var buttonColorPtr = ImGui.GetStyleColorVec4(ImGuiCol.Button);
            buttonColor = (*buttonColorPtr);
        }
        
        
        if(isVisible.HasValue == false || isVisible.Value == false)
        {
            color = new Vector4(color.X * 0.5f, color.Y * 0.5f, color.Z * 0.5f, color.W);
            buttonColor = new Vector4(buttonColor.X * 0.2f, buttonColor.Y * 0.2f, buttonColor.Z * 0.2f, color.W);
        }
        
        ImGui.PushStyleColor(ImGuiCol.Text, ImGui.GetColorU32(color));
        ImGui.PushStyleColor(ImGuiCol.Button, ImGui.GetColorU32(buttonColor));
        if(ImGui.Button($"{icon} {messageCountText}", toggleButtonWidth)) //Debug
        {
            if(Logger.VaultGuiConsoleTarget != null)
            {
                if(logLevel == LogLevel.Fatal)
                {
                    Logger.VaultGuiConsoleTarget.FatalMessagesVisible = 
                        !Logger.VaultGuiConsoleTarget.FatalMessagesVisible;
                }
                else if(logLevel == LogLevel.Error)
                {
                    Logger.VaultGuiConsoleTarget.ErrorMessagesVisible = 
                        !Logger.VaultGuiConsoleTarget.ErrorMessagesVisible ;
                }
                else if(logLevel == LogLevel.Warn)
                {
                    Logger.VaultGuiConsoleTarget.WarningMessagesVisible = 
                        !Logger.VaultGuiConsoleTarget.WarningMessagesVisible;
                }
                else if(logLevel == LogLevel.Debug)
                {
                    Logger.VaultGuiConsoleTarget.DebugMessagesVisible = 
                        !Logger.VaultGuiConsoleTarget.DebugMessagesVisible;
                }
                else
                {
                    Logger.VaultGuiConsoleTarget.InfoMessagesVisible = 
                        !Logger.VaultGuiConsoleTarget.InfoMessagesVisible;
                }
            }
            
        }

        ImGui.PopStyleColor(2);
    }

    private Vector4 GetMessageColor(LogLevel logLevel)
    {
        if(logLevel == LogLevel.Fatal)
        {
            return FatalColor;
        }

        if(logLevel == LogLevel.Error)
        {
            return ErrorColor;
        }

        if(logLevel == LogLevel.Warn)
        {
            return WarningColor;
        }

        if(logLevel == LogLevel.Debug)
        {
            return DebugColor;
        }

        return InfoColor;
    }
}