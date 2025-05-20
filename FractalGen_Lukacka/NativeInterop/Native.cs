using System;
using System.Runtime.InteropServices;
using Microsoft.AspNetCore.Mvc;
using FractalExplorer.Native;
using SixLabors.ImageSharp;                   // Image
using SixLabors.ImageSharp.PixelFormats;      // Rgb24

namespace FractalExplorer.Native;

internal static class NativeFractal
{
    [StructLayout(LayoutKind.Sequential)]
    public struct Params
    {
        public double xCenter;
        public double yCenter;
        public double zoom;
        public int width;
        public int height;
        public int maxIter;
        public int palette;   // 0=gray,1=rainbow,2=fire
    }

    private const string CpuDll = "fractal_omp.dll";
    private const string GpuDll = "fractal_cuda.dll";

    [DllImport(CpuDll, EntryPoint = "GenerateFractal",
               CallingConvention = CallingConvention.Cdecl)]
    private static extern int CpuGenerate(ref Params p, IntPtr rgb);

    [DllImport(GpuDll, EntryPoint = "GenerateFractal",
               CallingConvention = CallingConvention.Cdecl)]
    private static extern int GpuGenerate(ref Params p, IntPtr rgb);

    public static void Generate(bool useGpu, ref Params p, Span<byte> rgb)
    {
        if (rgb.Length != p.width * p.height * 3)
            throw new ArgumentException("RGB buffer size mismatch.");

        // pin the span
        unsafe
        {
            fixed (byte* ptr = rgb)
            {
                int err = useGpu
                    ? GpuGenerate(ref p, (IntPtr)ptr)
                    : CpuGenerate(ref p, (IntPtr)ptr);
                if (err != 0) throw new InvalidOperationException($"Native error {err}");
            }
        }
    }
}
