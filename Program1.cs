using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;

class Program
{
    static void Main(string[] args)
    {
        if (args.Length != 4)
        {
            Console.WriteLine("Usage: program.exe <image_path> <bounds_width> <bounds_height> <output_path>");
            Console.WriteLine("For GTA IV, use approximate bounds like 4000x4000 meters (aspect ratio 1:1).");
            return;
        }

        try
        {
            // Parse command-line arguments
            string imagePath = args[0];
            double boundsWidth = double.Parse(args[1]);
            double boundsHeight = double.Parse(args[2]);
            string outputPath = args[3];

            // Load the image to get its dimensions
            using (Bitmap image = new Bitmap(imagePath))
            {
                int imageWidth = image.Width;
                int imageHeight = image.Height;

                // Calculate aspect ratios
                double boundsAspectRatio = boundsWidth / boundsHeight;
                double imageAspectRatio = (double)imageWidth / imageHeight;

                // Check if aspect ratios match (within a small tolerance)
                const double tolerance = 0.001;
                if (Math.Abs(boundsAspectRatio - imageAspectRatio) < tolerance)
                {
                    // Aspect ratios match; copy the original image to output
                    File.Copy(imagePath, outputPath, true);
                    Console.WriteLine($"Aspect ratios match. Original image copied to {outputPath}");
                    Console.WriteLine($"Dimensions: {imageWidth} x {imageHeight} pixels");
                    return;
                }

                int newWidth, newHeight;

                // Determine new dimensions
                if (boundsAspectRatio > imageAspectRatio)
                {
                    // Image is too tall; extend width
                    newHeight = imageHeight;
                    newWidth = (int)Math.Ceiling(imageHeight * boundsAspectRatio);
                }
                else
                {
                    // Image is too wide; extend height
                    newWidth = imageWidth;
                    newHeight = (int)Math.Ceiling(imageWidth / boundsAspectRatio);
                }

                // Create new image with extended dimensions
                using (Bitmap newImage = new Bitmap(newWidth, newHeight, PixelFormat.Format32bppArgb))
                {
                    using (Graphics graphics = Graphics.FromImage(newImage))
                    {
                        // Clear with transparent background
                        graphics.Clear(Color.Transparent);

                        // Calculate offsets to center the original image
                        int offsetX = (newWidth - imageWidth) / 2;
                        int offsetY = (newHeight - imageHeight) / 2;

                        // Draw the original image in the center
                        graphics.DrawImage(image, offsetX, offsetY, imageWidth, imageHeight);

                        // Fill extended areas
                        // Left extension (if any) - use mirroring
                        if (offsetX > 0)
                        {
                            // Mirror left side
                            using (Bitmap leftMirror = new Bitmap(offsetX, imageHeight))
                            {
                                for (int x = 0; x < offsetX; x++)
                                {
                                    for (int y = 0; y < imageHeight; y++)
                                    {
                                        leftMirror.SetPixel(offsetX - 1 - x, y, image.GetPixel(x % imageWidth, y));
                                    }
                                }
                                graphics.DrawImage(leftMirror, 0, offsetY);
                            }

                            // Mirror right side
                            using (Bitmap rightMirror = new Bitmap(offsetX, imageHeight))
                            {
                                for (int x = 0; x < offsetX; x++)
                                {
                                    for (int y = 0; y < imageHeight; y++)
                                    {
                                        rightMirror.SetPixel(x, y, image.GetPixel(imageWidth - 1 - (x % imageWidth), y));
                                    }
                                }
                                graphics.DrawImage(rightMirror, offsetX + imageWidth, offsetY);
                            }
                        }

                        // Top and bottom extension (if any) - use wrapping like Google Maps
                        if (offsetY > 0)
                        {
                            // Top extension: copy from bottom of original image
                            using (Bitmap topExtension = new Bitmap(newWidth, offsetY))
                            {
                                for (int x = 0; x < newWidth; x++)
                                {
                                    for (int y = 0; y < offsetY; y++)
                                    {
                                        int srcX = Math.Min(Math.Max(x - offsetX, 0), imageWidth - 1);
                                        // Wrap: take pixels from the bottom
                                        int srcY = imageHeight - offsetY + y;
                                        topExtension.SetPixel(x, y, image.GetPixel(srcX, srcY % imageHeight));
                                    }
                                }
                                graphics.DrawImage(topExtension, 0, 0);
                            }

                            // Bottom extension: copy from top of original image
                            using (Bitmap bottomExtension = new Bitmap(newWidth, offsetY))
                            {
                                for (int x = 0; x < newWidth; x++)
                                {
                                    for (int y = 0; y < offsetY; y++)
                                    {
                                        int srcX = Math.Min(Math.Max(x - offsetX, 0), imageWidth - 1);
                                        // Wrap: take pixels from the top
                                        int srcY = y;
                                        bottomExtension.SetPixel(x, y, image.GetPixel(srcX, srcY % imageHeight));
                                    }
                                }
                                graphics.DrawImage(bottomExtension, 0, offsetY + imageHeight);
                            }
                        }
                    }

                    // Save the new image
                    newImage.Save(outputPath, ImageFormat.Png);
                    Console.WriteLine($"New image saved to {outputPath}");
                    Console.WriteLine($"New dimensions: {newWidth} x {newHeight} pixels");
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
        }
    }
}
