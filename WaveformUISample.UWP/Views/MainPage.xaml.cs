using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Windows.Media.Core;
using Windows.Media.Playback;
using Windows.Storage;
using Windows.System.Threading;
using Windows.UI;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Input;
using Microsoft.Toolkit.Uwp.Helpers;
using WaveformUISample.UWP.Models;
using WaveformUISample.UWP.Services;

namespace WaveformUISample.UWP.Views
{
    public sealed partial class MainPage : INotifyPropertyChanged
    {
        private const string RecordingsConst = "Recordings";
        private const string PlayRecordIconSourceConst = "ms-appx:///Assets/play-48.png";
        private const string PauseRecordIconSourceConst = "ms-appx:///Assets/pause-48.png";
        private const string GithubIconSourceConst = "ms-appx:///Assets/github_icon.png";
        private readonly string _soundBarDefaultColor = Colors.LightGray.ToString();

        private readonly WaveformService _waveformService = new WaveformService();
        private StorageFile _file;
        private MediaPlayer _mediaPlayer;
        private ThreadPoolTimer _timer;
        private bool _firstTimePlay;
        private bool _pressed;

        public MainPage()
        {
            InitializeComponent();
            SetIconSources();
            Initialize();
        }
        
        private double _soundPosition;
        public double SoundPosition
        {
            get => _soundPosition;
            set
            {
                _soundPosition = value;
                OnPropertyChanged();
            }
        }

        private string _soundPositionString;
        public string SoundPositionString
        {
            get => _soundPositionString;
            set
            {
                _soundPositionString = value;
                OnPropertyChanged();
            }
        }

        private bool _recordingShow;
        public bool RecordingShow
        {
            get => _recordingShow;
            set
            {
                _recordingShow = value;
                OnPropertyChanged();
            }
        }
        
        private bool _waveformShow;
        public bool WaveformShow
        {
            get => _waveformShow;
            set
            {
                _waveformShow = value;
                OnPropertyChanged();
            }
        }
        
        private bool _waveform2Show;
        public bool Waveform2Show
        {
            get => _waveform2Show;
            set
            {
                _waveform2Show = value;
                OnPropertyChanged();
            }
        }

        private bool _isRecordingPlaying;
        public bool IsRecordingPlaying
        {
            get => _isRecordingPlaying;
            set
            {
                _isRecordingPlaying = value;
                OnPropertyChanged();
            }
        }
        
        private bool _isRecordingNotPlaying;
        public bool IsRecordingNotPlaying
        {
            get => _isRecordingNotPlaying;
            set
            {
                _isRecordingNotPlaying = value;
                OnPropertyChanged();
            }
        }

        private bool _recordingFailShow;
        public bool RecordingFailShow
        {
            get => _recordingFailShow;
            set
            {
                _recordingFailShow = value;
                OnPropertyChanged();
            }
        }

        private bool _recordingSuccessShow;
        public bool RecordingSuccessShow
        {
            get => _recordingSuccessShow;
            set
            {
                _recordingSuccessShow = value;
                OnPropertyChanged();
            }
        }

        private string _playRecordIconSource;
        public string PlayRecordIconSource
        {
            get => _playRecordIconSource;
            set
            {
                _playRecordIconSource = value;
                OnPropertyChanged();
            }
        }

        private string _pauseRecordIconSource;
        public string PauseRecordIconSource
        {
            get => _pauseRecordIconSource;
            set
            {
                _pauseRecordIconSource = value;
                OnPropertyChanged();
            }
        }

        private string _githubIconSource;
        public string GithubIconSource
        {
            get => _githubIconSource;
            set
            {
                _githubIconSource = value;
                OnPropertyChanged();
            }
        }
        
        private string _soundBarColor;
        public string SoundBarColor
        {
            get => _soundBarColor;
            set
            {
                _soundBarColor = value;
                OnPropertyChanged();
            }
        }

        private List<(float min, float max)> _peakList;
        public List<(float min, float max)> PeakList
        {
            get => _peakList;
            set
            {
                _peakList = value;
                OnPropertyChanged();
            }
        }
        
        private List<(float min, float max)> _peakList2;
        public List<(float min, float max)> PeakList2
        {
            get => _peakList2;
            set
            {
                _peakList2 = value;
                OnPropertyChanged();
            }
        }

