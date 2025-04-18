using System;
using System.Windows;

public class GridSelector
{
    // Method to start recursive selection of all grid squares in the Rect
    public void SelectAllGrids(Rect rect)
    {
        // Convert to integer bounds
        int startX = (int)Math.Floor(rect.X);
        int startY = (int)Math.Floor(rect.Y);
        int endX = (int)Math.Ceiling(rect.X + rect.Width) - 1;
        int endY = (int)Math.Ceiling(rect.Y + rect.Height) - 1;

        // Start recursion at the top-left integer coordinate
        SelectGrid(startX, startY, endX, endY, startX, startY);
    }

    // Recursive method to select each grid square
    private void SelectGrid(int x, int y, int endX, int endY, int startX, int startY)
    {
        // Base case: if x or y is out of bounds, stop recursion
        if (x > endX || y > endY)
            return;

        // Process the current grid square (e.g., print or select it)
        Console.WriteLine($"Selecting grid at ({x}, {y})");

        // Move to the next column in the same row
        if (x < endX)
        {
            SelectGrid(x + 1, y, endX, endY, startX, startY);
        }
        // Move to the next row, reset x to startX
        else if (y < endY)
        {
            SelectGrid(startX, y + 1, endX, endY, startX, startY);
        }
    }
}
