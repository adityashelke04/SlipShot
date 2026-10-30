using System;
using System.Collections.Generic;
using System.Windows.Input;
using System.Windows.Interop;

namespace SlipShot.Services;

internal sealed class Hotkeys : IDisposable
{
    private readonly HwndSource source;
    private int activeId;
    private int nextId = 10;
    internal event Action? Invoked;
    internal Hotkeys()
    {
        source = new HwndSource(new HwndSourceParameters("SlipShot messages") { ParentWindow = new IntPtr(-3), Width=0, Height=0 });
        source.AddHook(Hook);
    }
    internal bool Register(string text)
    {
        if (!TryParse(text,out var modifiers,out var key)) return false;
        int proposed = ++nextId;
        if (!Native.RegisterHotKey(source.Handle,proposed,modifiers|0x4000,key)) return false;
        if (activeId != 0) Native.UnregisterHotKey(source.Handle,activeId);
        activeId = proposed;
        return true;
    }
    internal static bool TryParse(string text, out uint modifiers, out uint key)
    {
        modifiers=0; key=0;
        var parts = text.Split('+',StringSplitOptions.TrimEntries|StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length < 2) return false;
        var used = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        for(int i=0;i<parts.Length-1;i++)
        {
            if (!used.Add(parts[i])) return false;
            uint modifier = parts[i].ToLowerInvariant() switch { "ctrl"=>2u,"shift"=>4u,"alt"=>1u,_=>0u };
            if(modifier==0) return false;
            modifiers |= modifier;
        }
        string keyName=parts[^1];
        if(!Enum.TryParse<Key>(keyName,true,out var parsed))return false;
        bool letter=keyName.Length==1 && parsed>=Key.A && parsed<=Key.Z && char.IsAsciiLetter(keyName[0]);
        bool function=keyName.StartsWith("F",StringComparison.OrdinalIgnoreCase) && parsed>=Key.F1 && parsed<=Key.F24;
        if(!letter&&!function)return false;
        key=(uint)KeyInterop.VirtualKeyFromKey(parsed);
        return true;
    }
    private IntPtr Hook(IntPtr hwnd,int msg,IntPtr w,IntPtr l,ref bool handled)
    { if(msg==0x312 && w.ToInt32()==activeId) { handled=true; Invoked?.Invoke(); } return IntPtr.Zero; }
    public void Dispose() { if(activeId!=0) Native.UnregisterHotKey(source.Handle,activeId); source.RemoveHook(Hook); source.Dispose(); }
}
