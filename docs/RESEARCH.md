# Project Architecture & Ideas

## Layouts & Mapping

* **Piano Range:** 61 or 88 keys.
* **Layout to Note-ID Mapping (Inputs):**
    * QWERTY or AZERTY layout support (e.g., locating the physical 'M' key), mapped to 61 keys.
    * `Shift` + Key for sharp/black keys.
    * `Ctrl` + Key for the additional 88-key range (keeping `Space` reserved for the Sustain pedal).
    * **CRITICAL:** Must be physical key / scan-code based.
* **Scan-code to Displayed Key Mappings:**
    * Toggle physical mapping display (show what's actually written on the physical keys, e.g., digits displayed as 1-2-3, not &-é-").
    * Fetch layout from the OS (capable of displaying values for Dvorak-fr, etc.).
    * *Note:* With default French AZERTY, this will display special characters for the digit row.

---

## Components (Hexagonal Architecture / Ports & Adapters)

* **Options / Settings** (Global configuration)

* **Input Adapters (Publishers):**
    * *Role:* Receive user interactions (or file I/O, etc.) and output a note/action to the Core.
    * *Implementations:*
        * Keyboard input (`AvaloniaUI` natively, or `SharpHook` for headless/background mode).
            * *Note: Avalonia needs active focus for PhysicalKey; SharpHook handles unfocused background inputs.*
        * Mouse clicks on the UI.
        * MIDI input, similar to OpenPiano (using `DryWetMidi`).

* **Core / Domain (Event Bus / Instrument):**
    * *Role:* Receives input signals from any input adapter.
    * *Constraint:* Entirely decoupled from OS, Audio, and UI libraries.
    * *Output:* Broadcasts note events to all subscribed output adapters.
    * *Threading: Must marshal events to the UI thread (via Dispatcher) to prevent cross-thread crashes in Avalonia.*

* **UI (`AvaloniaUI`):**
    * Acts as an **Input Adapter** (mouse control, active window keyboard focus).
    * Acts as a **Visual Output Adapter**.
    * *Architecture: View acts as the adapter (Declarative C#), ViewModel subscribes to the Core Event Bus.*
    * *Features:*
        * Keyboard with white and black keys.
        * Display active note when pressed.
        * Labels on keys.
        * "Stay on Top" (`Topmost`) window property option.

* **Output Adapters (Subscribers):**
    * *Role:* Receive notes to play/process from the Core.
    * *Implementations:*
        * **UI Visualizer:** Shows the key being pressed.
        * **Audio Output:** Runs on a dedicated high-priority thread.
            * SoundFont synthesis
            * Pipe synth to audio output
            * Both using `FluidSynth`, simple package
                * *Warning: FluidSynth requires native interop and OS-specific binaries, complicating deployment.*
        * **MIDI Output:**
            * Recording (using `DryWetMidi`).
            * Virtual instrument (via `loopMIDI` or OS native loopback).
