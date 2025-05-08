using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading; // For Thread.Sleep
// Ensure NAudio.Wave is available if WaveFormat is used directly in Program.cs for display
// using NAudio.Wave; // Not strictly needed here if MusicPlayer formats the string

namespace AwoMusicPlayer
{
    public static class StringExtensions
    {
        public static string Ellipsis(this string value, int maxLength)
        {
            if (string.IsNullOrEmpty(value)) return value;
            if (maxLength <= 3) return new string('.', Math.Max(0, maxLength));
            return value.Length <= maxLength ? value : value.Substring(0, maxLength - 3) + "...";
        }
    }

    class Program
    {
        private static List<string> _configuredMusicFolders = new List<string>();
        private static MusicPlayer _player;
        private static bool _showMusicBar = false;

        // ADDED: New state flags for different views
        private static bool _showFullSongName = false;
        private static bool _showSongDetailsView = false;

        // Define approx how many lines the main content area might take before music bar
        private const int MaxMainContentLines = 20; // Generous estimate for commands/details


        static void Main(string[] args)
        {
            Console.Title = "AwoMusicPlayer";
            LoadConfiguredMusicFolders();

            if (!_configuredMusicFolders.Any())
            {
                Console.WriteLine("No music folders configured in MusicFoldersLocation.txt or it's missing.");
                PauseBeforeExit();
                return;
            }

            List<string> foldersToLoad;
            if (_configuredMusicFolders.Count > 1 || _configuredMusicFolders.Count == 0)
            {
                foldersToLoad = ChooseMusicFoldersFromConfig();
            }
            else
            {
                foldersToLoad = new List<string>(_configuredMusicFolders);
                Console.WriteLine($"Automatically loading music from: {_configuredMusicFolders.First()}");
            }

            if (!foldersToLoad.Any())
            {
                Console.WriteLine("No folders selected for loading.");
                PauseBeforeExit();
                return;
            }

            _player = new MusicPlayer();
            if (!_player.LoadMusic(foldersToLoad))
            {
                Console.WriteLine("No music files (.mp3, .wav) found in the selected folder(s).");
                _player.Dispose();
                PauseBeforeExit();
                return;
            }

            UpdateMainDisplay(); // Initial display (renamed from DisplayCommands for clarity)

            while (true)
            {
                if (Console.KeyAvailable)
                {
                    ConsoleKeyInfo keyInfo = Console.ReadKey(true);
                    HandleKeyPress(keyInfo);
                }

                if (_showMusicBar && _player != null && (_player.IsPlaying || _player.IsPaused))
                {
                    DisplayMusicBar();
                }
                Thread.Sleep(100); 
            }
        }

        static void LoadConfiguredMusicFolders()
        {
            string configFile = "MusicFoldersLocation.txt";
            if (File.Exists(configFile))
            {
                try
                {
                    _configuredMusicFolders.AddRange(File.ReadAllLines(configFile).Where(line => !string.IsNullOrWhiteSpace(line) && Directory.Exists(line.Trim())).Select(line => line.Trim()));
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error reading {configFile}: {ex.Message}");
                }
            }
        }
        static List<string> ChooseMusicFoldersFromConfig()
        {
            Console.Clear(); 
            if (!_configuredMusicFolders.Any()) {
                Console.WriteLine("No valid folders found in MusicFoldersLocation.txt to choose from.");
                Console.WriteLine("Please add folder paths to MusicFoldersLocation.txt, one per line.");
                Console.WriteLine("Press any key to exit.");
                Console.ReadKey();
                Environment.Exit(0);
            }

            Console.WriteLine("\nChoose music folder(s) to load:");
            for (int i = 0; i < _configuredMusicFolders.Count; i++)
            {
                Console.WriteLine($"{i + 1}. {_configuredMusicFolders[i]}");
            }
            Console.WriteLine($"{_configuredMusicFolders.Count + 1}. All Configured Folders");
            Console.WriteLine("0. Exit");

            List<string> selectedFolders = new List<string>();
            bool validInput = false;
            while (!validInput)
            {
                Console.Write($"Enter your choice (use numbers to select which folder to play or {_configuredMusicFolders.Count + 1} for All, or 0 to exit): ");
                string input = Console.ReadLine();

                if (input == "0") Environment.Exit(0);

                var parts = input.Split(',').Select(p => p.Trim()).ToList();
                List<string> tempSelected = new List<string>();
                bool allPartsValidThisAttempt = true;

                foreach (var part in parts)
                {
                    if (int.TryParse(part, out int choiceIndex))
                    {
                        if (choiceIndex > 0 && choiceIndex <= _configuredMusicFolders.Count)
                        {
                            tempSelected.Add(_configuredMusicFolders[choiceIndex - 1]);
                        }
                        else if (choiceIndex == _configuredMusicFolders.Count + 1)
                        {
                            tempSelected.AddRange(_configuredMusicFolders); 
                            break;
                        }
                        else
                        {
                            allPartsValidThisAttempt = false; break;
                        }
                    }
                    else
                    {
                        allPartsValidThisAttempt = false; break;
                    }
                }

                if (allPartsValidThisAttempt && tempSelected.Any())
                {
                    selectedFolders = tempSelected.Distinct().ToList();
                    validInput = true;
                }
                else
                {
                    Console.WriteLine("Invalid choice. Please try again.");
                }
            }
            Console.Clear(); 
            return selectedFolders;
        }

