// <Project Sdk="Microsoft.NET.Sdk">
//   <PropertyGroup><OutputType>Exe</OutputType><TargetFramework>net8.0</TargetFramework></PropertyGroup>
//   <ItemGroup><PackageReference Include="SkiaSharp" Version="2.88.6" /></ItemGroup>
// </Project>

using System;
using System.IO;
using SkiaSharp;

class Program
{
    static void Main(string[] args)
    {
        var inputPath = args.Length > 0 ? args[0] : "in.png";
        var outPath   = args.Length > 1 ? args[1] : "ocr_ready.png";

        using var src = SKBitmap.Decode(inputPath) ?? throw new FileNotFoundException(inputPath);

        // 1) Upscale 3x with high-quality resampling
        using var up = ResizeBitmap(src, scale: 3.0);

        // 2) Unsharp mask (light sharpening to restore edges after upscaling)
        using var sharp = Unsharp(up, radius: 1.2f, amount: 0.6f); // tweak amount 0.4–0.9

        // 3) Convert to grayscale + global threshold (simple + fast)
        using var gray = ToGrayscale(sharp);
        using var bin  = Threshold(gray, threshold: AutoThresholdOtsu(gray));

        // Save PNG (lossless) for tesseract
        using var img = SKImage.FromBitmap(bin);
        using var data = img.Encode(SKEncodedImageFormat.Png, 100);
        using var fs = File.Open(outPath, FileMode.Create, FileAccess.Write);
        data.SaveTo(fs);

        Console.WriteLine($"Saved: {outPath}");
    }

    // --- Helpers ---

    static SKBitmap ResizeBitmap(SKBitmap src, double scale)
    {
        int w = (int)Math.Round(src.Width * scale);
        int h = (int)Math.Round(src.Height * scale);

        var info = new SKImageInfo(w, h, SKColorType.Bgra8888, SKAlphaType.Premul);
        var dst = new SKBitmap(info);

        using var canvas = new SKCanvas(dst);
        canvas.Clear(SKColors.White);

        var paint = new SKPaint { FilterQuality = SKFilterQuality.High, IsAntialias = true };
        // Draw with destination rect to ensure proper resampling
        var srcRect = new SKRect(0, 0, src.Width, src.Height);
        var dstRect = new SKRect(0, 0, w, h);
        canvas.DrawBitmap(src, srcRect, dstRect, paint);

        return dst;
    }

    static SKBitmap Unsharp(SKBitmap src, float radius, float amount)
    {
        // Create a blurred image using ImageFilter (Gaussian)
        var info = new SKImageInfo(src.Width, src.Height, SKColorType.Bgra8888, SKAlphaType.Premul);
        using var surface = SKSurface.Create(info);
        var canvas = surface.Canvas;
        canvas.Clear(SKColors.Transparent);

        using var blurPaint = new SKPaint
        {
            ImageFilter = SKImageFilter.CreateBlur(radius, radius, SKShaderTileMode.Clamp),
            IsAntialias = true,
            FilterQuality = SKFilterQuality.High
        };
        canvas.DrawBitmap(src, 0, 0, blurPaint);
        using var blurredImg = surface.Snapshot();
        using var blurred = SKBitmap.FromImage(blurredImg);

        // Blend: sharp = src + amount*(src - blurred)
        var dst = new SKBitmap(info);
        using var spix = src.PeekPixels();
        using var bpix = blurred.PeekPixels();
        using var dpix = dst.PeekPixels();

        var sSpan = spix.GetPixelSpan<SKColor>();
        var bSpan = bpix.GetPixelSpan<SKColor>();
        var dSpan = dpix.GetPixelSpan<SKColor>();

        for (int i = 0; i < sSpan.Length; i++)
        {
            var s = sSpan[i];
            var b = bSpan[i];

            // operate on R,G,B; keep original alpha
            byte r = ClampByte(s.Red   + amount * (s.Red   - b.Red));
            byte g = ClampByte(s.Green + amount * (s.Green - b.Green));
            byte bl= ClampByte(s.Blue  + amount * (s.Blue  - b.Blue));

            dSpan[i] = new SKColor(r, g, bl, s.Alpha);
        }

        return dst;
    }

    static SKBitmap ToGrayscale(SKBitmap src)
    {
        var info = new SKImageInfo(src.Width, src.Height, SKColorType.Gray8, SKAlphaType.Opaque);
        var dst = new SKBitmap(info);

        using var spix = src.PeekPixels();
        using var dpix = dst.PeekPixels();

        var sSpan = spix.GetPixelSpan<SKColor>();
        var dSpan = dpix.GetPixelSpan<byte>();

        for (int i = 0; i < sSpan.Length; i++)
        {
            var c = sSpan[i];
            // Luma (BT.709)
            int y = (int)(0.2126 * c.Red + 0.7152 * c.Green + 0.0722 * c.Blue + 0.5);
            dSpan[i] = (byte)(y < 0 ? 0 : y > 255 ? 255 : y);
        }
        return dst;
    }

    static SKBitmap Threshold(SKBitmap gray, byte threshold)
    {
        var info = new SKImageInfo(gray.Width, gray.Height, SKColorType.Gray8, SKAlphaType.Opaque);
        var dst = new SKBitmap(info);

        using var gpix = gray.PeekPixels();
        using var dpix = dst.PeekPixels();
        var gSpan = gpix.GetPixelSpan<byte>();
        var dSpan = dpix.GetPixelSpan<byte>();

        for (int i = 0; i < gSpan.Length; i++)
            dSpan[i] = gSpan[i] > threshold ? (byte)255 : (byte)0;

        return dst;
    }

    static byte AutoThresholdOtsu(SKBitmap gray)
    {
        // Otsu histogram-based threshold for Gray8
        var hist = new int[256];
        using var gpix = gray.PeekPixels();
        var span = gpix.GetPixelSpan<byte>();
        foreach (var v in span) hist[v]++;

        int total = span.Length;
        long sum = 0;
        for (int t = 0; t < 256; t++) sum += t * (long)hist[t];

        long sumB = 0;
        int wB = 0;
        int wF = 0;
        double maxVar = -1;
        int threshold = 127;

        for (int t = 0; t < 256; t++)
        {
            wB += hist[t];
            if (wB == 0) continue;
            wF = total - wB;
            if (wF == 0) break;

            sumB += t * (long)hist[t];
            double mB = sumB / (double)wB;
            double mF = (sum - sumB) / (double)wF;
            double between = wB * (double)wF * (mB - mF) * (mB - mF);

            if (between > maxVar)
            {
                maxVar = between;
                threshold = t;
            }
        }
        return (byte)threshold;
    }

    static byte ClampByte(double v)
    {
        if (v < 0) return 0;
        if (v > 255) return 255;
        return (byte)v;
    }
}
