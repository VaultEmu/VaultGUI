using ImGuiNET;
using Veldrid;

namespace Vault;

public class ImGuiInput
{
    private bool _controlDown;
    private bool _shiftDown;
    private bool _altDown;
    private bool _winKeyDown;

    public ImGuiInput()
    {
        SetKeyMappings();
    }
    
    public void Update(InputSnapshot snapshot, double deltaSeconds)
    {
        var io = ImGui.GetIO();
        io.DeltaTime = (float)deltaSeconds; // DeltaTime is in seconds.
        UpdateImGuiInput(snapshot, io);
    }
    
     private void UpdateImGuiInput(InputSnapshot snapshot, ImGuiIOPtr io)
    {
        var mousePosition = snapshot.MousePosition;

        // Determine if any of the mouse buttons were pressed during this snapshot period, even if they are no longer held.
        var leftPressed = false;
        var middlePressed = false;
        var rightPressed = false;
        foreach (var me in snapshot.MouseEvents)
        {
            if(me.Down)
            {
                switch (me.MouseButton)
                {
                    case MouseButton.Left:
                        leftPressed = true;
                        break;
                    case MouseButton.Middle:
                        middlePressed = true;
                        break;
                    case MouseButton.Right:
                        rightPressed = true;
                        break;
                }
            }
        }

        io.MouseDown[0] = leftPressed || snapshot.IsMouseDown(MouseButton.Left);
        io.MouseDown[1] = rightPressed || snapshot.IsMouseDown(MouseButton.Right);
        io.MouseDown[2] = middlePressed || snapshot.IsMouseDown(MouseButton.Middle);
        io.MousePos = mousePosition;
        io.MouseWheel = snapshot.WheelDelta;

        var keyCharPresses = snapshot.KeyCharPresses;
        for (var i = 0; i < keyCharPresses.Count; i++)
        {
            var c = keyCharPresses[i];
            io.AddInputCharacter(c);
        }

        var keyEvents = snapshot.KeyEvents;
        for (var i = 0; i < keyEvents.Count; i++)
        {
            var keyEvent = keyEvents[i];
            io.KeysDown[(int)keyEvent.Key] = keyEvent.Down;
            if(keyEvent.Key == Key.ControlLeft || keyEvent.Key == Key.ControlRight)
            {
                _controlDown = keyEvent.Down;
            }
            if(keyEvent.Key == Key.ShiftLeft || keyEvent.Key == Key.ShiftRight)
            {
                _shiftDown = keyEvent.Down;
            }

            if(keyEvent.Key == Key.AltLeft || keyEvent.Key == Key.AltRight)
            {
                _altDown = keyEvent.Down;
            }

            if(keyEvent.Key == Key.WinLeft || keyEvent.Key == Key.WinRight)
            {
                _winKeyDown = keyEvent.Down;
            }
        }

        io.KeyCtrl = _controlDown;
        io.KeyAlt = _altDown;
        io.KeyShift = _shiftDown;
        io.KeySuper = _winKeyDown;
    }
    
    private static void SetKeyMappings()
    {
        var io = ImGui.GetIO();
        io.KeyMap[(int)ImGuiKey.Tab] = (int)Key.Tab;
        io.KeyMap[(int)ImGuiKey.LeftArrow] = (int)Key.Left;
        io.KeyMap[(int)ImGuiKey.RightArrow] = (int)Key.Right;
        io.KeyMap[(int)ImGuiKey.UpArrow] = (int)Key.Up;
        io.KeyMap[(int)ImGuiKey.DownArrow] = (int)Key.Down;
        io.KeyMap[(int)ImGuiKey.PageUp] = (int)Key.PageUp;
        io.KeyMap[(int)ImGuiKey.PageDown] = (int)Key.PageDown;
        io.KeyMap[(int)ImGuiKey.Home] = (int)Key.Home;
        io.KeyMap[(int)ImGuiKey.End] = (int)Key.End;
        io.KeyMap[(int)ImGuiKey.Delete] = (int)Key.Delete;
        io.KeyMap[(int)ImGuiKey.Backspace] = (int)Key.BackSpace;
        io.KeyMap[(int)ImGuiKey.Enter] = (int)Key.Enter;
        io.KeyMap[(int)ImGuiKey.Escape] = (int)Key.Escape;
        io.KeyMap[(int)ImGuiKey.Space] = (int)Key.Space;
        io.KeyMap[(int)ImGuiKey.A] = (int)Key.A;
        io.KeyMap[(int)ImGuiKey.B] = (int)Key.B;
        io.KeyMap[(int)ImGuiKey.C] = (int)Key.C;
        io.KeyMap[(int)ImGuiKey.D] = (int)Key.D;
        io.KeyMap[(int)ImGuiKey.E] = (int)Key.E;
        io.KeyMap[(int)ImGuiKey.F] = (int)Key.F;
        io.KeyMap[(int)ImGuiKey.G] = (int)Key.G;
        io.KeyMap[(int)ImGuiKey.H] = (int)Key.H;
        io.KeyMap[(int)ImGuiKey.I] = (int)Key.I;
        io.KeyMap[(int)ImGuiKey.J] = (int)Key.J;
        io.KeyMap[(int)ImGuiKey.K] = (int)Key.K;
        io.KeyMap[(int)ImGuiKey.L] = (int)Key.L;
        io.KeyMap[(int)ImGuiKey.M] = (int)Key.M;
        io.KeyMap[(int)ImGuiKey.N] = (int)Key.N;
        io.KeyMap[(int)ImGuiKey.O] = (int)Key.O;
        io.KeyMap[(int)ImGuiKey.P] = (int)Key.P;
        io.KeyMap[(int)ImGuiKey.Q] = (int)Key.Q;
        io.KeyMap[(int)ImGuiKey.R] = (int)Key.R;
        io.KeyMap[(int)ImGuiKey.S] = (int)Key.S;
        io.KeyMap[(int)ImGuiKey.T] = (int)Key.T;
        io.KeyMap[(int)ImGuiKey.U] = (int)Key.U;
        io.KeyMap[(int)ImGuiKey.V] = (int)Key.V;
        io.KeyMap[(int)ImGuiKey.W] = (int)Key.W;
        io.KeyMap[(int)ImGuiKey.X] = (int)Key.X;
        io.KeyMap[(int)ImGuiKey.Y] = (int)Key.Y;
        io.KeyMap[(int)ImGuiKey.Z] = (int)Key.Z;
    }
}