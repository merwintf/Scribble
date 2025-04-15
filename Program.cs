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

                // Check if aspect ratios match (within a small tolerance to handle floating-point imprecision)
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

                        // Fill extended areas with mirrored content
                        // Left extension (if any)
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

                        // Top extension (if any)
                        if (offsetY > 0)
                        {
                            // Mirror top side
                            using (Bitmap topMirror = new Bitmap(newWidth, offsetY))
                            {
                                for (int x = 0; x < newWidth; x++)
                                {
                                    for (int y = 0; y < offsetY; y++)
                                    {
                                        int srcX = Math.Min(Math.Max(x - offsetX, 0), imageWidth - 1);
                                        topMirror.SetPixel(x, offsetY - 1 - y, image.GetPixel(srcX, y % imageHeight));
                                    }
                                }
                                graphics.DrawImage(topMirror, 0, 0);
                            }

                            // Mirror bottom side
                            using (Bitmap bottomMirror = new Bitmap(newWidth, offsetY))
                            {
                                for (int x = 0; x < newWidth; x++)
                                {
                                    for (int y = 0; y < offsetY; y++)
                                    {
                                        int srcX = Math.Min(Math.Max(x - offsetX, 0), imageWidth - 1);
                                        bottomMirror.SetPixel(x, y, image.GetPixel(srcX, imageHeight - 1 - (y % imageHeight)));
                                    }
                                }
                                graphics.DrawImage(bottomMirror, 0, offsetY + imageHeight);
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
