using System;
using System.Runtime.InteropServices;

internal static class ScoreFixture
{
    [DllImport("kernel32.dll")] static extern IntPtr VirtualAlloc(IntPtr at, UIntPtr size, uint type, uint protection);
    [DllImport("kernel32.dll")] static extern bool VirtualFree(IntPtr at, UIntPtr size, uint type);
    static IntPtr image;
    static void Put(int offset, byte[] bytes) { Marshal.Copy(bytes, 0, new IntPtr(image.ToInt64()+offset), bytes.Length); }
    static void Field(int index, int kind, double value)
    {
        int obj = 0x20000+index*32;
        Put(0x10000+index*8+4, BitConverter.GetBytes((uint)(image.ToInt64()+obj)));
        Put(obj, BitConverter.GetBytes((uint)(image.ToInt64()+0x6B8BF4)));
        Put(obj+4, BitConverter.GetBytes(kind));
        Put(obj+8, kind==1 ? BitConverter.GetBytes((int)value) : BitConverter.GetBytes(value));
    }
    static int Main()
    {
        image = VirtualAlloc(IntPtr.Zero, (UIntPtr)0xC10000, 0x3000, 4);
        if (image==IntPtr.Zero) return 1;
        try
        {
            Put(0xBFFCD8, BitConverter.GetBytes((uint)(image.ToInt64()+0x10000)));
            Field(0x24F, 2, 60); Field(0x271, 1, 1);
            Field(0x24A, 2, 500); Field(0x69, 2, 1000); Field(0x254, 1, 995);
            Console.WriteLine(image.ToInt64()); Console.Out.Flush();
            if (Console.ReadLine()!="check") return 2;
            foreach(int index in new[] {0x24A,0x69,0x254})
            {
                byte[] bytes = new byte[16]; Marshal.Copy(new IntPtr(image.ToInt64()+0x20000+index*32),bytes,0,16);
                double value = BitConverter.ToInt32(bytes,4)==1 ? BitConverter.ToInt32(bytes,8) : BitConverter.ToDouble(bytes,8);
                if (value!=200000) return 3;
            }
            Console.WriteLine("PASS"); return 0;
        }
        finally { VirtualFree(image,UIntPtr.Zero,0x8000); }
    }
}