        // RENAMED and Overhauled DisplayCommands to UpdateMainDisplay
        static void UpdateMainDisplay()
        {
            Console.SetCursorPosition(0, 0); 

            if (!_showMusicBar)
            {
                Console.Clear(); 
            }
            else
            {
                // If music bar is on, clear a fixed large enough area for main content
                for (int i = 0; i < MaxMainContentLines; i++)
                {
                    if (Console.WindowHeight > i)
                    {
                        Console.SetCursorPosition(0, i);
                        Console.Write(new string(' ', Console.WindowWidth > 0 ? Console.WindowWidth - 1 : 0));
                    } else {
                        break;
                    }
                }
                Console.SetCursorPosition(0, 0); 
            }

            Console.WriteLine("--- AwoMusicPlayer ---");

            if (_showSongDetailsView)
            {
                DisplaySongDetails();
            }
            else // Show commands and potentially full song name
            {
                
                
                
                Console.WriteLine("Controls:");
                Console.WriteLine("  R         - Play a Random song");
                Console.WriteLine("  Spacebar  - Pause/Resume");
                Console.WriteLine("  L         - Toggle Loop current song");
                Console.WriteLine("  RightArrow- Next song");
                Console.WriteLine("  LeftArrow - Previous song");
                Console.WriteLine("  UpArrow   - Volume Up");
                Console.WriteLine("  DownArrow - Volume Down");
                Console.WriteLine("  1 (Num)   - Skip Forward 10s");
                Console.WriteLine("  2 (Num)   - Rewind 10s");
                Console.WriteLine("  N         - Search for a song by name");
                Console.WriteLine("  Z         - Toggle Music Bar");
                Console.WriteLine("  F         - Full Filename");
                Console.WriteLine("  D         - Song Details");
                Console.WriteLine("  Esc       - Exit Player");
                Console.WriteLine("----------------------");
                
                if (_player != null)
                {
                    string volInfo = $"Vol:{_player.Volume}%";
                    string loopInfo = _player.IsLoopEnabled ? "Loop:On" : "Loop:Off";
                    string playStatus = _player.IsPaused ? "Paused" : (_player.IsPlaying ? "Playing" : "Stopped");
                    string songInfoForStatus = "";
                    if (!_showFullSongName && !_showSongDetailsView) { // Show truncated song name only if no other dedicated display
                        songInfoForStatus = _player.CurrentSongName.Ellipsis(Console.WindowWidth > 70 ? 20 : 5) + " | ";
                    }
                    string statusLine = $"{songInfoForStatus}{volInfo} | {loopInfo} | {playStatus}";
                    Console.WriteLine(statusLine.Ellipsis(Console.WindowWidth > 1 ? Console.WindowWidth - 1 : 0));
                }
                else
                {
                    Console.WriteLine("Player not fully initialized.");
                }
                
                Console.WriteLine("----------------------");
                if (_showFullSongName && _player != null && !string.IsNullOrEmpty(_player.CurrentSongFilePath))
                {
                    Console.WriteLine($"Now Playing: {_player.CurrentSongName}".Ellipsis(Console.WindowWidth > 1 ? Console.WindowWidth -1 : 0));
                    Console.WriteLine("----------------------");
                }
            }

            // At the end of UpdateMainDisplay()
            try
            {
                // Attempt to move cursor below the block of text just written,
                // but above the music bar if it's active.
                int lastLineOfContent = Console.CursorTop; // Get current line after all WriteLine calls
                int targetCursorLine = lastLineOfContent;

                if (_showMusicBar && targetCursorLine >= Console.WindowHeight - 1) {
                    // If we'd write on or below music bar line, move cursor just above music bar
                    targetCursorLine = Math.Max(0, Console.WindowHeight - 2);
                } else if (targetCursorLine >= Console.WindowHeight) {
                    // If cursor is beyond window height (e.g. after clearing a tall console)
                    // bring it to the last visible line or just above bar
                    targetCursorLine = _showMusicBar ? Math.Max(0, Console.WindowHeight - 2) : Math.Max(0, Console.WindowHeight -1);
                }


                if (Console.WindowHeight > targetCursorLine && targetCursorLine >=0) { // Ensure target is valid
                    Console.SetCursorPosition(0, targetCursorLine);
                }
                // If console is very short, cursor might end up on the last printed line.
            } catch {} // Ignore cursor setting errors
        }

