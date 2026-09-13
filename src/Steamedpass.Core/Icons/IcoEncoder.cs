using System.Drawing;
using System.Drawing.Imaging;

namespace Steamedpass.Core.Icons;

/// <summary>
/// Writes a single-frame, PNG-compressed .ico file (the format Windows Vista+
/// supports for icons larger than 256x256) from a bitmap.
/// </summary>
public static class IcoEncoder
{
    public static void SaveAsIco(Bitmap bitmap, string destinationPath)
    {
        using var pngStream = new MemoryStream();
        bitmap.Save(pngStream, ImageFormat.Png);
        byte[] pngBytes = pngStream.ToArray();

        using var fileStream = new FileStream(destinationPath, FileMode.Create, FileAccess.Write);
        using var writer = new BinaryWriter(fileStream);

        // ICONDIR
        writer.Write((ushort)0);   // reserved
        writer.Write((ushort)1);   // type: 1 = icon
        writer.Write((ushort)1);   // image count

        // ICONDIRENTRY
        writer.Write((byte)(bitmap.Width >= 256 ? 0 : bitmap.Width));
        writer.Write((byte)(bitmap.Height >= 256 ? 0 : bitmap.Height));
        writer.Write((byte)0);     // color palette: none
        writer.Write((byte)0);     // reserved
        writer.Write((ushort)1);   // color planes
        writer.Write((ushort)32);  // bits per pixel
        writer.Write((uint)pngBytes.Length);
        writer.Write((uint)22);    // offset of image data (6 + 16 header bytes)

        writer.Write(pngBytes);
    }
}
