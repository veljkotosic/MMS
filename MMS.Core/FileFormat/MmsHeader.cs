using System.Runtime.InteropServices;
using MMS.Core.FileFormat.Colorspace;
using MMS.Core.FileFormat.Compression;

namespace MMS.Core.FileFormat;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct MmsHeader
{
    public uint Magic; 
    public ushort Version;
    public ushort HeaderLength;
    public uint Width;
    public uint Height;
    public byte Channels;
    public MmsColorspace Colorspace;
    public MmsCompression Compression;
    public byte Reserved;
    public uint MetadataLength;
    public uint PixelsLength;
    
    public const uint MagicValue = 0x3049534D;
}