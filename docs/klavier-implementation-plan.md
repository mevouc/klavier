# Klavier Implementation Plan: POC to Working Vertical Slice

## Context

Klavier is a desktop piano app (C#/.NET 10.0) inspired by OpenPiano. Currently it only has a
console POC that plays Middle C via FluidSynth with hardcoded paths. The goal is to restructure
into a proper hexagonal architecture and iteratively build toward a working vertical slice:
**keyboard → core → audio + visual piano**.

The user wants strict hexagonal ports from day one, separate projects per concern, and
`IOptionsMonitor<T>` with `appsettings.json` for external configuration.

### Inspiration from OpenPiano

OpenPiano (Python/PySide6) is the feature reference. We take inspiration from its features,
not its architecture. Key features to integrate across iterations:

- **Configurable velocity** (1-127, default 100) and **volume** (0-100%, default 60%)
- **Transpose** (-21 to +21 semitones)
- **Sustain pedal** with percentage-based decay (future refinement) — simple on/off first
- **Note name styles**: Alphabetic (C, D, E) and Syllabic (Do, Re, Mi)
- **Key colors**: static theme constants first, user-configurable later
- **Dark/Light theme**
- **Key labels** (keyboard bindings) and **note labels** (note names) toggles
- **All Notes OFF** button (if simple to implement)
- **MIDI recording** to .mid files (last iterations)
- **Custom keybinds** (later iteration)

---

## Architecture Decisions

### Core Event Flow

```
[Input Adapter] --calls--> PianoEngine.NoteOn/NoteOff
                                |
                    iterates registered handlers
                       /                    \
          IAudioOutput.OnNoteOn    INoteEventHandler.OnNoteOn
          (FluidSynth plays)       (UI highlights key)
```

**Input side**: Adapters call `PianoEngine` methods directly. Simple, debuggable, testable.
No event bus — unnecessary at this scale. Can add `IObservable<T>` later if N-to-N is needed.

**Output side**: `PianoEngine` holds a `List<INoteEventHandler>` and calls each synchronously.
Audio adapter handles inline (FluidSynth is thread-safe). UI adapter marshals to UI thread internally.

### Threading

- Keyboard events arrive on Avalonia UI thread → call `PianoEngine` synchronously
- `PianoEngine` fans out to handlers on the calling thread
- `FluidSynthAudioOutput`: FluidSynth's `Synth.NoteOn` is thread-safe and fast (µs). Its `AudioDriver` runs its own background audio thread — no extra thread needed
- `PianoViewModel`: called on UI thread (from keyboard input) → updates directly. When called from non-UI thread (future MIDI), marshals via `Dispatcher.UIThread.Post()`

### Native DLLs

Move the platform-conditional `<Content>` items from `Klavier.csproj` to `Klavier.Audio.csproj`.
Content items with `CopyToOutputDirectory` propagate through `<ProjectReference>` chains to the
final executable output.

---

## Project Structure

```
src/
  Klavier.Core/      # Zero dependencies. Domain types, port interfaces, key mapping logic
  Klavier.Audio/     # FluidSynth adapter. Depends on: Core, NFluidsynth, Options
  Klavier.UI/        # Avalonia app. Depends on: Core, Avalonia
  Klavier/           # Host. Composition root, DI, appsettings.json. Depends on: all above
tests/
  Klavier.Core.Tests/ # Unit tests for Core (optional per iteration)
```

Dependency graph:
```
Klavier (Host/Exe)
  ├── Klavier.Core
  ├── Klavier.Audio ──→ Klavier.Core
  └── Klavier.UI ─────→ Klavier.Core
```

---

## Iteration 2: Solution Restructure + Core + Audio Adapter + DI Host

**Goal**: Same POC behavior (play Middle C, wait, exit) but with proper hexagonal structure,
DI, and config from `appsettings.json`. Console app — no UI yet.

### 2.1 — Klavier.Core (new project, zero dependencies)

| File | Purpose |
|------|---------|
| `src/Klavier.Core/Klavier.Core.csproj` | Class library, no PackageReferences |
| `src/Klavier.Core/Events/NoteOnEvent.cs` | `readonly record struct NoteOnEvent(int Pitch, int Velocity)` |
| `src/Klavier.Core/Events/NoteOffEvent.cs` | `readonly record struct NoteOffEvent(int Pitch)` |
| `src/Klavier.Core/Ports/INoteEventHandler.cs` | Output port: `OnNoteOn(NoteOnEvent)`, `OnNoteOff(NoteOffEvent)` |
| `src/Klavier.Core/Ports/IAudioOutput.cs` | Extends `INoteEventHandler` + `IDisposable`. Adds `Initialize()` (load SF, start driver) |
| `src/Klavier.Core/Engine/PianoEngine.cs` | Orchestrator: `RegisterHandler()`, `NoteOn(pitch)`, `NoteOff(pitch)`. Tracks active notes in `HashSet<int>`. Applies transpose and default velocity from settings before dispatching |
| `src/Klavier.Core/Options/PlaybackConfig.cs` | `int Velocity` (1-127, default 100), `int Transpose` (-21 to +21, default 0) |

### 2.2 — Klavier.Audio (new project)

| File | Purpose |
|------|---------|
| `src/Klavier.Audio/Klavier.Audio.csproj` | References Core. PackageRefs: `SpaceWizards.NFluidsynth`, `Microsoft.Extensions.Options`. Owns native DLL copy items (moved from Klavier.csproj) |
| `src/Klavier.Audio/Options/AudioConfig.cs` | `string SoundFontPath`, `string AudioDriver`, `int Volume` (0-100, default 60) |
| `src/Klavier.Audio/FluidSynthAudioOutput.cs` | Implements `IAudioOutput`. Takes `IOptionsMonitor<AudioConfig>`. Creates NFluidsynth `Settings`/`Synth`/`AudioDriver`. `OnNoteOn` → `_synth.NoteOn(0, pitch, velocity)`. Reacts to volume changes via `IOptionsMonitor.OnChange` |
| `src/Klavier.Audio/ServiceCollectionExtensions.cs` | `AddFluidSynthAudio(IServiceCollection, IConfiguration)` — binds config section, registers singleton |

**Design note on transpose**: `PianoEngine` applies transpose (shifts pitch by N semitones)
before dispatching to handlers. This way the audio adapter and UI both receive the already-transposed pitch.
The engine clamps the result to valid MIDI range (0-127).

**Design note on velocity**: `PianoEngine.NoteOn(pitch)` uses the default velocity from
`PlaybackConfig`. A future overload `NoteOn(pitch, velocity)` will be used by MIDI input
to pass through the device's velocity.

### 2.3 — Klavier Host (restructure existing project)

| File | Purpose |
|------|---------|
| `src/Klavier/Klavier.csproj` | Exe. References Core + Audio. Adds `Microsoft.Extensions.Hosting`. Removes direct NFluidsynth ref and native DLL items |
| `src/Klavier/Program.cs` | Builds `IHost` with `appsettings.json`. Registers services. Resolves `PianoEngine` + `IAudioOutput`, registers handler, calls NoteOn/NoteOff as POC proof |
| `src/Klavier/appsettings.json` | See below |

```json
{
  "Audio": {
    "SoundFontPath": "C:\\path\\to\\soundfont.sf2",
    "AudioDriver": "dsound",
    "Volume": 60
  },
  "Playback": {
    "Velocity": 100,
    "Transpose": 0
  }
}
```

### 2.4 — Solution & Package Management

- `Directory.Packages.props`: add `Microsoft.Extensions.Hosting`, `Microsoft.Extensions.Options`, `Microsoft.Extensions.Options.ConfigurationExtensions`
- `Klavier.slnx`: register new projects under `/src/`

### 2.5 — Verification
- `dotnet build` succeeds for all projects
- `dotnet run --project src/Klavier` plays Middle C using config from appsettings.json
- Changing `Volume` or `Velocity` in appsettings.json affects playback
- `Klavier.Core.csproj` has zero `<PackageReference>` elements

---

## Iteration 3: Avalonia + Keyboard Input (End-to-End Sound)

**Goal**: An Avalonia window opens. Pressing 3-4 hardcoded physical keys produces piano sound.
No visual piano yet — just a window that captures keys.

### 3.1 — Klavier.UI (new project)

| File | Purpose |
|------|---------|
| `src/Klavier.UI/Klavier.UI.csproj` | References Core. PackageRefs: `Avalonia`, `Avalonia.Desktop`, `Avalonia.Themes.Fluent` |
| `src/Klavier.UI/App.cs` | Avalonia `Application` subclass (declarative C#, no XAML). Receives `IServiceProvider`, resolves `MainWindow` in `OnFrameworkInitializationCompleted` |
| `src/Klavier.UI/Views/MainWindow.cs` | `Window` subclass. Overrides `OnKeyDown`/`OnKeyUp` using `e.PhysicalKey`. Hardcoded mapping: A→C4, S→D4, D→E4, F→F4. Calls `PianoEngine.NoteOn/NoteOff` |

### 3.2 — Host changes

- `src/Klavier/Klavier.csproj`: add reference to `Klavier.UI`
- `src/Klavier/Program.cs`: rewrite to bootstrap Avalonia via `AppBuilder.Configure<App>().UsePlatformDetect().StartWithClassicDesktopLifetime(args)`. DI wiring happens before Avalonia starts; `App` receives the service provider.

### 3.3 — Package Management
- `Directory.Packages.props`: add Avalonia packages with pinned versions

### 3.4 — Verification
- Window opens on `dotnet run`
- Pressing A/S/D/F plays C4/D4/E4/F4
- Releasing stops the note
- No visual feedback yet

---

## Iteration 4: Piano Visual + Key Highlighting + Note Labels

**Goal**: Render a visual piano keyboard (61 keys). Keys highlight when pressed.
Mouse clicks on keys produce sound. Note labels displayed (Alphabetic + Syllabic styles).

### 4.1 — Theme & Colors

| File | Purpose |
|------|---------|
| `src/Klavier.UI/Theme/PianoColors.cs` | Static class with color constants: `WhiteKey`, `WhiteKeyPressed`, `BlackKey`, `BlackKeyPressed`, dark/light theme variants. Not configurable yet — just constants |
| `src/Klavier.UI/Theme/KlavierTheme.cs` | Static class: `AppBackground`, `PanelBackground`, `TextPrimary`, `Accent` for Dark and Light modes |

### 4.2 — Note Naming (Core)

| File | Purpose |
|------|---------|
| `src/Klavier.Core/Music/NoteNameStyle.cs` | Enum: `Alphabetic`, `Syllabic` |
| `src/Klavier.Core/Music/NoteNames.cs` | Static methods: `GetNoteName(int pitch, NoteNameStyle style)` → "C4", "Do4", etc. |

### 4.3 — ViewModels

| File | Purpose |
|------|---------|
| `src/Klavier.UI/ViewModels/PianoViewModel.cs` | Implements `INoteEventHandler` + `INotifyPropertyChanged`. Holds `ObservableCollection<PianoKeyViewModel>`. Marshals to UI thread via `Dispatcher.UIThread.Post`. Exposes `NoteNameStyle` property (bound to settings) |
| `src/Klavier.UI/ViewModels/PianoKeyViewModel.cs` | `int Pitch`, `bool IsPressed`, `bool IsBlack`, `string KeyLabel` (keyboard binding), `string NoteLabel` (note name). Raises `PropertyChanged` |

### 4.4 — Views

| File | Purpose |
|------|---------|
| `src/Klavier.UI/Views/PianoView.cs` | Custom panel rendering 61 keys. White keys as rectangles, black keys overlaid. Binds background color to `IsPressed` using `PianoColors` |
| `src/Klavier.UI/Views/PianoKeyControl.cs` | Individual key visual. Handles `PointerPressed`/`PointerReleased` for mouse input → calls `PianoEngine`. Displays key label and note label |
| `src/Klavier.UI/Views/MainWindow.cs` | Updated: content is now `PianoView`. Still handles keyboard input. Reads `Topmost` from `UIConfig` via `IOptionsMonitor` |

### 4.5 — Settings additions

```json
{
  "UI": {
    "Theme": "Dark",
    "Topmost": false,
    "ShowKeyLabels": true,
    "ShowNoteLabels": true,
    "NoteNameStyle": "Alphabetic"
  }
}
```

| File | Purpose |
|------|---------|
| `src/Klavier.UI/Options/UIConfig.cs` | `string Theme`, `bool Topmost`, `bool ShowKeyLabels`, `bool ShowNoteLabels`, `string NoteNameStyle` |

### 4.6 — Wiring
`PianoViewModel` registered as `INoteEventHandler` on `PianoEngine` alongside audio handler.
Keyboard press → Engine → both audio plays AND key highlights.

### 4.7 — Verification
- Visual piano rendered with white and black keys (dark theme by default)
- Keyboard input highlights keys AND plays sound
- Mouse click on piano key plays sound AND highlights
- Note labels show Alphabetic names (C4, D#4, etc.)
- Key labels show keyboard bindings on keys

---

## Iteration 5: Full Key Mapping + Sustain Pedal

**Goal**: Complete scan-code-to-note mapping for AZERTY and QWERTY. Simple on/off sustain
via Space key.

### 5.1 — Full Key Mapping

| File | Purpose |
|------|---------|
| `src/Klavier.Core/Options/KeyMappingConfig.cs` | `string Layout` ("AZERTY" / "QWERTY") |
| `src/Klavier.Core/Mapping/ScanCodeToNote.cs` | Layout tables as `IReadOnlyDictionary<PhysicalKey, int>`. Shift → sharps/black keys. Ctrl → extended 88-key range. 61-key default, 88-key with Ctrl modifiers |
| `src/Klavier.Core/Mapping/PhysicalKey.cs` | Core-defined enum mirroring USB HID scan codes (subset). Numeric values match Avalonia's `PhysicalKey` for easy casting |
| `src/Klavier.Core/Mapping/ModifierKeys.cs` | Flags enum: `None`, `Shift`, `Ctrl` |

```json
{
  "KeyMapping": {
    "Layout": "AZERTY"
  }
}
```

### 5.2 — Sustain Pedal (Simple On/Off)

- `PianoEngine`: add `SustainOn()` / `SustainOff()` methods
- When sustain is on, `NoteOff` queues the release instead of dispatching immediately
- When sustain is released, all queued notes are dispatched as `NoteOffEvent`
- `INoteEventHandler`: add `OnSustainChanged(bool isOn)`
- `FluidSynthAudioOutput`: maps sustain to FluidSynth CC64 (sustain controller)
- `MainWindow`: Space key → `PianoEngine.SustainOn/Off`

### 5.3 — All Notes OFF

- `PianoEngine`: add `AllNotesOff()` method — releases all active notes and clears sustain queue
- `FluidSynthAudioOutput`: sends All Notes Off MIDI message
- Exposed as a button in the UI (simple addition to MainWindow)

### 5.4 — Verification
- Full keyboard mapping works for AZERTY layout
- Shift+key plays sharps
- Space key enables/disables sustain (notes ring while held)
- All Notes OFF button silences everything
- Changing layout in appsettings switches mapping

---

## Iteration 6+: Polish & Extended Features (Sketch)

These are planned but not detailed. Architecture must not block them.

### Sustain Percentage & Fade (Refinement)
- `PlaybackConfig`: add `SustainPercent` (0-100, default 100), `SustainFade` (0-100, default 0)
- 0% = no sustain, 100% = infinite hold, 1-99% = time-based decay (80ms-2400ms curve)
- Fade controls volume decay during sustain release
- UI sliders for both values

### Dark/Light Theme Toggle
- `KlavierTheme` already has both color sets (from Iteration 4)
- Add toggle in settings UI to switch between themes at runtime
- `IOptionsMonitor<UIConfig>` reacts to theme change

### Key Color Customization
- Promote `PianoColors` constants to user-configurable settings
- 4 hex color pickers in settings UI: white/black × pressed/unpressed

### Settings UI Panel
- `SettingsView.cs`: collapsible panel with sections:
  - Sound: Volume slider, Velocity slider, Transpose slider, SoundFont file picker
  - Keyboard: Layout dropdown, Show key labels, Show note labels, Note name style
  - Interface: Theme toggle, Topmost toggle, key color pickers

### Custom Keybinds
- Interactive editor: click a piano key, then press desired keyboard key
- Persisted as JSON in settings
- Override default layout mapping

### MIDI Recording (Output)
- New project: `Klavier.Midi/` with `DryWetMidi` dependency
- `MidiRecorderOutput` implements `INoteEventHandler` — captures note events with timestamps
- Export to .mid (Type 0) file
- UI: Record/Stop button, elapsed time display

### MIDI Input (External Devices)
- `Klavier.Midi/`: `MidiDeviceInputAdapter` using `DryWetMidi`
- Reads NoteOn/NoteOff from external MIDI device, calls `PianoEngine`
- Preserves device velocity (bypasses default velocity setting)
- UI: device dropdown selector

### SharpHook (Background Keyboard Capture)
- New project: `Klavier.GlobalInput/` with `SharpHook` dependency
- Captures keyboard input even when Klavier window is not focused
- Uses same Core `ScanCodeToNote` mapping

---

## Critical Files Summary

| File | Action |
|------|--------|
| `src/Klavier/Program.cs` | Rewrite into DI composition root |
| `src/Klavier/Klavier.csproj` | Remove NFluidsynth, add project refs + Hosting |
| `Directory.Packages.props` | Add Hosting, Options, Avalonia packages |
| `Klavier.slnx` | Register all new projects |
| `src/Klavier.Audio/Klavier.Audio.csproj` | Inherits native DLL copy items from current Klavier.csproj |
