using System;

namespace WebViewer
{
    [Flags]
    public enum PressedKeys
    {
        //
        // Summary:
        //     No modifiers are pressed.
        None = 0,
        Alt = 2,
        Ctrl = 4,
        Shift = 8,
        Win = 16,
        Esc = 32,
        F4 = 64,
        Del = 128,
        RButton = 256,
        Tab = 512
    }
}
