<#
.SYNOPSIS
    Builds the mod, launches Bannerlord in capture mode and collects screenshots of every
    Start As Anyone screen into .saa-shots/.

.DESCRIPTION
    Capture mode is armed by writing saa-capture.txt into the deployed module folder, which
    CaptureDirector reads on load. The game then starts a sandbox campaign by itself, walks
    the three screens, writes a PNG per step and quits.

    The settings file carries an expiry and is deleted on the way out, so an interrupted run
    cannot leave capture mode armed for the next ordinary launch.

.EXAMPLE
    powershell -File tools/saa-capture.ps1
    powershell -File tools/saa-capture.ps1 -SkipBuild -KeepOpen
#>
[CmdletBinding()]
param(
    # Where the PNGs land. Defaults to .saa-shots/ at the repository root.
    [string] $OutputDirectory,

    # Hard ceiling for the whole run, including campaign generation.
    [int] $TimeoutSeconds = 900,

    # How long the UI is given to settle after each interaction before the shot.
    [int] $SettleMs = 1200,

    # Reuse whatever is already deployed to the game folder.
    [switch] $SkipBuild,

    # Leave the game running after the last shot so the screens can be poked at by hand.
    [switch] $KeepOpen,

    # Shots are downscaled to this width. Full 1080p is more detail than judging layout needs.
    [int] $MaxWidth = 1600
)

$ErrorActionPreference = 'Stop'

# Win32 helper for dismissing the engine's native dialogs. After any unclean shutdown the
# game opens a modal "Safe Mode" prompt that blocks module loading entirely, and a run that
# hits it looks identical to a hang from the outside.
try {
    Add-Type @'
using System;
using System.Text;
using System.Runtime.InteropServices;
public static class SaaDialog {
  delegate bool EnumProc(IntPtr h, IntPtr p);
  [DllImport("user32.dll")] static extern bool EnumWindows(EnumProc cb, IntPtr p);
  [DllImport("user32.dll")] static extern bool EnumChildWindows(IntPtr h, EnumProc cb, IntPtr p);
  [DllImport("user32.dll", CharSet=CharSet.Unicode)] static extern int GetWindowTextW(IntPtr h, StringBuilder s, int n);
  [DllImport("user32.dll")] static extern IntPtr SendMessage(IntPtr h, uint msg, IntPtr w, IntPtr l);
  [DllImport("user32.dll")] static extern bool IsWindowVisible(IntPtr h);
  const uint BM_CLICK = 0x00F5;
  static string Text(IntPtr h) { var sb = new StringBuilder(512); GetWindowTextW(h, sb, 512); return sb.ToString(); }
  public static bool Click(string title, string button) {
    IntPtr top = IntPtr.Zero;
    EnumWindows((h, p) => { if (IsWindowVisible(h) && Text(h) == title) { top = h; return false; } return true; }, IntPtr.Zero);
    if (top == IntPtr.Zero) return false;
    IntPtr btn = IntPtr.Zero;
    EnumChildWindows(top, (h, p) => { if (Text(h).Replace("&", "") == button) { btn = h; return false; } return true; }, IntPtr.Zero);
    if (btn == IntPtr.Zero) return false;
    SendMessage(btn, BM_CLICK, IntPtr.Zero, IntPtr.Zero);
    return true;
  }
}
'@
} catch { }

$repo = Split-Path -Parent $PSScriptRoot
if (-not $OutputDirectory) { $OutputDirectory = Join-Path $repo '.saa-shots' }

$game = $env:BANNERLORD_GAME_DIR
if (-not $game) { throw 'BANNERLORD_GAME_DIR is not set.' }
if (-not (Test-Path $game)) { throw "BANNERLORD_GAME_DIR does not point at a real folder: $game" }

$binDir = Join-Path $game 'bin\Win64_Shipping_Client'

# Bannerlord.exe rather than Bannerlord.BLSE.Standalone.exe: the installed BLSE build throws
# MissingMethodException on LauncherPlatform.Initialize() against this game version and dies
# before loading any module. Harmony loads fine as a plain module without BLSE.
$exe = Join-Path $binDir 'Bannerlord.exe'
if (-not (Test-Path $exe)) { throw "Bannerlord.exe not found under $binDir" }

