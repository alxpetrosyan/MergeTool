using System.Runtime.InteropServices;

namespace MergeTool.Folders.Operations;

/// <summary>Windows Recycle Bin delete via SHFileOperation; unavailable on other platforms.</summary>
public static class RecycleBinSupport
{
    public static bool IsAvailable => OperatingSystem.IsWindows();

    public static void SendToRecycleBin(string fullPath)
    {
        if (!IsAvailable)
        {
            throw new PlatformNotSupportedException("Recycle Bin is only available on Windows.");
        }

        string path = Path.GetFullPath(fullPath);
        if (!path.EndsWith('\0'))
        {
            path += '\0';
        }

        SHFILEOPSTRUCT fileOp = new()
        {
            WFunc = FO_DELETE,
            PFrom = path,
            FFlags = FOF_ALLOWUNDO | FOF_NOCONFIRMATION | FOF_SILENT | FOF_NOERRORUI,
        };

        int result = SHFileOperation(ref fileOp);
        if (result != 0 || fileOp.FAnyOperationsAborted)
        {
            throw new IOException($"Recycle Bin delete failed for '{fullPath}' (HRESULT 0x{result:X8}).");
        }
    }

    private const int FO_DELETE = 0x0003;
    private const ushort FOF_SILENT = 0x0004;
    private const ushort FOF_NOCONFIRMATION = 0x0010;
    private const ushort FOF_ALLOWUNDO = 0x0040;
    private const ushort FOF_NOERRORUI = 0x0400;

    [DllImport("shell32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern int SHFileOperation(ref SHFILEOPSTRUCT fileOp);

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct SHFILEOPSTRUCT
    {
        public IntPtr Hwnd;
        public int WFunc;
        public string PFrom;
        public string? PTo;
        public ushort FFlags;
        public bool FAnyOperationsAborted;
        public IntPtr HNameMappings;
        public string? LpszProgressTitle;
    }
}
