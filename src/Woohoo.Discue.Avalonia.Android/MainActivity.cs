// Copyright (c) Hugues Valois. All rights reserved.
// Licensed under the MIT license. See LICENSE in the project root for license information.

namespace Woohoo.Discue.Avalonia.Android;

using global::Android.App;
using global::Android.Content.PM;
using global::Avalonia.Android;

[Activity(
    Label = "Woohoo.Discue.Avalonia.Android",
    Theme = "@style/MyTheme.NoActionBar",
    Icon = "@drawable/icon",
    MainLauncher = true,
    ConfigurationChanges = ConfigChanges.Orientation | ConfigChanges.ScreenSize | ConfigChanges.UiMode)]
public class MainActivity : AvaloniaMainActivity
{
    public static MainActivity? Instance { get; private set; }

    protected override void OnCreate(Bundle? savedInstanceState)
    {
        Instance = this;
        base.OnCreate(savedInstanceState);
    }

    protected override void OnDestroy()
    {
        Instance = null;
        base.OnDestroy();
    }
}
