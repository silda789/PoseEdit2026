# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## What this project is

**PoseEdit2026** is an AutoCAD 2026 .NET plugin (C#, .NET 8, WPF) for structural/rebar drafting. It registers commands in AutoCAD via `[CommandMethod]` attributes and provides a WPF dialog for editing rebar position blocks (`RL-POS`, `RL-POS2`).

## Build

```
dotnet build PoseEdit2026.csproj
```

Output: `bin\Debug\net8.0-windows\PoseEdit2026.dll`

To load in AutoCAD: run command `NETLOAD` and select the DLL, or drop it into AutoCAD's trusted paths.

There are no automated tests.

## AutoCAD commands exposed by this plugin

**Naming convention:** the old AutoLISP files (`Temp/POSEDIT.LSP`, `Temp/Command/*.lsp`) can still be
loaded in the same AutoCAD session as this plugin. To avoid command-name collisions, every C# port of
a LISP command gets an `N` suffix (the same convention already used for `EE` → `EEN`). E.g. LISP
`adet` → C# `ADETN`. The old LISP command keeps working under its original name if still loaded.

| Command | Method | Purpose | Old LISP name |
|---------|--------|---------|----------------|
| `EEN` | `Commands.EditPoseCommand` | Main editor — select or insert an `RL-POS`/`RL-POS2` block, open WPF dialog to edit attributes | `ee` |
| `RQTN` | `QuantityTableGenerator.CreateQuantityTables` | Build the rebar quantity/specification table | `RQT` |
| `CREATELAYERS` / `CL` | `LayerCreator.CreateLayersCommand` | Create 41 standard layers with prefix, color, linetype, weight | — |
| `ADETN` | `LegacyCommands.ChangeAdetCommand` | Change quantity (adet) in TB, keep multiplier/cap/aralik | `adet` |
| `ADET2N` | `LegacyCommands.ChangeAdetCarpiCommand` | Change quantity multiplier ("3x" prefix) in TB | `adet2` |
| `CAPN` | `LegacyCommands.ChangeCapCommand` | Change diameter in TB (sets TIK=1) | `cap` |
| `ARALIKN` | `LegacyCommands.ChangeAralikCommand` | Change spacing in TB and ARALIK attribute, auto-recalculates adet | `aralik` |
| `GRUPN` | `LegacyCommands.ChangeGrupCommand` | Change GC (group multiplier) | `grup` |
| `DEGISN` | `LegacyCommands.ChangeAttributeMassCommand` | Mass find/replace one field's value across selected blocks | `degis` |
| `TDDKN` | `LegacyCommands.CopyAttributesCommand` | Copy all matching-tag attributes from a reference block to target blocks | `tddk` |
| `TDDUN` | `LegacyCommands.ApplyReferenceToMatchingPozCommand` | Apply reference position's data to all blocks sharing the same POZ | `tddu` |
| `TDD1N`/`TDD2N`/`TDD3N` | `LegacyCommands.RearrangeByScheme` | Force TB/BOY/NOT into layout scheme 1, 2, or 3 | `tdd1`/`tdd2`/`tdd3` |
| `TDDHN` | `LegacyCommands.PrintErrorLogCommand` | Print RQTN's `error.txt` validation log into the drawing as text | `tddh` |
| `DIEZN` | `LegacyCommands.MarkHashPositionsCommand` | Mark blocks containing `#` in attributes with arrows | `diez` |
| `PPPN` | `LegacyCommands.DrawArrowsToPozCommand` | Draw arrows from positions matching a POZ to a point | `ppp` |
| `PPP2N` | `LegacyCommands.FindAndReviewPozCommand` | Find positions by POZ, zoom to each, interactive review/delete | `ppp2` |
| `POZVERN` | `LegacyCommands.AutoNumberPositionsCommand` | Auto-assign POZ numbers, grouping identical-geometry positions | `pozver` |
| `77N` | `LegacyCommands.CreateLinkedCalloutCommand` | Create a linked RL-POS2 callout block with ACAD_FIELD references | `77` |
| `PZGN` | `LegacyCommands.SyncBlockDefinitionCommand` | Redefine RL-POS from the embedded template and ATTSYNC all instances (batch op — save first) | `pzg` |
| `POZCLAYERN` | `LegacyCommands.MoveToRenMtrTbLayerCommand` | Move all RL-POS blocks to layer `ren.mtr.tb`, creating it if missing | `pozclayer` |
| `TDDBN` | `LegacyCommands.CalculateBendLengthCommand` | Compute rebar length from A-F/R using per-shape-type coefficients (`Resources/PZ_TUM.txt`), write to BOY | `tddb` |
| `POZSILN` | `LegacyCommands.ClearPozCommand` | Reset POZ to `0` on selected RL-POS blocks (undo of POZVERN's numbering) | `pozsil` |
| `LAYSHIFT` | `RoutineCommands.ShiftLayoutNumbersCommand` | Renumber every numbered Layout tab by a constant offset (e.g. all 55-199 -> 66-211) | — |

### Not ported

| Old LISP command | Why not ported |
|---|---|
| `77b` | Walks undocumented internal FIELD-object DXF structure (dictionary group codes 360/331) that can't be verified without a real AutoCAD session — porting blind risked shipping a silently-broken command. Old LISP `77b` still works if loaded. Deliberately excluded, not just deferred. |
| `pzredef` | Not a position-editing command — it refreshes standalone `PZ_00`..`PZ_99` block *definitions* from `PZ_<tip>.dwg` template files (+ `ATTSYNC`), for blocks that may exist independently somewhere in a drawing. Nothing in `EEN`/`RQTN`/etc. ever inserts these blocks, so it's disconnected from the rest of this plugin's workflow. Deliberately excluded for now, not just deferred. The `.dwg` templates do exist (found alongside `PZ_TUM.txt`, see below) if this is ever wanted. |

`PZ_TUM.txt` (bend-length coefficients used by `TDDBN`) was not in this repo or in `Temp/` — it turned up
in a sibling repo,
`AutoCAD2024Final/MISC_Files/RCP-KJ_metraj_LISP_R2/Ren_LISP_R2/Statik_Standart/RENAISSANCE_SERVER/Standard/`,
outside this repository, and is now embedded at `Resources/PZ_TUM.txt`. The `PZ_<tip>.dwg` templates for
`pzredef` are at that same source path if it's ever wanted — see `Temp/Command/POSREDEF.LSP`.

## Architecture

### Key classes

See skill `poseedit-architecture` for the per-file breakdown of what each core class does (`Commands.cs`, `PoseEditWindow`, `AppSettings.cs`, `BlockHelper.cs`, `RebarRecognizer.cs`, `LayerCreator.cs`, `QuantityTableGenerator.cs`, `PozHelper.cs`, `LegacyCommands.cs`, `ExtensionApp.cs`).

`RoutineCommands.cs` is a grab-bag for small AutoCAD utility commands that aren't about editing
RL-POS blocks (drawing-wide housekeeping like renumbering Layout tabs). Not yet covered by the
`poseedit-architecture` skill — add new one-off "quick task" commands here rather than starting
another file, unless the command grows into its own subsystem.

### Patterns

- All AutoCAD database writes happen inside a `Transaction` (open → modify → `Commit()`).
- System variable state is saved before a command runs and restored in a `finally` block.
- Embedded DWG resources (`RL-POS.dwg`, `RL-POS2.dwg`) are extracted to `%TEMP%` at runtime before insertion via `db.Insert`.
- Shape images are WPF resources (not embedded), referenced via `pack://application:,,,/` URIs.
- `#nullable disable` is used at the top of each file — do not remove it.

### Polyline-linked positions ("Determination" auto-sync)

When `EEN`'s "Determination" button recognizes a shape from a polyline, the polyline's handle is
saved as XDATA on the `RL-POS` block (`RebarRecognizer.SetLinkedPolyline`, app name `POSEDIT_LINK`) —
survives save/reopen. Two things re-run recognition against that saved link and overwrite
TIP/A-F/R/BOY from the polyline's *current* geometry, so edits to the polyline (stretch, grip-edit)
eventually reach the position without reopening `EEN`:
- `REGEN`/`REGENALL` (any document) — resyncs every linked `RL-POS` block in the drawing. Wired up
  in `ExtensionApp.cs` (`IExtensionApplication`, subscribes to `Document.CommandEnded` on load).
- `TDDBN` — resyncs each block it's about to compute a bend length for, before computing.
`EEN` itself does no special sync-on-open work — it just reads whatever is currently in the block's
attributes, which REGEN/TDDBN have already kept fresh.

