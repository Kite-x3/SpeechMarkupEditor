using System;
using Avalonia.Controls;
using Avalonia.Media.Imaging;
using Avalonia.Platform;

namespace SpeechMarkupEditor.Assets;

public static class WindowIconFactory
{
    private static readonly Lazy<WindowIcon?> AppIcon = new(() => LoadIcon(AssetPaths.APP_ICON_URI));

    public static void ApplyAppIcon(Window window)
    {
        window.Icon = AppIcon.Value;
    }

    public static WindowIcon? CreateIcon(string uri)
    {
        return LoadIcon(uri);
    }

    private static WindowIcon? LoadIcon(string uri)
    {
        try
        {
            using var bitmap = new Bitmap(AssetLoader.Open(new Uri(uri)));
            return new WindowIcon(bitmap);
        }
        catch
        {
            return null;
        }
    }
}
