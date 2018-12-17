using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace WebViewer
{
    internal static class KeyBlocker
    {
        //http://www.csharpcoderr.com/2014/02/block-keyboard-keys.html
        //https://docs.microsoft.com/en-us/windows/desktop/api/winuser/nf-winuser-setwindowshookexa

        //Установка перехвата клавиатуры
        [System.Runtime.InteropServices.DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelKeyboardProcDelegate lpfn, IntPtr hMod, int dwThreadId);

        //Разблокировка клавиатуры
        [System.Runtime.InteropServices.DllImport("user32.dll", SetLastError = true)]
        private static extern bool UnhookWindowsHookEx(IntPtr hhk);

        //Hook handle
        [System.Runtime.InteropServices.DllImport("Kernel32.dll", SetLastError = true)]
        private static extern IntPtr GetModuleHandle(IntPtr lpModuleName);

        //Вызов следующего хука
        [System.Runtime.InteropServices.DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

        private const int WH_KEYBOARD_LL = 13;//Keyboard hook;
        private delegate IntPtr LowLevelKeyboardProcDelegate(int nCode, IntPtr wParam, IntPtr lParam);
        private static readonly IntPtr _blockHookPtr = (IntPtr)1;
        private static IntPtr _hHook;
        private static LowLevelKeyboardProcDelegate _hookCallback;
        private static PressedKeys _keys;
        private static object _keysLocker = new object();
        private static List<PressedKeys> _combinations = new List<PressedKeys>();

        //https://docs.microsoft.com/ru-ru/windows/desktop/inputdev/virtual-key-codes
        private const UInt32 VK_TAB = 0x09;
        private const UInt32 VK_SHIFT = 0x10;
        private const UInt32 VK_LSHIFT = 0xA0;
        private const UInt32 VK_RSHIFT = 0xA1;
        private const UInt32 VK_CONTROL = 0x11;
        private const UInt32 VK_LCONTROL = 0xA2;
        private const UInt32 VK_RCONTROL = 0xA3;
        private const UInt32 VK_ESCAPE = 0x1B;
        private const UInt32 VK_DELETE = 0x2E;
        private const UInt32 VK_LWIN = 0x5B;
        private const UInt32 VK_RWIN = 0x5C;
        private const UInt32 VK_F4 = 0x73;
        private const UInt32 VK_MENU = 0x12;  //Alt
        private const UInt32 VK_LMENU = 0xA4; //Alt
        private const UInt32 VK_RMENU = 0xA5; //Alt
        private const UInt32 VK_RBUTTON = 0x02;

        //Keys data structure
        [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential)]
        private struct KBDLLHOOKSTRUCT
        { public UInt32 key; }

        static KeyBlocker()
        {
            InitKeyBoardProtection();
        }

        private static void InitKeyBoardProtection()
        {
            _hookCallback = LowLevelKeyboardHookProc;
            _combinations.Clear();
            _combinations.Add(PressedKeys.Win);
            _combinations.Add(PressedKeys.Ctrl | PressedKeys.Alt | PressedKeys.Del);
            _combinations.Add(PressedKeys.Alt | PressedKeys.Tab);
            _combinations.Add(PressedKeys.Alt | PressedKeys.F4);
            _combinations.Add(PressedKeys.Ctrl | PressedKeys.Shift | PressedKeys.Esc);
        }


        private static IntPtr LowLevelKeyboardHookProc(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0) {
                KBDLLHOOKSTRUCT objKeyInfo = (KBDLLHOOKSTRUCT)System.Runtime.InteropServices.Marshal.PtrToStructure(lParam, typeof(KBDLLHOOKSTRUCT));
                if (HookKey(objKeyInfo.key)) {
                    if (CheckCombinations()) {
                        return _blockHookPtr;
                    }
                }
            }
            return CallNextHookEx(_hHook, nCode, wParam, lParam);
        }

        private static bool HookKey(uint key)
        {
            switch (key) {
                case VK_RWIN:
                case VK_LWIN:
                    SetKey(PressedKeys.Win);
                    return true;
                case VK_CONTROL:
                case VK_LCONTROL:
                case VK_RCONTROL:
                    SetKey(PressedKeys.Ctrl);
                    return true;
                case VK_DELETE:
                    SetKey(PressedKeys.Del);
                    return true;
                case VK_ESCAPE:
                    SetKey(PressedKeys.Esc);
                    return true;
                case VK_F4:
                    SetKey(PressedKeys.F4);
                    return true;
                case VK_LMENU:
                case VK_RMENU:
                case VK_MENU:
                    SetKey(PressedKeys.Alt);
                    return true;
                case VK_LSHIFT:
                case VK_RSHIFT:
                case VK_SHIFT:
                    SetKey(PressedKeys.Shift);
                    return true;
                case VK_RBUTTON:
                    SetKey(PressedKeys.RButton);
                    return true;
                default:
                    break;
            }
            //MessageBox.Show(objKeyInfo.key.ToString("X"));
            return false;
        }

        private static void SetKey(PressedKeys key)
        {
            lock (_keysLocker) {
                _keys ^= key;
            }
        }

        private static bool CheckCombinations()
        {
            lock (_keysLocker) {
                foreach (PressedKeys comb in _combinations) {
                    if ((_keys & comb) == comb) {
                        Console.WriteLine(string.Format("Blocked: {0}", comb));
                        return true;
                    }
                }
            }
            return false;
        }


        #region Protect KeyBoard

        public static void ProtectKeyBoard()
        {
            _hHook = SetWindowsHookEx(WH_KEYBOARD_LL, _hookCallback, GetModuleHandle(IntPtr.Zero), 0);
            if (_hHook == null) {
                Console.WriteLine("Не удалось установить перехватчик клавиатуры!");
            }
        }

        public static void UnProtectKeyBoard()
        {
            UnhookWindowsHookEx(_hHook);
        }

        #endregion
    }
}
