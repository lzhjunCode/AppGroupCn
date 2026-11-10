using System;
using System.Runtime.InteropServices;
using System.Text;

// 纯 COM interop 调用 UI Automation（不依赖 WPF/PresentationNative）
class Uia {
    public const int UIA_ControlTypePropertyId = 30003;
    public const int UIA_NamePropertyId = 30005;
    public const int UIA_AutomationIdPropertyId = 30011;
    public const int UIA_ClassNamePropertyId = 30012;
    public const int UIA_BoundingRectanglePropertyId = 30001;

    public const int TreeScope_Children = 0x2;
    public const int TreeScope_Descendants = 0x4;

    [StructLayout(LayoutKind.Sequential)]
    public struct RECT { public int left, top, right, bottom; }

    [StructLayout(LayoutKind.Sequential)]
    public struct POINT { public int X, Y; }

    [ComImport, Guid("30CBE57D-D9D0-452A-AB13-7AC5AC4825EE"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    public interface IUIAutomation {
        void _CompareElements();
        void _CompareRuntimeIds();
        void _GetRootElement();
        void ElementFromHandle(IntPtr hwnd, [MarshalAs(UnmanagedType.Interface)] out IUIAutomationElement element);
        void _ElementFromPoint();
        void _GetFocusedElement();
        void _GetRootElementBuildCache();
        void _ElementFromHandleBuildCache();
        void _ElementFromPointBuildCache();
        void _GetFocusedElementBuildCache();
        void _CreateTreeWalker();
        void _get_ControlViewWalker();
        void _get_ContentViewWalker();
        void _get_RawViewWalker();
        void _get_RawViewCondition();
        void _get_ControlViewCondition();
        void _get_ContentViewCondition();
        void _CreateCacheRequest();
        void CreateTrueCondition([MarshalAs(UnmanagedType.Interface)] out object condition);
        void _CreateFalseCondition();
        void CreatePropertyCondition(int propertyId, [MarshalAs(UnmanagedType.Struct)] object value, [MarshalAs(UnmanagedType.Interface)] out object condition);
    }

    [ComImport, Guid("D22108AA-8AC5-49A5-837B-37BBB3D7591E"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    public interface IUIAutomationElement {
        void _SetFocus();
        void _GetRuntimeId();
        void _FindFirst();
        [return: MarshalAs(UnmanagedType.Interface)]
        IUIAutomationElementArray FindAll(int scope, [MarshalAs(UnmanagedType.Interface)] object condition);
        void _FindFirstBuildCache();
        void _FindAllBuildCache();
        void _BuildUpdatedCache();
        void _GetCurrentPropertyValue();
        void _GetCurrentPropertyValueEx();
        void _GetCachedPropertyValue();
        void _GetCachedPropertyValueEx();
        void _GetCurrentPatternAs();
        void _GetCachedPatternAs();
        void _GetCurrentPattern();
        void _GetCachedPattern();
        void _GetCachedParent();
        void _GetCachedChildren();
        void _get_CurrentProcessId();
        int get_CurrentControlType();
        void _get_CurrentLocalizedControlType();
        [return: MarshalAs(UnmanagedType.BStr)]
        string get_CurrentName();
        void _get_CurrentAcceleratorKey();
        void _get_CurrentAccessKey();
        void _get_CurrentHasKeyboardFocus();
        void _get_CurrentIsKeyboardFocusable();
        void _get_CurrentIsEnabled();
        [return: MarshalAs(UnmanagedType.BStr)]
        string get_CurrentAutomationId();
        [return: MarshalAs(UnmanagedType.BStr)]
        string get_CurrentClassName();
        void _get_CurrentHelpText();
        void _get_CurrentCulture();
        void _get_CurrentIsControlElement();
        void _get_CurrentIsContentElement();
        void _get_CurrentIsPassword();
        void _get_CurrentNativeWindowHandle();
        void _get_CurrentItemType();
        void _get_CurrentIsOffscreen();
        void _get_CurrentOrientation();
        void _get_CurrentFrameworkId();
        void _get_CurrentIsRequiredForForm();
        void _get_CurrentItemStatus();
        RECT get_CurrentBoundingRectangle();
    }

    [ComImport, Guid("14314595-B4BC-4055-95F2-58F2E42C9855"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    public interface IUIAutomationElementArray {
        int get_Length();
        [return: MarshalAs(UnmanagedType.Interface)]
        IUIAutomationElement GetElement(int index);
    }

    [DllImport("ole32.dll")]
    static extern int CoInitializeEx(IntPtr pv, uint coInit);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    static extern IntPtr FindWindow(string cls, string title);

    public static IUIAutomation Create() {
        CoInitializeEx(IntPtr.Zero, 2 /*STA*/);
        var t = Type.GetTypeFromCLSID(new Guid("FF48DBA4-60EF-4201-AA87-54103EEF594E"));
        return (IUIAutomation)Activator.CreateInstance(t);
    }

    public static IntPtr FindTaskbar() => FindWindow("Shell_TrayWnd", null);
}
