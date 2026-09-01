using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using TaleWorlds.MountAndBlade;

namespace StartAsAnyone
{
    /// <summary>
    /// Drives a headless screenshot run: start a campaign from the main menu, walk the three
    /// Start As Anyone screens once the stage opens, shoot each one, then quit. Every step is
    /// timed off the wall clock rather than frame counts because campaign generation can drop
    /// the frame rate to almost nothing.
    /// </summary>
    internal static class CaptureDirector
    {
        private enum Phase
        {
            Disabled,
            Idle,
            WaitingForMainMenu,
            WaitingForStage,
            Running,
            Finished
        }

        private sealed class Step
        {
            public Step(string label, Action? action)
            {
                Label = label;
                Action = action;
            }

            public string Label { get; }

            /// <summary>Ran before the shot; null means "shoot the screen as it already is".</summary>
            public Action? Action { get; }
        }

        private static readonly Stopwatch Timer = new Stopwatch();

        private static Phase _phase = Phase.Disabled;
        private static List<Step> _script = new List<Step>();
        private static TimeSpan _delay;
        private static int _index;
        private static string? _pendingShot;
        private static int _shotsTaken;
        private static string _logPath = string.Empty;

        public static void Initialize()
        {
            if (!CaptureConfig.IsEnabled)
            {
                // Explain the refusal only when a capture was actually requested: a run that
                // never arms itself is otherwise indistinguishable from a game that never
                // started. A plain player launch has no settings file and writes nothing.
                if (CaptureConfig.SettingsFileFound)
                {
                    WriteBreadcrumb();
                }
                return;
            }

            try
            {
                Directory.CreateDirectory(CaptureConfig.OutputDirectory);
                _logPath = Path.Combine(CaptureConfig.OutputDirectory, "capture.log");
                File.WriteAllText(_logPath, string.Empty);
            }
            catch (Exception)
            {
                // If we cannot even open the log there is no point running; stay disabled so a
                // misconfigured capture never disturbs the game itself.
                _logPath = string.Empty;
                return;
            }

            _phase = Phase.Idle;
            Log("capture mode enabled, output=" + CaptureConfig.OutputDirectory);
        }

        public static void OnMainMenuReady()
        {
            if (_phase != Phase.Idle)
            {
                return;
            }

            Log("main menu reached");
            _phase = Phase.WaitingForMainMenu;
            StartDelay(CaptureConfig.MainMenuDelay);
        }

        public static void OnStageReady(StartAsAnyoneRootVM root)
        {
            if (_phase != Phase.WaitingForStage)
            {
                return;
            }

            Log("stage view ready after " + Timer.Elapsed.TotalSeconds.ToString("F1", CultureInfo.InvariantCulture) + "s");
            BuildScript(root);
            _index = 0;
            _pendingShot = null;
            _phase = Phase.Running;
            StartDelay(CaptureConfig.SettleDelay);
        }

        public static void OnStageClosed()
        {
            if (_phase == Phase.Running)
            {
                Finish(false, "stage closed before the script finished");
            }
        }

        public static void Tick()
        {
            switch (_phase)
            {
                case Phase.WaitingForMainMenu:
                    TickWaitingForMainMenu();
                    break;
                case Phase.WaitingForStage:
                    TickWaitingForStage();
                    break;
                case Phase.Running:
                    TickRunning();
                    break;
            }
        }

        // -- phases ----------------------------------------------------------

        private static void TickWaitingForMainMenu()
        {
            if (Timer.Elapsed < _delay)
            {
                return;
            }

            string id = ResolveStateOptionId();
            if (id.Length == 0)
            {
                Finish(false, "no initial state option matched '" + CaptureConfig.StateOptionId + "'");
                return;
            }

            Log("starting a new campaign via initial state option '" + id + "'");
            try
            {
                Module.CurrentModule.ExecuteInitialStateOptionWithId(id);
            }
            catch (Exception e)
            {
                Finish(false, "ExecuteInitialStateOptionWithId threw: " + e.Message);
                return;
            }

            _phase = Phase.WaitingForStage;
            StartDelay(TimeSpan.Zero);
        }

        private static void TickWaitingForStage()
        {
            if (Timer.Elapsed > CaptureConfig.StageTimeout)
            {
                Finish(false, "timed out waiting for the Start As Anyone stage to open");
            }
        }

        private static void TickRunning()
        {
            if (Timer.Elapsed < _delay)
            {
                return;
            }

            // A shot is taken one turn after its action so the layout has settled first.
            if (_pendingShot != null)
            {
                Shoot(_pendingShot);
                _pendingShot = null;
                StartDelay(CaptureConfig.PostShotDelay);
                return;
            }

            if (_index >= _script.Count)
            {
                Finish(true, "walked every screen");
                return;
            }

            // Whatever the mouse happens to be resting on picks up a hover highlight and hint
            // popup, which makes shots differ run to run. Park it clear of every widget.
            ParkCursor();

            Step step = _script[_index++];
            if (step.Action != null)
            {
                try
                {
                    step.Action();
                }
                catch (Exception e)
                {
                    // Keep going: a broken step still leaves the remaining screens worth seeing.
                    Log("step '" + step.Label + "' action threw: " + e.Message);
                }
            }

            _pendingShot = step.Label;
            StartDelay(CaptureConfig.SettleDelay);
        }

        // -- script ----------------------------------------------------------

