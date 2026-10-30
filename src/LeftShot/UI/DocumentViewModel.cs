using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using LeftShot.Core;

namespace LeftShot.UI;

/// <summary>Document and selection state shared by the editor controls; pointer gestures remain view concerns.</summary>
public sealed class DocumentViewModel : INotifyPropertyChanged, IDisposable
{
    private ToolKind _selectedTool = ToolKind.Select;
    private Guid? _selectedAnnotationId;
    private bool _isBusy;

    public DocumentViewModel(EditDocument document)
    {
        Document = document ?? throw new ArgumentNullException(nameof(document));
        Document.Changed += DocumentChanged;
    }

    public EditDocument Document { get; }
    public bool CanUndo => !IsBusy && Document.CanUndo;
    public bool CanRedo => !IsBusy && Document.CanRedo;
    public bool IsDirty => Document.IsDirty;
    public string Title => "LeftShot — Editor" + (IsDirty ? " • Edited" : "");

    public ToolKind SelectedTool
    {
        get => _selectedTool;
        set { if (_selectedTool != value) { _selectedTool = value; Notify(); } }
    }

    public Guid? SelectedAnnotationId
    {
        get => _selectedAnnotationId;
        set { if (_selectedAnnotationId != value) { _selectedAnnotationId = value; Notify(); } }
    }

    public bool IsBusy
    {
        get => _isBusy;
        set
        {
            if (_isBusy == value) return;
            _isBusy = value;
            Notify();
            Notify(nameof(CanUndo));
            Notify(nameof(CanRedo));
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    private void Notify([CallerMemberName] string? property = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(property));
    private void DocumentChanged()
    {
        Notify(nameof(CanUndo));
        Notify(nameof(CanRedo));
        Notify(nameof(IsDirty));
        Notify(nameof(Title));
    }
    public void Dispose() => Document.Changed -= DocumentChanged;
}
