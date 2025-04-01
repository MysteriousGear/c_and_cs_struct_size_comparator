using System;
using System.Runtime.InteropServices;

namespace StructSizeComparer
{
    // Simple struct with basic types (17 bytes)
    [StructLayout(LayoutKind.Sequential, Pack = 1, Size = 17)]
    public struct SimpleStruct
    {
        public int id;        // 4 bytes
        public bool flag;     // 1 byte
        public char letter;   // 2 bytes in C# (UTF-16)
        public double value;  // 8 bytes

        // Returns the size of this struct in bytes
        public static int Size() => Marshal.SizeOf<SimpleStruct>();
    }

    // Struct with arrays (30 bytes)
    [StructLayout(LayoutKind.Sequential, Pack = 1, Size = 30)]
    public struct ArrayStruct
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 5)]
        public int[] counts;  // 5 * 4 = 20 bytes
        
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 10)]
        public byte[] name;   // 10 bytes

        // Returns the size of this struct in bytes
        public static int Size() => Marshal.SizeOf<ArrayStruct>();
    }

    // Nested struct example (8 bytes)
    [StructLayout(LayoutKind.Sequential, Pack = 1, Size = 8)]
    public struct Point
    {
        public int x;         // 4 bytes
        public int y;         // 4 bytes

        // Returns the size of this struct in bytes
        public static int Size() => Marshal.SizeOf<Point>();
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1, Size = 16)]
    public struct Rectangle
    {
        public Point topLeft;     // 8 bytes
        public Point bottomRight; // 8 bytes

        // Returns the size of this struct in bytes
        public static int Size() => Marshal.SizeOf<Rectangle>();
    }

    // Complex struct with mixed types (35 bytes)
    [StructLayout(LayoutKind.Sequential, Pack = 1, Size = 35)]
    public struct ComplexStruct
    {
        public long id;           // 8 bytes
        public ushort flags;      // 2 bytes
        
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)]
        public double[] values;   // 3 * 8 = 24 bytes
        
        public byte type;         // 1 byte

        // Returns the size of this struct in bytes
        public static int Size() => Marshal.SizeOf<ComplexStruct>();
    }

    // A struct with a missing pair in the .h file
    [StructLayout(LayoutKind.Sequential, Pack = 1, Size = 8)]
    public struct CSharpOnlyStruct
    {
        public int id;         // 4 bytes
        public int value;      // 4 bytes

        // Returns the size of this struct in bytes
        public static int Size() => Marshal.SizeOf<CSharpOnlyStruct>();
    }
}
