using System.Runtime.CompilerServices;
using Vault.Input;
using Vault.Input.Gamepad;
using Vault.Input.Mouse;
using VaultCore.Input.Source.Features;
using Veldrid.Sdl2;

namespace Vault;

public class InputManager : IInputReceiver
{
    private readonly Logger m_logger;

    private readonly KeyboardDevice _keyboardDevice = new("Default Keyboard");
    private readonly MouseDevice _mouseDevice = new("Default Mouse");
    private readonly List<SDLGamepad> _gamepadDevices = new();
    private readonly Dictionary<int, SDLGamepad> _deviceInstanceIdGamepadLookup = new();

    public IKeyboardDevice KeyboardDevice => _keyboardDevice;
    public IMouseDevice MouseDevice => _mouseDevice;
    public IReadOnlyList<IGamepadDevice> GamepadDevices => _gamepadDevices;

    public InputManager(Logger mLogger)
    {
        m_logger = mLogger;
        Sdl2Events.Subscribe(SDLEventHandler);
    }

    private unsafe void SDLEventHandler(ref SDL_Event ev)
    {
        var sdlEvent = ev;

        switch (ev.type)
        {
            case SDL_EventType.KeyDown:
            case SDL_EventType.KeyUp:
                HandleKeyboardInput(Unsafe.Read<SDL_KeyboardEvent>(&sdlEvent));
                break;

            case SDL_EventType.MouseMotion:
                HandleMouseMovementInput(Unsafe.Read<SDL_MouseMotionEvent>(&sdlEvent));
                break;
            case SDL_EventType.MouseButtonDown:
            case SDL_EventType.MouseButtonUp:
                HandleMouseButtonInput(Unsafe.Read<SDL_MouseButtonEvent>(&sdlEvent));
                break;
            case SDL_EventType.MouseWheel:
                HandleMouseWheelInput(Unsafe.Read<SDL_MouseWheelEvent>(&sdlEvent));
                break;

            case SDL_EventType.ControllerAxisMotion:
                HandleControllerAnalogInput(Unsafe.Read<SDL_ControllerAxisEvent>(&sdlEvent));
                break;
            case SDL_EventType.ControllerButtonDown:
            case SDL_EventType.ControllerButtonUp:
                HandleControllerButtonInput(Unsafe.Read<SDL_ControllerButtonEvent>(&sdlEvent));
                break;
            case SDL_EventType.ControllerDeviceAdded:
                HandleControllerAdded(Unsafe.Read<SDL_ControllerDeviceEvent>(&sdlEvent));
                break;
            case SDL_EventType.ControllerDeviceRemoved:
                HandleControllerRemoved(Unsafe.Read<SDL_ControllerDeviceEvent>(&sdlEvent));
                break;
        }
    }


    public void SnapshotDevices(float deltaTime)
    {
        _keyboardDevice.SnapshotState(deltaTime);
        _mouseDevice.SnapshotState(deltaTime);

        foreach (var gamepad in _gamepadDevices)
        {
            gamepad.SnapshotState(deltaTime);
        }
    }

    public void OnCoreAcquiresFeature(Type coreType, Type featureType, List<Type> allCoreFeaturesNeeded) { }

    public void OnCoreReleasesFeature(Type coreType, Type featureType, List<Type> allCoreFeaturesNeeded) { }

    private void HandleControllerAnalogInput(SDL_ControllerAxisEvent controllerAxisEvent)
    {
        if(!_deviceInstanceIdGamepadLookup.TryGetValue(controllerAxisEvent.which, out var gamepad))
        {
            m_logger.LogError($"Trying to get gamepad with instance ID {controllerAxisEvent.which}, but not added");
            return;
        }
        
        var gamepadAxis = MapGamepadAxis(controllerAxisEvent.axis);

        if(gamepadAxis.HasValue == false)
        {
            return;
        }
        
        gamepad.UpdateAnalogInputValueForNextSnapShot(gamepadAxis.Value, controllerAxisEvent.value / (float)short.MaxValue);
    }

