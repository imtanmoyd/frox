using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;

namespace FROX.Config;

/// <summary>
/// Persists user settings to %LocalAppData%\FROX\settings.json.
/// The API key is encrypted with the Windows DPAPI (current-user scope),
/// so it is never stored as plain text on disk.
/// </summary>
public sealed class SecureSettingsStore
{
    private readonly string _settingsPath;

    public SecureSettingsStore(string? settingsPath = null)
    {
        var directory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "FROX");
        Directory.CreateDirectory(directory);
        _settingsPath = settingsPath ?? Path.Combine(directory, "settings.json");
    }

    public string SettingsPath => _settingsPath;

    public void Save(AppSettings settings)
    {
        var payload = new Dictionary<string, string?>
        {
            ["characterId"] = settings.CharacterId,
            ["username"] = settings.Username,
            ["displayName"] = settings.DisplayName,
            ["provider"] = settings.Provider,
            ["mode"] = settings.Mode,
            ["model"] = settings.OpenRouterModel,
            ["apiKey"] = string.IsNullOrWhiteSpace(settings.ApiKey) ? null : Protect(settings.ApiKey!),
        };

        try
        {
            File.WriteAllText(_settingsPath, JsonSerializer.Serialize(payload, new JsonSerializerOptions { WriteIndented = true }));
        }
        catch
        {
            // Non-critical — settings simply won't persist this session.
        }
    }

    public void Load(AppSettings settings)
    {
        if (!File.Exists(_settingsPath))
        {
            return;
        }

        try
        {
            using var document = JsonDocument.Parse(File.ReadAllText(_settingsPath));
            var root = document.RootElement;

            settings.CharacterId = GetString(root, "characterId", settings.CharacterId);
            settings.Username = GetString(root, "username", settings.Username);
            settings.DisplayName = GetString(root, "displayName", settings.DisplayName);
            settings.Provider = GetString(root, "provider", settings.Provider);
            settings.Mode = GetString(root, "mode", settings.Mode);
settings.OpenRouterModel = GetString(root, "model", settings.OpenRouterModel);

            var protectedKey = GetString(root, "apiKey", null);
            if (!string.IsNullOrWhiteSpace(protectedKey))
            {
                settings.ApiKey = Unprotect(protectedKey!);
            }
        }
        catch
        {
            // Corrupt settings are ignored; defaults are used instead.
        }
    }

    private static string GetString(JsonElement root, string name, string? fallback)
    {
        if (root.TryGetProperty(name, out var property) && property.ValueKind == JsonValueKind.String)
        {
            return property.GetString() ?? fallback ?? string.Empty;
        }

        return fallback ?? string.Empty;
    }

    // ------------------------------------------------------------------
    // DPAPI helpers (crypt32.dll) — no external NuGet dependency needed.
    // ------------------------------------------------------------------

    private const uint CRYPTPROTECT_UI_FORBIDDEN = 0x1;

    public static string Protect(string plainText)
    {
        var bytes = Encoding.UTF8.GetBytes(plainText);
        var blobIn = new DATA_BLOB { cbData = bytes.Length, pbData = Marshal.AllocHGlobal(bytes.Length) };
        try
        {
            Marshal.Copy(bytes, 0, blobIn.pbData, bytes.Length);

            if (!CryptProtectData(ref blobIn, "FROX settings", IntPtr.Zero, IntPtr.Zero, IntPtr.Zero,
                    CRYPTPROTECT_UI_FORBIDDEN, out var blobOut))
            {
                throw new InvalidOperationException($"DPAPI encryption failed (Win32 0x{Marshal.GetLastWin32Error():X}).");
            }

            try
            {
                var encrypted = new byte[blobOut.cbData];
                Marshal.Copy(blobOut.pbData, encrypted, 0, blobOut.cbData);
                return Convert.ToBase64String(encrypted);
            }
            finally
            {
                LocalFree(blobOut.pbData);
            }
        }
        finally
        {
            Marshal.FreeHGlobal(blobIn.pbData);
        }
    }

    public static string Unprotect(string protectedText)
    {
        var encrypted = Convert.FromBase64String(protectedText);
        var blobIn = new DATA_BLOB { cbData = encrypted.Length, pbData = Marshal.AllocHGlobal(encrypted.Length) };
        try
        {
            Marshal.Copy(encrypted, 0, blobIn.pbData, encrypted.Length);

            if (!CryptUnprotectData(ref blobIn, out _, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero,
                    CRYPTPROTECT_UI_FORBIDDEN, out var blobOut))
            {
                throw new InvalidOperationException($"DPAPI decryption failed (Win32 0x{Marshal.GetLastWin32Error():X}).");
            }

            try
            {
                var decrypted = new byte[blobOut.cbData];
                Marshal.Copy(blobOut.pbData, decrypted, 0, blobOut.cbData);
                return Encoding.UTF8.GetString(decrypted);
            }
            finally
            {
                LocalFree(blobOut.pbData);
            }
        }
        finally
        {
            Marshal.FreeHGlobal(blobIn.pbData);
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    // ReSharper disable once InconsistentNaming
    private struct DATA_BLOB
    {
        public int cbData;
        public IntPtr pbData;
    }

    [DllImport("crypt32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern bool CryptProtectData(ref DATA_BLOB pDataIn, string? szDataDescr,
        IntPtr pOptionalEntropy, IntPtr pvReserved, IntPtr pPromptStruct, uint dwFlags, out DATA_BLOB pDataOut);

    [DllImport("crypt32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern bool CryptUnprotectData(ref DATA_BLOB pDataIn, out string? ppszDataDescr,
        IntPtr pOptionalEntropy, IntPtr pvReserved, IntPtr pPromptStruct, uint dwFlags, out DATA_BLOB pDataOut);

    [DllImport("kernel32.dll")]
    private static extern IntPtr LocalFree(IntPtr hMem);
}
