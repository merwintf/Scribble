using System;
using System.Drawing;
using System.Drawing.Imaging;

public class ImageCropper
{
    public static void CropBottom10Percent(string inputPath, string outputPath)
    {
        try
        {
            // Load the original image
            using (Bitmap originalImage = new Bitmap(inputPath))
            {
                // Calculate the height of the bottom 10%
                int cropHeight = (int)(originalImage.Height * 0.1); // 10% of height
                int cropY = originalImage.Height - cropHeight; // Start Y at bottom - 10%

                // Define the crop rectangle (full width, bottom 10% height)
                Rectangle cropArea = new Rectangle(0, cropY, originalImage.Width, cropHeight);

                // Ensure the crop area is valid
                if (cropHeight <= 0 || cropY < 0)
                {
                    throw new ArgumentException("Image is too small to crop 10% of height.");
                }

                // Create a new bitmap for the cropped image
                using (Bitmap croppedImage = new Bitmap(originalImage.Width, cropHeight))
                {
                    // Copy the cropped area with high-quality settings
                    using (Graphics graphics = Graphics.FromImage(croppedImage))
                    {
                        graphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                        graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;
                        graphics.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.HighQuality;
                        graphics.CompositingQuality = System.Drawing.Drawing2D.CompositingQuality.HighQuality;

                        graphics.DrawImage(originalImage, 0, 0, cropArea, GraphicsUnit.Pixel);
                    }

                    // Save the cropped image in the original format
                    ImageFormat format = originalImage.RawFormat;
                    croppedImage.Save(outputPath, format);
                }
            }
            Console.WriteLine("Bottom 10% of image cropped successfully!");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
        }
    }
}
