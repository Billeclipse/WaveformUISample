namespace WaveformUISample.UWP.Models
{
    public class WaveformRendererSettings
    {
        public const int DefaultWidth = 800;
        public const int DefaultHeight = 80;

        public int Width { get; set; } = DefaultWidth;

        public int TopHeight { get; set; } = DefaultHeight;

        public int BottomHeight { get; set; } = DefaultHeight;

        public int PixelsPerPeak { get; set; } = 1;

        public int SpacerPixels { get; set; } = 0;

        public bool DecibelScale { get; set; }
    }
}