        // NEW Method to display song details
        static void DisplaySongDetails()
        {
            Console.WriteLine("--- Song Details ---");
            if (_player != null && !string.IsNullOrEmpty(_player.CurrentSongFilePath)) // Use new property
            {
                Console.WriteLine($"File:     {_player.CurrentSongName.Ellipsis(Console.WindowWidth - 12)}");
                Console.WriteLine($"Path:     {_player.CurrentSongFilePath.Ellipsis(Console.WindowWidth - 12)}"); // Use new property
                Console.WriteLine($"Duration: {_player.TotalSongTime:mm\\:ss}");

                NAudio.Wave.WaveFormat wf = _player.GetCurrentWaveFormat(); // Use new method
                if (wf != null)
                {
                    Console.WriteLine($"Format:   {wf.Encoding}, {wf.SampleRate}Hz, {wf.Channels}ch, {wf.BitsPerSample}-bit");
                }
                else
                {
                    Console.WriteLine("Format:   N/A");
                }
                Console.WriteLine("Artist:   (metadata not implemented)"); 
                Console.WriteLine("Album:    (metadata not implemented)"); 
                Console.WriteLine("----------------------");
                Console.WriteLine("Press 'D' to hide Details, or other command keys.");
            }
            else
            {
                Console.WriteLine("No song currently loaded or playing.");
                Console.WriteLine("----------------------");
                Console.WriteLine("Press 'D' to hide Details, or other command keys.");
            }
            Console.WriteLine("----------------------");
        }


        static void HandleKeyPress(ConsoleKeyInfo keyInfo)
        {
            if (_player == null) return; 

            bool requiresScreenRefresh = true; 

            switch (keyInfo.Key)
            {
                case ConsoleKey.R: _player.PlayRandom(); break;
                case ConsoleKey.Spacebar: _player.TogglePause(); break;
                case ConsoleKey.L: _player.ToggleLoop(); break;
                case ConsoleKey.RightArrow: _player.PlayNext(); break;
                case ConsoleKey.LeftArrow: _player.PlayPrevious(); break;
                case ConsoleKey.UpArrow: _player.Volume += 10; break;
                case ConsoleKey.DownArrow: _player.Volume -= 10; break;
                case ConsoleKey.D1: case ConsoleKey.NumPad1:
                    _player.SkipForward(); break;
                case ConsoleKey.D2: case ConsoleKey.NumPad2:
                    _player.Rewind(); break;
                
                // ADDED: Cases for F and D keys
                case ConsoleKey.F: 
                    _showFullSongName = !_showFullSongName;
                    if (_showFullSongName) _showSongDetailsView = false; // Mutually exclusive with details for clarity
                    break; 
                case ConsoleKey.D:
                    _showSongDetailsView = !_showSongDetailsView;
                    if (_showSongDetailsView) _showFullSongName = false; // Mutually exclusive with full name
                    break;

                case ConsoleKey.N:
                    PerformSearch(); 
                    UpdateMainDisplay(); 
                    requiresScreenRefresh = true; 
                    break; 
                case ConsoleKey.Z:
                    _showMusicBar = !_showMusicBar;
                    if (!_showMusicBar) ClearMusicBarLine(); 
                    UpdateMainDisplay(); 
                    requiresScreenRefresh = false; 
                    break;
                case ConsoleKey.Escape:
                    Console.WriteLine("Exiting player...");
                    _player.Dispose(); Environment.Exit(0);
                    requiresScreenRefresh = false; 
                    break;
                default: 
                    requiresScreenRefresh = false; break;
            }

            if (requiresScreenRefresh)
            {
                UpdateMainDisplay(); // Use the new name
            }
        }

