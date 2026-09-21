using System.Runtime.InteropServices;

namespace Nag0mi.Common.Helper;

// Win32 打开文件对话框（comdlg32）：供字体/背景图文件选择使用。
// ImGui 线程直接调用会阻塞渲染, 调用方应包 Task.Run。
[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
public struct OPENFILENAME
{
    public int lStructSize;
    public IntPtr hwndOwner;
    public IntPtr hInstance;
    public string lpstrFilter;
    public string lpstrCustomFilter;
    public int nMaxCustFilter;
    public int nFilterIndex;
    public string lpstrFile;
    public int nMaxFile;
    public string lpstrFileTitle;
    public int nMaxFileTitle;
    public string lpstrInitialDir;
    public string lpstrTitle;
    public int Flags;
    public short nFileOffset;
    public short nFileExtension;
    public string lpstrDefExt;
    public IntPtr lCustData;
    public IntPtr lpfnHook;
    public string lpTemplateName;
}

public static class Win32FileDialog
{
    [DllImport("comdlg32.dll", SetLastError = true, CharSet = CharSet.Auto)]
    public static extern bool GetOpenFileName(ref OPENFILENAME ofn);

    // 统一的打开文件对话框入口：filter 为 Win32 双零结尾格式（"说明\0*.ext\0"）。
    public static string? OpenFile(string title, string filter)
    {
        try
        {
            var ofn = new OPENFILENAME
            {
                lStructSize = Marshal.SizeOf<OPENFILENAME>(),
                lpstrFilter = filter,
                lpstrFile = new string(new char[512]),
                lpstrFileTitle = new string(new char[128]),
                lpstrTitle = title,
                // OFN_EXPLORER | OFN_FILEMUSTEXIST | OFN_PATHMUSTEXIST | OFN_NOCHANGEDIR
                Flags = 0x00080000 | 0x00001000 | 0x00000800 | 0x00000008,
            };
            ofn.nMaxFile = ofn.lpstrFile.Length;
            ofn.nMaxFileTitle = ofn.lpstrFileTitle.Length;

            if (GetOpenFileName(ref ofn))
                return ofn.lpstrFile;
        }
        catch (Exception ex)
        {
            ECommons.DalamudServices.Svc.Log.Error($"[Nag0mi] 调用文件对话框失败: {ex.Message}");
        }
        return null;
    }
}
