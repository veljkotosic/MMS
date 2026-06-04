namespace MMS.Core.FileFormat.Compression.ShannonFano;

public struct LookUpTableEntry
{
    public byte Value;
    public byte Length;
    public bool IsValid;
}