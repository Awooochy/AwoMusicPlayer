using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NAudio.Wave;

namespace AwoMusicPlayer
{
    public class MusicPlayer : IDisposable
    {
        private List<string> _musicFiles = new List<string>();
        private string _currentSongPath = "";
        private int _currentSongIndex = -1;
        private bool _isPaused = false;
        private float _volume = 0.5f; // 0.0 to 1.0 for NAudio
        private bool _isLoopEnabled = false;
        private bool _enableAutoplay = true;

        private IWavePlayer _waveOutDevice;
        private AudioFileReader _audioFileReader;

        public string CurrentSongName => string.IsNullOrEmpty(_currentSongPath) ? "None" : Path.GetFileName(_currentSongPath);
        public bool IsPlaying => _waveOutDevice != null && _waveOutDevice.PlaybackState == PlaybackState.Playing;
        public bool IsPaused => _isPaused;
        
        public string CurrentSongFilePath => _currentSongPath; // ADDED: Expose the full path
        
        public NAudio.Wave.WaveFormat GetCurrentWaveFormat() 
        {
            return _audioFileReader?.WaveFormat;
        }
        
        
        public int Volume // 0-100 for user display/input
        {
            get => (int)(_volume * 100);
            set
            {
                _volume = Math.Max(0, Math.Min(100, value)) / 100.0f;
                if (_waveOutDevice != null)
                {
                    _waveOutDevice.Volume = _volume;
                }
                Console.WriteLine($"Volume set to {Volume}%");
            }
        }
        public bool IsLoopEnabled => _isLoopEnabled;
        public TimeSpan CurrentSongTime => _audioFileReader?.CurrentTime ?? TimeSpan.Zero;
        public TimeSpan TotalSongTime => _audioFileReader?.TotalTime ?? TimeSpan.Zero;


        public MusicPlayer()
        {
            // Initialize with default volume
            if (_waveOutDevice != null)
            {
                _waveOutDevice.Volume = _volume;
            }
        }

        public bool LoadMusic(IEnumerable<string> folderPaths)
        {
            _musicFiles.Clear();
            foreach (string folder in folderPaths)
            {
                if (Directory.Exists(folder))
                {
                    try
                    {
                        _musicFiles.AddRange(Directory.GetFiles(folder, "*.mp3"));
                        _musicFiles.AddRange(Directory.GetFiles(folder, "*.wav"));
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error loading files from {folder}: {ex.Message}");
                    }
                }
                else
                {
                    Console.WriteLine($"Warning: Folder not found - {folder}");
                }
            }
            _musicFiles = _musicFiles.Distinct().OrderBy(f => f).ToList(); // Remove duplicates and sort
            return _musicFiles.Any();
        }

        public void Play(int? songIndex = null)
        {
            if (!_musicFiles.Any())
            {
                Console.WriteLine("No music files loaded.");
                return;
            }

            if (songIndex.HasValue)
            {
                if (songIndex.Value >= 0 && songIndex.Value < _musicFiles.Count)
                {
                    _currentSongIndex = songIndex.Value;
                }
                else
                {
                    Console.WriteLine("Invalid song index.");
                    return;
                }
            }
            else if (_currentSongIndex == -1) // If no song was ever chosen, play the first one
            {
                _currentSongIndex = 0;
            }
            // If songIndex is null and _currentSongIndex is valid, it means we're resuming or re-playing the current.

            _currentSongPath = _musicFiles[_currentSongIndex];
            StopPlayback(); // Stop any current playback and dispose resources

            try
            {
                _audioFileReader = new AudioFileReader(_currentSongPath);
                _waveOutDevice = new WaveOutEvent();
                _waveOutDevice.Volume = _volume;
                _waveOutDevice.Init(_audioFileReader);
                _waveOutDevice.PlaybackStopped += OnPlaybackStopped;
                _waveOutDevice.Play();
                _isPaused = false;
                Console.WriteLine($"Now playing: {CurrentSongName}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error playing song {_currentSongPath}: {ex.Message}");
                _currentSongPath = "";
                _currentSongIndex = -1;
            }
        }

        public void PlayRandom()
        {
            if (!_musicFiles.Any())
            {
                Console.WriteLine("No music files to play randomly.");
                return;
            }
            Random random = new Random();
            int randomIndex = random.Next(0, _musicFiles.Count);
            Play(randomIndex);
        }

