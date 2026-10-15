using System;
using Windows.ApplicationModel.DataTransfer;

namespace SeaGlass.Sample.Common;

/// <summary>
/// Helper to safely copy text to the Windows system clipboard.
/// </summary>
public static class ClipboardHelper
{
    public static bool CopyText(string text)
    {
        if (string.IsNullOrEmpty(text))
            return false;

        try
        {
            var dataPackage = new DataPackage();
            dataPackage.SetText(text);
            Clipboard.SetContent(dataPackage);
            return true;
        }
        catch
        {
            return false;
        }
    }
}
