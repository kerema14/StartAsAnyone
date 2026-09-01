# Start As Anyone

Mount & Blade II: Bannerlord mod (C# + Harmony). Adds a character creation stage letting the
player take over any living lord instead of creating a new character.

## Seeing the UI

The UI is Gauntlet: XML prefabs in `_Module/GUI/Prefabs/` bound to view models in
`ViewModels/SAA/`. You cannot judge a prefab change by reading it — layout, brushes, clipping and
portraits only show up rendered. Run the capture harness:

```powershell
powershell -File tools/saa-capture.ps1
```

It builds, launches the game, starts a sandbox campaign by itself, walks all three screens,
writes one PNG per step into `.saa-shots/` (gitignored), then quits. About 4 minutes, most of it
campaign generation. Read the PNGs to see exactly what a player sees.

Flags: `-SkipBuild`, `-KeepOpen` (leave the game up to drive by hand), `-MaxWidth` (shot
downscale, default 1600), `-TimeoutSeconds`.

The harness lives in `Capture/` and is compiled **only in Debug**, behind the `SAA_CAPTURE`
symbol; Release drops `Capture/**` entirely because it must never ship to players. If you add a
call into it, guard the call site with `#if SAA_CAPTURE`.

To capture a different state (a filtered list, a minor faction, an empty result), edit the step
list in `BuildScript` in `Capture/CaptureDirector.cs`.

## Launch gotchas

Each of these looks identical to the game simply hanging:

- **BLSE is broken on this install.** `Bannerlord.BLSE.Standalone.exe` throws
  `MissingMethodException: LauncherPlatform.Initialize()` and dies before loading any module. Use
  `Bannerlord.exe`; Harmony loads fine as a plain module. The "BLSE Bannerlord" entries in
  `.vscode/launch.json` fail the same way.
- **Steam must be running**, or the game stops on a modal "Unable to initialize Steam API".
- **A forced kill arms a modal "Safe Mode" prompt on the _next_ launch**, which silently blocks
  all module loading. Answer "No", and shut down with `CloseMainWindow` rather than killing. The
  harness does both.
- **`LauncherData.xml` is read by the launcher, not the game.** Module selection has to come from
  the `_MODULES_` argument, with working directory `bin\Win64_Shipping_Client`.

`TaleWorlds.Engine.Utilities.TakeScreenshot(path)` writes an uncompressed BMP whatever extension
it is given; the runner converts to PNG afterwards.

## Conventions

- Prefab bindings are already covered by a GauntletUI LSP the author runs. Do not build or
  propose linters for `@Prop` / `{DataSource}` / `Command.X` resolution.
- Much of the older `*VM.cs` is adapted from decompiled Bannerlord assemblies. Check the XML
  prefab binding before calling something a bug.
- The csproj globs sources, so moving files between folders needs no csproj edit.
- Never add `Co-Authored-By` or any Claude Code attribution to commits.
