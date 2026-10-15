## `GameData/4kSP/PluginData/ModWindowScaler.cfg`

Controls the mod-window scaler (`4kSP-ModWindows.dll`). Plain text,
reloaded on Save from the in-game window — no restart needed for hand
edits either, just re-enter a scene (or reopen the mod window and hit
"Apply").

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

### Default per-mod entries

The shipped config and newly generated configs include these `exclude` entries
(1× additional scaling), leaving the mods' native UI sizing in charge:

| Mod / window family | Exact callback owner assembly |
|---|---|
| [Principia](https://github.com/mockingbirdnest/Principia/blob/master/ksp_plugin_adapter/ksp_plugin_adapter.csproj) | `principia.ksp_plugin_adapter` |
| [Principia error-dialog stub](https://github.com/mockingbirdnest/Principia/blob/master/ksp_plugin_adapter_stub/ksp_plugin_adapter_stub.csproj) | `principia.ksp_plugin_adapter_stub` |
| [MechJeb](https://github.com/MuMech/MechJeb2/blob/d23edfb7cadc67d089b91a0e9db4b9a5cc5bece7/MechJeb2/DisplayModule.cs#L163-L182) | `MechJeb2` |
| [KerbalEngineer](https://github.com/jrbudda/KerbalEngineer/blob/1.1.9.5/KerbalEngineer/UIControls/PopOutElement.cs#L158) | `KerbalEngineer` |
| [Kerbal Inventory System](https://github.com/ihsoft/KIS/blob/master/Source/ModuleKISInventory.cs) | `KIS` |

Matching uses the window callback's declaring assembly, not its title, namespace,
or DLL filename. Principia renames its DLL without changing this assembly name.
`KerbalEngineer.Unity` has retained Unity UI, not these IMGUI callbacks.
`KSPDev_Utils.2.6` owns shared, unscaled debug windows and is not excluded.

Exclusions apply to the entire owner assembly. KIS book/bomb dialogs and some
KerbalEngineer secondary dialogs have fixed sizing; these also retain native
size. To opt a mod back into global scaling, uncheck **Exclude** and save, or
remove its `exclude` line. Existing configs retain their saved choices rather
than being overwritten; merge the entries above to apply these defaults to an
existing installation.

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

Windows that enter with a non-identity `GUI.matrix` are skipped automatically.
This detects outer matrix scaling, not font/layout scaling such as
KerbalEngineer's own display-size setting. The explicit defaults above also
protect identity-matrix windows belonging to those owners.
