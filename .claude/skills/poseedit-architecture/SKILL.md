---
name: poseedit-architecture
description: Per-file breakdown of what each core class in PoseEdit2026 does (Commands.cs, PoseEditWindow, AppSettings.cs, BlockHelper.cs, RebarRecognizer.cs, LayerCreator.cs, QuantityTableGenerator.cs, PozHelper.cs, LegacyCommands.cs, ExtensionApp.cs). Use when navigating the codebase, figuring out which file/class handles something, or before adding new functionality.
---

**`Commands.cs`** — All `[CommandMethod]` entry points. The `EEN` command is the main flow: saves AutoCAD system variables (`CLAYER`, `DIMZIN`, `ATTREQ`), ensures the `ren.mtr.tb` layer exists, prompts for block selection or insertion point, extracts `RL-POS.dwg`/`RL-POS2.dwg` from embedded resources to a temp file, inserts them, then opens `PoseEditWindow`.

**`PoseEditWindow.xaml/.cs`** — Main WPF dialog. Displays 94 rebar shape images (`Shape_00.png`–`Shape_93.png`), material selectors, dimension fields (A–F, R), quantity, and notes. Writes results back to block attributes on dialog close.

**`AppSettings.cs`** — Static singleton for global state: drawing units (m/cm/mm), sheet scale, table language, project name. Shared across all commands.

**`BlockHelper.cs`** — Static utilities for reading/writing AutoCAD block attributes as dictionaries. Used everywhere attributes are accessed.

**`RebarRecognizer.cs`** — Geometric algorithm that analyzes a polyline and auto-detects the BS 8666 rebar shape code (00=straight, 11=L-bend, 21=U-bend, etc.) and extracts dimensions A–F and radius R. Also owns the polyline-link feature: `SetLinkedPolyline`/`SyncFromLinkedPolyline`/`SyncAllLinkedBlocksInDatabase` store a linked polyline's handle as XDATA on the RL-POS block and re-run recognition against it later (see `ExtensionApp.cs`).

**`LayerCreator.cs`** — `CREATELAYERS`/`CL` commands. Creates 41 standard layers.

**`QuantityTableGenerator.cs`** — `RQTN` command. Aggregates all rebar position blocks and generates an AutoCAD specification table. Also exposes `GetClientPath`/`GetUnits`/`GetScale`/`ParseBoyInt` as `internal` for reuse by `LegacyCommands.cs`.

**`PozHelper.cs`** — Shared engine for the `*N` legacy-command ports: TB-string parsing (`GetAdetCarpi`/`GetAdet`/`GetCap`/`GetAralik`), `RepositionShapeText` (auto-snaps BOY/NOT next to TB after an edit — port of AutoLISP `poz_sekil_topla`), and low-level attribute-position helpers (`MoveAttrTo`, `GetBlockGeometry`).

**`LegacyCommands.cs`** — All the `*N`-suffixed command ports listed in the command table (`ADETN`, `CAPN`, `DEGISN`, `TDDUN`, `POZVERN`, `PZGN`, etc.), translated from `Temp/Command/QUANTITY2.LSP` and related files.

**`ExtensionApp.cs`** — `IExtensionApplication` entry point (`Initialize`/`Terminate`, invoked automatically by AutoCAD on NETLOAD/unload). Subscribes to `Document.CommandEnded` on every open/newly-opened document; on `REGEN`/`REGENALL` calls `RebarRecognizer.SyncAllLinkedBlocksInDatabase` to refresh all polyline-linked positions in that drawing.