    private void HandleControllerButtonInput(SDL_ControllerButtonEvent controllerButtonEvent)
    {
        if(!_deviceInstanceIdGamepadLookup.TryGetValue(controllerButtonEvent.which, out var gamepad))
        {
            m_logger.LogError($"Trying to get gamepad with instance ID {controllerButtonEvent.which}, but not added");
            return;
        }

        var gamepadButton = MapGamepadButtons(controllerButtonEvent.button);

        if(gamepadButton.HasValue == false)
        {
            return;
        }

        gamepad.UpdateDigitalButtonDownForNextSnapShot(gamepadButton.Value, 
            controllerButtonEvent.type == (int)SDL_EventType.ControllerButtonDown);
    }

    private void HandleControllerAdded(SDL_ControllerDeviceEvent controllerDeviceEvent)
    {
        var addedGamepad = new SDLGamepad(controllerDeviceEvent.which);
        _gamepadDevices.Add(addedGamepad);
        _deviceInstanceIdGamepadLookup.Add(addedGamepad.SDLDeviceInstanceID, addedGamepad);

        m_logger.Log($"Controller Added: {addedGamepad.DeviceName} ({addedGamepad.SDLDeviceInstanceID})");
    }

    private void HandleControllerRemoved(SDL_ControllerDeviceEvent controllerDeviceEvent)
    {
        if(!_deviceInstanceIdGamepadLookup.TryGetValue(controllerDeviceEvent.which, out var gamepad))
        {
            m_logger.LogError($"Trying to remove gamepad with instance ID {controllerDeviceEvent.which}, but not added");
            return;
        }

        gamepad.Dispose();
        _gamepadDevices.Remove(gamepad);
        _deviceInstanceIdGamepadLookup.Remove(controllerDeviceEvent.which);

        m_logger.Log($"Controller Removed: {gamepad.DeviceName} ({gamepad.SDLDeviceInstanceID})");
    }

    private void HandleMouseWheelInput(SDL_MouseWheelEvent mouseWheelEvent)
    {
        float scrollDelta = mouseWheelEvent.y;

        if(mouseWheelEvent.direction != 0)
        {
            scrollDelta *= -1.0f;
        }
        
        _mouseDevice.UpdateScrollForNextSnapShot(scrollDelta);
    }

    private void HandleMouseMovementInput(SDL_MouseMotionEvent mouseMotionEvent)
    {
        _mouseDevice.UpdateRelativeMouseXYForNextSnapShot(mouseMotionEvent.xrel, mouseMotionEvent.yrel);
    }

    private void HandleMouseButtonInput(SDL_MouseButtonEvent mouseButtonEvent)
    {
        var mouseButton = MapMouseButton(mouseButtonEvent.button);

        if(mouseButton.HasValue == false)
        {
            return;
        }

        _mouseDevice.UpdateMouseButtonDownForNextSnapShot(mouseButton.Value, mouseButtonEvent.type == SDL_EventType.MouseButtonDown);
    }


    private void HandleKeyboardInput(SDL_KeyboardEvent keyboardEvent)
    {
        var key = MapKey(keyboardEvent.keysym);

        if(key.HasValue == false)
        {
            return;
        }

        _keyboardDevice.UpdateKeyDownForNextSnapShot(key.Value, keyboardEvent.type == SDL_EventType.KeyDown);
    }

