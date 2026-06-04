namespace MMS.Core.FileFormat.Compression.ShannonFano;

internal class ShannonFanoTreeNode
{
    public ShannonFanoTreeNode? Left { get; set; }
    public ShannonFanoTreeNode? Right { get; set; }
    public byte? Value { get; set; }
}