        private static void BuildScript(StartAsAnyoneRootVM root)
        {
            _script = new List<Step>
            {
                new Step("01-path", null),
                new Step("02-path-existing-hero", () => root.Path.ExistingHeroCard?.ExecuteSelect()),
                new Step("03-factions", root.ExecuteForward),
                new Step("04-faction-selected", () => SelectFirstFaction(root)),
                new Step("05-minor-factions", root.Factions.ExecuteSelectMinorFactionsTab),
                new Step("06-kingdoms-restored", () =>
                {
                    root.Factions.ExecuteSelectKingdomsTab();
                    SelectFirstFaction(root);
                }),
                new Step("07-characters", root.ExecuteForward),
                new Step("08-character-selected", () => root.Characters.Rows.FirstOrDefault()?.ExecuteSelect()),
                // Prefer a woman: the first row is a male ruler, so "did the tableau
                // actually repoint at this hero" is then answerable from the shot alone.
                // Reading the name off the panel is not enough — the panel rebinding and
                // the tableau rebinding are separate things, and they came apart before.
                new Step("09-other-character", () => SelectContrastingHero(root)),
                // No action: reshoots the previous state a settle-delay later, which
                // separates "the tableau never rebound" from "the tableau is still
                // building a character it has not rendered before".
                new Step("09b-other-character-settled", null),
                new Step("10-filtered", () =>
                {
                    // The toggle widget owns IsSelected and flips it before the command
                    // runs, so a scripted click has to set it the same way.
                    SAAFilterItemVM? filter = root.Characters.FilterGroups
                        .FirstOrDefault()?.Filters.FirstOrDefault();
                    if (filter != null)
                    {
                        filter.IsSelected = true;
                        filter.ExecuteOnFilterActivated();
                    }
                })
            };
        }

        private static void SelectFirstFaction(StartAsAnyoneRootVM root)
        {
            root.Factions.VisibleFactions.FirstOrDefault()?.ExecuteSelect();
        }

        private static void SelectContrastingHero(StartAsAnyoneRootVM root)
        {
            HeroRowVM? row = root.Characters.Rows.FirstOrDefault(r => r.Hero != null && r.Hero.IsFemale)
                             ?? root.Characters.Rows.Skip(9).FirstOrDefault();
            row?.ExecuteSelect();
        }

        // -- helpers ---------------------------------------------------------

        private static string ResolveStateOptionId()
        {
            Module? module = Module.CurrentModule;
            if (module == null)
            {
                return string.Empty;
            }

            List<string> ids;
            try
            {
                ids = module.GetInitialStateOptions().Select(option => option.Id).ToList();
            }
            catch (Exception e)
            {
                Log("could not read the initial state options: " + e.Message);
                return string.Empty;
            }

            Log("initial state options: " + string.Join(", ", ids));

            string configured = CaptureConfig.StateOptionId;
            if (ids.Any(id => string.Equals(id, configured, StringComparison.Ordinal)))
            {
                return configured;
            }

            // Fall back to whatever this mod list calls the sandbox entry rather than giving up.
            return ids.FirstOrDefault(id => id.IndexOf("sandbox", StringComparison.OrdinalIgnoreCase) >= 0)
                   ?? string.Empty;
        }

        private static void Shoot(string label)
        {
            // TakeScreenshot writes an uncompressed bitmap whatever the extension says, so
            // name it honestly; tools/saa-capture.ps1 converts to PNG afterwards.
            string path = Path.Combine(CaptureConfig.OutputDirectory, label + ".bmp");
            try
            {
                TaleWorlds.Engine.Utilities.TakeScreenshot(path);
                _shotsTaken++;
                Log("shot " + label);
            }
            catch (Exception e)
            {
                Log("shot " + label + " FAILED: " + e.Message);
            }
        }

        private static void Finish(bool succeeded, string reason)
        {
            _phase = Phase.Finished;
            Log("STATUS: " + (succeeded ? "OK" : "FAILED") + " - " + reason + " (shots=" + _shotsTaken + ")");

            if (CaptureConfig.KeepOpen)
            {
                Log("SAA_CAPTURE_KEEP_OPEN is set; leaving the game running");
                return;
            }

            try
            {
                TaleWorlds.Engine.Utilities.QuitGame();
            }
            catch (Exception e)
            {
                Log("QuitGame threw: " + e.Message);
            }
        }

        private static void StartDelay(TimeSpan delay)
        {
            _delay = delay;
            Timer.Restart();
        }

        [DllImport("user32.dll")]
        private static extern bool SetCursorPos(int x, int y);

        private static void ParkCursor()
        {
            try
            {
                // Top-left is header backdrop on all three screens: nothing hoverable there.
                SetCursorPos(4, 4);
            }
            catch (Exception)
            {
                // Cosmetic only; a stuck cursor is not worth failing a run over.
            }
        }

        private static void WriteBreadcrumb()
        {
            if (CaptureConfig.ModuleDirectory.Length == 0)
            {
                return;
            }

            try
            {
                File.WriteAllText(
                    Path.Combine(CaptureConfig.ModuleDirectory, "saa-capture-last.log"),
                    DateTime.Now.ToString("s", CultureInfo.InvariantCulture) + "  " + CaptureConfig.Summary + Environment.NewLine);
            }
            catch (Exception)
            {
                // Best effort only.
            }
        }

        private static void Log(string message)
        {
            if (_logPath.Length == 0)
            {
                return;
            }

            try
            {
                File.AppendAllText(
                    _logPath,
                    DateTime.Now.ToString("HH:mm:ss.fff", CultureInfo.InvariantCulture) + "  " + message + Environment.NewLine);
            }
            catch (Exception)
            {
                // Losing a log line must never take the run down.
            }
        }
    }
}