    private KeyInput? MapKey(SDL_Keysym keysym)
    {
        switch (keysym.sym)
        {
            case SDL_Keycode.SDLK_a:
                return KeyInput.A;
            case SDL_Keycode.SDLK_b:
                return KeyInput.B;
            case SDL_Keycode.SDLK_c:
                return KeyInput.C;
            case SDL_Keycode.SDLK_d:
                return KeyInput.D;
            case SDL_Keycode.SDLK_e:
                return KeyInput.E;
            case SDL_Keycode.SDLK_f:
                return KeyInput.F;
            case SDL_Keycode.SDLK_g:
                return KeyInput.G;
            case SDL_Keycode.SDLK_h:
                return KeyInput.H;
            case SDL_Keycode.SDLK_i:
                return KeyInput.I;
            case SDL_Keycode.SDLK_j:
                return KeyInput.J;
            case SDL_Keycode.SDLK_k:
                return KeyInput.K;
            case SDL_Keycode.SDLK_l:
                return KeyInput.L;
            case SDL_Keycode.SDLK_m:
                return KeyInput.M;
            case SDL_Keycode.SDLK_n:
                return KeyInput.N;
            case SDL_Keycode.SDLK_o:
                return KeyInput.O;
            case SDL_Keycode.SDLK_p:
                return KeyInput.P;
            case SDL_Keycode.SDLK_q:
                return KeyInput.Q;
            case SDL_Keycode.SDLK_r:
                return KeyInput.R;
            case SDL_Keycode.SDLK_s:
                return KeyInput.S;
            case SDL_Keycode.SDLK_t:
                return KeyInput.T;
            case SDL_Keycode.SDLK_u:
                return KeyInput.U;
            case SDL_Keycode.SDLK_v:
                return KeyInput.V;
            case SDL_Keycode.SDLK_w:
                return KeyInput.W;
            case SDL_Keycode.SDLK_x:
                return KeyInput.X;
            case SDL_Keycode.SDLK_y:
                return KeyInput.Y;
            case SDL_Keycode.SDLK_z:
                return KeyInput.Z;
            case SDL_Keycode.SDLK_0:
                return KeyInput.Alpha0;
            case SDL_Keycode.SDLK_1:
                return KeyInput.Alpha1;
            case SDL_Keycode.SDLK_2:
                return KeyInput.Alpha2;
            case SDL_Keycode.SDLK_3:
                return KeyInput.Alpha3;
            case SDL_Keycode.SDLK_4:
                return KeyInput.Alpha4;
            case SDL_Keycode.SDLK_5:
                return KeyInput.Alpha5;
            case SDL_Keycode.SDLK_6:
                return KeyInput.Alpha6;
            case SDL_Keycode.SDLK_7:
                return KeyInput.Alpha7;
            case SDL_Keycode.SDLK_8:
                return KeyInput.Alpha8;
            case SDL_Keycode.SDLK_9:
                return KeyInput.Alpha9;
            case SDL_Keycode.SDLK_RETURN:
                return KeyInput.Enter;
            case SDL_Keycode.SDLK_ESCAPE:
                return KeyInput.Escape;
            case SDL_Keycode.SDLK_BACKSPACE:
                return KeyInput.BackSpace;
            case SDL_Keycode.SDLK_TAB:
                return KeyInput.Tab;
            case SDL_Keycode.SDLK_SPACE:
                return KeyInput.Space;
            case SDL_Keycode.SDLK_MINUS:
                return KeyInput.AlphaMinus;
            case SDL_Keycode.SDLK_EQUALS:
                return KeyInput.AlphaEquals;
            case SDL_Keycode.SDLK_LEFTBRACKET:
                return KeyInput.BracketLeft;
            case SDL_Keycode.SDLK_RIGHTBRACKET:
                return KeyInput.BracketRight;
            case SDL_Keycode.SDLK_QUOTE:
                return KeyInput.Quote;
            case SDL_Keycode.SDLK_COMMA:
                return KeyInput.Comma;
            case SDL_Keycode.SDLK_PERIOD:
                return KeyInput.Period;
            case SDL_Keycode.SDLK_SLASH:
                return KeyInput.Slash;
            case SDL_Keycode.SDLK_SEMICOLON:
                return KeyInput.Semicolon;
            case SDL_Keycode.SDLK_BACKSLASH:
                return KeyInput.BackSlash;
            case SDL_Keycode.SDLK_BACKQUOTE:
                return KeyInput.Tilde;
            case SDL_Keycode.SDLK_CAPSLOCK:
                return KeyInput.CapsLock;
            case SDL_Keycode.SDLK_F1:
                return KeyInput.F1;
            case SDL_Keycode.SDLK_F2:
                return KeyInput.F2;
            case SDL_Keycode.SDLK_F3:
                return KeyInput.F3;
            case SDL_Keycode.SDLK_F4:
                return KeyInput.F4;
            case SDL_Keycode.SDLK_F5:
                return KeyInput.F5;
            case SDL_Keycode.SDLK_F6:
                return KeyInput.F6;
            case SDL_Keycode.SDLK_F7:
                return KeyInput.F7;
            case SDL_Keycode.SDLK_F8:
                return KeyInput.F8;
            case SDL_Keycode.SDLK_F9:
                return KeyInput.F9;
            case SDL_Keycode.SDLK_F10:
                return KeyInput.F10;
            case SDL_Keycode.SDLK_F11:
                return KeyInput.F12;
            case SDL_Keycode.SDLK_F12:
                return KeyInput.F12;
            case SDL_Keycode.SDLK_F13:
                return KeyInput.F13;
            case SDL_Keycode.SDLK_F14:
                return KeyInput.F14;
            case SDL_Keycode.SDLK_F15:
                return KeyInput.F15;
            case SDL_Keycode.SDLK_F16:
                return KeyInput.F16;
            case SDL_Keycode.SDLK_F17:
                return KeyInput.F17;
            case SDL_Keycode.SDLK_F18:
                return KeyInput.F18;
            case SDL_Keycode.SDLK_F19:
                return KeyInput.F19;
            case SDL_Keycode.SDLK_F20:
                return KeyInput.F20;
            case SDL_Keycode.SDLK_F21:
                return KeyInput.F21;
            case SDL_Keycode.SDLK_F22:
                return KeyInput.F22;
            case SDL_Keycode.SDLK_F23:
                return KeyInput.F23;
            case SDL_Keycode.SDLK_F24:
                return KeyInput.F24;
            case SDL_Keycode.SDLK_PRINTSCREEN:
                return KeyInput.PrintScreen;
            case SDL_Keycode.SDLK_SCROLLLOCK:
                return KeyInput.ScrollLock;
            case SDL_Keycode.SDLK_PAUSE:
                return KeyInput.Pause;
            case SDL_Keycode.SDLK_INSERT:
                return KeyInput.Insert;
            case SDL_Keycode.SDLK_HOME:
                return KeyInput.Home;
            case SDL_Keycode.SDLK_PAGEUP:
                return KeyInput.PageUp;
            case SDL_Keycode.SDLK_DELETE:
                return KeyInput.Delete;
            case SDL_Keycode.SDLK_END:
                return KeyInput.End;
            case SDL_Keycode.SDLK_PAGEDOWN:
                return KeyInput.PageDown;
            case SDL_Keycode.SDLK_RIGHT:
                return KeyInput.ArrowRight;
            case SDL_Keycode.SDLK_LEFT:
                return KeyInput.ArrowLeft;
            case SDL_Keycode.SDLK_DOWN:
                return KeyInput.ArrowDown;
            case SDL_Keycode.SDLK_UP:
                return KeyInput.ArrowUp;
            case SDL_Keycode.SDLK_NUMLOCKCLEAR:
                return KeyInput.NumLock;
            case SDL_Keycode.SDLK_KP_DIVIDE:
                return KeyInput.KeypadDivide;
            case SDL_Keycode.SDLK_KP_MULTIPLY:
                return KeyInput.KeypadMultiply;
            case SDL_Keycode.SDLK_KP_MINUS:
                return KeyInput.KeypadMinus;
            case SDL_Keycode.SDLK_KP_PLUS:
                return KeyInput.KeypadPlus;
            case SDL_Keycode.SDLK_KP_ENTER:
                return KeyInput.KeypadEnter;
            case SDL_Keycode.SDLK_KP_1:
                return KeyInput.Keypad1;
            case SDL_Keycode.SDLK_KP_2:
                return KeyInput.Keypad2;
            case SDL_Keycode.SDLK_KP_3:
                return KeyInput.Keypad3;
            case SDL_Keycode.SDLK_KP_4:
                return KeyInput.Keypad4;
            case SDL_Keycode.SDLK_KP_5:
                return KeyInput.Keypad5;
            case SDL_Keycode.SDLK_KP_6:
                return KeyInput.Keypad6;
            case SDL_Keycode.SDLK_KP_7:
                return KeyInput.Keypad7;
            case SDL_Keycode.SDLK_KP_8:
                return KeyInput.Keypad8;
            case SDL_Keycode.SDLK_KP_9:
                return KeyInput.Keypad9;
            case SDL_Keycode.SDLK_KP_0:
                return KeyInput.Keypad0;
            case SDL_Keycode.SDLK_KP_PERIOD:
                return KeyInput.KeypadPeriod;
            case SDL_Keycode.SDLK_APPLICATION:
                return KeyInput.Application;
            case SDL_Keycode.SDLK_LCTRL:
                return KeyInput.ControlLeft;
            case SDL_Keycode.SDLK_LSHIFT:
                return KeyInput.ShiftLeft;
            case SDL_Keycode.SDLK_LALT:
                return KeyInput.AltLeft;
            case SDL_Keycode.SDLK_LGUI:
                return KeyInput.WinLeft;
            case SDL_Keycode.SDLK_RCTRL:
                return KeyInput.ControlRight;
            case SDL_Keycode.SDLK_RSHIFT:
                return KeyInput.ShiftRight;
            case SDL_Keycode.SDLK_RALT:
                return KeyInput.AltRight;
            case SDL_Keycode.SDLK_RGUI:
                return KeyInput.WinRight;
        }

        return null;
    }