        private ObservableCollection<string> _samples;
        public ObservableCollection<string> Samples
        {
            get => _samples;
            set
            {
                _samples = value;
                OnPropertyChanged();
            }
        }

        private string _selectedSample;
        public string SelectedSample
        {
            get => _selectedSample;
            set
            {
                if (_selectedSample == value) return;
                _selectedSample = value;
                OnPropertyChanged();
            }
        }

        private void SetIconSources()
        {
            PlayRecordIconSource = PlayRecordIconSourceConst;
            PauseRecordIconSource = PauseRecordIconSourceConst;
            GithubIconSource = GithubIconSourceConst;
        }
        
        private async void Initialize()
        {
            SoundPosition = WaveformRendererSettings.DefaultWidth;
            SoundBarColor = _soundBarDefaultColor;
            IsRecordingNotPlaying = true;
            await InitializeMp3Samples();
            await LoadSelectedRecording();
        }

        private async Task InitializeMp3Samples()
        {
            try
            {
                Samples = new ObservableCollection<string>();
                var appInstalledFolder = Windows.ApplicationModel.Package.Current.InstalledLocation;
                var recordingsFolder = await appInstalledFolder.GetFolderAsync(RecordingsConst);
                var filesList = await recordingsFolder.GetFilesAsync();
                foreach (var file in filesList) Samples.Add(file.Name);
                SelectedSample = new string(Samples[0].ToCharArray());
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Exception caught at InitializeMp3Samples -> " + ex.Message);
            }
        }

        private async Task LoadSelectedRecording()
        {
            SoundBarColor = _soundBarDefaultColor;
            var recordingFile = await GetMp3File();
            await DrawWaveform(recordingFile);
            _firstTimePlay = false;
        }
        
        public async Task<StorageFile> GetMp3File()
        {
            try
            {
                if(SelectedSample == null) return null;
                var appInstalledFolder = Windows.ApplicationModel.Package.Current.InstalledLocation;
                var recordingsFolder = await appInstalledFolder.GetFolderAsync(RecordingsConst);
                var file = await recordingsFolder.GetFileAsync(SelectedSample);
                return file;
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Exception caught at GetMp3File -> " + ex.Message);
                return null;
            }
        }
        
        private async void StartPlayingRecording_Click(object sender, RoutedEventArgs e)
        {
            if (SelectedSample == null) return;
            _file = await GetMp3File();
            PlayMediaPlayer();
        }

        private void PausePlayingRecord_OnClick(object sender, RoutedEventArgs e)
        {
            PauseMediaPlayer();
        }

        private async void WaveformTest_Click(object sender, RoutedEventArgs e)
        {
            if (SelectedSample == null) return;
            _file = await GetMp3File();
            SoundBarColor = _soundBarDefaultColor;
            await DrawWaveform(_file);
            _firstTimePlay = false;
        }

        private void PlayMediaPlayer()
        {
            if (!_firstTimePlay)
            {
                _mediaPlayer = new MediaPlayer
                {
                    Source = MediaSource.CreateFromStorageFile(_file),
                    AudioCategory = MediaPlayerAudioCategory.Speech
                };
                _firstTimePlay = true;
            }
            _mediaPlayer.Play();
            _mediaPlayer.MediaEnded += MediaEnded;
            StartMediaTimer();
            IsRecordingPlaying = true;
            IsRecordingNotPlaying = !IsRecordingPlaying;
        }

        private void PauseMediaPlayer()
        {
            _mediaPlayer.Pause();
            StopMediaTimer();
            IsRecordingPlaying = false;
            IsRecordingNotPlaying = !IsRecordingPlaying;
        }

        private void StopMediaPlayer(MediaPlayer mediaPlayer)
        {
            StopMediaTimer();
            SoundPosition = 0;
            SoundPositionString = "00:00:00";
            mediaPlayer.MediaEnded -= MediaEnded;
            mediaPlayer.Dispose();
            IsRecordingPlaying = false;
            IsRecordingNotPlaying = !IsRecordingPlaying;
            _firstTimePlay = false;
        }

        private async void MediaEnded(MediaPlayer sender, object args)
        {
            await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => { StopMediaPlayer(sender); });
        }

