using System.IO.Hashing;
using System.Runtime.InteropServices;
using MMS.Core.FileFormat;
using MMS.Core.ImageResource;
using MMS.Core.Utility;

namespace MMS.Core.FileManager;

public class MmsFileManager : 
    IFileManager<MmsFile>, 
    IFileManager<IImageResource>
{
    public MmsFile LoadImage(string path)
    {
        using var stream = File.OpenRead(path);
        using var reader = new BinaryReader(stream);
        var crc32 = new Crc32();

        var headerSize = Marshal.SizeOf<MmsHeader>();
        var headerBytes = reader.ReadBytes(headerSize);
        
        if (headerBytes.Length < headerSize)
        {
            throw new InvalidDataException("Invalid header.");
        }        
        
        crc32.Append(headerBytes);
        
        var header = StructureUtility.BytesToStructure<MmsHeader>(headerBytes);
        
        if (header.Magic != MmsHeader.MagicValue)
        {
            throw new InvalidDataException("Invalid magic number.");
        }        
        
        if (header.HeaderLength != headerSize)
        {
            throw new InvalidDataException("Invalid header size.");
        }        
        
        byte[] meta = [];
        
        if (header.MetadataLength > 0)
        {
            meta = reader.ReadBytes((int)header.MetadataLength);
            crc32.Append(meta);           
        }
        
        var pixels = reader.ReadBytes((int)header.PixelsLength);
        crc32.Append(pixels);       
        
        var storedCrc = reader.ReadUInt32();
        var calculatedCrc = crc32.GetCurrentHashAsUInt32();

        if (calculatedCrc != storedCrc)
        {
            throw new InvalidDataException("Corrupted file. CRC mismatch.");
        }
        
        return new MmsFile
        {
            Header = header,
            Metadata = meta,
            Pixels = pixels,
            Crc32 = storedCrc
        };
    }

    public void SaveImage(string path, IImageResource data) => SaveImage(path, (MmsFile)data);  

    public void SaveImage(string path, MmsFile data)
    {
        using var stream = File.Create(path);
        using var writer = new BinaryWriter(stream);
        var crc32 = new Crc32();       

        var headerBytes = StructureUtility.StructureToBytes(data.Header);
        
        writer.Write(headerBytes);
        crc32.Append(headerBytes);      

        if (data.Metadata.Length > 0)
        {
            writer.Write(data.Metadata);
            crc32.Append(data.Metadata);         
        }

        writer.Write(data.Pixels);
        crc32.Append(data.Pixels);      

        writer.Write(crc32.GetCurrentHashAsUInt32());       
    }

    IImageResource IFileManager<IImageResource>.LoadImage(string path) => LoadImage(path);   
}