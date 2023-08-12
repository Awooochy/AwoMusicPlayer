using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NAudio.Wave;
using System.Media;
using System.Threading;

namespace AwoMusicPlayer
{
    class Program
    {
        static List<string> musicFolders = new List<string>();
        static List<string> musicFiles = new List<string>();
        static string currentSong = "";
        static bool isPaused = false;
        static int currentVolume = 50; // Initial volume set to 50%
        static int currentSongPosition = 0;
        static bool showMusicBar = false;

        static void Main(string[] args)
        {
            LoadMusicFolders();
            ChooseMusicFolder();

            Console.WriteLine("Music Player Commands:");
            Console.WriteLine("Press 'r' or 'R' for a random song.");
            Console.WriteLine("Press 'spacebar' to pause/unpause the music.");
            Console.WriteLine("Press 'right arrow' to play the next song.");
            Console.WriteLine("Press 'left arrow' to play the previous song.");
            Console.WriteLine("Press 'up arrow' to increase volume.");
            Console.WriteLine("Press 'down arrow' to decrease volume.");
            Console.WriteLine("Press '1' to skip forward 10 seconds.");
            Console.WriteLine("Press '2' to rewind 10 seconds.");
            Console.WriteLine("Press 'n' to search for a song by name.");
            Console.WriteLine("Press 'esc' to close the program.");

            while (true)
            {
                ConsoleKeyInfo keyInfo = Console.ReadKey(true);
                HandleKeyPress(keyInfo);
            }
        }

