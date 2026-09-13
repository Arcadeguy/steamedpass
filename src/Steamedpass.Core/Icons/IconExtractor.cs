using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using Vanara.PInvoke;
using static Vanara.PInvoke.Gdi32;
using static Vanara.PInvoke.Shell32;

namespace Steamedpass.Core.Icons;

/// <summary>
/// Extracts the icon Windows Explorer shows for a UWP app, given its AUMID,
/// via IShellItemImageFactory on "shell:AppsFolder\&lt;AUMID&gt;" - no shortcut
/// file is created. Returns null if extraction fails (caller should leave the
/// destination shortcut's icon unset rather than show a wrong icon).
/// </summary>
public static class IconExtractor
{
    private const int IconSize = 256;

    public static Bitmap? TryExtractIcon(string aumid)
    {
        try
        {
            string parsingName = $@"shell:AppsFolder\{aumid}";
            var item = SHCreateItemFromParsingName<IShellItem>(parsingName, null);
            var factory = (IShellItemImageFactory)item;

            factory.GetImage(new SIZE(IconSize, IconSize),
                SIIGBF.SIIGBF_ICONONLY | SIIGBF.SIIGBF_BIGGERSIZEOK,
                out SafeHBITMAP hBitmap).ThrowIfFailed();

            using (hBitmap)
            {
                return ToStraightAlphaBitmap(hBitmap);
            }
        }
        catch
        {
            return null;
        }
    }

    private static Bitmap ToStraightAlphaBitmap(SafeHBITMAP hBitmap)
    {
        IntPtr handle = ((HBITMAP)hBitmap).DangerousGetHandle();

        if (GetObject(handle, Marshal.SizeOf<BITMAP>(), out BITMAP bmp) == 0)
        {
            throw new InvalidOperationException("GetObject failed while reading the extracted icon bitmap.");
        }

        // GetImage() returns a top-down 32bpp DIB section with premultiplied alpha;
        // wrap it directly, then clone into straight alpha for PNG encoding.
        using var premultiplied = new Bitmap(bmp.bmWidth, bmp.bmHeight, bmp.bmWidthBytes,
            PixelFormat.Format32bppPArgb, bmp.bmBits);

        return premultiplied.Clone(new Rectangle(0, 0, bmp.bmWidth, bmp.bmHeight), PixelFormat.Format32bppArgb);
    }

    [DllImport("gdi32.dll")]
    private static extern int GetObject(IntPtr hObject, int nCount, out BITMAP lpObject);

    [StructLayout(LayoutKind.Sequential)]
    private struct BITMAP
    {
        public int bmType;
        public int bmWidth;
        public int bmHeight;
        public int bmWidthBytes;
        public ushort bmPlanes;
        public ushort bmBitsPixel;
        public IntPtr bmBits;
    }
}
