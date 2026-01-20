using System.Numerics;
using System.Text;
using Silk.NET.Input;
using VoxelGame.Core;
using VoxelGame.Core.Data.Input;
using Key = VoxelGame.Core.Data.Input.Key;
using SilkKey = Silk.NET.Input.Key;
using SilkMouseBtn = Silk.NET.Input.MouseButton;

namespace VoxelGame.Engine;

public class VkInput : IInput
{
    private IInputContext _inputContext = null!;
    private IKeyboard _keyboard = null!;
    private IMouse _mouse = null!;

    private int _frameCounter = 0;
    
    private readonly StringBuilder _charBuffer = new();
    private Dictionary<Key, (bool pressed, int updateFrame)> _keyboardState = [];
    private Dictionary<MouseBtn, (bool pressed, int updateFrame)> _mouseButtonState = [];
    
    public string Clipboard
    {
        get => _keyboard.ClipboardText;
        set => _keyboard.ClipboardText = value;
    }
    public string CharacterBuffer => _charBuffer.ToString();
    
    internal void Initialize()
    {
        _inputContext = ((VkWindow)Singletons.Window).Window.CreateInput();

        _keyboard = _inputContext.Keyboards[0];
        _mouse = _inputContext.Mice[0];

        Console.WriteLine("Selected keyboard: " + _keyboard.Name);
        Console.WriteLine("Selected mouse:    " + _mouse.Name);

        _keyboard.KeyChar += KeyboardOnKeyChar;
        _keyboard.KeyDown += KeyboardOnKeyDown;
        _keyboard.KeyUp += KeyboardOnKeyUp;
        
        _mouse.MouseMove += MouseOnMouseMove;
        _mouse.MouseDown += MouseOnMouseDown;
        _mouse.MouseUp += MouseOnMouseUp;
    }
    internal void Update()
    {
        _charBuffer.Clear();
        _frameCounter++;
    }
    
    private void KeyboardOnKeyChar(IKeyboard dev, char c) => _charBuffer.Append(c);
    private void KeyboardOnKeyDown(IKeyboard dev, SilkKey key, int keycode)
        => _keyboardState[(Key)key] = (true, _frameCounter);
    private void KeyboardOnKeyUp(IKeyboard dev, SilkKey key, int keycode)
        => _keyboardState[(Key)key] = (false, _frameCounter);

    private void MouseOnMouseMove(IMouse dev, Vector2 position) {}
    private void MouseOnMouseDown(IMouse dev, SilkMouseBtn button)
        => _mouseButtonState[(MouseBtn)button] = (true, _frameCounter);
    private void MouseOnMouseUp(IMouse dev, SilkMouseBtn button)
        =>  _mouseButtonState[(MouseBtn)button] = (false, _frameCounter);

    public bool IsPressed(Key key) => _keyboardState.TryGetValue(key, out var v) && v.pressed;
    public bool IsJustPressed(Key key) =>_keyboardState.TryGetValue(key, out var v) && v.pressed && v.updateFrame == _frameCounter;
    public bool IsReleased(Key key) => _keyboardState.TryGetValue(key, out var v) && !v.pressed;

    public bool IsPressed(MouseBtn btn) => _mouseButtonState.TryGetValue(btn, out var v) && v.pressed;
    public bool IsJustPressed(MouseBtn btn) => _mouseButtonState.TryGetValue(btn, out var v) && v.pressed && v.updateFrame == _frameCounter;
    public bool IsReleased(MouseBtn btn) => _mouseButtonState.TryGetValue(btn, out var v) && !v.pressed;
}
