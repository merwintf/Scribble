using SkiaSharp;

public class ImageCropper
{
    public static void CropBottomStripAuto(string inputPath, string outputPath)
    {
        try
        {
            // Load the image
            using (SKBitmap originalBitmap = SKBitmap.Decode(inputPath))
            {
                int width = originalBitmap.Width;
                int height = originalBitmap.Height;
                int blackRows = 0;

                // Scan from bottom up to detect black strip
                for (int y = height - 1; y >= 0; y--)
                {
                    bool isBlackRow = true;
                    for (int x = 0; x < width; x++)
                    {
                        SKColor pixel = originalBitmap.GetPixel(x, y);
                        // Check if pixel is approximately black (adjust threshold as needed)
                        if (pixel.Red > 30 || pixel.Green > 30 || pixel.Blue > 30)
                        {
                            isBlackRow = false;
                            break;
                        }
                    }
                    if (isBlackRow)
                        blackRows++;
                    else
                        break; // Stop when a non-black row is found
                }

                if (blackRows == 0)
                {
                    throw new InvalidOperationException("No black strip detected at the bottom.");
                }

                // Define crop rectangle for the detected strip
                int x = 0;
                int y = height - blackRows;
                int cropWidth = width;
                int cropHeight = blackRows;

                // Create a new bitmap for the cropped area
                using (SKBitmap croppedBitmap = new SKBitmap(cropWidth, cropHeight))
                {
                    SKRectI cropRect = new SKRectI(x, y, x + cropWidth, y + cropHeight);
                    originalBitmap.ExtractSubset(croppedBitmap, cropRect);

                    // Save with the same format to preserve quality
                    using (SKData data = croppedBitmap.Encode(originalBitmap.Info.ColorType == SKColorType.Rgb565 ? SKEncodedImageFormat.Png : originalBitmap.EncodedImageFormat, 100))
                    using (FileStream stream = File.OpenWrite(outputPath))
                    {
                        data.SaveTo(stream);
                    }
                }
            }
            Console.WriteLine("Bottom strip cropped successfully!");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
        }
    }
}