$moduleDir = Join-Path $game 'Modules\StartAsAnyone'
if (-not (Test-Path $moduleDir)) { throw "StartAsAnyone is not deployed to $moduleDir - build first." }
$settingsFile = Join-Path $moduleDir 'saa-capture.txt'
$breadcrumb = Join-Path $moduleDir 'saa-capture-last.log'

# --- build -----------------------------------------------------------------

if (-not $SkipBuild) {
    Write-Host '==> building' -ForegroundColor Cyan
    & dotnet build (Join-Path $repo 'Start As Anyone.csproj') -c Debug -f net472 -v minimal -nologo
    if ($LASTEXITCODE -ne 0) { throw "build failed (exit $LASTEXITCODE)" }
}

# --- prepare output --------------------------------------------------------

if (Test-Path $OutputDirectory) {
    Remove-Item (Join-Path $OutputDirectory '*') -Recurse -Force -ErrorAction SilentlyContinue
}
New-Item -ItemType Directory -Force -Path $OutputDirectory | Out-Null
$OutputDirectory = (Resolve-Path $OutputDirectory).Path
$log = Join-Path $OutputDirectory 'capture.log'
Remove-Item $breadcrumb -Force -ErrorAction SilentlyContinue

# --- arm capture mode ------------------------------------------------------

# Expire a little after our own timeout so a killed run disarms itself regardless.
$expiry = (Get-Date).ToUniversalTime().AddSeconds($TimeoutSeconds + 600).ToString('yyyy-MM-ddTHH:mm:ssZ')
if ($KeepOpen) { $keep = '1' } else { $keep = '0' }
$lines = @(
    '# Written by tools/saa-capture.ps1; deleted when the run ends.',
    'enabled=1',
    "out=$OutputDirectory",
    "settle_ms=$SettleMs",
    "keep_open=$keep",
    'state_option=Sandbox',
    "expires_utc=$expiry"
)
# WriteAllLines gives UTF-8 with no BOM; a BOM would corrupt the first key.
[IO.File]::WriteAllLines($settingsFile, $lines)

