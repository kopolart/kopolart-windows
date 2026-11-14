using Kopolart.Windows.Infrastructure.Helpers;
using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace Kopolart.Windows.UI.Helpers
{
    /// <summary>
    /// Manages the Windows Taskbar Jump List tasks for Kopolart.
    /// Provides quick context actions: Dashboard, Downloads, and Last Visited Page.
    /// </summary>
    public static class TaskbarJumpListManager
    {
        private static readonly Guid IID_IObjectArray = new("92CA9DCD-5622-4bba-A805-5E9F541BD8C8");
        private static bool _initialized;

        /// <summary>
        /// Sets the process explicit AppUserModelID so Windows groups the taskbar button,
        /// notifications, and Jump List under a unified identity.
        /// </summary>
        public static void EnsureAppUserModelId()
        {
            try
            {
                NativeMethods.SetCurrentProcessExplicitAppUserModelID(NativeMethods.AppUserModelId);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[JumpList] SetCurrentProcessExplicitAppUserModelID failed: {ex.Message}");
            }
        }

        /// <summary>
        /// Registers or updates the application Jump List tasks in the Windows Taskbar.
        /// </summary>
        public static void InitializeJumpList()
        {
            if (_initialized) return;

            try
            {
                EnsureAppUserModelId();

                var targetExe = AppPathHelper.GetTargetExecutablePath();
                if (string.IsNullOrWhiteSpace(targetExe) || !File.Exists(targetExe))
                {
                    Debug.WriteLine("[JumpList] Target executable not found. Skipping JumpList initialization.");
                    return;
                }

                var destList = (ICustomDestinationList)new DestinationList();
                destList.SetAppID(NativeMethods.AppUserModelId);

                var iid = IID_IObjectArray;
                int hr = destList.BeginList(out _, ref iid, out _);
                if (hr != 0)
                {
                    Debug.WriteLine($"[JumpList] BeginList failed with HRESULT 0x{hr:X8}");
                    return;
                }

                var collection = (IObjectCollection)new EnumerableObjectCollection();

                // 1. Dashboard Task
                var dashboardLink = CreateShellLink(
                    targetExe: targetExe,
                    arguments: "--dashboard",
                    title: "داشبورد کپل‌آرت",
                    description: "ورود مستقیم به داشبورد و امکانات کپل‌آرت");
                if (dashboardLink != null) collection.AddObject(dashboardLink);

                // 2. Downloads Task
                var downloadsLink = CreateShellLink(
                    targetExe: targetExe,
                    arguments: "--downloads",
                    title: "دانلودها",
                    description: "مشاهده و مدیریت فایل‌های دانلود شده");
                if (downloadsLink != null) collection.AddObject(downloadsLink);

                // 3. Last Visited Page Task
                var lastPageLink = CreateShellLink(
                    targetExe: targetExe,
                    arguments: "--last-page",
                    title: "آخرین صفحه بازشده",
                    description: "ادامه مشاهده آخرین صفحه در کپل‌آرت");
                if (lastPageLink != null) collection.AddObject(lastPageLink);

                destList.AddUserTasks(collection);
                destList.CommitList();

                _initialized = true;
                Debug.WriteLine("[JumpList] Successfully registered Taskbar JumpList tasks.");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[JumpList] InitializeJumpList error: {ex.Message}");
            }
        }

        private static object? CreateShellLink(string targetExe, string arguments, string title, string description)
        {
            try
            {
                var link = (IShellLinkW)new ShellLink();
                link.SetPath(targetExe);
                link.SetArguments(arguments);
                link.SetDescription(description);

                var workingDir = Path.GetDirectoryName(targetExe);
                if (!string.IsNullOrWhiteSpace(workingDir))
                {
                    link.SetWorkingDirectory(workingDir);
                }

                link.SetIconLocation(targetExe, 0);

                var propStore = (IPropertyStore)link;
                var key = PropertyKey.PKEY_Title;
                var pv = PropVariant.FromString(title);
                try
                {
                    propStore.SetValue(ref key, ref pv);
                    propStore.Commit();
                }
                finally
                {
                    pv.Dispose();
                }

                return link;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[JumpList] Failed to create link '{title}': {ex.Message}");
                return null;
            }
        }

        #region Native COM Definitions

        [ComImport]
        [Guid("77f101b4-e653-4bc8-b507-ab662d210541")]
        [ClassInterface(ClassInterfaceType.None)]
        private class DestinationList { }

        [ComImport]
        [Guid("6332debf-87b5-4670-90c0-5e57b408a49e")]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        private interface ICustomDestinationList
        {
            void SetAppID([MarshalAs(UnmanagedType.LPWStr)] string pszAppID);
            [PreserveSig]
            int BeginList(out uint pcMinSlots, [In] ref Guid riid, [Out, MarshalAs(UnmanagedType.Interface)] out object ppv);
            [PreserveSig]
            int AppendCategory([MarshalAs(UnmanagedType.LPWStr)] string pszCategory, [In, MarshalAs(UnmanagedType.Interface)] object poa);
            void AppendKnownCategory(int category);
            [PreserveSig]
            int AddUserTasks([In, MarshalAs(UnmanagedType.Interface)] object poa);
            void CommitList();
            void GetRemovedDestinations([In] ref Guid riid, [Out, MarshalAs(UnmanagedType.Interface)] out object ppv);
            void DeleteList([MarshalAs(UnmanagedType.LPWStr)] string pszAppID);
            void AbortList();
        }

        [ComImport]
        [Guid("2d3468c1-36a7-43b6-ac24-d3f02fd9607a")]
        [ClassInterface(ClassInterfaceType.None)]
        private class EnumerableObjectCollection { }

        [ComImport]
        [Guid("5632b1a4-e38a-400a-928a-d4cd63230295")]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        private interface IObjectCollection
        {
            [PreserveSig]
            int GetCount(out uint pcObjects);
            [PreserveSig]
            int GetAt([In] uint uiIndex, [In] ref Guid riid, [Out, MarshalAs(UnmanagedType.Interface)] out object ppv);
            [PreserveSig]
            int AddObject([In, MarshalAs(UnmanagedType.Interface)] object pvObject);
            [PreserveSig]
            int AddFromArray([In, MarshalAs(UnmanagedType.Interface)] object poaSource);
            [PreserveSig]
            int RemoveObjectAt([In] uint uiIndex);
            [PreserveSig]
            int Clear();
        }

        [ComImport]
        [Guid("00021401-0000-0000-C000-000000000046")]
        private class ShellLink { }

        [ComImport]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        [Guid("000214F9-0000-0000-C000-000000000046")]
        private interface IShellLinkW
        {
            void GetPath([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszFile, int cchMaxPath, out IntPtr pfd, uint fFlags);
            void GetIDList(out IntPtr ppidl);
            void SetIDList(IntPtr pidl);
            void GetDescription([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszName, int cchMaxName);
            void SetDescription([MarshalAs(UnmanagedType.LPWStr)] string pszName);
            void GetWorkingDirectory([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszDir, int cchMaxPath);
            void SetWorkingDirectory([MarshalAs(UnmanagedType.LPWStr)] string pszDir);
            void GetArguments([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszArgs, int cchMaxPath);
            void SetArguments([MarshalAs(UnmanagedType.LPWStr)] string pszArgs);
            void GetHotkey(out short pwHotkey);
            void SetHotkey(short wHotkey);
            void GetShowCmd(out int piShowCmd);
            void SetShowCmd(int iShowCmd);
            void GetIconLocation([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszIconPath, int cchIconPath, out int piIcon);
            void SetIconLocation([MarshalAs(UnmanagedType.LPWStr)] string pszIconPath, int iIcon);
            void SetRelativePath([MarshalAs(UnmanagedType.LPWStr)] string pszPathRel, uint dwReserved);
            void Resolve(IntPtr hwnd, uint fFlags);
            void SetPath([MarshalAs(UnmanagedType.LPWStr)] string pszFile);
        }

        [ComImport]
        [Guid("886d8eeb-8cf2-4446-8d02-cdba1dbdcf99")]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        private interface IPropertyStore
        {
            [PreserveSig]
            int GetCount(out uint cProps);
            [PreserveSig]
            int GetAt([In] uint iProp, out PropertyKey pkey);
            [PreserveSig]
            int GetValue([In] ref PropertyKey key, out PropVariant pv);
            [PreserveSig]
            int SetValue([In] ref PropertyKey key, [In] ref PropVariant pv);
            [PreserveSig]
            int Commit();
        }

        [StructLayout(LayoutKind.Sequential, Pack = 4)]
        private struct PropertyKey
        {
            public Guid fmtid;
            public uint pid;

            public PropertyKey(Guid guid, uint id)
            {
                fmtid = guid;
                pid = id;
            }

            public static readonly PropertyKey PKEY_Title = new(new Guid("F29F85E0-4FF9-1068-AB91-08002B27B3D9"), 2);
        }

        [StructLayout(LayoutKind.Explicit)]
        private struct PropVariant : IDisposable
        {
            [FieldOffset(0)]
            public ushort vt;
            [FieldOffset(8)]
            public IntPtr pointerValue;

            public static PropVariant FromString(string val)
            {
                return new PropVariant
                {
                    vt = 31, // VT_LPWSTR
                    pointerValue = Marshal.StringToCoTaskMemUni(val)
                };
            }

            public void Dispose()
            {
                if (pointerValue != IntPtr.Zero)
                {
                    Marshal.FreeCoTaskMem(pointerValue);
                    pointerValue = IntPtr.Zero;
                    vt = 0;
                }
            }
        }

        #endregion
    }
}