        static void PerformSearch()
        {
            // 1. Save current view state (which view was active)
            bool wasShowingDetailsView = _showSongDetailsView; // CORRECTED VARIABLE NAME
            bool wasShowingFullSongName = _showFullSongName;   // CORRECTED VARIABLE NAME

            // 2. Prepare screen for search: Clear the main content area.
            // The music bar (if active) will remain because we only clear above it.
            Console.SetCursorPosition(0, 0);
            if (!_showMusicBar) {
                Console.Clear(); // Full clear if no music bar
            } else {
                // Clear everything above the music bar line
                for (int i = 0; i < Console.WindowHeight - 1; i++) { // -1 to spare the music bar line
                    if (Console.WindowHeight > i) { // Check if console is tall enough
                        Console.SetCursorPosition(0, i);
                        Console.Write(new string(' ', Console.WindowWidth > 0 ? Console.WindowWidth - 1 : 0));
                    } else {
                        break; // Stop if console height is less
                    }
                }
                Console.SetCursorPosition(0, 0); // Reset cursor to top for search UI
            }

            // 3. Display Search UI
            Console.WriteLine("--- Search Song ---");
            // Console.WriteLine("(Press Esc to cancel search - Note: Basic ReadLine used)"); // Optional: reminder about Esc
            Console.Write("Enter song name to search: ");

            // 4. Get search query
            string query = Console.ReadLine(); 

            if (string.IsNullOrWhiteSpace(query)) { // Handle empty input as cancellation or no search
                Console.WriteLine("\nSearch cancelled or empty input."); Thread.Sleep(1000);
                 _showSongDetailsView = wasShowingDetailsView;  // CORRECTED: Restore original state
                 _showFullSongName = wasShowingFullSongName;    // CORRECTED: Restore original state
                return; // UpdateMainDisplay will be called by HandleKeyPress
            }

            // Optional: Clear the search prompt lines if desired, though UpdateMainDisplay will handle it
            try {
                if(Console.CursorTop > 0) { 
                    Console.SetCursorPosition(0, Console.CursorTop -1 ); 
                    Console.Write(new string(' ', Console.WindowWidth > 0 ? Console.WindowWidth -1:0)); 
                    if (Console.CursorTop > 0) { // Check again if we can go up one more line for the "--- Search Song ---"
                         Console.SetCursorPosition(0, Console.CursorTop -1);
                         Console.Write(new string(' ', Console.WindowWidth > 0 ? Console.WindowWidth -1:0));
                    }
                    Console.SetCursorPosition(0, Console.CursorTop > 0 ? Console.CursorTop : 0); // Position for results, ensuring not negative
                }
            } catch {}

            var results = _player.SearchSongs(query);

            if (!results.Any())
            {
                Console.WriteLine("\nNo songs found matching your query.");
            }
            else
            {
                Console.WriteLine("\nMatching songs:");
                for (int i = 0; i < results.Count; i++)
                {
                    Console.WriteLine($"{i + 1}. {results[i].Name.Ellipsis(Console.WindowWidth - 6)}");
                }

                Console.Write("\nSelect a song number to play (or 0/Enter to cancel): ");
                string choiceInput = Console.ReadLine();
                if (int.TryParse(choiceInput, out int choice) && choice > 0 && choice <= results.Count)
                {
                    _player.Play(results[choice - 1].Index);
                }
                else
                {
                    Console.WriteLine("Search selection cancelled or invalid.");
                }
            }

            Console.WriteLine("\nPress any key to return to the main view...");
            Console.ReadKey(true); 

            // 5. Restore the view state flags.
            _showSongDetailsView = wasShowingDetailsView;  // CORRECTED VARIABLE NAME
            _showFullSongName = wasShowingFullSongName;    // CORRECTED VARIABLE NAME
            // UpdateMainDisplay will be called by HandleKeyPress after this method returns
        }
        static void ClearMusicBarLine()
        {
            if (Console.WindowHeight <= 0) return;
            int barLine = Console.WindowHeight - 1;
            if (barLine < 0) barLine = 0;

            int originalCursorTop = Console.CursorTop;
            int originalCursorLeft = Console.CursorLeft;
            bool canRestoreCursor = originalCursorTop != barLine || originalCursorLeft != 0;

            try
            {
                Console.SetCursorPosition(0, barLine);
                Console.Write(new string(' ', Console.WindowWidth > 0 ? Console.WindowWidth - 1 : 0));

                if (canRestoreCursor)
                {
                    Console.SetCursorPosition(originalCursorLeft, originalCursorTop);
                }
            }
            catch (Exception) { /* Ignore console errors */ }
        }


