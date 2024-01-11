using System.Collections.Generic;
using System.IO;

namespace WaveformUISample.UWP.Interfaces
{
    public interface IWaveformService
    {
        List<(float min, float max)> GenerateAudioData(Stream stream);
        List<(float min, float max)> GenerateAudioData(byte[] audioBytes);
    }
}
