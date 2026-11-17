# Continuous Tools for Space Engineers 2

Independent experimental plugin providing a double-left-click latch for the hand drill, welder,
and grinder. It does not modify or depend on Build Planner.

## Install

1. Download the latest ZIP from
   [Releases](https://github.com/ElSolid96/SpaceEngineers2ContinuousTools/releases), then extract
   the complete folder somewhere permanent, outside the SE2 game directory. For example:

   ```text
   C:\SE2Mods\ContinuousTools\
   ```

2. Keep `ContinuousTools.dll` and its bundled dependency DLLs together.

3. In Steam, open **Space Engineers 2 → Properties → General → Launch Options** and add:

   ```text
   -plugins:C:\SE2Mods\ContinuousTools\ContinuousTools.dll
   ```

4. Launch SE2. A successful load writes `ContinuousTools ready` to the plugin log.

To uninstall, remove its DLL path from the Steam launch option. The plugin does not modify the
game installation or save files.

## Controls

- A normal left-button hold remains native.
- Double-click left within `DoubleClickWindowMs` (default 300 ms) to latch the equipped supported
  tool's primary action.
- While latched, one left click turns it off. It does not turn back on from that click.
- Opening a real menu or terminal, losing window focus, unequipping/removing the tool, or leaving
  the world forces the native release path and clears the latch. Normal in-world tool HUD overlays
  do not interrupt manual use or a latch. It never resumes automatically.

This implementation does not inject OS mouse input. It hooks the same native request methods used
by SE2, so reach, power, component use, cooldowns, inventory rules, and server validation remain the
game's responsibility.

## Settings

On first load, `%APPDATA%\SpaceEngineers2\ContinuousTools\settings.json` is created:

```json
{
  "Enabled": true,
  "DoubleClickWindowMs": 300,
  "EnableDrill": true,
  "EnableWelder": true,
  "EnableGrinder": true,
  "DiagnosticLogging": false
}
```

The click window is clamped to 150–1000 ms. Restart the game after editing. The log is written to
the same folder as `ContinuousTools.log`. An on-screen active indicator is not included in the first
build; latch changes are logged without per-frame spam.

## Troubleshooting

The runtime log is written to:

```text
%APPDATA%\SpaceEngineers2\ContinuousTools\ContinuousTools.log
```

To watch it live in PowerShell:

```powershell
Get-Content "$env:APPDATA\SpaceEngineers2\ContinuousTools\ContinuousTools.log" -Tail 50 -Wait
```

If a double-click is difficult to trigger, increase `DoubleClickWindowMs` in `settings.json`, then
restart SE2. A normal press-and-hold always follows the game's native input path. Transient welder
and grinder HUD overlays are ignored; opening a terminal or menu, changing tools, Alt-Tabbing, or
leaving the world intentionally releases an active latch.

If the plugin does not load, confirm the launch option points to `ContinuousTools.dll`, and keep
all DLLs from the staged/release folder together. When combining plugins, use one `-plugins:`
argument with semicolon-separated DLL paths.

## Build and staging

Run `Build-And-Stage.ps1`. It restores, builds Release, runs tests, and writes a self-contained
plugin folder to `stage`. The script does not install anything.

Installation is deliberately separate: `Install-ContinuousTools.ps1` refuses to run while SE2 is
open and requires typing `INSTALL`. `Rollback-ContinuousTools.ps1` requires an explicit backup DLL
and confirmation. Neither script addresses `BuildPlanner.dll` or the game directory.

## Launch option alongside Build Planner

The installed `VRage.Core.dll` was inspected. `PluginHost.LoadPluginsFromArgs` reads the first
`-plugins:` argument and `LoadPlugins` splits its value on semicolons. Therefore use one argument:

```text
-plugins:C:\SE2Mods\BuildPlanner\BuildPlanner.dll;C:\SE2Mods\ContinuousTools\ContinuousTools.dll
```

If either path contains spaces, quote the entire argument. Do not use two `-plugins:` arguments;
the host selects only the first.

## Verified game APIs (SE2 4.0.0.3165)

- `DrillToolComponent.RequestPrimaryAction(bool)` calls
  `DrillComponent.TrySetPrimaryAction(bool, bool)`.
- `IntegrityHandToolComponent.RequestActivatePrimaryActions(bool)` calls
  `IntegrityToolComponent.TrySetActivation(bool, bool)`.
- Integrity mode `Component` is the welder action; `Raw` is the grinder action.
- `DrillToolComponent.OnRemovedFromScene` and
  `IntegrityHandToolComponent.OnBeforeRemoved` provide tool-removal cancellation boundaries.
- `ScreenManager.FinalizeOpen`, `WindowBase.HandleDeactivated`, and
  `InputGameComponent.Destroy` provide screen, focus, and world/input teardown boundaries.

These were verified against the installed DLLs, not guessed. Compilation only verifies signatures;
the plugin remains experimental until the manual acceptance tests below pass.

## Manual acceptance test

Use a disposable offline save. Test normal hold, latch, and single-click stop independently for
drill, welder, and grinder. Then test switching tools, empty hands, weapon/block placement, cockpit,
death/respawn, inventory, terminal, chat, pause, Alt-Tab/focus loss, and world exit. Confirm no
automatic resume or stuck action. Finally verify Build Planner right-click queue and its other
bindings remain unchanged. Review `ContinuousTools.log` after the run.
