using System.Runtime.InteropServices;

namespace MMS.Core.Utility;

internal static class StructureUtility
{
    internal static byte[] StructureToBytes<T>(T structure) 
        where T : struct
    {
        var size = Marshal.SizeOf(structure);
        var bytes = new byte[size];
        
        IntPtr ptr = Marshal.AllocHGlobal(size);
        
        Marshal.StructureToPtr(structure, ptr, true);
        Marshal.Copy(ptr, bytes, 0, size);
        Marshal.FreeHGlobal(ptr);
        
        return bytes;
    }

    internal static T BytesToStructure<T>(byte[] bytes)
        where T : struct
    {
        var size = Marshal.SizeOf<T>();
        IntPtr ptr = Marshal.AllocHGlobal(size);
        
        Marshal.Copy(bytes, 0, ptr, size);
        
        T structure = Marshal.PtrToStructure<T>(ptr);
        
        Marshal.FreeHGlobal(ptr);
        
        return structure;
    }
}