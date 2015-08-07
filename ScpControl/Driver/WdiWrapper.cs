﻿using System;
using System.Runtime.InteropServices;

namespace ScpControl.Driver
{
    public enum WdiErrorCode : int
    {
        WDI_SUCCESS = 0,
        WDI_ERROR_IO = -1,
        WDI_ERROR_INVALID_PARAM = -2,
        WDI_ERROR_ACCESS = -3,
        WDI_ERROR_NO_DEVICE = -4,
        WDI_ERROR_NOT_FOUND = -5,
        WDI_ERROR_BUSY = -6,
        WDI_ERROR_TIMEOUT = -7,
        WDI_ERROR_OVERFLOW = -8,
        WDI_ERROR_PENDING_INSTALLATION = -9,
        WDI_ERROR_INTERRUPTED = -10,
        WDI_ERROR_RESOURCE = -11,
        WDI_ERROR_NOT_SUPPORTED = -12,
        WDI_ERROR_EXISTS = -13,
        WDI_ERROR_USER_CANCEL = -14,
        WDI_ERROR_NEEDS_ADMIN = -15,
        WDI_ERROR_WOW64 = -16,
        WDI_ERROR_INF_SYNTAX = -17,
        WDI_ERROR_CAT_MISSING = -18,
        WDI_ERROR_UNSIGNED = -19,
        WDI_ERROR_OTHER = -99
    }

    public static class WdiWrapper
    {
        public enum WdiDriverType : int
        {
            WDI_WINUSB,
            WDI_LIBUSB0,
            WDI_LIBUSBK,
            WDI_USER,
            WDI_NB_DRIVERS
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
        private struct wdi_device_info
        {
            public IntPtr next;
            public ushort vid;
            public ushort pid;
            public bool is_composite;
            public char mi;
            [MarshalAs(UnmanagedType.LPStr)]
            public string desc;
            public IntPtr driver;
            [MarshalAs(UnmanagedType.LPStr)]
            public string device_id;
            [MarshalAs(UnmanagedType.LPStr)]
            public string hardware_id;
            [MarshalAs(UnmanagedType.LPStr)]
            public string compatible_id;
            [MarshalAs(UnmanagedType.LPStr)]
            public string upper_filter;
            public UInt64 driver_version;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct wdi_options_create_list
        {
            public bool list_all;
            public bool list_hubs;
            public bool trim_whitespaces;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct wdi_options_prepare_driver
        {
            [MarshalAs(UnmanagedType.I4)]
            public WdiDriverType driver_type;
            [MarshalAs(UnmanagedType.LPStr)]
            public string vendor_name;
            [MarshalAs(UnmanagedType.LPStr)]
            public string device_guid;
            public bool disable_cat;
            public bool disable_signing;
            [MarshalAs(UnmanagedType.LPStr)]
            public string cert_subject;
            public bool use_wcid_driver;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct wdi_options_install_driver
        {
            public IntPtr hWnd;
            public bool install_filter_driver;
            public UInt32 pending_install_timeout;
        }

        [DllImport("libwdi.dll", EntryPoint = "wdi_strerror", ExactSpelling = false)]
        private static extern IntPtr wdi_strerror(int errcode);

        [DllImport("libwdi.dll", EntryPoint = "wdi_get_vendor_name", ExactSpelling = false)]
        private static extern IntPtr wdi_get_vendor_name(ushort vid);

        [DllImport("libwdi.dll", EntryPoint = "wdi_create_list", ExactSpelling = false)]
        private static extern int wdi_create_list(ref IntPtr list,
            ref wdi_options_create_list options);

        [DllImport("libwdi.dll", EntryPoint = "wdi_prepare_driver", ExactSpelling = false)]
        private static extern WdiErrorCode wdi_prepare_driver(IntPtr device_info,
            [MarshalAs(UnmanagedType.LPStr)] string path,
            [MarshalAs(UnmanagedType.LPStr)] string inf_name,
            ref wdi_options_prepare_driver options);

        [DllImport("libwdi.dll", EntryPoint = "wdi_install_driver", ExactSpelling = false)]
        private static extern WdiErrorCode wdi_install_driver(IntPtr device_info,
            [MarshalAs(UnmanagedType.LPStr)] string path,
            [MarshalAs(UnmanagedType.LPStr)] string inf_name,
            ref wdi_options_install_driver options);

        [DllImport("libwdi.dll", EntryPoint = "wdi_destroy_list", ExactSpelling = false)]
        private static extern int wdi_destroy_list(IntPtr list);
        
        [DllImport("libwdi.dll", EntryPoint = "wdi_get_wdf_version", ExactSpelling = false)]
        private static extern int wdi_get_wdf_version();

        public static WdiErrorCode InstallWinUsbDriver(string hardwareId, string deviceGuid, string driverPath, string infName, IntPtr hwnd)
        {
            var result = WdiErrorCode.WDI_SUCCESS;
            IntPtr pList = IntPtr.Zero;
            var listOpts = new wdi_options_create_list
            {
                list_all = true,
                list_hubs = false,
                trim_whitespaces = false
            };

            var prepOpts = new wdi_options_prepare_driver
            {
                driver_type = WdiDriverType.WDI_WINUSB,
                device_guid = deviceGuid
            };

            var intOpts = new wdi_options_install_driver { hWnd = hwnd };

            wdi_create_list(ref pList, ref listOpts);
            var devices = pList;

            while (pList != IntPtr.Zero)
            {
                var info = (wdi_device_info)Marshal.PtrToStructure(pList, typeof(wdi_device_info));

                if (string.CompareOrdinal(info.hardware_id, hardwareId) == 0)
                {
                    if (wdi_prepare_driver(pList, driverPath, infName, ref prepOpts) == WdiErrorCode.WDI_SUCCESS)
                    {
                        result = wdi_install_driver(pList, driverPath, infName, ref intOpts);
                        break;
                    }
                }

                pList = info.next;
            }

            wdi_destroy_list(devices);

            return result;
        }
        
        public static string GetErrorMessage(WdiErrorCode errcode)
        {
            var msgPtr = wdi_strerror((int)errcode);
            return Marshal.PtrToStringAnsi(msgPtr);
        }

        public static string GetVendorName(ushort vendorId)
        {
            var namePtr = wdi_get_vendor_name(vendorId);
            return Marshal.PtrToStringAnsi(namePtr);
        }

        public static int WdfVersion
        {
            get { return wdi_get_wdf_version(); }
        }
    }
}