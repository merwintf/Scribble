using System.Drawing;

public class GridSelector
{
    // Method to start recursive selection of all grid squares in the rectangle
    public void SelectAllGrids(Rectangle rect)
    {
        SelectGrid(rect, rect.X, rect.Y);
    }

    // Recursive method to select each grid square
    private void SelectGrid(Rectangle rect, int x, int y)
    {
        // Base case: if x or y is out of bounds, stop recursion
        if (x >= rect.X + rect.Width || y >= rect.Y + rect.Height)
            return;

        // Process the current grid square (e.g., print or select it)
        Console.WriteLine($"Selecting grid at ({x}, {y})");

        // Move to the next column in the same row
        if (x + 1 < rect.X + rect.Width)
        {
            SelectGrid(rect, x + 1, y);
        }
        // Move to the next row, reset x to the starting X
        else if (y + 1 < rect.Y + rect.Height)
        {
            SelectGrid(rect, rect.X, y + 1);
        }
    }
}
