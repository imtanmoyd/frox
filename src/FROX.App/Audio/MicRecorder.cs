using System.IO;
using System.Runtime.InteropServices;

namespace FROX.App.Audio;

/// <summary>
/// Minimal wave-audio recorder built on the Windows MCI layer (winmm.dll).
/// No external NuGet package is required. Recordings are saved as .wav files
/// under %TEMP%\FROX and handed back to the UI for attaching to a message.
/// </summary>
public sealed class MicRecorder : IDisposable
{
    private const string Alias = "froxmic";
    private string? _outputPath;

    public bool IsRecording { get; private set; }

    /// <summary>Fires whenever <see cref="IsRecording"/> changes.</summary>
    public event EventHandler<bool>? RecordingChanged;

    public string? LastFilePath { get; private set; }

    /// <summary>Starts capturing from the default wave-in device.</summary>
    /// <returns>True if recording started, false if no usable audio input.</returns>
    public bool Start()
    {
        if (IsRecording)
        {
            return true;
        }

        // Reset any stale alias from a previous session.
        Send($"close {Alias}");

        var directory = Path.Combine(Path.GetTempPath(), "FROX");
        Directory.CreateDirectory(directory);
        _outputPath = Path.Combine(directory, $"voice-{DateTime.Now:yyyyMMdd-HHmmss}.wav");

        if (Send($"open new Type waveaudio alias {Alias}") != 0)
        {
            return false;
        }

        if (Send($"record {Alias}") != 0)
        {
            Send($"close {Alias}");
            return false;
        }

        IsRecording = true;
        LastFilePath = null;
        RecordingChanged?.Invoke(this, true);
        return true;
    }

    /// <summary>Stops recording (if active) and returns the .wav path, or null.</summary>
    public string? Stop()
    {
        if (!IsRecording)
        {
            return null;
        }

        IsRecording = false;

        Send($"stop {Alias}");
        if (!string.IsNullOrEmpty(_outputPath))
        {
            Send($"save {Alias} \"{_outputPath}\"");
        }

        Send($"close {Alias}");

        var file = _outputPath;
        if (file is not null && File.Exists(file) && new FileInfo(file).Length > 44)
        {
            LastFilePath = file;
        }
        else
        {
            file = null;
        }

        _outputPath = null;
        RecordingChanged?.Invoke(this, false);
        return file;
    }

    public void Cancel()
    {
        if (!IsRecording)
        {
            return;
        }

        IsRecording = false;
        Send($"stop {Alias}");
        Send($"close {Alias}");
        if (_outputPath is not null)
        {
            try
            {
                if (File.Exists(_outputPath))
                {
                    File.Delete(_outputPath);
                }
            }
            catch
            {
                // Best-effort cleanup.
            }
        }

        _outputPath = null;
        RecordingChanged?.Invoke(this, false);
    }

    private static int Send(string command)
    {
        return mciSendString(command, null, 0, IntPtr.Zero);
    }

    public void Dispose()
    {
        Cancel();
    }

    [DllImport("winmm.dll", CharSet = CharSet.Unicode)]
    private static extern int mciSendString(string command, string? returnString, int returnLength, IntPtr hwndCallback);
}