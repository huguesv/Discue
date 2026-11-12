// Copyright (c) Hugues Valois. All rights reserved.
// Licensed under the MIT license. See LICENSE in the project root for license information.

namespace Woohoo.Discue.Helpers;

using System;
using System.Runtime.InteropServices;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Windows.UI;
using Windows.UI.ViewManagement;

// Helper class to workaround custom title bar bugs.
// DISCLAIMER: The resource key names and color values used below are subject to change. Do not depend on them.
// https://github.com/microsoft/TemplateStudio/issues/4516
internal static class TitleBarHelper
{
    private const int WAINACTIVE = 0x00;
    private const int WAACTIVE = 0x01;
    private const int WMACTIVATE = 0x0006;

    public static void UpdateTitleBar(Window mainWindow, ElementTheme theme)
    {
        if (mainWindow.ExtendsContentIntoTitleBar)
        {
            if (theme == ElementTheme.Default)
            {
                var uiSettings = new UISettings();
                var background = uiSettings.GetColorValue(UIColorType.Background);

                theme = background == Colors.White ? ElementTheme.Light : ElementTheme.Dark;
            }

            if (theme == ElementTheme.Default)
            {
                theme = Application.Current.RequestedTheme == ApplicationTheme.Light ? ElementTheme.Light : ElementTheme.Dark;
            }

            mainWindow.AppWindow.TitleBar.ButtonForegroundColor = theme switch
            {
                ElementTheme.Dark => Colors.White,
                ElementTheme.Light => Colors.Black,
                _ => Colors.Transparent,
            };

            mainWindow.AppWindow.TitleBar.ButtonInactiveForegroundColor = theme switch
            {
                ElementTheme.Dark => Colors.Gray,
                ElementTheme.Light => Colors.Gray,
                _ => Colors.Transparent,
            };

            mainWindow.AppWindow.TitleBar.ButtonHoverForegroundColor = theme switch
            {
                ElementTheme.Dark => Colors.White,
                ElementTheme.Light => Colors.Black,
                _ => Colors.Transparent,
            };

            mainWindow.AppWindow.TitleBar.ButtonHoverBackgroundColor = theme switch
            {
                ElementTheme.Dark => Color.FromArgb(0x33, 0xFF, 0xFF, 0xFF),
                ElementTheme.Light => Color.FromArgb(0x33, 0x00, 0x00, 0x00),
                _ => Colors.Transparent,
            };

            mainWindow.AppWindow.TitleBar.ButtonPressedBackgroundColor = theme switch
            {
                ElementTheme.Dark => Color.FromArgb(0x66, 0xFF, 0xFF, 0xFF),
                ElementTheme.Light => Color.FromArgb(0x66, 0x00, 0x00, 0x00),
                _ => Colors.Transparent,
            };

            mainWindow.AppWindow.TitleBar.BackgroundColor = Colors.Transparent;

            var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(mainWindow);
            if (hwnd == GetActiveWindow())
            {
                SendMessage(hwnd, WMACTIVATE, WAINACTIVE, nint.Zero);
                SendMessage(hwnd, WMACTIVATE, WAACTIVE, nint.Zero);
            }
            else
            {
                SendMessage(hwnd, WMACTIVATE, WAACTIVE, nint.Zero);
                SendMessage(hwnd, WMACTIVATE, WAINACTIVE, nint.Zero);
            }
        }
    }

    [DllImport("user32.dll")]
    private static extern nint GetActiveWindow();

    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    private static extern nint SendMessage(nint hWnd, int msg, int wParam, nint lParam);
}