        private async void StartMediaTimer()
        {
            if (_file == null) return;
            SoundBarColor = _soundBarDefaultColor;
            await DrawWaveform(_file);
            _timer = ThreadPoolTimer.CreatePeriodicTimer(ClockTimerTick, TimeSpan.FromMilliseconds(10));
        }

        private void StopMediaTimer()
        {
            _timer?.Cancel();
        }

        private async void ClockTimerTick(ThreadPoolTimer timer)
        {
            try
            {
                var dispatcher = Windows.ApplicationModel.Core.CoreApplication.MainView.CoreWindow.Dispatcher;
                await dispatcher.RunAsync(
                    CoreDispatcherPriority.Normal, () =>
                    {
                        var totalDuration = _mediaPlayer.PlaybackSession.NaturalDuration.TotalMilliseconds;
                        if (totalDuration <= 0) return;
                        var currentPos = _mediaPlayer.PlaybackSession.Position;
                        SoundPosition = currentPos.TotalMilliseconds * WaveformRendererSettings.DefaultWidth / totalDuration;
                        SoundPositionString = $"{currentPos.Hours * 60 + currentPos.Minutes:00}:{currentPos.Seconds:00}:{currentPos.Milliseconds:00}";
                    });
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Exception caught at ClockTimerTick -> " + ex.Message);
            }
        }

        private async Task DrawWaveform(StorageFile file)
        {
            if (file == null) return;
            var audioBytes = await file.ReadBytesAsync();
            await BassGenerateAudioData(audioBytes);
            var audioStream = await file.OpenStreamForReadAsync();
            await NAudioGenerateAudioData(audioStream);
            switch (PeakList)
            {
                case null when PeakList2 == null:
                    WaveformShow = Waveform2Show = RecordingShow = false;
                    return;
                case null:
                    WaveformShow = false;
                    Waveform2Show = RecordingShow = true;
                    return;
                default:
                {
                    if (PeakList2 == null)
                    {
                        Waveform2Show = false;
                        WaveformShow = RecordingShow = true;
                        return;
                    }
                    break;
                }
            }
            WaveformShow = Waveform2Show = RecordingShow = true;
        }

        private async Task BassGenerateAudioData(byte[] audioBytes)
        {
            List<(float min, float max)> peakList = null;

            await Task.Run(() =>
            {
                peakList = _waveformService.GenerateAudioData(audioBytes);
            });

            PeakList2 = peakList;
        }

        private async Task NAudioGenerateAudioData(Stream audioStream)
        {
            List<(float min, float max)> peakList = null;

            await Task.Run(() =>
            {
                peakList = _waveformService.GenerateAudioData(audioStream);
            });

            PeakList = peakList;
        }

        private void Waveform_OnPointerPressed(object sender, PointerRoutedEventArgs e)
        {
            if (!_firstTimePlay) return;
            WaveformPointerControl(sender, e);
            _pressed = true;
        }

        private void Waveform_OnPointerReleased(object sender, PointerRoutedEventArgs e)
        {
            if (!_firstTimePlay) return;
            WaveformPointerControl(sender, e);
            _pressed = false;
        }

        private void Waveform_OnPointerMoved(object sender, PointerRoutedEventArgs e)
        {
            if (!_pressed) return;
            WaveformPointerControl(sender, e);
        }

        private void WaveformPointerControl(object sender, PointerRoutedEventArgs e)
        {
            try
            {
                var point = e.GetCurrentPoint(sender as UIElement);
                SoundPosition = point.Position.X;
                var totalMilliseconds = SoundPosition * _mediaPlayer.PlaybackSession.NaturalDuration.TotalMilliseconds / WaveformRendererSettings.DefaultWidth;
                var currentPos = TimeSpan.FromMilliseconds(totalMilliseconds);
                _mediaPlayer.PlaybackSession.Position = currentPos;
                SoundPositionString = $"{currentPos.Hours * 60 + currentPos.Minutes:00}:{currentPos.Seconds:00}:{currentPos.Milliseconds:00}";
                Debug.WriteLine("Point: " + (point.Position.X)); //TODO: DELETE DEBUG LINE IF NOT NEEDED
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Exception caught at WaveformPointerControl -> " + ex.Message);
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
