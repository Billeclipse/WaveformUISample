using System.Linq;
using NAudio.WaveFormRenderer;

namespace WaveformUISample.UWP.Models.Providers
{
    public class MaxPeakProvider : PeakProvider
    {
        public override PeakInfo GetNextPeak()
        {
            if (ReadBuffer == null) return null;
            var samplesRead = Provider.Read(ReadBuffer, 0, ReadBuffer.Length);
            var max = samplesRead == 0 ? 0 : ReadBuffer.Take(samplesRead).Max();
            var min = samplesRead == 0 ? 0 : ReadBuffer.Take(samplesRead).Min();
            return new PeakInfo(min, max);
        }
    }
}