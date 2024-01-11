using NAudio.Wave;
using NAudio.WaveFormRenderer;

namespace WaveformUISample.UWP.Interfaces
{
    public interface IPeakProvider
    {
        void Init(ISampleProvider reader, int samplesPerPixel);

        PeakInfo GetNextPeak();
    }
}