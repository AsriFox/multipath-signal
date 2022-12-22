namespace MultipathSignal.Views;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Threading.Tasks;
using Avalonia;

public partial class OpenGlPage : Avalonia.Controls.Window
{
    public readonly IList<Vector3> points = Array.Empty<Vector3>();
    public readonly IList<ushort> indices = Array.Empty<ushort>();

    public OpenGlPage() 
    {
        InitializeComponent();
    }

    public OpenGlPage(IList<Vector3> points, IList<ushort> indices) 
    {
        this.points = points;
        this.indices = indices;
        InitializeComponent();
    }

    public static OpenGlPage FromValues(double[,] values, Rect bounds) 
    {
        int width = values.GetLength(0);
        int height = values.GetLength(1);
        var points = new Vector3[width * height];
        var indices = new ushort[(width - 1) * (height - 1) * 6];
        Parallel.For(0, width, i => {
            Parallel.For(0, height, j => {
                float x = (float)(bounds.Left + i / bounds.Width);
                float y = (float)(bounds.Top + j / bounds.Height);
                points[j * width + i] = new Vector3(x, y, (float)values[i, j]);
            });
        });
        Parallel.For(0, width - 1, i => {
            Parallel.For(0, height - 1, j => {
                ushort k = (ushort)(j * width + i); // Anchor point (top-left) index
                ushort k2 = (ushort)(k + width);    // Bottom-left point index
                int p = 6 * (j * (width - 1) + i);  // Triangle point index
                // Top triangle:
                indices[p++] = k++;
                indices[p++] = k;
                indices[p++] = k2;
                // Bottom triangle:
                indices[p++] = k;
                indices[p++] = k2++;
                indices[p]   = k2;

                // // Top triangle:
                // indices[p  ] = (ushort)(k);
                // indices[p+1] = (ushort)(k + 1);
                // indices[p+2] = (ushort)(k + width);
                // // Bottom triangle:
                // indices[p+3] = (ushort)(k + 1);
                // indices[p+4] = (ushort)(k + width);
                // indices[p+5] = (ushort)(k + width + 1);
            });
        });
        return new OpenGlPage(points, indices);
    }

    public static OpenGlPage FromValues(double[,] values) => 
        FromValues(values, new(-1, -1, 2, 2));
}
