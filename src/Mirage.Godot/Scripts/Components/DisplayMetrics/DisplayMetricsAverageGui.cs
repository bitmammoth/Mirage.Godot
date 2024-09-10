using Godot;
using Mirage.SocketLayer;
using System;

public partial class DisplayMetricsAverageGui : Control
{
    public Metrics Metrics { get; set; }

    private Color backgroundColor = new Color(0.1f, 0.1f, 0.1f, 0.8f); // Background color
    private Font font; // Load a custom font if needed
    private Rect2 offset = new Rect2(10, 10, 400, 800); // Position and size of the drawn area

    public override void _Ready()
    {
        // Load the font (optional)
        font = (Font)GD.Load("res://path_to_your_font.tres");
        QueueRedraw(); // Request the draw call
    }

    // Redraw the screen when metrics change
    public override void _Process(double delta)
    {
        if (Metrics != null)
        {
            QueueRedraw();
        }
    }

    // Override the _Draw method to custom-draw the metrics
    public override void _Draw()
    {
        if (Metrics == null) return;

        // Draw a semi-transparent background
        DrawRect(offset, backgroundColor);

        // Start drawing the metrics text
        DrawAverage();
    }

    private void DrawAverage()
    {
        // Initialize metric values
        double connectionCount = 0;
        double sendCount = 0;
        double sendBytes = 0;
        double receiveCount = 0;
        double receiveBytes = 0;

        var array = Metrics.buffer;
        var count = 0;
        for (var i = 0; i < array.Length; i++)
        {
            if (!array[i].init)
            {
                continue;
            }

            count++;
            connectionCount += array[i].connectionCount;

            sendCount += array[i].sendCount;
            sendBytes += array[i].sendBytes;

            receiveCount += array[i].receiveCount;
            receiveBytes += array[i].receiveBytes;
        }

        // Compute averages
        if (count > 0)
        {
            connectionCount /= count;
            sendCount /= count;
            sendBytes /= count;
            receiveCount /= count;
            receiveBytes /= count;
        }

        // Draw text for each metric
        var startPos = new Vector2(20, 30); // Starting position inside the drawn rectangle
        var verticalSpacing = 20; // Space between lines

        DrawString(font, startPos, $"Connection Count: {connectionCount:0.0}", HorizontalAlignment.Left, -1, 16, Colors.White);
        DrawString(font, startPos + new Vector2(0, verticalSpacing), $"Send Count: {sendCount:0.0}", HorizontalAlignment.Left, -1, 16, Colors.White);
        DrawString(font, startPos + new Vector2(0, verticalSpacing * 2), $"Send Bytes: {sendBytes:0.00}", HorizontalAlignment.Left, -1, 16, Colors.White);
        DrawString(font, startPos + new Vector2(0, verticalSpacing * 3), $"Receive Count: {receiveCount:0.0}", HorizontalAlignment.Left, -1, 16, Colors.White);
        DrawString(font, startPos + new Vector2(0, verticalSpacing * 4), $"Receive Bytes: {receiveBytes:0.00}", HorizontalAlignment.Left, -1, 16, Colors.White);


    }
}
