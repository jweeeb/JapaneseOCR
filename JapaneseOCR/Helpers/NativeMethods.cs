using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;

namespace JapaneseOCR.Helpers
{
   public static class NativeMethods
   {
      public class MouseEventModel
      {
         public MouseMessage EventType { get; set; }
         public Point Coordinates { get; set; }
      }

      public class KeyboardEventModel
      {
         public KeyboardMessage EventType { get; set; }
         public Keys Key { get; set; }
      }

      public enum HookType : int
      {
         WH_KEYBOARD = 2,
         WH_MOUSE = 7,
         WH_KEYBOARD_LL = 13,
         WH_MOUSE_LL = 14
      }

      public enum MouseMessage
      {
         WM_MOUSEMOVE = 0x0200,
         WM_LBUTTONDOWN = 0x0201,
         WM_LBUTTONUP = 0x0202,
         WM_LBUTTONDBLCLK = 0x0203,
         WM_RBUTTONDOWN = 0x0204,
         WM_RBUTTONUP = 0x0205,
         WM_RBUTTONDBLCLK = 0x0206,
         WM_MBUTTONDOWN = 0x0207,
         WM_MBUTTONUP = 0x0208,
         WM_MBUTTONDBLCLK = 0x0209,

         WM_MOUSEWHEEL = 0x020A,
         WM_MOUSEHWHEEL = 0x020E,

         WM_NCMOUSEMOVE = 0x00A0,
         WM_NCLBUTTONDOWN = 0x00A1,
         WM_NCLBUTTONUP = 0x00A2,
         WM_NCLBUTTONDBLCLK = 0x00A3,
         WM_NCRBUTTONDOWN = 0x00A4,
         WM_NCRBUTTONUP = 0x00A5,
         WM_NCRBUTTONDBLCLK = 0x00A6,
         WM_NCMBUTTONDOWN = 0x00A7,
         WM_NCMBUTTONUP = 0x00A8,
         WM_NCMBUTTONDBLCLK = 0x00A9
      }

      public enum KeyboardMessage
      {
         WM_KEYDOWN = 0x0100,
      }

      [DllImport("user32.dll")]
      [return: MarshalAs(UnmanagedType.Bool)]
      static extern bool GetCursorPos(ref Point lpPoint);

      [DllImport("user32.dll", CharSet = CharSet.Auto)]
      public static extern IntPtr SetWindowsHookEx(HookType hookType, HookProc callback, IntPtr hMod, uint dwThreadId);
      [DllImport("user32.dll", CharSet = CharSet.Auto)]
      public static extern bool UnhookWindowsHookEx(IntPtr hhk);
      [DllImport("user32.dll", CharSet = CharSet.Auto)]
      public static extern int CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

      [DllImport("User32.dll")]
      public static extern IntPtr GetDC(IntPtr hwnd);
      [DllImport("User32.dll")]
      public static extern void ReleaseDC(IntPtr hwnd, IntPtr dc);


      public delegate int HookProc(int nCode, IntPtr wParam, IntPtr lParam);
      public delegate void MouseEventDelegate(MouseEventModel info);
      public static MouseEventDelegate MouseHandle;
      public delegate void KeyboardEventDelegate(KeyboardEventModel info);
      public static KeyboardEventDelegate KeyboardHandle;

      private static HookProc _globalLlMouseHookCallback;
      private static HookProc _globalLlKeyboardHookCallback;
      private static IntPtr _hGlobalLlMouseHook;
      private static IntPtr _hGlobalLlKeyboardHook;

      static NativeMethods()
      {
         _hGlobalLlMouseHook = IntPtr.Zero;
         _hGlobalLlKeyboardHook = IntPtr.Zero;
         _globalLlMouseHookCallback = LowLevelMouseProc;
         _globalLlKeyboardHookCallback = LowLevelKeyboardProc;
         SetUpHook();
      }

      private static void SetUpHook()
      {
         _hGlobalLlMouseHook = SetWindowsHookEx(HookType.WH_MOUSE_LL, _globalLlMouseHookCallback, Marshal.GetHINSTANCE(Assembly.GetExecutingAssembly().GetModules()[0]), 0);

         if (_hGlobalLlMouseHook == IntPtr.Zero)
         {
            throw new Win32Exception("Unable to set MouseHook");
         }

         _hGlobalLlKeyboardHook = SetWindowsHookEx(HookType.WH_KEYBOARD_LL, _globalLlKeyboardHookCallback, Marshal.GetHINSTANCE(Assembly.GetExecutingAssembly().GetModules()[0]), 0);

         if (_hGlobalLlKeyboardHook == IntPtr.Zero)
         {
            throw new Win32Exception("Unable to set KeyboardHook");
         }
      }

      public static int LowLevelMouseProc(int nCode, IntPtr wParam, IntPtr lParam)
      {
         if (nCode >= 0)
         {
            MouseEventModel eventModel = new MouseEventModel();
            // Get the mouse WM from the wParam parameter
            var wmMouse = (MouseMessage)wParam;
            eventModel.EventType = wmMouse;
            Point p = new Point();
            GetCursorPos(ref p);
            eventModel.Coordinates = p;
            MouseHandle?.Invoke(eventModel);

         }

         // Pass the hook information to the next hook procedure in chain
         return CallNextHookEx(_hGlobalLlMouseHook, nCode, wParam, lParam);
      }


      public static int LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam)
      {
         if (nCode >= 0)
         {
            KeyboardEventModel eventModel = new KeyboardEventModel();
            int vkCode = Marshal.ReadInt32(lParam);
            eventModel.EventType = (KeyboardMessage)wParam;
            eventModel.Key = (Keys)vkCode;
            KeyboardHandle?.Invoke(eventModel);
         }

         // Pass the hook information to the next hook procedure in chain
         return CallNextHookEx(_hGlobalLlKeyboardHook, nCode, wParam, lParam);
      }

      public static void ClearHook()
      {
         if (_hGlobalLlMouseHook != IntPtr.Zero)
         {
            // Unhook the low-level mouse hook
            if (!UnhookWindowsHookEx(_hGlobalLlMouseHook))
            {
               throw new Win32Exception("Unable to clear MouseHook");
            }
            _hGlobalLlMouseHook = IntPtr.Zero;
         }
         if (_hGlobalLlKeyboardHook != IntPtr.Zero)
         {
            // Unhook the low-level mouse hook
            if (!UnhookWindowsHookEx(_hGlobalLlKeyboardHook))
            {
               throw new Win32Exception("Unable to clear KeyboardHook");
            }
            _hGlobalLlKeyboardHook = IntPtr.Zero;
         }
      }
   }
}
