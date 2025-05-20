using Microsoft.AspNetCore.Mvc;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using FractalExplorer.Native;
using static System.Net.Mime.MediaTypeNames;
using Image = SixLabors.ImageSharp.Image;

namespace FractalExplorer.Web.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class FractalController : ControllerBase
{
    [HttpGet("generate")]
    public IActionResult Generate(
        double x,           // ?x=-0.5
        double y,           // ?y=0
        double zoom,        // ?zoom=1
        int iterations,     // ?iterations=1000
        int w, int h,       // ?w=800&h=800
        string palette,     // ?palette=grayscale|rainbow|fire
        string backend = "gpu") // ?backend=gpu|cpu
    {
        int paletteId = palette switch
        {
            "grayscale" => 0,
            "rainbow" => 1,
            "fire" => 2,
            _ => 0
        };

        var par = new NativeFractal.Params
        {
            xCenter = x,
            yCenter = y,
            zoom = zoom,
            width = w,
            height = h,
            maxIter = iterations,
            palette = paletteId
        };

        byte[] rgb = GC.AllocateUninitializedArray<byte>(w * h * 3);
        NativeFractal.Generate(useGpu: backend.Equals("gpu", StringComparison.OrdinalIgnoreCase),
                               ref par, rgb);

        // Convert raw RGB to PNG
        using var img = Image.WrapMemory<Rgb24>(rgb, w, h);
        var ms = new MemoryStream();
        img.SaveAsPng(ms);
        ms.Position = 0;
        return File(ms, "image/png");
    }
}
