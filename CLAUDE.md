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

| Command | Method | Purpose |
|---------|--------|---------|
| `EEN` | `EditPoseCommand` | Main editor — select or insert an `RL-POS`/`RL-POS2` block, open WPF dialog to edit attributes |
| `POZVER` | — | Assign position numbers to selected `RL-POS` blocks |
| `POZCLAYER` | — | Move selected `RL-POS*` blocks to layer `ren.mtr.tb` |
| `ADET` | — | Change quantity (adet) in a TB block |
| `ADET2` | — | Change quantity multiplier (adet carpi) in a TB block |
| `CAP` | — | Change diameter in TB (sets TIK=1) |
| `ARALIK` | — | Change spacing in TB and ARALIK attribute |
| `GRUP` | — | Change GC (group multiplier) |
| `DEGIS` | — | Mass-replace attribute values across blocks |
| `TDDK` | — | Copy attributes from a reference block to selected blocks |
| `TDDB` | — | Auto-fill BOY by summing segments A–F |
| `TDDH` | — | Output error.txt content as model text |
| `TDD1/2/3` | — | Rearrange TB/BOY/NOT by scheme 1, 2, or 3 |
| `PZG` | — | Sync RL-POS attributes (ATTSYNC) |
| `77` | — | Create a linked callout block RL-POS2 with fields |
| `77B` | — | Find ACAD_FIELD sources and zoom to them |
| `PZREDEF` | — | Redefine PZ_* blocks from a type list |
| `DIEZ` | — | Mark blocks containing `#` in attributes with arrows |
| `TDDU` | — | Apply reference block data to other blocks with same POZ |
| `PPP` | — | Draw arrows from found positions to a point |
| `PPP2` | — | Find positions, zoom, interactive review/delete |
| `POZSIL` | — | Clear POZ attribute on selected blocks (sets to 0) |
| `CREATELAYERS` / `CL` | `LayerCreator` | Create 41 standard layers with prefix, color, linetype, weight |

## Architecture

### Key classes

**`Commands.cs`** — All `[CommandMethod]` entry points. The `EEN` command is the main flow: saves AutoCAD system variables (`CLAYER`, `DIMZIN`, `ATTREQ`), ensures the `ren.mtr.tb` layer exists, prompts for block selection or insertion point, extracts `RL-POS.dwg`/`RL-POS2.dwg` from embedded resources to a temp file, inserts them, then opens `PoseEditWindow`.

**`PoseEditWindow.xaml/.cs`** — Main WPF dialog. Displays 94 rebar shape images (`Shape_00.png`–`Shape_93.png`), material selectors, dimension fields (A–F, R), quantity, and notes. Writes results back to block attributes on dialog close.

**`AppSettings.cs`** — Static singleton for global state: drawing units (m/cm/mm), sheet scale, table language, project name. Shared across all commands.

**`BlockHelper.cs`** — Static utilities for reading/writing AutoCAD block attributes as dictionaries. Used everywhere attributes are accessed.

**`RebarRecognizer.cs`** — Geometric algorithm that analyzes a polyline and auto-detects the BS 8666 rebar shape code (00=straight, 11=L-bend, 21=U-bend, etc.) and extracts dimensions A–F and radius R.

**`LayerCreator.cs`** — `CREATELAYERS`/`CL` commands. Creates 41 standard layers.

**`QuantityTableGenerator.cs`** — `RQT` command. Aggregates all rebar position blocks and generates an AutoCAD specification table.

### Patterns

- All AutoCAD database writes happen inside a `Transaction` (open → modify → `Commit()`).
- System variable state is saved before a command runs and restored in a `finally` block.
- Embedded DWG resources (`RL-POS.dwg`, `RL-POS2.dwg`) are extracted to `%TEMP%` at runtime before insertion via `db.Insert`.
- Shape images are WPF resources (not embedded), referenced via `pack://application:,,,/` URIs.
- `#nullable disable` is used at the top of each file — do not remove it.

## Dependencies

- `AutoCAD.NET` / `AutoCAD.NET.Core` / `AutoCAD.NET.Model` v25.1.0 — AutoCAD 2025/2026 API
- Target: `net8.0-windows`, WPF enabled, C# `latest` (12)
