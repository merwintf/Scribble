using SkiaSharp;

// 1) Decode at full fidelity (sRGB, 8-bit RGBA)
using var data = SKData.Create("input.jpg");
using var codec = SKCodec.Create(data);
var info = new SKImageInfo(codec.Info.Width, codec.Info.Height,
    SKColorType.Rgba8888, SKAlphaType.Premul, SKColorSpace.CreateSrgb());
using var src = SKBitmap.Decode(codec);

// 2) Crop
var crop = new SKRectI(x, y, x + width, y + height);
using var srcImg = SKImage.FromBitmap(src);
using var cropped = srcImg.Subset(crop);

// 3) Optional: upscale/downscale with high-quality sampling
// Mitchell–Netravali (B=C=1/3) is a great general-purpose choice
var sampling = new SKSamplingOptions(new SKCubicResampler(1f/3f, 1f/3f));
var targetW = desiredWidth;
var targetH = desiredHeight;

using var surface = SKSurface.Create(new SKImageInfo(targetW, targetH,
    SKColorType.Rgba8888, SKAlphaType.Premul, SKColorSpace.CreateSrgb()));
var canvas = surface.Canvas;
canvas.Clear(SKColors.Transparent);

var paint = new SKPaint { IsAntialias = true, IsDither = true, FilterQuality = SKFilterQuality.None };
// Sharpen kernel (subtle). Comment out if you don’t want sharpening.
float[] kernel = {
     0, -1,  0,
    -1,  5, -1,
     0, -1,  0
};
paint.ImageFilter = SKImageFilter.CreateMatrixConvolution(
    new SKSizeI(3,3), kernel, 1f, 0f, new SKPointI(1,1),
    SKMatrixConvolutionTileMode.Clamp, true);

var dest = new SKRect(0, 0, targetW, targetH);
canvas.DrawImage(cropped, dest, sampling, paint);
canvas.Flush();

// 4) Encode at high quality (use PNG for lossless, JPEG 90–95, or WebP)
using var finalImg = surface.Snapshot();
// JPEG
using var outData = finalImg.Encode(SKEncodedImageFormat.Jpeg, 95);
using var fs = File.OpenWrite("output.jpg");
outData.SaveTo(fs);
