using Microsoft.Win32;
using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace Mantis;

public partial class MainWindow : Window
{
    readonly string musicMenu = Path.Combine(Path.GetTempPath(), "MUSIC22.WAV");
    readonly string musicGood = Path.Combine(Path.GetTempPath(), "MUSIC10.WAV");

    readonly string soundSmall = Path.Combine(Path.GetTempPath(), "Sound0016padded.WAV");
    readonly string soundBig = Path.Combine(Path.GetTempPath(), "Sound0052padded.WAV");
    readonly string soundShutdown = Path.Combine(Path.GetTempPath(), "Sound0058.WAV");
    readonly string soundPowerup = Path.Combine(Path.GetTempPath(), "Sound0059.WAV");
    readonly string soundNew = Path.Combine(Path.GetTempPath(), "Betty0029.WAV");

    readonly string soundSuccess = Path.Combine(Path.GetTempPath(), "Betty0005.WAV");
    readonly string soundFailure = Path.Combine(Path.GetTempPath(), "Betty0004.WAV");

    public MainWindow()
    {
        InitializeComponent();

        ExtractAudioFiles();

        MusicPlayer.Source = new Uri(musicMenu, UriKind.Absolute);
        MusicPlayer.Play();
    }

    private static void ExtractAudioFiles() // embedded resources + runtime-extraction allows distribution of single exe file
    {
        Assembly assembly = Assembly.GetExecutingAssembly();
        string[] resourceNames = assembly.GetManifestResourceNames();

        var audioFiles = resourceNames
            .Where(r => r.StartsWith("Mantis.snd.")
                        && (r.EndsWith(".wav", StringComparison.OrdinalIgnoreCase))
                        && !r.EndsWith(".g.resources"))
            .ToList();

        string tempDir = Path.GetTempPath();
        foreach (var resourceName in audioFiles)
        {
            string fileName = resourceName.Substring("Mantis.snd.".Length);

            Stream audioStream = assembly.GetManifestResourceStream(resourceName);
            string tempPath = Path.Combine(tempDir, fileName);

            using var fileStream = File.Create(tempPath);
            audioStream.CopyTo(fileStream);
        }
    }

    string? inputExePath;
    string? outputExePath;
    string? inputFolder;
    string? outputFolder;
    string? mainSystemCfgPath;
    string? backupSystemCfgPath;

    private void MusicPlayer_MediaEnded(object sender, RoutedEventArgs e)
    {
        MusicPlayer.Position = TimeSpan.Zero;
        MusicPlayer.Source = new Uri(musicMenu, UriKind.Absolute);
        MusicPlayer.Volume = 0.5;
        MusicPlayer.Play();
    }

    private void BrowseInput_Click(object sender, RoutedEventArgs e)
    {
        if (SoundMusicCheckbox.IsChecked == false)
        {
            ClickSound.Source = new Uri(soundBig, UriKind.Absolute);
            ClickSound.Play();
        }

        OpenFileDialog openFileDialog = new OpenFileDialog();
        openFileDialog.Filter = "Executable files (*.exe)|*.exe|All files (*.*)|*.*";
        openFileDialog.Title = "Select MechCommander Gold exe (e.g., MCX.exe)";

        if (inputFolder is null)
        {
            openFileDialog.InitialDirectory = AppDomain.CurrentDomain.BaseDirectory;
        }
        else
        {
            openFileDialog.InitialDirectory = inputFolder;
        }
        openFileDialog.FileName = "MCX.exe";

        if (openFileDialog.ShowDialog() == true)
        {
            inputExePath = openFileDialog.FileName;
            InputExeTextBox.Text = inputExePath;

            string inputFolder = Directory.GetParent(inputExePath).ToString();
            this.inputFolder = inputFolder;

            if (outputExePath is null)
            {
                outputExePath = Path.Combine(inputFolder, "MCX_patched.exe");
                OutputExeTextBox.Text = outputExePath;
                outputFolder = inputFolder;
            }

            /// Paths are actually relative from MCX.exe, so this should normally be a non-issue
            //if (inputExePath.Length > 60)
            //{
            //    string title = "Well, I think we can count on Mister Harrison";
            //    string msg = "FYI: MechCommander *may* (or may not) have issues with longer file paths because it stores "
            //        + "certain file paths in memory chunks that are only 80 bytes long. If you quickly experience issues "
            //        + "while playing, consider reinstalling to a different location.";
            //    MessageBox.Show(msg, title, MessageBoxButton.OK, MessageBoxImage.Information);
            //}
        }
    }