try {
    # --- Steam ---------------------------------------------------------------

    # Without a running Steam client the game stops on a modal "Unable to initialize Steam
    # API" dialog, which looks exactly like a hang from the outside.
    if (-not (Get-Process -Name 'steam' -ErrorAction SilentlyContinue)) {
        $steamExe = (Get-ItemProperty 'HKCU:\Software\Valve\Steam' -ErrorAction SilentlyContinue).SteamExe
        if ($steamExe) { $steamExe = $steamExe -replace '/', '\' }
        if (-not $steamExe -or -not (Test-Path $steamExe)) {
            throw 'Steam is not running and steam.exe could not be found in the registry.'
        }

        Write-Host '==> starting Steam' -ForegroundColor Cyan
        Start-Process -FilePath $steamExe -ArgumentList '-silent' | Out-Null

        $steamDeadline = (Get-Date).AddSeconds(180)
        while ((Get-Date) -lt $steamDeadline) {
            if (Get-Process -Name 'steamwebhelper' -ErrorAction SilentlyContinue) { break }
            Start-Sleep -Seconds 3
        }
        if (-not (Get-Process -Name 'steamwebhelper' -ErrorAction SilentlyContinue)) {
            throw 'Steam did not finish starting within 180s (is a login prompt waiting?).'
        }
        # The client accepts API connections a little after the helper appears.
        Start-Sleep -Seconds 15
    }

    # --- launch ------------------------------------------------------------

    # Module order and the working directory both matter: this mirrors the profile in
    # Properties/launchSettings.json, which is the combination known to load the mod.
    $modules = '_MODULES_*Bannerlord.Harmony*Native*SandBoxCore*CustomBattle*Sandbox*StoryMode*StartAsAnyone*_MODULES_'

    Write-Host "==> launching $(Split-Path $exe -Leaf)" -ForegroundColor Cyan
    $launchedAt = Get-Date
    $proc = Start-Process -FilePath $exe -ArgumentList '/singleplayer', $modules -WorkingDirectory $binDir -PassThru

    # --- wait for the run to report itself done ----------------------------

    $deadline = $launchedAt.AddSeconds($TimeoutSeconds)
    $status = $null
    $safeModeSeen = $false
    while ((Get-Date) -lt $deadline) {
        # Answering "No" keeps mods enabled; left alone this dialog blocks the run forever.
        if (-not $safeModeSeen) {
            if ([SaaDialog]::Click('Safe Mode', 'No')) {
                Write-Host '==> dismissed the Safe Mode prompt (previous run shut down uncleanly)' -ForegroundColor Yellow
                $safeModeSeen = $true
            }
        }

        if (Test-Path $log) {
            $hit = Select-String -Path $log -Pattern 'STATUS:' -ErrorAction SilentlyContinue | Select-Object -Last 1
            if ($hit) { $status = $hit.Line; break }
        }

        $alive = Get-Process -Name 'Bannerlord*' -ErrorAction SilentlyContinue |
            Where-Object { $_.StartTime -ge $launchedAt }
        if ($proc.HasExited -and -not $alive) {
            Write-Warning 'the game exited before writing a STATUS line'
            break
        }

        Start-Sleep -Seconds 3
    }

    if (-not $status) {
        Write-Warning "no STATUS line after $([int]((Get-Date) - $launchedAt).TotalSeconds)s"
    }

    # Shut down gracefully wherever possible: a forced kill makes the *next* launch open the
    # Safe Mode prompt, so being impatient here costs the following run.
    Start-Sleep -Seconds 5
    if (-not $KeepOpen) {
        $live = Get-Process -Id $proc.Id -ErrorAction SilentlyContinue
        if ($live) {
            $null = $live.CloseMainWindow()
            $null = $live.WaitForExit(30000)
        }
        Get-Process -Name 'Bannerlord*' -ErrorAction SilentlyContinue |
            Where-Object { $_.StartTime -ge $launchedAt } |
            Stop-Process -Force -ErrorAction SilentlyContinue
    }
}
finally {
    Remove-Item $settingsFile -Force -ErrorAction SilentlyContinue
}

# --- convert to PNG --------------------------------------------------------

# The engine writes uncompressed bitmaps regardless of the file extension, so convert here:
# PNG is roughly six times smaller and is what everything downstream can actually open.
Add-Type -AssemblyName System.Drawing
foreach ($raw in @(Get-ChildItem $OutputDirectory -Filter '*.bmp' -ErrorAction SilentlyContinue)) {
    try {
        $img = [Drawing.Image]::FromFile($raw.FullName)
        $w = [Math]::Min($MaxWidth, $img.Width)
        $h = [int]($img.Height * $w / $img.Width)
        $canvas = New-Object Drawing.Bitmap $w, $h
        $gfx = [Drawing.Graphics]::FromImage($canvas)
        $gfx.InterpolationMode = [Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
        $gfx.DrawImage($img, 0, 0, $w, $h)
        $canvas.Save((Join-Path $OutputDirectory ($raw.BaseName + '.png')), [Drawing.Imaging.ImageFormat]::Png)
        $gfx.Dispose(); $canvas.Dispose(); $img.Dispose()
        Remove-Item $raw.FullName -Force -ErrorAction SilentlyContinue
    }
    catch {
        Write-Warning "could not convert $($raw.Name): $_"
    }
}

# --- report ----------------------------------------------------------------

if (Test-Path $log) {
    Write-Host '==> capture.log' -ForegroundColor Cyan
    Get-Content $log | ForEach-Object { "   $_" }
}
elseif (Test-Path $breadcrumb) {
    # Capture never armed; this says whether the mod loaded and how it resolved its settings.
    Write-Host '==> capture never started; module breadcrumb:' -ForegroundColor Yellow
    Get-Content $breadcrumb | ForEach-Object { "   $_" }
}
else {
    Write-Host '==> no capture.log and no breadcrumb.' -ForegroundColor Yellow
    Write-Host '    Either the mod did not load, or the deployed build is a Release one:' -ForegroundColor Yellow
    Write-Host '    Release drops Capture/** entirely. Re-run without -SkipBuild to redeploy Debug.' -ForegroundColor Yellow
}

$shots = @(Get-ChildItem $OutputDirectory -Filter '*.png' -ErrorAction SilentlyContinue | Sort-Object Name)
Write-Host "==> $($shots.Count) screenshot(s) in $OutputDirectory" -ForegroundColor Cyan
$shots | ForEach-Object { "   $($_.Name)  $([math]::Round($_.Length / 1KB))KB" }

if ($shots.Count -eq 0) { exit 1 }
exit 0
