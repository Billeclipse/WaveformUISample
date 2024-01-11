using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using ManagedBass;
using NAudio.Wave;
using WaveformUISample.UWP.Interfaces;
using WaveformUISample.UWP.Models;
using WaveformUISample.UWP.Models.Providers;
using IPeakProvider = WaveformUISample.UWP.Interfaces.IPeakProvider;
using MaxPeakProvider = WaveformUISample.UWP.Models.Providers.MaxPeakProvider;

namespace WaveformUISample.UWP.Services
{
    public class WaveformService : IWaveformService
    {
        public WaveformRendererSettings Settings { get; } = new WaveformRendererSettings();
        private IPeakProvider _peakProvider = new MaxPeakProvider();
        
        public List<(float min, float max)> GenerateAudioData(Stream stream)
        {
            try
            {
                ISampleProvider isp;
                long samples;

                using (var reader = new StreamMediaFoundationReader(stream))
                {
                    isp = reader.ToSampleProvider();
                    var buffer = new float[reader.Length / 2];
                    isp.Read(buffer, 0, buffer.Length);

                    var bytesPerSample = reader.WaveFormat.BitsPerSample / 8;
                    samples = reader.Length / bytesPerSample;

                    reader.Close();
                }

                return GenerateAudioData(isp, samples);
            }
            catch(Exception ex)
            {
                Debug.WriteLine("Exception caught at GenerateAudioData(stream) -> " + ex.Message);
                stream.Close();
                return null;
            }
        }

        public List<(float min, float max)> GenerateAudioData(byte[] audioBytes)
        {
            try
            {
                var peakList = new List<(float min, float max)>();
                
                const int waveformCompressedPointCount = 1000;
                Bass.Init();
                var stream = Bass.CreateStream(audioBytes, 0, audioBytes.Length, BassFlags.Decode | BassFlags.Float | BassFlags.Prescan);
                var frameLength = (int)Bass.ChannelSeconds2Bytes(stream, 0.02);
                var streamLength = Bass.ChannelGetLength(stream);
                var frameCount = (int)(streamLength / (double)frameLength);
                var waveformLength = frameCount * 2;
                var waveformData = new float[waveformLength];

                var actualPoints = Math.Min(waveformCompressedPointCount, frameCount);
                
                var waveMaxPointIndexes = new List<int>();
                for (var i = 1; i <= actualPoints; i++)
                {
                    waveMaxPointIndexes.Add((int)Math.Round(waveformLength * (i / (double)actualPoints), 0));
                }

                var maxLeftPointLevel = float.MinValue;
                var maxRightPointLevel = float.MinValue;
                var currentPointIndex = 0;
                for (var i = 0; i < waveformLength; i += 2)
                {
                    var levels = Bass.ChannelGetLevel(stream, 0.02f, LevelRetrievalFlags.Stereo);

                    waveformData[i] = levels[0];
                    waveformData[i + 1] = levels[1];

                    if (levels[0] > maxLeftPointLevel)
                    {
                        maxLeftPointLevel = levels[0];
                    }
                    if (levels[1] > maxRightPointLevel)
                    {
                        maxRightPointLevel = levels[1];
                    }

                    if (i <= waveMaxPointIndexes[currentPointIndex]) continue;
                    peakList.Add((-maxLeftPointLevel, maxRightPointLevel));
                    maxLeftPointLevel = float.MinValue;
                    maxRightPointLevel = float.MinValue;
                    currentPointIndex++;
                }
                Bass.StreamFree(stream);

                if (peakList.Count < Settings.Width)
                {
                    for (var i = peakList.Count; i < Settings.Width; i++)
                    {
                        peakList.Add(((float)-0.01, (float)0.01));
                    }
                }
                //TODO: DELETE DEBUG LINES
                Debug.WriteLine("ManagedBass Waveform created normally!");
                return peakList;
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Exception caught at GenerateAudioData(byte[]) -> " + ex.Message);
                Debug.WriteLine("Bass.LastError: " + Bass.LastError);
                return null;
            }
        }

        private List<(float min, float max)> GenerateAudioData(ISampleProvider isp, long samples)
        {
            try
            {
                var samplesPerPixel = (int)(samples / Settings.Width);
                var stepSize = Settings.PixelsPerPeak + Settings.SpacerPixels;
                _peakProvider.Init(isp, samplesPerPixel * stepSize);

                // DecibelScale - if true, convert values to decibels for a logarithmic waveform
                if (Settings.DecibelScale)
                {
                    _peakProvider = new DecibelPeakProvider(_peakProvider, 48);
                }

                var peakList = new List<(float min, float max)>();
                for (var i = 0; i < Settings.Width; i++)
                {
                    var peak = _peakProvider.GetNextPeak();
                    peakList.Add((peak.Min, peak.Max));
                }
                //TODO: DELETE DEBUG LINES
                Debug.WriteLine("NAudio Waveform created normally!");
                return peakList;
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Exception caught at GenerateAudioData(ISampleProvider,long) -> " + ex.Message);
                return null;
            }
        }
    }
}