    private MouseInputs? MapMouseButton(SDL_MouseButton button)
    {
        switch (button)
        {
            case SDL_MouseButton.Left:
                return MouseInputs.LeftMouseButton;
            case SDL_MouseButton.Middle:
                return MouseInputs.MiddleMouseButton;
            case SDL_MouseButton.Right:
                return MouseInputs.RightMouseButton;
            case SDL_MouseButton.X1:
                return MouseInputs.ExtraMouseButton1;
            case SDL_MouseButton.X2:
                return MouseInputs.ExtraMouseButton2;
        }

        return null;
    }

    private GamepadInputs? MapGamepadButtons(SDL_GameControllerButton button)
    {
        switch (button)
        {
            case SDL_GameControllerButton.A:
                return GamepadInputs.A;
            case SDL_GameControllerButton.B:
                return GamepadInputs.B;
            case SDL_GameControllerButton.X:
                return GamepadInputs.X;
            case SDL_GameControllerButton.Y:
                return GamepadInputs.Y;
            case SDL_GameControllerButton.Back:
                return GamepadInputs.Select;
            case SDL_GameControllerButton.Start:
                return GamepadInputs.Start;
            case SDL_GameControllerButton.LeftStick:
                return GamepadInputs.LeftStickButton;
            case SDL_GameControllerButton.RightStick:
                return GamepadInputs.RightStickButton;
            case SDL_GameControllerButton.LeftShoulder:
                return GamepadInputs.LeftBumper;
            case SDL_GameControllerButton.RightShoulder:
                return GamepadInputs.RightBumper;
            case SDL_GameControllerButton.DPadUp:
                return GamepadInputs.DPadUp;
            case SDL_GameControllerButton.DPadDown:
                return GamepadInputs.DPadDown;
            case SDL_GameControllerButton.DPadLeft:
                return GamepadInputs.DPadLeft;
            case SDL_GameControllerButton.DPadRight:
                return GamepadInputs.DPadRight;
        }

        return null;
    }
    
    private GamepadInputs? MapGamepadAxis(SDL_GameControllerAxis axis)
    {
        switch(axis)
        {
            case SDL_GameControllerAxis.LeftX:
                return GamepadInputs.LeftStickX;
            case SDL_GameControllerAxis.LeftY:
                return GamepadInputs.LeftStickY;
            case SDL_GameControllerAxis.RightX:
                return GamepadInputs.RightStickX;
            case SDL_GameControllerAxis.RightY:
                return GamepadInputs.RightStickY;
            case SDL_GameControllerAxis.TriggerLeft:
                return GamepadInputs.LeftTrigger;
            case SDL_GameControllerAxis.TriggerRight:
                return GamepadInputs.RightTrigger;
        }
        
        return null;
    }
}