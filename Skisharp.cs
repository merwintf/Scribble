using SkiaSharp;
using Tesseract;
using System;
using System.IO;

class Program
{
    static void Main(string[] args)
    {
        string inputImagePath = "compressed_image.jpg"; // Path to your blurry image
        string outputImagePath = "sharpened_image.png"; // Path to save sharpened image

        // Load the image with SkiaSharp
        using (var inputStream = File.OpenRead(inputImagePath))
        using (var bitmap = SKBitmap.Decode(inputStream))
        {
            // Create a new bitmap for the sharpened image
            using (var sharpenedBitmap = new SKBitmap(bitmap.Width, bitmap.Height))
            using (var canvas = new SKCanvas(sharpenedBitmap))
            {
                // Define a sharpening kernel (3x3 convolution matrix)
                float[] sharpenKernel = new float[]
                {
                    0, -1,  0,
                    -1,  5, -1,
                    0, -1,  0
                };

                // Create a convolution filter
                using (var filter = SKImageFilter.CreateMatrixConvolution(
                    new SKSizeI(3, 3), // Kernel size
                    sharpenKernel,     // Sharpening kernel
                    1f,                // Gain
                    0f,                // Bias
                    new SKPointI(1, 1),// Kernel offset
                    SKMatrixConvolutionTileMode.Clamp,
                    false))
                {
                    // Apply the filter to the bitmap
                    using (var paint = new SKPaint())
                    {
                        paint.ImageFilter = filter;
                        canvas.DrawBitmap(bitmap, 0, 0, paint);
                    }
                }

                // Save the sharpened image
                using (var outputStream = File.OpenWrite(outputImagePath))
                using (var image = SKImage.FromBitmap(sharpenedBitmap))
                using (var data = image.Encode(SKEncodedImageFormat.Png, 100))
                {
                    data.SaveTo(outputStream);
                }
            }
        }

        // Perform OCR with Tesseract on the sharpened image
        try
        {
            using (var engine = new TesseractEngine(@"./tessdata", "eng", EngineMode.Default))
            using (var img = Pix.LoadFromFile(outputImagePath))
            using (var page = engine.Process(img))
            {
                string text = page.GetText();
                Console.WriteLine("OCR Result:\n" + text);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine("OCR Error: " + ex.Message);
        }
    }
}