        static void DisplayMusicBar()
        {
            if (Console.WindowHeight <= 0 || _player == null) return;

            int barLine = Console.WindowHeight - 1;
            if (barLine < 0) barLine = 0; 

            // Avoid drawing over main content if console is extremely short
            if (barLine < MaxMainContentLines && barLine != 0) { // if barLine is 0, it's the only line, so draw.
                // Check if it would overlap where UpdateMainDisplay usually prints
                // This threshold is a heuristic.
                if (barLine < 5) { /* allow if console is super tiny, like 1-4 lines */ }
                else return; 
            }

            int originalCursorTop = Console.CursorTop;
            int originalCursorLeft = Console.CursorLeft;
            bool cursorCanBeRestored = (originalCursorTop != barLine || originalCursorLeft != 0);

            try
            {
                Console.SetCursorPosition(0, barLine);
                Console.Write(new string(' ', Console.WindowWidth > 0 ? Console.WindowWidth - 1 : 0));
                Console.SetCursorPosition(0, barLine); 

                TimeSpan current = _player.CurrentSongTime;
                TimeSpan total = _player.TotalSongTime;
                double progressPercent = 0;
                if (total > TimeSpan.Zero && total >= current && current >= TimeSpan.Zero)
                {
                    progressPercent = current.TotalSeconds / total.TotalSeconds;
                }
                progressPercent = Math.Max(0, Math.Min(1, progressPercent)); 

                int actualProgressBarWidth = 50; // Increased from 30 as song name is removed

                // Song name is removed from here
                string timePart = $"{current:mm\\:ss}/{total:mm\\:ss}".PadRight(12);
                string statusPart = (_player.IsPaused ? "Paused" : "Playing").PadRight(8);
                string volPart = $"Vol:{_player.Volume}%".PadRight(9);
                string loopPart = (_player.IsLoopEnabled ? "Loop:On" : "Loop:Off").PadRight(9);

                int filledBarChars = (int)(progressPercent * actualProgressBarWidth);
                string barItself = $"[{new string('=', filledBarChars)}{new string(' ', actualProgressBarWidth - filledBarChars)}]";

                // REMOVED songNamePart from this string
                string fullBarString = $"{timePart}|{barItself}|{statusPart}|{volPart}|{loopPart}";

                Console.Write(fullBarString.Ellipsis(Console.WindowWidth > 0 ? Console.WindowWidth - 1 : 0));

                if (cursorCanBeRestored)
                {
                    Console.SetCursorPosition(originalCursorLeft, originalCursorTop);
                }
            }
            catch (Exception) { /* Silently ignore console drawing errors */ }
        }

        static void PauseBeforeExit(int milliseconds = 2000)
        {
            Console.WriteLine($"Exiting in {milliseconds/1000} seconds...");
            Thread.Sleep(milliseconds);
        }
    }
}