using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;

namespace StartAsAnyone
{
    /// <summary>
    /// Capture mode is opt-in and read from "saa-capture.txt" next to the module's
    /// SubModule.xml, with environment variables as an override for hand runs. A file beats
    /// environment variables here because the launcher may hand the game off to another
    /// process, and a child started that way does not reliably inherit our environment.
    ///
    /// The file carries an expiry so an interrupted run can never leave capture mode armed
    /// for the next ordinary launch.
    /// </summary>
    internal static class CaptureConfig
    {
        private const string SettingsFileName = "saa-capture.txt";

        static CaptureConfig()
        {
            Dictionary<string, string> settings = LoadSettingsFile();

            OutputDirectory = Read(settings, "out", "SAA_CAPTURE_OUT") ?? string.Empty;
            StateOptionId = Read(settings, "state_option", "SAA_CAPTURE_STATE_OPTION") ?? "Sandbox";
            KeepOpen = Read(settings, "keep_open", "SAA_CAPTURE_KEEP_OPEN") == "1";
            SettleDelay = TimeSpan.FromMilliseconds(ReadInt(settings, "settle_ms", "SAA_CAPTURE_SETTLE_MS", 1200));

            // Without an output directory there is nothing to capture, so treat that as "off"
            // rather than discovering it half way through a run.
            IsEnabled = Read(settings, "enabled", "SAA_CAPTURE") == "1"
                        && OutputDirectory.Length > 0
                        && !HasExpired(settings);

            Summary = "enabled=" + IsEnabled
                      + " module='" + ModuleDirectory + "'"
                      + " settings=" + (settings.Count > 0 ? "read" : "absent")
                      + " out='" + OutputDirectory + "'"
                      + " expired=" + HasExpired(settings);
        }

        /// <summary>One-line description of how capture mode resolved, for the breadcrumb log.</summary>
        public static string Summary { get; } = string.Empty;

        public static bool IsEnabled { get; }

        public static string OutputDirectory { get; }

        /// <summary>Id of the main menu option that starts a new campaign.</summary>
        public static string StateOptionId { get; }

        /// <summary>Leave the game running after the last shot instead of quitting.</summary>
        public static bool KeepOpen { get; }

        /// <summary>How long to let the UI settle after an interaction before shooting.</summary>
        public static TimeSpan SettleDelay { get; }

        public static TimeSpan PostShotDelay => TimeSpan.FromMilliseconds(400);

        /// <summary>The main menu needs a moment before it will accept a state option.</summary>
        public static TimeSpan MainMenuDelay => TimeSpan.FromMilliseconds(4000);

        /// <summary>Campaign generation is slow and varies wildly with machine and mod list.</summary>
        public static TimeSpan StageTimeout => TimeSpan.FromMinutes(6);

        /// <summary>
        /// Where the settings file and, by default, the log live. Walks up from the assembly
        /// until it finds the folder holding SubModule.xml so it does not care how deep the
        /// binaries are nested for a given platform.
        /// </summary>
        public static string ModuleDirectory { get; private set; } = string.Empty;

        /// <summary>
        /// Whether a settings file was present at all. Gates every write this class's caller
        /// makes, so an ordinary player launch touches nothing on disk.
        /// </summary>
        public static bool SettingsFileFound { get; private set; }

        private static Dictionary<string, string> LoadSettingsFile()
        {
            var settings = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            try
            {
                ModuleDirectory = FindModuleDirectory();
                if (ModuleDirectory.Length == 0)
                {
                    return settings;
                }

                string path = Path.Combine(ModuleDirectory, SettingsFileName);
                if (!File.Exists(path))
                {
                    return settings;
                }

                // Only from here on has somebody deliberately asked for a capture run. Player
                // installs never reach this point, and so never get written to.
                SettingsFileFound = true;

                foreach (string line in File.ReadAllLines(path))
                {
                    string trimmed = line.Trim();
                    if (trimmed.Length == 0 || trimmed[0] == '#')
                    {
                        continue;
                    }

                    int split = trimmed.IndexOf('=');
                    if (split > 0)
                    {
                        settings[trimmed.Substring(0, split).Trim()] = trimmed.Substring(split + 1).Trim();
                    }
                }
            }
            catch (Exception)
            {
                // An unreadable settings file means "not capturing", never a broken game.
            }

            return settings;
        }

        private static string FindModuleDirectory()
        {
            string? dir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            for (int depth = 0; depth < 6 && !string.IsNullOrEmpty(dir); depth++)
            {
                if (File.Exists(Path.Combine(dir!, "SubModule.xml")))
                {
                    return dir!;
                }
                dir = Path.GetDirectoryName(dir!);
            }
            return string.Empty;
        }

        private static bool HasExpired(Dictionary<string, string> settings)
        {
            if (!settings.TryGetValue("expires_utc", out string? raw) || string.IsNullOrWhiteSpace(raw))
            {
                // No expiry recorded: an environment-driven hand run, which is deliberate.
                return false;
            }

            if (!DateTime.TryParse(raw, CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal, out DateTime expiry))
            {
                return true;
            }

            return DateTime.UtcNow > expiry;
        }

        private static string? Read(Dictionary<string, string> settings, string key, string variable)
        {
            try
            {
                string? value = Environment.GetEnvironmentVariable(variable);
                if (!string.IsNullOrWhiteSpace(value))
                {
                    return value!.Trim();
                }
            }
            catch (Exception)
            {
                // Fall through to the file.
            }

            if (settings.TryGetValue(key, out string? fromFile) && !string.IsNullOrWhiteSpace(fromFile))
            {
                return fromFile.Trim();
            }

            return null;
        }

        private static int ReadInt(Dictionary<string, string> settings, string key, string variable, int fallback)
        {
            string? raw = Read(settings, key, variable);
            if (raw != null && int.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out int value) && value >= 0)
            {
                return value;
            }
            return fallback;
        }
    }
}
