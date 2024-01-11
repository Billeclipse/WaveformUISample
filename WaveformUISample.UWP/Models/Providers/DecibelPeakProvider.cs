using System;
using NAudio.Wave;
using NAudio.WaveFormRenderer;
using IPeakProvider = WaveformUISample.UWP.Interfaces.IPeakProvider;

namespace WaveformUISample.UWP.Models.Providers
{
    public class DecibelPeakProvider : IPeakProvider
    {
        private readonly IPeakProvider _sourceProvider;
        private readonly double _dynamicRange;

        public DecibelPeakProvider(IPeakProvider sourceProvider, double dynamicRange)
        {
            _sourceProvider = sourceProvider;
            _dynamicRange = dynamicRange;
        }

        public void Init(ISampleProvider reader, int samplesPerPixel)
        {
            throw new NotImplementedException();
        }

        public PeakInfo GetNextPeak()
        {
            var peak = _sourceProvider.GetNextPeak();
            var decibelMax = 20 * Math.Log10(peak.Max);
            if (decibelMax < 0 - _dynamicRange) decibelMax = 0 - _dynamicRange;
            var linear = (float)((_dynamicRange + decibelMax) / _dynamicRange);
            return new PeakInfo(0 - linear, linear);
        }
    }
}