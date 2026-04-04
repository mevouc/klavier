using System.Collections.Frozen;
using Avalonia.Controls;
using Avalonia.Input;
using Klavier.Core.Engine;

namespace Klavier.UI.Views;

public class MainWindow : Window
{
    private static readonly FrozenDictionary<PhysicalKey, ushort> _KeyToNote = new Dictionary<PhysicalKey, ushort>
    {
        [PhysicalKey.Q] = 60,  // C4
        [PhysicalKey.S] = 62,  // D4
        [PhysicalKey.D] = 64,  // E4
        [PhysicalKey.F] = 65,  // F4
    }.ToFrozenDictionary();

    private readonly IPianoEngine _pianoEngine;
    private readonly HashSet<PhysicalKey> _heldKeys = [];

    public MainWindow(IPianoEngine pianoEngine)
    {
        _pianoEngine = pianoEngine;

        Title = "Klavier";
        Width = 800;
        Height = 200;
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        if (_KeyToNote.TryGetValue(e.PhysicalKey, out ushort note) && _heldKeys.Add(e.PhysicalKey))
        {
            _pianoEngine.NoteOn(note);
            e.Handled = true;
        }

        base.OnKeyDown(e);
    }

    protected override void OnKeyUp(KeyEventArgs e)
    {
        if (_KeyToNote.TryGetValue(e.PhysicalKey, out ushort note) && _heldKeys.Remove(e.PhysicalKey))
        {
            _pianoEngine.NoteOff(note);
            e.Handled = true;
        }

        base.OnKeyUp(e);
    }
}