    private void BrowseOutput_Click(object sender, RoutedEventArgs e)
    {
        if (SoundMusicCheckbox.IsChecked == false)
        {
            ClickSound.Source = new Uri(soundBig, UriKind.Absolute);
            ClickSound.Play();
        }

        SaveFileDialog saveFileDialog = new SaveFileDialog();
        saveFileDialog.Filter = "Executable files (*.exe)|*.exe|All files (*.*)|*.*";
        saveFileDialog.Title = "Select name and location for saving patched exe (e.g., MCX_patched.exe)";

        outputFolder ??= inputFolder;
        if (outputFolder is null)
        {
            saveFileDialog.InitialDirectory = AppDomain.CurrentDomain.BaseDirectory;
        }
        else
        {
            saveFileDialog.InitialDirectory = inputFolder;
        }
        saveFileDialog.FileName = "MCX_patched.exe";

        if (saveFileDialog.ShowDialog() == true)
        {
            string selectedPath = saveFileDialog.FileName;

            // Ensure it has the .exe extension by adding it if it doesn't
            if (!selectedPath.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
            {
                selectedPath += ".exe";
            }

            outputExePath = selectedPath;
            OutputExeTextBox.Text = outputExePath;

            string outputFolder = Directory.GetParent(outputExePath).ToString();
            this.outputFolder = outputFolder;
        }
    }

    private void SoundMusicCheckbox_Checked(object sender, RoutedEventArgs e)
    {
        ShutdownSound.Source = new Uri(soundShutdown, UriKind.Absolute);
        ShutdownSound.Volume = 0.2;
        ShutdownSound.Play();

        MusicPlayer.Stop();
        BettySound.Stop();
    }

    private void SoundMusicCheckbox_Unchecked(object sender, RoutedEventArgs e)
    {
        ShutdownSound.Source = new Uri(soundPowerup, UriKind.Absolute);
        ShutdownSound.Volume = 0.3;
        ShutdownSound.Play();

        MusicPlayer.Source = new Uri(musicMenu, UriKind.Absolute);
        MusicPlayer.Volume = 0.5;
        MusicPlayer.Play();
    }

    private void ResCheckbox_Checked(object sender, RoutedEventArgs e)
    {
        ResWidth.IsEnabled = true;
        ResHeight.IsEnabled = true;

        ResWidth.Foreground = new SolidColorBrush(Colors.Black);
        ResHeight.Foreground = new SolidColorBrush(Colors.Black);

        if (SoundMusicCheckbox.IsChecked == false)
        {
            ClickSound.Source = new Uri(soundSmall, UriKind.Absolute);
            ClickSound.Play();

            BettySound.Source = new Uri(soundNew, UriKind.Absolute);
            BettySound.Play();
        }
    }

    private void ResCheckbox_Unchecked(object sender, RoutedEventArgs e)
    {
        ResWidth.IsEnabled = false;
        ResHeight.IsEnabled = false;

        ResWidth.Foreground = new SolidColorBrush(Colors.Gray);
        ResHeight.Foreground = new SolidColorBrush(Colors.Gray);

        if (SoundMusicCheckbox.IsChecked == false)
        {
            ClickSound.Source = new Uri(soundSmall, UriKind.Absolute);
            ClickSound.Play();
        }
    }

    private void NoCdCheckbox_Checked(object sender, RoutedEventArgs e)
    {
        FixCfgPathsCheckbox.IsEnabled = true;
        FixCfgPathsCheckbox.Foreground = new SolidColorBrush(Colors.WhiteSmoke);

        if (SoundMusicCheckbox.IsChecked == false)
        {
            ClickSound.Source = new Uri(soundSmall, UriKind.Absolute);
            ClickSound.Play();

            BettySound.Source = new Uri(soundNew, UriKind.Absolute);
            BettySound.Play();
        }
    }

    private void NoCdCheckbox_Unchecked(object sender, RoutedEventArgs e)
    {
        FixCfgPathsCheckbox.IsEnabled = false;
        FixCfgPathsCheckbox.Foreground = new SolidColorBrush(Colors.Gray);

        if (SoundMusicCheckbox.IsChecked == false)
        {
            ClickSound.Source = new Uri(soundSmall, UriKind.Absolute);
            ClickSound.Play();
        }
    }

    private void PatchButton_Click(object sender, RoutedEventArgs e)
    {
        if (SoundMusicCheckbox.IsChecked == false)
        {
            ClickSound.Source = new Uri(soundBig, UriKind.Absolute);
            ClickSound.Play();
        }

        try
        {
            ApplyPatchesAndSaveFile();

            if (SoundMusicCheckbox.IsChecked == false)
            {
                BettySound.Source = new Uri(soundSuccess, UriKind.Absolute);
                BettySound.Play();

                MusicPlayer.Source = new Uri(musicGood, UriKind.Absolute);
                MusicPlayer.Volume = 0.4;
                MusicPlayer.Play();
            }

            string msg = $"Patch applied and saved successfully to:\n{outputExePath}";
            if (NoCdCheckbox.IsChecked == true && FixCfgPathsCheckbox.IsChecked == true)
            {
                msg += "\n\n" + $"A backup of the old SYSTEM.CFG file was created at {backupSystemCfgPath}";
            }
            MessageBox.Show(msg, "Harrison - excellent job", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (ArgumentOutOfRangeException ex)
        {
            MessageBox.Show($"{ex.Message}", "Commander, I assume you have this under control", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
        catch (ArgumentNullException)
        {
            string title = "Colonel, flight deck needs final confirmation";
            string msg = "A MechCommander Gold game exe is required - use the first \"Browse...\" button near the top of the program.";
            MessageBox.Show(msg, title, MessageBoxButton.OK, MessageBoxImage.Warning);
        }
        catch (ArgumentException)
        {
            string title = "I have an invasion to go to";
            string msg = "No SYSTEM.CFG file found at the path of the MechCommander Gold exe path.";
            MessageBox.Show(msg, title, MessageBoxButton.OK, MessageBoxImage.Warning);
        }
        catch (FileNotFoundException ex)
        {
            MessageBox.Show($"{ex.Message}", "Track that pod", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
        catch (InvalidDataException ex)
        {
            MessageBox.Show($"{ex.Message}", "I'm ejecting, ejecting, ejecting", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
        catch (InvalidOperationException)
        {
            MessageBox.Show("None of the patch options are selected so there's nothing to do - no action taken.", "Commander, reporting Charlie Zone all clear", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (OperationCanceledException)
        {
            MessageBox.Show("No action has been taken.", "Sweeping last sector now... sir", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            if (SoundMusicCheckbox.IsChecked == false)
            {
                BettySound.Source = new Uri(soundFailure, UriKind.Absolute);
                BettySound.Play();
            }

            MessageBox.Show($"Unhandled error while applying patch or saving file - patching aborted:\n\n{ex}", "Sir, they've got us ranged", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void ApplyPatchesAndSaveFile()
    {
        if (LeftArmCheckbox.IsChecked == false && WeaponSizeCheckbox.IsChecked == false && ResCheckbox.IsChecked == false && NoCdCheckbox.IsChecked == false)
        {
            throw new InvalidOperationException();
        }

        if (inputExePath is null)
        {
            throw new ArgumentNullException();
        }

        if (outputExePath is null)
        {
            throw new Exception(); // shouldn't be possible, so use fall through to a harsher exception
        }

        if (!File.Exists(inputExePath))
        {
            throw new FileNotFoundException(nameof(inputExePath), $"Game exe not found at: {inputExePath}");
        }

        if (File.Exists(outputExePath))
        {
            string title = "Clear the channels, I have enemy contact";
            string msg = "A file already exists at the specified output path. Do you want to overwrite it?";
            if (MessageBox.Show(msg, title, MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.No)
            {
                throw new OperationCanceledException("Patching aborted by user due to existing file.");
            }
        }

        // Note: byte arrays in C# are reference types, so this array 
        //   will be updated by each of the functions as they run.
        byte[] fileData = File.ReadAllBytes(inputExePath);

        if (LeftArmCheckbox.IsChecked == true) // nullable bool means the == true must be explicit
        {
            PatchLeftArmJump(fileData);
            PatchLeftArmFix(fileData);
        }
        if (WeaponSizeCheckbox.IsChecked == true) // nullable bool means the == true must be explicit
        {
            PatchWeaponSizeCutoff(fileData);
            PatchWeaponSizeClassification(fileData);
        }
        if (ResCheckbox.IsChecked == true) // nullable bool means the == true must be explicit
        {
            PatchResolutionHeight(fileData);
            PatchResolutionWidth(fileData);
        }
        if (NoCdCheckbox.IsChecked == true) // nullable bool means the == true must be explicit
        {
            PatchCdCheckStartup(fileData);
            PatchCdCheckShortJumps(fileData);
            PatchCdCheckLongJumps(fileData);

            if (FixCfgPathsCheckbox.IsChecked == true)
            {
                BackupSystemCfgFile();
                FixSystemCfgPaths();
            }
        }

        File.WriteAllBytes(outputExePath, fileData);
    }

    private static void PatchLeftArmJump(byte[] fileData)
    {
        int offset = 0xEB48C;
        byte[] expectedBytes = [0xE8, 0x7F];
        byte[] replacementBytes = [0xE9, 0x70];

        PatchBytesAtOffset(fileData, offset, expectedBytes, replacementBytes);
    }

    private static void PatchLeftArmFix(byte[] fileData)
    {
        int offset = 0xEB601;
        byte[] expectedBytes = [0x90, 0x90, 0x90, 0x90, 0x90, 0x90, 0x90, 0x90, 0x90, 0x90, 0x90, 0x90, 0x90, 0x90, 0x90];
        byte[] replacementBytes = [0xE8, 0x0A, 0x00, 0x00, 0x00, 0xBF, 0x02, 0x00, 0x00, 0x00, 0xE9, 0x81, 0xFE, 0xFF, 0xFF];

        PatchBytesAtOffset(fileData, offset, expectedBytes, replacementBytes);
    }

    private static void PatchWeaponSizeCutoff(byte[] fileData)
    {
        int offset = 0xEB1AD;
        byte[] expectedBytes = [0x64];
        byte[] replacementBytes = [0x62];

        PatchBytesAtOffset(fileData, offset, expectedBytes, replacementBytes);
    }

    private static void PatchWeaponSizeClassification(byte[] fileData)
    {
        int offset = 0xEB5B0;
        byte[] expectedBytes = [0x55, 0x8B, 0xEC, 0x8A, 0x45, 0x08, 0x3C, 0x64, 0x72, 0x04, 0x3C, 0x68, 0x76, 0x3A, 0x3C, 0x6E, 
            0x72, 0x04, 0x3C, 0x71, 0x76, 0x32, 0x3C, 0x79, 0x74, 0x2E, 0x3C, 0x7A, 0x74, 0x2A, 0x3C, 0x83, 0x74, 0x26, 0x3C, 0x84, 
            0x74, 0x22, 0x3C, 0x8D, 0x74, 0x1E, 0x3C, 0x8E, 0x74, 0x1A, 0x3C, 0x91, 0x74, 0x16, 0x3C, 0x92, 0x74, 0x12, 0x3C, 0x96, 
            0x74, 0x0E, 0x3C, 0x97, 0x74, 0x0A, 0x3C, 0x9A, 0x74, 0x06, 0x33, 0xC0, 0x5D, 0xC2, 0x04, 0x00, 0xB8, 0x01, 0x00, 0x00, 
            0x00, 0x5D, 0xC2, 0x04, 0x00];
        byte[] replacementBytes = [0x55, 0x8B, 0xEC, 0x8A, 0x45, 0x08, 0x3C, 0x62, 0x72, 0x04, 0x3C, 0x68, 0x76, 0x3A, 0x3C, 0x6B, 
            0x72, 0x04, 0x3C, 0x71, 0x76, 0x32, 0x3C, 0x74, 0x72, 0x04, 0x3C, 0x76, 0x76, 0x2A, 0x3C, 0x7E, 0x74, 0x26, 0x3C, 0x8B, 
            0x72, 0x04, 0x3C, 0x8E, 0x76, 0x1E, 0x3C, 0x91, 0x72, 0x04, 0x3C, 0x92, 0x76, 0x16, 0x3C, 0x96, 0x72, 0x04, 0x3C, 0x97, 
            0x76, 0x0E, 0x3C, 0x9A, 0x74, 0x0A, 0x3C, 0xA0, 0x74, 0x06, 0x33, 0xC0, 0x5D, 0xC2, 0x04, 0x00, 0xB8, 0x01, 0x00, 0x00, 
            0x00, 0x5D, 0xC2, 0x04, 0x00];

        PatchBytesAtOffset(fileData, offset, expectedBytes, replacementBytes);
    }

    private void PatchResolutionHeight(byte[] fileData)
    {
        if (!ushort.TryParse(ResHeight.Text, out ushort numericValue))
        {
            throw new ArgumentOutOfRangeException(nameof(fileData), "Resolution height must be a valid number 0-65535.");
        }
        
        int offset = 0x10E7B;
        byte[] expectedBytes = [0xE0, 0x01];
        byte[] replacementBytes = BitConverter.GetBytes(numericValue); // ushort automatically limits us to 2 bytes

        // Should be little-endian on Windows, but juuust in case
        if (!BitConverter.IsLittleEndian)
        {
            Array.Reverse(replacementBytes);
        }

        PatchBytesAtOffset(fileData, offset, expectedBytes, replacementBytes);
    }

    private void PatchResolutionWidth(byte[] fileData)
    {
        if (!ushort.TryParse(ResWidth.Text, out ushort numericValue))
        {
            throw new ArgumentOutOfRangeException(nameof(fileData), "Resolution width must be a valid number 0-65535.");
        }

        int offset = 0x10E80;
        byte[] expectedBytes = [0x80, 0x02];
        byte[] replacementBytes = BitConverter.GetBytes(numericValue); // ushort automatically limits us to 2 bytes

        // Should be little-endian on Windows, but juuust in case
        if (!BitConverter.IsLittleEndian)
        {
            Array.Reverse(replacementBytes);
        }

        PatchBytesAtOffset(fileData, offset, expectedBytes, replacementBytes);
    }

    private static void PatchCdCheckStartup(byte[] fileData)
    {
        int offset = 0x158DB1;
        byte[] expectedBytes = [0x75, 0x07];
        byte[] replacementBytes = [0x90, 0x90];

        PatchBytesAtOffset(fileData, offset, expectedBytes, replacementBytes);
    }

    private static void PatchCdCheckShortJumps(byte[] fileData)
    {
        List<int> offsets = [0x100AD2, 0x100E82, 0x101232, 0x1015A2, 0x101B42, 0x101EB2, 0x102222, 0x1028C2];
        byte[] expectedBytes = [0x75, 0x59];
        byte[] replacementBytes = [0xEB, 0x59];

        foreach (int offset in offsets)
        {
            PatchBytesAtOffset(fileData, offset, expectedBytes, replacementBytes);
        }
    }

    private static void PatchCdCheckLongJumps(byte[] fileData)
    {
        List<int> offsets = [0x100C54, 0x101004, 0x1013B4, 0x101724, 0x101CC4, 0x102034, 0x1023A4, 0x102A44];
        byte[] expectedBytes = [0x0F, 0x84, 0x72, 0xFE, 0xFF, 0xFF];
        byte[] replacementBytes = [0x90, 0x90, 0x90, 0x90, 0x90, 0x90];

        foreach (int offset in offsets)
        {
            PatchBytesAtOffset(fileData, offset, expectedBytes, replacementBytes);
        }
    }

    private static void PatchBytesAtOffset(byte[] fileData, int offset, byte[] expectedBytes, byte[] replacementBytes)
    {
        if (replacementBytes.Length != expectedBytes.Length)
        {
            throw new InvalidDataException($"Length of replacement bytes ({replacementBytes.Length}) doesn't match expected length ({expectedBytes.Length}).");
        }

        if (offset + expectedBytes.Length > fileData.Length)
        {
            throw new InvalidDataException($"Offset address {offset:X2} is larger than the file ({fileData.Length} bytes).");
        }

        byte[] bytesAtOffset = new byte[expectedBytes.Length];
        Array.Copy(fileData, offset, bytesAtOffset, 0, expectedBytes.Length);

        if (bytesAtOffset.SequenceEqual(replacementBytes))
        {
            return;
        }

        if (bytesAtOffset.SequenceEqual(expectedBytes))
        {
            Array.Copy(replacementBytes, 0, fileData, offset, replacementBytes.Length);
        }
        else
        {
            throw new InvalidDataException($"Bytes at offset {offset:X2} don't match the expected sequence in the MechCommander Gold exe. Was the wrong exe file selected as the input?");
        }
    }

    private void BackupSystemCfgFile()
    {
        if (inputFolder is null || outputFolder is null)
        {
            throw new ArgumentNullException();
        }

        mainSystemCfgPath = Path.Combine(outputFolder, "SYSTEM.CFG");
        backupSystemCfgPath = Path.Combine(outputFolder, "SYSTEM.CFG.backup");

        if (!File.Exists(mainSystemCfgPath))
        {
            throw new FileNotFoundException();
        }

        File.Copy(mainSystemCfgPath, backupSystemCfgPath, true);
    }

    private void FixSystemCfgPaths()
    {
        var paths = new Dictionary<string, string>
        {
            { "CDspritePath", @"data\sprites\" },
            { "CDsoundPath", @"data\sound\" },
            { "CDmoviepath", @"data\movies\" }
        };

        UpdateCfgFile(mainSystemCfgPath, paths);
    }

    // TODO: this one --> works on regex101, but need to test against the actual file: st\s+(\w+)\s*=\s*"([^"]*)"
    [GeneratedRegex(@"^\s*st\s+(\w+)\s+=\s+""([^""]*)")]
    private static partial Regex SystemCfgRegex();

    private static void UpdateCfgFile(string filePath, Dictionary<string, string> updates)
    {
        var lines = File.ReadAllLines(filePath);

        for (int i = 0; i < lines.Length; i++)
        {
            string line = lines[i];

            // Check if line matches pattern: st keyName = "value"
            var match = SystemCfgRegex().Match(line);
            if (match.Success)
            {
                string key = match.Groups[1].Value;

                if (updates.TryGetValue(key, out string? newValue))
                {
                    lines[i] = $@"st {key} = ""{newValue}""";
                }
            }
        }

        File.WriteAllLines(filePath, lines);
    }

    private void Normal_Click(object sender, RoutedEventArgs e)
    {
        if (SoundMusicCheckbox.IsChecked == false)
        {
            ClickSound.Source = new Uri(soundSmall, UriKind.Absolute);
            ClickSound.Play();
        }
    }

    private void BrowseInput_MouseEnter(object sender, MouseEventArgs e)
    {
        SetDescriptionText("This exe is used as the \"base\" for patching and won't be modified as part of the patching process. This file is normally named \"MCX.exe\".");
    }

    private void BrowseInput_MouseLeave(object sender, MouseEventArgs e)
    {
        SetDescriptionText("");
    }

    private void BrowseOutput_MouseEnter(object sender, MouseEventArgs e)
    {
        SetDescriptionText("Optionally change name/location of new patched exe. By default, named \"MCX_patched.exe\" and saved in same location as original exe.");
    }

    private void BrowseOutput_MouseLeave(object sender, MouseEventArgs e)
    {
        SetDescriptionText("");
    }

    private void SoundMusicCheckbox_MouseEnter(object sender, MouseEventArgs e)
    {
        SetDescriptionText("Turn off Mantis' music and sound effects. Only affects this program and has no effect on the patching itself.");
    }

    private void SoundMusicCheckbox_MouseLeave(object sender, MouseEventArgs e)
    {
        SetDescriptionText("");
    }

    private void LeftArmCheckbox_MouseEnter(object sender, MouseEventArgs e)
    {
        SetDescriptionText("Distribute \"large\" weapons across the mech's arms and side torsos as intended, not just to left arm.");
    }

    private void LeftArmCheckbox_MouseLeave(object sender, MouseEventArgs e)
    {
        SetDescriptionText("");
    }

    private void WeaponSizeCheckbox_MouseEnter(object sender, MouseEventArgs e)
    {
        SetDescriptionText("Re-categorize Desperate Measures weapons as \"large\". NOTE: mods that add/modify weapons may cause unexpected results with this fix.");
    }

    private void WeaponSizeCheckbox_MouseLeave(object sender, MouseEventArgs e)
    {
        SetDescriptionText("");
    }

    private void ResCheckbox_MouseEnter(object sender, MouseEventArgs e)
    {
        SetDescriptionText("Width/height during \"combat phase\". Doesn't affect UI/menus. NOTE: very high resolutions may not work on some systems.");
    }

    private void ResCheckbox_MouseLeave(object sender, MouseEventArgs e)
    {
        SetDescriptionText("");
    }

    private void ResWidth_MouseEnter(object sender, MouseEventArgs e)
    {
        SetDescriptionText("Game width. Increasingly high values seem less likely to work. Try modest values if game doesn't run at high values.");
    }

    private void ResWidth_MouseLeave(object sender, MouseEventArgs e)
    {
        SetDescriptionText("");
    }

    private void ResHeight_MouseEnter(object sender, MouseEventArgs e)
    {
        SetDescriptionText("Game height. Increasingly high values seem less likely to work. Try modest values if game doesn't run at high values.");
    }

    private void ResHeight_MouseLeave(object sender, MouseEventArgs e)
    {
        SetDescriptionText("");
    }

    private void NoCdCheckbox_MouseEnter(object sender, MouseEventArgs e)
    {
        SetDescriptionText("AKA \"No CD\". Disables checks for the game CD, allowing play without requiring a physical disc or mounting a disc image.");
    }
     
    private void NoCdCheckbox_MouseLeave(object sender, MouseEventArgs e)
    {
        SetDescriptionText("");
    }

    private void FixCfgPathsCheckbox_MouseEnter(object sender, MouseEventArgs e)
    {
        SetDescriptionText("Ensures saved paths point to local game files. Some saved paths may otherwise point to a previously used disc / disc image.");
    }

    private void FixCfgPathsCheckbox_MouseLeave(object sender, MouseEventArgs e)
    {
        SetDescriptionText("");
    }

    private void PatchButton_MouseEnter(object sender, MouseEventArgs e)
    {
        SetDescriptionText("Apply the selected settings and save the patched game exe to the selected location.");
    }

    private void PatchButton_MouseLeave(object sender, MouseEventArgs e)
    {
        SetDescriptionText("");
    }

    private void SetDescriptionText(string text)
    {
        DescriptionLabel.Content = new TextBlock
        {
            Text = text,
            TextWrapping = TextWrapping.Wrap
        };
    }
}