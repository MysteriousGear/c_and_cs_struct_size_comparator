using System;
using System.Runtime.InteropServices;

namespace StructSizeComparer
{
    // Simple struct with basic types
    [StructLayout(LayoutKind.Sequential, Pack = 1, Size = 17)]
    public struct SimpleStruct
    {
        public int id;
        public bool flag;
        public char letter;
        public double value;

        // Returns the size of this struct in bytes
        public static int Size() => Marshal.SizeOf<SimpleStruct>();
    }

    // Struct with arrays
    [StructLayout(LayoutKind.Sequential, Pack = 1, Size = 30)]
    public struct ArrayStruct
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 5)]
        public int[] counts;
        
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 10)]
        public byte[] name;

        // Returns the size of this struct in bytes
        public static int Size() => Marshal.SizeOf<ArrayStruct>();
    }

    // Nested struct example
    [StructLayout(LayoutKind.Sequential, Pack = 1, Size = 8)]
    public struct Point
    {
        public int x;
        public int y;

        // Returns the size of this struct in bytes
        public static int Size() => Marshal.SizeOf<Point>();
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1, Size = 16)]
    public struct Rectangle
    {
        public Point topLeft;
        public Point bottomRight;

        // Returns the size of this struct in bytes
        public static int Size() => Marshal.SizeOf<Rectangle>();
    }

    // Complex struct with mixed types
    [StructLayout(LayoutKind.Sequential, Pack = 1, Size = 35)]
    public struct ComplexStruct
    {
        public long id;
        public ushort flags;
        
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)]
        public double[] values;
        
        public byte type;

        // Returns the size of this struct in bytes
        public static int Size() => Marshal.SizeOf<ComplexStruct>();
    }

    // A struct with a missing pair in the .h file
    [StructLayout(LayoutKind.Sequential, Pack = 1, Size = 8)]
    public struct CSharpOnlyStruct
    {
        public int id;
        public int value;

        // Returns the size of this struct in bytes
        public static int Size() => Marshal.SizeOf<CSharpOnlyStruct>();
    }
}
