## `GameData/4kSP/PluginData/ModWindowScaler.cfg`

Controls the original mod-window scaler (`4kSP-ModWindows.dll`).
Open the globally available **4kSP Expanded Settings** toolbar window and
select **Mod Window Scaling**. Apply changes affect subsequent windows;
Save also writes the global config. The file is loaded at startup, so restart
KSP after hand edits. Reopening the menu does not reset applied values.

```
MODWINDOWSCALER
{
	enabled = True
	useStockUIScale = True
	scale = 1.5
	keepOnScreen = True
	logWindows = False

	OVERRIDE
	{
		assembly = WaypointManager
		scale = 1.75
	}

	exclude = SCANsat
}
```

| Key | Default | Meaning |
|---|---|---|
| `enabled` | `True` | Master on/off for the whole DLL. Also controllable per-save from Difficulty Settings ("Enable Mod Window Scaler") - both have to be true for scaling to happen. |
| `useStockUIScale` | `True` | If true, follows the stock UI Scale slider (Settings > General). If false, uses the fixed `scale` value below instead. |
| `scale` | `1.5` | Only read when `useStockUIScale = False`. Range 1.0-3.0. |
| `keepOnScreen` | `True` | If a scaled window would spill off-screen, shrinks that window's effective scale just enough to fit instead of clipping it. |
| `logWindows` | `False` | Logs one line per mod the first time it opens a window - see below, this is how you find the assembly name to use in `OVERRIDE`/`exclude`. |

### Excluding a mod

One line per assembly:

```
exclude = SCANsat
```

This is shorthand for `OVERRIDE { assembly = SCANsat  scale = 1 }` - the
mod's windows are left completely untouched.

### Custom scale for one mod

```
OVERRIDE
{
	assembly = WaypointManager
	scale = 1.75
}
```

An `OVERRIDE` block wins over `scale`/`useStockUIScale` for that assembly
only. You can list as many `OVERRIDE` blocks as you want.

### Finding the assembly name

Set `logWindows = True`, open the mod's window once in-game, then check
`KSP.log` for a line like:

```
[4kSP-ModWindows] window from assembly 'WaypointManager' (requested scale: 1.5)
```

The quoted name is exactly what goes in `assembly =` or `exclude =`.
Turn `logWindows` back off once you're done - it's meant for figuring
out names, not for normal play (one log line per mod, so it's harmless
either way, just noisy).

Mods that already scale their own windows (MechJeb with its own UI Scale
setting, KIS/KAS, Docking Port Alignment Indicator, ...) are detected and
skipped automatically - you never need to `exclude` those.