        public void TogglePause()
        {
            if (_waveOutDevice == null)
            {
                Console.WriteLine("No song is loaded to pause/resume.");
                return;
            }

            if (_isPaused)
            {
                _waveOutDevice.Play();
                _isPaused = false;
                Console.WriteLine("Resumed playback.");
            }
            else
            {
                if (_waveOutDevice.PlaybackState == PlaybackState.Playing)
                {
                    _waveOutDevice.Pause();
                    _isPaused = true;
                    Console.WriteLine("Paused playback.");
                }
            }
        }

        public void PlayNext()
        {
            if (!_musicFiles.Any()) return;
            _currentSongIndex++;
            if (_currentSongIndex >= _musicFiles.Count)
            {
                _currentSongIndex = 0; // Wrap around
            }
            Play(_currentSongIndex);
        }

        public void PlayPrevious()
        {
            if (!_musicFiles.Any()) return;
            _currentSongIndex--;
            if (_currentSongIndex < 0)
            {
                _currentSongIndex = _musicFiles.Count - 1; // Wrap around to the last song
            }
            Play(_currentSongIndex);
        }

        public void SkipForward(int seconds = 10)
        {
            if (_audioFileReader != null && _waveOutDevice != null && _waveOutDevice.PlaybackState != PlaybackState.Stopped)
            {
                TimeSpan newTime = _audioFileReader.CurrentTime + TimeSpan.FromSeconds(seconds);
                if (newTime < _audioFileReader.TotalTime)
                {
                    _audioFileReader.CurrentTime = newTime;
                }
                else
                {
                    PlayNext(); // Skip to next song if seeking past the end
                }
            }
        }

        public void Rewind(int seconds = 10)
        {
            if (_audioFileReader != null && _waveOutDevice != null && _waveOutDevice.PlaybackState != PlaybackState.Stopped)
            {
                TimeSpan newTime = _audioFileReader.CurrentTime - TimeSpan.FromSeconds(seconds);
                _audioFileReader.CurrentTime = (newTime < TimeSpan.Zero) ? TimeSpan.Zero : newTime;
            }
        }

        public void ToggleLoop()
        {
            _isLoopEnabled = !_isLoopEnabled;
            Console.WriteLine(_isLoopEnabled ? "Looping is enabled." : "Looping is disabled.");
        }

        public List<(string Name, int Index)> SearchSongs(string query)
        {
            var results = new List<(string Name, int Index)>();
            if (string.IsNullOrWhiteSpace(query)) return results;

            for (int i = 0; i < _musicFiles.Count; i++)
            {
                if (Path.GetFileName(_musicFiles[i]).ToLowerInvariant().Contains(query.ToLowerInvariant()))
                {
                    results.Add((Path.GetFileName(_musicFiles[i]), i));
                }
            }
            return results;
        }


        private void OnPlaybackStopped(object sender, StoppedEventArgs e)
        {
            // This event can be triggered by Stop(), song finishing, or an error.
            if (e.Exception != null)
            {
                Console.WriteLine($"Playback error: {e.Exception.Message}");
                // Optionally try to play next or stop everything
                return;
            }

            // Check if playback was stopped manually or naturally ended
            // If audioFileReader is null, it means StopPlayback was called, so don't autoplay.
            if (_audioFileReader == null) return;


            // If the current time is (almost) the total time, the song finished naturally.
            bool songFinishedNaturally = (_audioFileReader.CurrentTime >= _audioFileReader.TotalTime - TimeSpan.FromMilliseconds(500));


            if (songFinishedNaturally)
            {
                if (_isLoopEnabled)
                {
                    // Re-initialize for looping
                    _audioFileReader.Position = 0;
                    _waveOutDevice?.Play(); // waveOutDevice should still be valid here if loop is on
                }
                else if (_enableAutoplay)
                {
                     // Call PlayNext on the main thread or a new task if needed,
                     // but for console simple direct call might be okay.
                     // For robustness, especially in GUI, invoke on UI thread or queue action.
                    PlayNext();
                }
            }
            // If not looping and not autoplaying, or if stopped manually before end,
            // playback will just stop. Resources are cleaned up by Play() or Dispose().
        }


        private void StopPlayback()
        {
            if (_waveOutDevice != null)
            {
                _waveOutDevice.PlaybackStopped -= OnPlaybackStopped; // Important to unhook
                _waveOutDevice.Stop();
                _waveOutDevice.Dispose();
                _waveOutDevice = null;
            }
            if (_audioFileReader != null)
            {
                _audioFileReader.Dispose();
                _audioFileReader = null;
            }
            _isPaused = false; // Reset pause state
        }

        public void Dispose()
        {
            StopPlayback();
            // No other unmanaged resources in this class directly.
            // _musicFiles list will be garbage collected.
        }
    }
}