        static void LoadMusicFolders()
        {
            try
            {
                string configFile = "MusicFoldersLocation.txt";
                if (File.Exists(configFile))
                {
                    string[] lines = File.ReadAllLines(configFile);
                    musicFolders.AddRange(lines);
                }
                else
                {
                    Console.WriteLine("No music folders configured.");
                    Console.WriteLine("Closing");
                    Thread.Sleep(1000);
                    Console.Write(".");
                    Thread.Sleep(1000);
                    Console.Write(".");
                    Thread.Sleep(1000);
                    Console.Write(".");
                    Thread.Sleep(100);
                    Environment.Exit(0);
                }

                foreach (string folder in musicFolders)
                {
                    string[] files = Directory.GetFiles(folder, "*.mp3")
                                             .Concat(Directory.GetFiles(folder, "*.wav"))
                                             .Concat(Directory.GetFiles(folder, "*.ogg"))
                                             .Concat(Directory.GetFiles(folder, "*.mid"))
                                             .ToArray();
                    musicFiles.AddRange(files);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("An error occurred while loading music folders: " + ex.Message);
                Environment.Exit(0);
            }
        }

        static void ChooseMusicFolder()
        {
            Console.WriteLine("Choose a music folder:");
            for (int i = 0; i < musicFolders.Count; i++)
            {
                Console.WriteLine($"{i + 1}. {musicFolders[i]}");
            }
            Console.WriteLine($"{musicFolders.Count + 1}. All Folders");

            int choice = GetChoice(musicFolders.Count + 1);
            if (choice <= musicFolders.Count)
            {
                string selectedFolder = musicFolders[choice - 1];
                musicFiles = Directory.GetFiles(selectedFolder, "*.mp3")
                                     .Concat(Directory.GetFiles(selectedFolder, "*.wav"))
                                     .Concat(Directory.GetFiles(selectedFolder, "*.ogg"))
                                     .Concat(Directory.GetFiles(selectedFolder, "*.mid"))
                                     .ToList();
            }

            if (musicFiles.Count == 0)
            {
                Console.WriteLine("No music files found in the selected folder(s).");
                Console.WriteLine("Closing");
                Thread.Sleep(1000);
                Console.Write(".");
                Thread.Sleep(1000);
                Console.Write(".");
                Thread.Sleep(1000);
                Console.Write(".");
                Thread.Sleep(100);
                Environment.Exit(0);
            }
        }

        static void HandleKeyPress(ConsoleKeyInfo keyInfo)
        {
            switch (keyInfo.Key)
            {
                case ConsoleKey.R:
                    PlayRandomSong();
                    break;
                case ConsoleKey.Spacebar:
                    TogglePause();
                    break;
                case ConsoleKey.RightArrow:
                    PlayNextSong();
                    break;
                case ConsoleKey.LeftArrow:
                    PlayPreviousSong();
                    break;
                case ConsoleKey.UpArrow:
                    IncreaseVolume();
                    break;
                case ConsoleKey.DownArrow:
                    DecreaseVolume();
                    break;
                case ConsoleKey.D1:
                    SkipForward();
                    break;
                case ConsoleKey.D2:
                    Rewind();
                    break;
                case ConsoleKey.N:
                    SearchByName();
                    break;
                case ConsoleKey.Z:
                    ToggleMusicBar();
                    break;
                case ConsoleKey.Escape:
                    Environment.Exit(0);
                    break;
            }
        }

        static void PlayRandomSong()
        {
            Random random = new Random();
            int randomIndex = random.Next(0, musicFiles.Count);
            currentSongPosition = randomIndex;
            PlaySong(musicFiles[randomIndex]);
        }

        static void TogglePause()
        {
            if (!string.IsNullOrEmpty(currentSong))
            {
                if (isPaused)
                {
                    Console.WriteLine("Resumed playback.");
                    isPaused = false;
                    ResumeSong();
                }
                else
                {
                    Console.WriteLine("Paused playback.");
                    isPaused = true;
                    PauseSong();
                }
            }
        }

        static void PlayNextSong()
        {
            if (!string.IsNullOrEmpty(currentSong))
            {
                currentSongPosition++;
                if (currentSongPosition >= musicFiles.Count)
                {
                    currentSongPosition = 0;
                }
                PlaySong(musicFiles[currentSongPosition]);
            }
        }

        static void PlayPreviousSong()
        {
            if (!string.IsNullOrEmpty(currentSong))
            {
                currentSongPosition--;
                if (currentSongPosition < 0)
                {
                    Console.WriteLine("Cannot be done. This is the first song that you play.");
                    currentSongPosition = 0;
                    return;
                }
                PlaySong(musicFiles[currentSongPosition]);
            }
        }

        static void IncreaseVolume()
        {
            if (currentVolume < 100)
            {
                currentVolume += 10;
                Console.WriteLine($"Volume increased to {currentVolume}%");
                AdjustVolume();
            }
            else
            {
                Console.WriteLine("Volume is already at maximum.");
            }
        }

        static void DecreaseVolume()
        {
            if (currentVolume > 0)
            {
                currentVolume -= 10;
                Console.WriteLine($"Volume decreased to {currentVolume}%");
                AdjustVolume();
            }
            else
            {
                Console.WriteLine("Volume is already at minimum.");
            }
        }

        static void SkipForward()
        {
            if (waveOutDevice != null && waveOutDevice.PlaybackState == PlaybackState.Playing)
            {
                TimeSpan skipAmount = TimeSpan.FromSeconds(10); // Skip forward by 10 seconds

                if (waveOutDevice.PlaybackState == PlaybackState.Playing && audioFileReader != null)
                {
                    var currentPosition = audioFileReader.CurrentTime;
                    var newPosition = currentPosition + skipAmount;
                    if (newPosition < audioFileReader.TotalTime)
                    {
                        waveOutDevice.Pause();
                        audioFileReader.CurrentTime = newPosition;
                        waveOutDevice.Play();
                    }
                    else
                    {
                        PlayNextSong(); // Skip to the next song if skip amount exceeds song length
                    }
                }
            }
        }

        static void Rewind()
        {
            if (waveOutDevice != null && waveOutDevice.PlaybackState == PlaybackState.Playing)
            {
                TimeSpan rewindAmount = TimeSpan.FromSeconds(10); // Rewind by 10 seconds

                if (waveOutDevice.PlaybackState == PlaybackState.Playing && audioFileReader != null)
                {
                    var currentPosition = audioFileReader.CurrentTime;
                    var newPosition = currentPosition - rewindAmount;
                    if (newPosition >= TimeSpan.Zero)
                    {
                        waveOutDevice.Pause();
                        audioFileReader.CurrentTime = newPosition;
                        waveOutDevice.Play();
                    }
                    else
                    {
                        PlayPreviousSong(); // Rewind to the previous song if position goes before the start
                    }
                }
            }
        }

        static void SearchByName()
        {
            Console.Write("Enter the name or letters of the song: ");
            string searchQuery = Console.ReadLine().ToLower();

            var matchingSongs = musicFiles.Where(song => Path.GetFileName(song).ToLower().Contains(searchQuery)).ToList();

            if (matchingSongs.Count == 0)
            {
                Console.WriteLine("No matching songs found.");
                return;
            }

            Console.WriteLine("Matching songs:");
            for (int i = 0; i < matchingSongs.Count; i++)
            {
                Console.WriteLine($"{i + 1}. {Path.GetFileName(matchingSongs[i])}");
            }

            Console.Write("Select a song to play by entering its number: ");
            int choice = GetChoice(matchingSongs.Count);

            currentSongPosition = musicFiles.IndexOf(matchingSongs[choice - 1]);
            PlaySong(matchingSongs[choice - 1]);
        }


        static void ToggleMusicBar()
        {
            showMusicBar = !showMusicBar;
        }

        static SoundPlayer soundPlayer; // Declare soundPlayer as a class-level variable

        static int GetChoice(int maxChoice)
        {
            int choice;
            while (!int.TryParse(Console.ReadLine(), out choice) || choice < 1 || choice > maxChoice)
            {
                Console.Write($"Invalid input. Please enter a number between 1 and {maxChoice}: ");
            }
            return choice;
        }

        static void PlaySong(string filePath)
        {
            StopSong();

            currentSong = Path.GetFileName(filePath);
            Console.WriteLine($"Now playing: {currentSong}");

            audioFileReader = new AudioFileReader(filePath); // Initialize audioFileReader
            waveOutDevice = new WaveOutEvent(); // Initialize waveOutDevice
            waveOutDevice.Init(audioFileReader);
            waveOutDevice.Play();
        }


        static IWavePlayer waveOutDevice;
        static AudioFileReader audioFileReader;
        static void ResumeSong()
        {
            if (!string.IsNullOrEmpty(currentSong) && isPaused)
            {
                Console.WriteLine("Resumed playback.");
                isPaused = false;

                if (waveOutDevice != null && waveOutDevice.PlaybackState == PlaybackState.Paused)
                {
                    waveOutDevice.Play(); // Resume playback using the existing WaveOutEvent instance
                }
                else
                {
                    Console.WriteLine("No song is currently paused.");
                }
            }
        }


        static void PauseSong()
        {
            if (waveOutDevice != null && waveOutDevice.PlaybackState == PlaybackState.Playing)
            {
                waveOutDevice.Pause(); // Pause playback without disposing
            }
            else
            {
                Console.WriteLine("No song is currently playing.");
            }
        }


        static void AdjustVolume()
        {
            if (waveOutDevice != null)
            {
                waveOutDevice.Volume = currentVolume / 100.0f; // Adjust volume
            }
        }

        static void StopSong()
        {
            if (waveOutDevice != null)
            {
                waveOutDevice.Stop();
                waveOutDevice.Dispose();
                waveOutDevice = null;
            }
        }
    }
}
