using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using SlipShot.Core;
using SlipShot.Services;

namespace SlipShot.UI;

/// <summary>An image-pixel editor. Only completed gestures enter the document history.</summary>
public sealed class EditorWindow : Window
{
    private enum Gesture { None, Draw, Crop, Move, Resize, Pan }
    private readonly DocumentViewModel _viewModel;
    private EditDocument _document => _viewModel.Document;
    private readonly Func<EditDocument, Task<bool>> _save;
    private readonly Func<EditDocument, Task<bool>> _saveAs;
    private readonly Func<EditDocument, Task> _copy;
    private readonly Action<BitmapSource> _pin;
    private readonly DrawingSurface _surface;
    private readonly ScrollViewer _scroll;
    private readonly ListBox _layers = new();
    private readonly TextBlock _status = Theme.Text("", 12, true);
    private readonly TextBlock _zoomLabel = Theme.Text("100%", 12);
    private readonly TextBlock _dimensions = Theme.Text("", 11, true);
    private readonly TextBlock _contextHint = Theme.Text("", 12, true);
    private readonly TextBlock _contextTitle = Theme.Text("Select", 12);
    private readonly TextBlock _layerHint = Theme.Text("Draw on the image to add an annotation.", 12, true);
    private readonly Dictionary<ToolKind, Button> _toolButtons = new();
    private readonly List<Button> _colorButtons = new();
    private readonly ComboBox _stroke = new();
    private readonly ComboBox _font = new();
    private readonly CheckBox _fill = new() { Content = "Fill", VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(8, 0, 12, 0) };
    private readonly TextBox _hex = new() { Width = 82, MaxLength = 7, ToolTip = "Opaque color as #RRGGBB" };
    private readonly TextBox _text = new() { Text = "Text", MinWidth = 130, Width = 200, MaxLength = 4000, ToolTip = "Text content. Press Enter or Apply text to update the selected text." };
    private readonly WrapPanel _textOptions = new();
    private readonly StackPanel _colorOptions = new() { Orientation = Orientation.Horizontal };
    private readonly StackPanel _strokeOptions = new() { Orientation = Orientation.Horizontal };
    private readonly Button _undo;
    private readonly Button _redo;
    private readonly Button _saveButton;
    private readonly Button _saveMore;
    private readonly Button _copyButton;
    private readonly Button _pinButton;
    private readonly Button _deleteButton;
    private readonly Button _resetCrop;
    private readonly Button _layerButton;
    private readonly ColumnDefinition _layerColumn = new() { Width = new GridLength(0) };
    private readonly Border _sidebarBorder;
    private bool _showLayers;
    private ToolKind _tool { get => _viewModel.SelectedTool; set => _viewModel.SelectedTool = value; }
    private Color _color = Color.FromRgb(230, 67, 64);
    private double _strokeWidth = 4;
    private double _fontSize = 24;
    private double _zoom = 1;
    private bool _fit = true;
    private bool _syncing;
    private bool _busy { get => _viewModel.IsBusy; set => _viewModel.IsBusy = value; }
    private bool _closing;
    private bool _allowClose;
    private Guid? _selectedId { get => _viewModel.SelectedAnnotationId; set => _viewModel.SelectedAnnotationId = value; }
    private Gesture _gesture;
    private DocumentState? _before;
    private DocumentState? _preview;
    private Annotation? _original;
    private Point _start;
    private Point _panStart;
    private double _panHorizontal;
    private double _panVertical;
    private int _handle;
    private readonly List<Point> _points = new();
    private Rect? _cropPreview;

    public EditorWindow(EditDocument document, Func<EditDocument, Task<bool>> save,
        Func<EditDocument, Task> copy, Action<BitmapSource> pin, Func<EditDocument,Task<bool>>? saveAs = null)
    {
        _viewModel = new DocumentViewModel(document);
        DataContext = _viewModel;
        _save = save;
        _saveAs = saveAs ?? save;
        _copy = copy;
        _pin = pin;
        SetBinding(TitleProperty, new System.Windows.Data.Binding(nameof(DocumentViewModel.Title)));
        Rect workArea = SystemParameters.WorkArea;
        MinWidth = Math.Min(640, Math.Max(320, workArea.Width - 32));
        MinHeight = Math.Min(480, Math.Max(320, workArea.Height - 32));
        Width = Math.Max(MinWidth, Math.Min(1240, workArea.Width - 32));
        Height = Math.Max(MinHeight, Math.Min(820, workArea.Height - 32));
        WindowStartupLocation = WindowStartupLocation.CenterScreen;
        Theme.Apply(this);
        Activated+=(_,_)=>ImageFiles.RefreshSavedState(_document);

        _surface = new DrawingSurface(this) { Focusable = true, SnapsToDevicePixels = true };
        AutomationProperties.SetName(_surface, "Screenshot editing canvas");
        _scroll = new ScrollViewer
        {
            Content = new Border { Child = _surface, Margin = new Thickness(20), BorderBrush = Theme.BorderBrush, BorderThickness = new Thickness(1) },
            HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            HorizontalContentAlignment = HorizontalAlignment.Center,
            VerticalContentAlignment = VerticalAlignment.Center,
            Focusable = false
        };
        _scroll.SetResourceReference(BackgroundProperty, "Canvas");

        var root = new DockPanel { LastChildFill = true };
        var header = new Grid { Margin = new Thickness(16, 10, 16, 10) };
        header.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        header.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        header.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        var identity = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };
        var documentIdentity = new StackPanel { MinWidth = 86, Margin = new Thickness(0, 0, 12, 0) };
        var wordmark = Theme.Text("SlipShot", 15);
        wordmark.FontWeight = FontWeights.SemiBold;
        documentIdentity.Children.Add(wordmark);
        _dimensions.TextWrapping = TextWrapping.NoWrap;
        _dimensions.Margin = new Thickness(0, 2, 0, 0);
        documentIdentity.Children.Add(_dimensions);
        identity.Children.Add(documentIdentity);
        _undo = Theme.IconButton("Undo (Ctrl+Z)", "M 6,4 L 2,8 L 6,12 M 2,8 L 10,8 C 15,8 15,15 10,15", () => { CancelGesture(); _document.Undo(); }, size: 32);
        _redo = Theme.IconButton("Redo (Ctrl+Y)", "M 10,4 L 14,8 L 10,12 M 14,8 L 6,8 C 1,8 1,15 6,15", () => { CancelGesture(); _document.Redo(); }, size: 32);
        _undo.Margin = _redo.Margin = new Thickness(0);
        identity.Children.Add(_undo);
        identity.Children.Add(_redo);
        _undo.SetBinding(IsEnabledProperty, new System.Windows.Data.Binding(nameof(DocumentViewModel.CanUndo)));
        _redo.SetBinding(IsEnabledProperty, new System.Windows.Data.Binding(nameof(DocumentViewModel.CanRedo)));
        header.Children.Add(identity);

        var zoomControls = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(8, 0, 8, 0) };
        var zoomOut = Theme.IconButton("Zoom out (Ctrl+minus)", "M 3,8 L 13,8", () => SetZoom(_zoom / 1.25), size: 28);
        zoomOut.Margin = new Thickness(0);
        zoomControls.Children.Add(zoomOut);
        _zoomLabel.VerticalAlignment = VerticalAlignment.Center;
        _zoomLabel.Width = 44;
        _zoomLabel.TextAlignment = TextAlignment.Center;
        _zoomLabel.TextWrapping = TextWrapping.NoWrap;
        _zoomLabel.ToolTip = "Image pixels relative to screen pixels";
        zoomControls.Children.Add(_zoomLabel);
        var zoomIn = Theme.IconButton("Zoom in (Ctrl+plus)", "M 3,8 L 13,8 M 8,3 L 8,13", () => SetZoom(_zoom * 1.25), size: 28);
        zoomIn.Margin = new Thickness(0, 0, 6, 0);
        zoomControls.Children.Add(zoomIn);
        var fitButton = AddButton(zoomControls, "Fit", FitImage, "Fit image to the canvas (Ctrl+0)");
        var actualSizeButton = AddButton(zoomControls, "100%", ActualSize, "One image pixel per screen pixel (Ctrl+1)");
        foreach (var button in new[] { fitButton, actualSizeButton })
        {
            button.Padding = new Thickness(8, 4, 8, 4);
            button.MinHeight = 30;
            button.FontSize = 12;
            button.Margin = new Thickness(0);
            button.BorderThickness = new Thickness(0);
            button.Background = Brushes.Transparent;
        }
        Grid.SetColumn(zoomControls, 1);
        header.Children.Add(zoomControls);
        var exports = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right, VerticalAlignment = VerticalAlignment.Center };
        _copyButton = AddButton(exports, "Copy", () => _ = CopyAsync(), "Copy flattened image (Ctrl+C)");
        _saveButton = AddButton(exports, "Save", () => _ = SaveAsync(), "Save PNG to your SlipShot folder (Ctrl+S)", true);
        var saveMenu=new ContextMenu();
        var saveAsItem=new MenuItem {Header="Save a copy as…",InputGestureText="Ctrl+Shift+S"};
        saveAsItem.Click+=(_,_)=>_ = SaveAsync(true);saveMenu.Items.Add(saveAsItem);
        _saveMore=Theme.IconButton("Save a separate copy…","M 3,5 L 8,10 L 13,5",()=>saveMenu.IsOpen=true,size:28);
        saveMenu.PlacementTarget=_saveMore;_saveMore.ContextMenu=saveMenu;exports.Children.Add(_saveMore);
        _copyButton.Padding = _saveButton.Padding = new Thickness(14, 7, 14, 7);
        _copyButton.Margin = new Thickness(0, 0, 6, 0);
        _saveButton.Margin = new Thickness(0);
        Grid.SetColumn(exports, 2);
        header.Children.Add(exports);
        var headerBorder = new Border { Child = header, Background = Theme.PanelBrush, BorderBrush = Theme.BorderBrush, BorderThickness = new Thickness(0, 0, 0, 1) };
        DockPanel.SetDock(headerBorder, Dock.Top);
        root.Children.Add(headerBorder);

        var statusBorder = new Border { Background = Theme.PanelBrush, BorderBrush = Theme.BorderBrush, BorderThickness = new Thickness(0, 1, 0, 0), Padding = new Thickness(16, 7, 16, 7) };
        _status.TextTrimming = TextTrimming.CharacterEllipsis;
        _status.TextWrapping = TextWrapping.NoWrap;
        statusBorder.Child = _status;
        DockPanel.SetDock(statusBorder, Dock.Bottom);
        root.Children.Add(statusBorder);

        var rail = new DockPanel { LastChildFill = true, Width = 112, Margin = new Thickness(6, 8, 6, 8) };
        var railFooter = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Center, Margin = new Thickness(0, 8, 0, 0) };
        _pinButton = Theme.IconButton("Pin a flattened copy above other windows", "M 5,2 L 11,2 M 6,2 L 6,7 L 3,10 L 13,10 L 10,7 L 10,2 M 8,10 L 8,15", Pin);
        _layerButton = Theme.IconButton("Show annotations", "M 2,5 L 8,2 L 14,5 L 8,8 Z M 2,8 L 8,11 L 14,8 M 2,11 L 8,14 L 14,11", () => { _showLayers = !_showLayers; UpdateLayerVisibility(); });
        _pinButton.Margin = new Thickness(0, 0, 8, 0);
        _layerButton.Margin = new Thickness(0);
        railFooter.Children.Add(_pinButton);
        railFooter.Children.Add(_layerButton);
        DockPanel.SetDock(railFooter, Dock.Bottom);
        rail.Children.Add(railFooter);
        var tools = new WrapPanel { Width = 112 };
        foreach (var item in new (ToolKind Kind, string Label, string Key)[]
        {
            (ToolKind.Select, "Select", "V"), (ToolKind.Crop, "Crop", "C"),
            (ToolKind.Arrow, "Arrow", "A"), (ToolKind.Line, "Line", "L"),
            (ToolKind.Rectangle, "Box", "R"), (ToolKind.Ellipse, "Ellipse", "E"),
            (ToolKind.Pen, "Pen", "P"), (ToolKind.Highlighter, "Marker", "H"),
            (ToolKind.Text, "Text", "T"), (ToolKind.Step, "Step", "N"),
            (ToolKind.Redaction, "Redact", "B"), (ToolKind.Pan, "Pan", "Space / hand tool")
        })
        {
            ToolKind kind = item.Kind;
            var button = AddButton(tools, item.Label, () => ChooseTool(kind), $"{item.Label} ({item.Key})");
            button.Width = 54;
            button.Height = 46;
            button.Padding = new Thickness(2, 4, 2, 3);
            button.Margin = new Thickness(1, 0, 1, 2);
            button.BorderThickness = new Thickness(1);
            button.BorderBrush = Brushes.Transparent;
            button.Background = Brushes.Transparent;
            var content = new StackPanel { HorizontalAlignment = HorizontalAlignment.Center };
            content.Children.Add(new ToolIcon(kind) { Width = 16, Height = 16, Margin = new Thickness(0, 0, 0, 4), IsHitTestVisible = false });
            var label = Theme.Text(item.Label, 11);
            label.TextWrapping = TextWrapping.NoWrap;
            label.HorizontalAlignment = HorizontalAlignment.Center;
            content.Children.Add(label);
            button.Content = content;
            _toolButtons.Add(kind, button);
        }
        rail.Children.Add(new ScrollViewer { Content = tools, VerticalScrollBarVisibility = ScrollBarVisibility.Hidden, HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled, Focusable = false });
        var railBorder = new Border { Child = rail, Background = Theme.PanelBrush, BorderBrush = Theme.BorderBrush, BorderThickness = new Thickness(0, 0, 1, 0) };
        DockPanel.SetDock(railBorder, Dock.Left);
        root.Children.Add(railBorder);

        var workspace = new Grid();
        workspace.ColumnDefinitions.Add(new ColumnDefinition());
        workspace.ColumnDefinitions.Add(_layerColumn);
        var imageArea = new DockPanel();
        var properties = new WrapPanel { Margin = new Thickness(12, 5, 12, 5), MinHeight = 32, VerticalAlignment = VerticalAlignment.Center };
        _contextTitle.FontWeight = FontWeights.SemiBold;
        _contextTitle.VerticalAlignment = VerticalAlignment.Center;
        _contextTitle.Margin = new Thickness(0, 0, 12, 0);
        properties.Children.Add(_contextTitle);
        _contextHint.VerticalAlignment = VerticalAlignment.Center;
        _contextHint.TextWrapping = TextWrapping.NoWrap;
        _contextHint.TextTrimming = TextTrimming.CharacterEllipsis;
        properties.Children.Add(_contextHint);
        foreach (var swatch in new[] { "#E64340", "#F2AD32", "#F5E663", "#2C9D72", "#378FE7", "#FFFFFF", "#111111" })
        {
            var color = (Color)ColorConverter.ConvertFromString(swatch);
            var button = Theme.Button("", () => SetColor(color));
            button.Width = 22;
            button.Height = 30;
            button.MinHeight = 30;
            button.MinWidth = 0;
            button.Padding = new Thickness(2);
            button.Margin = new Thickness(0, 1, 1, 1);
            button.Background = Brushes.Transparent;
            button.BorderBrush = Brushes.Transparent;
            button.ToolTip = "Use " + swatch;
            button.Tag = color;
            button.Content = new Border { Background = new SolidColorBrush(color), BorderBrush = Theme.BorderBrush, BorderThickness = new Thickness(1), Width = 16, Height = 16, CornerRadius = new CornerRadius(3) };
            AutomationProperties.SetName(button, "Color " + swatch);
            _colorButtons.Add(button);
            _colorOptions.Children.Add(button);
        }
        StyleInput(_hex, "Hex color");
        _hex.KeyDown += (_, e) => { if (e.Key == Key.Enter) { ApplyHex(); e.Handled = true; } };
        _hex.LostKeyboardFocus += (_, _) => ApplyHex();
        _hex.Width = 78;
        _hex.FontSize = 12;
        _hex.Margin = new Thickness(4, 1, 8, 1);
        _colorOptions.Children.Add(_hex);
        properties.Children.Add(_colorOptions);
        AddLabel(_strokeOptions, "Stroke");
        SetupCombo(_stroke, new double[] { 1, 2, 4, 6, 8, 12, 20, 32 }, 4, "Stroke width in image pixels");
        _stroke.SelectionChanged += (_, _) =>
        {
            if (_syncing || _stroke.SelectedItem is not double width) return;
            _strokeWidth = width;
            ChangeSelected(a => a with { StrokeWidth = width }, a => a.Kind is not (ToolKind.Text or ToolKind.Redaction or ToolKind.Step));
        };
        _strokeOptions.Children.Add(_stroke);
        properties.Children.Add(_strokeOptions);
        _fill.Foreground = Theme.TextBrush;
        _fill.Checked += (_, _) => ApplyFill();
        _fill.Unchecked += (_, _) => ApplyFill();
        properties.Children.Add(_fill);
        _textOptions.VerticalAlignment = VerticalAlignment.Center;
        AddLabel(_textOptions, "Font");
        SetupCombo(_font, new double[] { 12, 16, 20, 24, 32, 40, 48, 64, 96 }, 24, "Text size in image pixels");
        _font.SelectionChanged += (_, _) =>
        {
            if (_syncing || _font.SelectedItem is not double size) return;
            _fontSize = size;
            ChangeSelected(a => SizeText(a with { FontSize = size }), a => a.Kind == ToolKind.Text);
        };
        _textOptions.Children.Add(_font);
        StyleInput(_text, "Annotation text");
        _text.KeyDown += (_, e) => { if (e.Key == Key.Enter) { ApplyText(); e.Handled = true; } };
        _textOptions.Children.Add(_text);
        _text.Width = 140;
        var applyText = Theme.IconButton("Apply text to the selected annotation", "M 3,8 L 6,11 L 13,4", ApplyText, size: 32);
        applyText.Margin = new Thickness(0);
        _textOptions.Children.Add(applyText);
        properties.Children.Add(_textOptions);
        _deleteButton = Theme.IconButton("Delete selected annotation (Delete)", "M 3,4 L 13,4 M 6,4 L 6,2 L 10,2 L 10,4 M 5,6 L 5,13 L 11,13 L 11,6 M 7,7 L 7,11 M 9,7 L 9,11", DeleteSelected, size: 32);
        _deleteButton.Margin = new Thickness(8, 0, 0, 0);
        properties.Children.Add(_deleteButton);
        _resetCrop = AddButton(properties, "Reset crop", () => { CancelGesture(); Commit(new DocumentState(ImageBounds, _document.State.Annotations)); }, "Restore the full image bounds");
        _resetCrop.Padding = new Thickness(10, 4, 10, 4);
        var propertyBorder = new Border { Child = properties, Background = Theme.PanelBrush, BorderBrush = Theme.BorderBrush, BorderThickness = new Thickness(0, 0, 0, 1) };
        DockPanel.SetDock(propertyBorder, Dock.Top);
        imageArea.Children.Add(propertyBorder);
        imageArea.Children.Add(_scroll);
        workspace.Children.Add(imageArea);
        var sidebar = new DockPanel { LastChildFill = true, Margin = new Thickness(12) };
        var layerHeading = Theme.Text("Annotations", 16);
        layerHeading.FontSize = 13;
        layerHeading.FontWeight = FontWeights.SemiBold;
        layerHeading.Margin = new Thickness(0, 4, 0, 16);
        DockPanel.SetDock(layerHeading, Dock.Top);
        sidebar.Children.Add(layerHeading);
        var layerActions = new StackPanel();
        var hint = Theme.Text("Select to move. Drag a handle to resize. Arrow keys nudge; Shift moves 10 px. Hold Space to pan.", 12, true);
        hint.TextWrapping = TextWrapping.Wrap;
        layerActions.Children.Add(hint);
        DockPanel.SetDock(layerActions, Dock.Bottom);
        sidebar.Children.Add(layerActions);
        _layerHint.TextWrapping = TextWrapping.Wrap;
        _layerHint.Margin = new Thickness(4, 8, 4, 8);
        DockPanel.SetDock(_layerHint, Dock.Top);
        sidebar.Children.Add(_layerHint);
        _layers.Background = Theme.PanelBrush;
        _layers.Foreground = Theme.TextBrush;
        _layers.BorderThickness = new Thickness(0);
        _layers.HorizontalContentAlignment = HorizontalAlignment.Stretch;
        ScrollViewer.SetHorizontalScrollBarVisibility(_layers, ScrollBarVisibility.Disabled);
        _layers.SelectionChanged += (_, _) =>
        {
            if (_syncing) return;
            CancelGesture();
            _selectedId = (_layers.SelectedItem as ListBoxItem)?.Tag as Guid?;
            _tool = ToolKind.Select;
            RefreshProperties();
            _surface.InvalidateVisual();
        };
        AutomationProperties.SetName(_layers, "Annotations, topmost first");
        sidebar.Children.Add(_layers);
        _sidebarBorder = new Border { Child = sidebar, Background = Theme.PanelBrush, BorderBrush = Theme.BorderBrush, BorderThickness = new Thickness(1, 0, 0, 0) };
        Grid.SetColumn(_sidebarBorder, 1);
        workspace.Children.Add(_sidebarBorder);
        root.Children.Add(workspace);
        Content = root;
        UpdateLayerVisibility();

        _surface.MouseDown += OnMouseDown;
        _surface.MouseMove += OnMouseMove;
        _surface.MouseUp += OnMouseUp;
        _surface.LostMouseCapture += (_, _) => { if (_gesture != Gesture.None) CancelGesture(); };
        _scroll.PreviewMouseWheel += (_, e) =>
        {
            if ((Keyboard.Modifiers & ModifierKeys.Control) == 0) return;
            SetZoom(_zoom * (e.Delta > 0 ? 1.25 : 0.8));
            e.Handled = true;
        };
        PreviewKeyDown += OnKeyDown;
        _scroll.SizeChanged += (_, _) => { if (_fit) FitImage(); };
        _document.Changed += DocumentChanged;
        Closing += OnClosing;
        Closed += (_, _) => { CancelGesture(); _document.Changed -= DocumentChanged; _viewModel.Dispose(); };
        Loaded += (_, _) => { FitImage(); Theme.AnimateEntrance(this); _surface.Focus(); };
        DpiChanged += (_, e) =>
        {
            if (_fit) FitImage();
            else SetZoom(_zoom * e.OldDpi.DpiScaleX / e.NewDpi.DpiScaleX);
        };
        DocumentChanged();
    }

    private Rect ImageBounds => new(0, 0, _document.Source.PixelWidth, _document.Source.PixelHeight);
    private Annotation? Selected => _document.State.Annotations.FirstOrDefault(a => a.Id == _selectedId);
    private DocumentState VisibleState => _preview ?? _document.State;
    public bool ClosePending => _allowClose || _closing;

    private void UpdateLayerVisibility()
    {
        _layerColumn.Width = new GridLength(_showLayers ? 190 : 0);
        _sidebarBorder.Visibility = _showLayers ? Visibility.Visible : Visibility.Collapsed;
        _layerButton.SetResourceReference(Control.BackgroundProperty, _showLayers ? "Selected" : "Panel");
        _layerButton.ToolTip = _showLayers ? "Hide annotations" : "Show annotations";
        AutomationProperties.SetName(_layerButton, _showLayers ? "Hide annotations" : "Show annotations");
    }

    private static Button AddButton(Panel parent, string text, Action action, string tip, bool primary = false)
    {
        var button = Theme.Button(text, action, primary);
        button.Margin = new Thickness(0, 2, 6, 2);
        button.ToolTip = tip;
        AutomationProperties.SetName(button, text.Replace("…", ""));
        parent.Children.Add(button);
        return button;
    }

    private static void AddLabel(Panel parent, string label)
    {
        var text = Theme.Text(label, 12, true);
        text.VerticalAlignment = VerticalAlignment.Center;
        text.Margin = new Thickness(8, 0, 6, 0);
        parent.Children.Add(text);
    }

    private static void StyleInput(Control control, string name)
    {
        control.Margin = new Thickness(0, 2, 6, 2);
        control.Padding = new Thickness(6, 4, 6, 4);
        control.VerticalContentAlignment = VerticalAlignment.Center;
        control.Background = Theme.BackgroundBrush;
        control.Foreground = Theme.TextBrush;
        control.BorderBrush = Theme.BorderBrush;
        AutomationProperties.SetName(control, name);
    }

    private static void SetupCombo(ComboBox combo, double[] values, double selected, string name)
    {
        combo.ItemsSource = values;
        combo.SelectedItem = selected;
        combo.Width = 62;
        combo.Margin = new Thickness(0, 2, 6, 2);
        combo.Padding = new Thickness(4);
        combo.ToolTip = name;
        AutomationProperties.SetName(combo, name);
    }

    private void ChooseTool(ToolKind tool)
    {
        CancelGesture();
        _tool = tool;
        if (tool is not ToolKind.Select) _selectedId = null;
        RefreshLayers();
        RefreshProperties();
        _surface.Focus();
        ShowStatus();
        _surface.InvalidateVisual();
    }

    private void DocumentChanged()
    {
        RefreshSaveState();
        if (_selectedId is not null && Selected is null) _selectedId = null;
        _resetCrop.IsEnabled = !_busy && _document.State.Crop != ImageBounds;
        var crop = _document.State.Crop;
        _dimensions.Text = $"{crop.Width:0} × {crop.Height:0} px";
        RefreshLayers();
        RefreshProperties();
        if (_fit) FitImage(); else ResizeSurface();
        ShowStatus();
        _surface.InvalidateVisual();
    }

    private void RefreshLayers()
    {
        _syncing = true;
        _layers.Items.Clear();
        foreach (var annotation in _document.State.Annotations.OrderBy(a => a.Kind == ToolKind.Redaction ? 1 : 0).Reverse())
        {
            string label = annotation.Kind switch
            {
                ToolKind.Text => "Text · " + annotation.Text,
                ToolKind.Step => "Step " + annotation.Number,
                ToolKind.Redaction => "Opaque redaction",
                _ => annotation.Kind.ToString()
            };
            var text = Theme.Text(label, 12);
            text.TextWrapping = TextWrapping.NoWrap;
            text.TextTrimming = TextTrimming.CharacterEllipsis;
            text.SetBinding(TextBlock.ForegroundProperty, new System.Windows.Data.Binding("Foreground")
            {
                RelativeSource = new System.Windows.Data.RelativeSource(System.Windows.Data.RelativeSourceMode.FindAncestor, typeof(ListBoxItem), 1)
            });
            var item = new ListBoxItem { Content = text, Tag = annotation.Id, Padding = new Thickness(8), ToolTip = label, IsSelected = annotation.Id == _selectedId };
            AutomationProperties.SetName(item, label);
            _layers.Items.Add(item);
        }
        _layerHint.Visibility = _layers.Items.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
        _syncing = false;
    }

    private void RefreshProperties()
    {
        _syncing = true;
        var selected = Selected;
        if (selected is not null)
        {
            _color = selected.Color;
            _strokeWidth = selected.StrokeWidth;
            _fontSize = selected.FontSize;
            _fill.IsChecked = selected.Filled;
            if (selected.Kind == ToolKind.Text) _text.Text = selected.Text;
        }
        _hex.Text = $"#{_color.R:X2}{_color.G:X2}{_color.B:X2}";
        _stroke.SelectedItem = _strokeWidth;
        if (!_font.Items.Contains(_fontSize)) _font.ItemsSource = new double[] { 12, 16, 20, 24, 32, 40, 48, 64, 96, _fontSize }.Distinct().OrderBy(size => size).ToArray();
        _font.SelectedItem = _fontSize;
        var kind = selected?.Kind ?? _tool;
        bool canColor = kind is not (ToolKind.Select or ToolKind.Crop or ToolKind.Pan or ToolKind.Redaction);
        bool canStroke = kind is ToolKind.Arrow or ToolKind.Line or ToolKind.Rectangle or ToolKind.Ellipse or ToolKind.Pen or ToolKind.Highlighter;
        _colorOptions.Visibility = canColor ? Visibility.Visible : Visibility.Collapsed;
        _strokeOptions.Visibility = canStroke ? Visibility.Visible : Visibility.Collapsed;
        _fill.Visibility = kind is ToolKind.Rectangle or ToolKind.Ellipse ? Visibility.Visible : Visibility.Collapsed;
        _contextTitle.Visibility = canColor ? Visibility.Collapsed : Visibility.Visible;
        _contextTitle.Text = kind == ToolKind.Redaction ? "Redact" : kind.ToString();
        _contextHint.Visibility = canColor || selected is not null ? Visibility.Collapsed : Visibility.Visible;
        _contextHint.Text = kind switch
        {
            ToolKind.Select => "Click an annotation to adjust it",
            ToolKind.Crop => "Drag to crop · Esc cancels",
            ToolKind.Pan => "Drag to move around the image",
            ToolKind.Redaction => "Draw an opaque block",
            _ => ""
        };
        _fill.IsEnabled = !_busy && kind is ToolKind.Rectangle or ToolKind.Ellipse;
        _stroke.IsEnabled = !_busy && canStroke;
        _textOptions.Visibility = kind == ToolKind.Text ? Visibility.Visible : Visibility.Collapsed;
        _deleteButton.Visibility = selected is not null ? Visibility.Visible : Visibility.Collapsed;
        _deleteButton.IsEnabled = !_busy && selected is not null;
        _resetCrop.Visibility = _document.State.Crop != ImageBounds && kind is ToolKind.Select or ToolKind.Crop ? Visibility.Visible : Visibility.Collapsed;
        _hex.IsEnabled = !_busy && kind != ToolKind.Redaction;
        foreach (var colorButton in _colorButtons) colorButton.IsEnabled = !_busy && kind != ToolKind.Redaction;
        if (kind == ToolKind.Redaction) _hex.Text = "#000000";
        foreach (var pair in _toolButtons)
        {
            pair.Value.BorderBrush = pair.Key == _tool ? Theme.FocusBrush : Brushes.Transparent;
            if (pair.Key == _tool) pair.Value.SetResourceReference(Control.BackgroundProperty, "Selected");
            else pair.Value.Background = Brushes.Transparent;
            pair.Value.BorderThickness = new Thickness(1);
            AutomationProperties.SetHelpText(pair.Value, pair.Key == _tool ? "Selected tool" : "");
        }
        foreach (var button in _colorButtons)
            button.BorderBrush = button.Tag is Color color && color == _color ? Theme.FocusBrush : Brushes.Transparent;
        _surface.Cursor = _tool switch { ToolKind.Pan => Cursors.Hand, ToolKind.Select => Cursors.Arrow, ToolKind.Text => Cursors.IBeam, _ => Cursors.Cross };
        _syncing = false;
    }

    private void SetColor(Color color)
    {
        _color = Color.FromRgb(color.R, color.G, color.B);
        ChangeSelected(a => a with { Color = _color });
        RefreshProperties();
    }

    private void ApplyHex()
    {
        if (_syncing) return;
        string value = _hex.Text.Trim().TrimStart('#');
        if (value.Length != 6 || !uint.TryParse(value, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out uint rgb))
        {
            _status.Text = "Enter a color as six hexadecimal digits, for example #E64340.";
            _hex.BorderBrush = Brushes.IndianRed;
            return;
        }
        _hex.BorderBrush = Theme.BorderBrush;
        SetColor(Color.FromRgb((byte)(rgb >> 16), (byte)(rgb >> 8), (byte)rgb));
    }

    private void ApplyFill()
    {
        if (!_syncing) ChangeSelected(a => a with { Filled = _fill.IsChecked == true }, a => a.Kind is ToolKind.Rectangle or ToolKind.Ellipse);
    }

    private void ApplyText()
    {
        if (string.IsNullOrWhiteSpace(_text.Text)) { _status.Text = "Type some text before applying or placing it."; return; }
        ChangeSelected(a => SizeText(a with { Text = _text.Text }), a => a.Kind == ToolKind.Text);
        if (Selected?.Kind != ToolKind.Text) _status.Text = "Click the image with the Text tool to place this text.";
    }

    private Annotation SizeText(Annotation annotation)
    {
        var text = new FormattedText(annotation.Text, CultureInfo.CurrentCulture, FlowDirection.LeftToRight,
            new Typeface("Segoe UI"), annotation.FontSize, Brushes.Black, 1);
        // The export renderer clips text at the image/crop edge; retain its natural bounds for selection.
        return annotation with { Bounds = new Rect(annotation.Bounds.TopLeft, new Size(Math.Max(8, text.WidthIncludingTrailingWhitespace), Math.Max(8, text.Height))) };
    }

    private void ChangeSelected(Func<Annotation, Annotation> change, Func<Annotation, bool>? applies = null)
    {
        if (_syncing || _busy || Selected is not { } selected || (applies is not null && !applies(selected))) return;
        CancelGesture();
        var updated = change(selected);
        if (updated == selected) return;
        Commit(new DocumentState(_document.State.Crop,
            _document.State.Annotations.Select(a => a.Id == selected.Id ? updated : a).ToArray()));
    }

    private void DeleteSelected()
    {
        if (_busy || _selectedId is null) return;
        CancelGesture();
        var remaining = _document.State.Annotations.Where(a => a.Id != _selectedId).ToArray();
        _selectedId = null;
        Commit(new DocumentState(_document.State.Crop, remaining));
    }

    private bool Commit(DocumentState state)
    {
        try { _document.Commit(state); return true; }
        catch (ArgumentException ex)
        {
            RefreshLayers();
            RefreshProperties();
            ShowStatus("The edit could not be applied. " + ex.Message);
            return false;
        }
    }

    private Point ImagePoint(MouseEventArgs e)
    {
        var position = e.GetPosition(_surface);
        var crop = _document.State.Crop;
        return new Point(Math.Clamp(crop.X + position.X / _zoom, crop.Left, crop.Right),
            Math.Clamp(crop.Y + position.Y / _zoom, crop.Top, crop.Bottom));
    }

    private void OnMouseDown(object sender, MouseButtonEventArgs e)
    {
        if (_busy || _gesture != Gesture.None || (e.ChangedButton != MouseButton.Left && e.ChangedButton != MouseButton.Middle)) return;
        _surface.Focus();
        if (e.ChangedButton == MouseButton.Middle || _tool == ToolKind.Pan || Keyboard.IsKeyDown(Key.Space))
        {
            _gesture = Gesture.Pan;
            _panStart = e.GetPosition(_scroll);
            _panHorizontal = _scroll.HorizontalOffset;
            _panVertical = _scroll.VerticalOffset;
            BeginCapture();
            e.Handled = true;
            return;
        }
        _start = ImagePoint(e);
        _before = _document.State;
        _points.Clear();
        _points.Add(_start);
        if (_tool == ToolKind.Select)
        {
            _handle = Selected is { } old ? HandleAt(old.Bounds, _start) : -1;
            if (_handle >= 0) { _original = Selected; _gesture = Gesture.Resize; }
            else
            {
                _original = _document.State.Annotations.OrderBy(a => a.Kind == ToolKind.Redaction ? 1 : 0)
                    .LastOrDefault(a => GeometryUtil.HitTest(a, _start, 6 / _zoom));
                _selectedId = _original?.Id;
                if (_original is not null) _gesture = Gesture.Move;
            }
            RefreshLayers();
            RefreshProperties();
            _surface.InvalidateVisual();
        }
        else if (_tool == ToolKind.Text || _tool == ToolKind.Step)
        {
            if (_tool == ToolKind.Text && string.IsNullOrWhiteSpace(_text.Text)) { _status.Text = "Type text in the text field before placing it."; return; }
            if (!CanAddAnnotation()) return;
            var bounds = _tool == ToolKind.Step ? new Rect(_start.X - 18, _start.Y - 18, 36, 36) : new Rect(_start, new Size(1, 1));
            var annotation = new Annotation(Guid.NewGuid(), _tool, bounds, _color, _strokeWidth,
                Text: _text.Text, FontSize: _fontSize,
                Number: _document.State.Annotations.Where(a => a.Kind == ToolKind.Step).Select(a => a.Number).DefaultIfEmpty(0).Max() + 1);
            if (_tool == ToolKind.Text) annotation = SizeText(annotation);
            _selectedId = annotation.Id;
            if (_tool == ToolKind.Text) _tool = ToolKind.Select;
            Commit(new DocumentState(_document.State.Crop, _document.State.Annotations.Append(annotation).ToArray()));
            e.Handled = true;
            return;
        }
        else
        {
            if (_tool != ToolKind.Crop && !CanAddAnnotation()) return;
            _selectedId = null;
            _gesture = _tool == ToolKind.Crop ? Gesture.Crop : Gesture.Draw;
            _original = new Annotation(Guid.NewGuid(), _tool, new Rect(_start, new Size(0, 0)), _color,
                _tool == ToolKind.Highlighter ? Math.Max(12, _strokeWidth) : _strokeWidth,
                Filled: _fill.IsChecked == true, FontSize: _fontSize);
        }
        if (_gesture != Gesture.None) BeginCapture();
        e.Handled = true;
    }

    private bool CanAddAnnotation()
    {
        if (_document.State.Annotations.Count < 1000) return true;
        _status.Text = "This image has 1,000 annotations. Delete an annotation before adding another.";
        return false;
    }

    private void BeginCapture()
    {
        if (!_surface.CaptureMouse()) CancelGesture();
    }

    private void OnMouseMove(object sender, MouseEventArgs e)
    {
        if (_gesture == Gesture.None)
        {
            if (_tool == ToolKind.Select && Selected is { } selected)
            {
                int handle = HandleAt(selected.Bounds, ImagePoint(e));
                _surface.Cursor = handle switch { 0 or 4 => Cursors.SizeNWSE, 2 or 6 => Cursors.SizeNESW, 1 or 5 => Cursors.SizeNS, 3 or 7 => Cursors.SizeWE, _ => Cursors.Arrow };
            }
            return;
        }
        UpdateGesture(e);
        e.Handled = true;
    }

    private void UpdateGesture(MouseEventArgs e)
    {
        if (_gesture == Gesture.Pan)
        {
            var delta = e.GetPosition(_scroll) - _panStart;
            _scroll.ScrollToHorizontalOffset(_panHorizontal - delta.X);
            _scroll.ScrollToVerticalOffset(_panVertical - delta.Y);
            return;
        }
        if (_before is null) return;
        var point = ImagePoint(e);
        if (_gesture == Gesture.Crop) _cropPreview = GeometryUtil.Normalize(_start, point, _before.Crop);
        else if (_original is not null)
        {
            Annotation annotation;
            if (_gesture == Gesture.Move)
                annotation = GeometryUtil.Move(_original, point - _start);
            else if (_gesture == Gesture.Resize)
            {
                Rect resized = ResizeBounds(_original.Bounds, _handle, point);
                annotation = GeometryUtil.Resize(_original, resized);
                if (annotation.Kind == ToolKind.Text)
                {
                    double ratio = _handle is 3 or 7 ? resized.Width / Math.Max(1, _original.Bounds.Width)
                        : resized.Height / Math.Max(1, _original.Bounds.Height);
                    annotation = SizeText(annotation with { FontSize = Math.Clamp(_original.FontSize * ratio, 1, 512) });
                }
            }
            else
            {
                IReadOnlyList<Point>? points = null;
                Rect bounds = GeometryUtil.Normalize(_start, point, _before.Crop);
                if (_tool is ToolKind.Pen or ToolKind.Highlighter)
                {
                    int remaining = Math.Min(50000, 100000 - _before.Annotations.Sum(a => a.Points?.Count ?? 0));
                    if ((point - _points[^1]).Length >= Math.Min(1, 1 / _zoom) && _points.Count < remaining) _points.Add(point);
                    if (_points.Count >= remaining) ShowStatus("Stroke detail limit reached. Release to finish; delete an older stroke to free space.");
                    points = _points.ToArray();
                    bounds = new Rect(new Point(_points.Min(p => p.X), _points.Min(p => p.Y)),
                        new Point(_points.Max(p => p.X), _points.Max(p => p.Y)));
                }
                else if (_tool is ToolKind.Arrow or ToolKind.Line) points = new[] { _start, point };
                annotation = _original with { Bounds = bounds, Points = points };
            }
            _preview = new DocumentState(_before.Crop, _gesture == Gesture.Draw
                ? _before.Annotations.Append(annotation).ToArray()
                : _before.Annotations.Select(a => a.Id == annotation.Id ? annotation : a).ToArray());
        }
        _surface.InvalidateVisual();
        if (_cropPreview is { } crop) _status.Text = $"Crop · {Math.Ceiling(crop.Width):0} × {Math.Ceiling(crop.Height):0} px · release to apply, Esc to cancel";
    }

    // Handles are clockwise, beginning at top left. Keep the opposite edge fixed.
    internal static Rect ResizeBounds(Rect bounds, int handle, Point point)
    {
        double left = bounds.Left, top = bounds.Top, right = bounds.Right, bottom = bounds.Bottom;
        if (handle is 0 or 6 or 7) left = Math.Min(point.X, right - 1);
        if (handle is 0 or 1 or 2) top = Math.Min(point.Y, bottom - 1);
        if (handle is 2 or 3 or 4) right = Math.Max(point.X, left + 1);
        if (handle is 4 or 5 or 6) bottom = Math.Max(point.Y, top + 1);
        return new Rect(new Point(left, top), new Point(right, bottom));
    }

    private void OnMouseUp(object sender, MouseButtonEventArgs e)
    {
        if (_gesture == Gesture.None || (e.ChangedButton != MouseButton.Left && e.ChangedButton != MouseButton.Middle)) return;
        UpdateGesture(e);
        var gesture = _gesture;
        var preview = _preview;
        var crop = _cropPreview;
        var original = _original;
        var before = _before;
        CancelGesture();
        if (gesture == Gesture.Crop && crop is { Width: >= 2, Height: >= 2 } rectangle && before is not null)
        {
            Commit(new DocumentState(rectangle, before.Annotations));
            _tool = ToolKind.Select;
            RefreshProperties();
        }
        else if (preview is not null && original is not null)
        {
            var changed = preview.Annotations.First(a => a.Id == original.Id);
            bool usefulDraw = changed.Kind is ToolKind.Pen or ToolKind.Highlighter
                ? changed.Points?.Count > 0
                : changed.Kind is ToolKind.Line or ToolKind.Arrow
                    ? (changed.Bounds.Width + changed.Bounds.Height) >= 1
                    : changed.Bounds.Width >= 1 && changed.Bounds.Height >= 1;
            bool changedGeometry = changed.Bounds != original.Bounds;
            if ((gesture == Gesture.Draw && usefulDraw) || (gesture is Gesture.Move or Gesture.Resize && changedGeometry))
            {
                _selectedId = changed.Id;
                Commit(preview);
            }
        }
        e.Handled = true;
    }

    private void CancelGesture()
    {
        bool active = _gesture != Gesture.None;
        _gesture = Gesture.None;
        _preview = null;
        _before = null;
        _original = null;
        _cropPreview = null;
        _points.Clear();
        if (_surface.IsMouseCaptured) _surface.ReleaseMouseCapture();
        _surface.InvalidateVisual();
        if (active)
        {
            RefreshLayers();
            RefreshProperties();
            ShowStatus();
        }
    }

    private static Point[] Handles(Rect r) => new[]
    {
        r.TopLeft, new Point(r.X + r.Width / 2, r.Top), r.TopRight,
        new Point(r.Right, r.Y + r.Height / 2), r.BottomRight,
        new Point(r.X + r.Width / 2, r.Bottom), r.BottomLeft,
        new Point(r.Left, r.Y + r.Height / 2)
    };

    private int HandleAt(Rect bounds, Point point)
    {
        var handles = Handles(bounds);
        for (int i = 0; i < handles.Length; i++)
            if (Math.Abs(handles[i].X - point.X) <= 6 / _zoom && Math.Abs(handles[i].Y - point.Y) <= 6 / _zoom) return i;
        return -1;
    }

    private void ResizeSurface()
    {
        _surface.Width = _document.State.Crop.Width * _zoom;
        _surface.Height = _document.State.Crop.Height * _zoom;
        double physicalZoom = _zoom * VisualTreeHelper.GetDpi(this).DpiScaleX;
        _zoomLabel.Text = $"{physicalZoom * 100:0}%";
        RenderOptions.SetBitmapScalingMode(_surface, physicalZoom >= 1 && Math.Abs(physicalZoom-Math.Round(physicalZoom))<0.001 ? BitmapScalingMode.NearestNeighbor : BitmapScalingMode.HighQuality);
        _surface.InvalidateVisual();
    }

    internal static double DipZoom(double physicalZoom, double dpiScale) => Math.Clamp(physicalZoom, 0.02, 8) / dpiScale;

    private void ActualSize() => SetZoom(DipZoom(1, VisualTreeHelper.GetDpi(this).DpiScaleX));

    private void FitImage()
    {
        _fit = true;
        if (_scroll.ActualWidth <= 42 || _scroll.ActualHeight <= 42) return;
        double dpiScale = VisualTreeHelper.GetDpi(this).DpiScaleX;
        double fitZoom = Math.Min((_scroll.ActualWidth - 42) / _document.State.Crop.Width,
            (_scroll.ActualHeight - 42) / _document.State.Crop.Height);
        _zoom = DipZoom(Math.Min(1,fitZoom * dpiScale), dpiScale);
        ResizeSurface();
    }

    private void SetZoom(double zoom)
    {
        CancelGesture();
        _fit = false;
        double oldZoom = _zoom;
        double centerX = (_scroll.HorizontalOffset + _scroll.ViewportWidth / 2) / oldZoom;
        double centerY = (_scroll.VerticalOffset + _scroll.ViewportHeight / 2) / oldZoom;
        double dpiScale = VisualTreeHelper.GetDpi(this).DpiScaleX;
        _zoom = DipZoom(zoom * dpiScale, dpiScale);
        ResizeSurface();
        _scroll.UpdateLayout();
        _scroll.ScrollToHorizontalOffset(centerX * _zoom - _scroll.ViewportWidth / 2);
        _scroll.ScrollToVerticalOffset(centerY * _zoom - _scroll.ViewportHeight / 2);
        ShowStatus();
    }

    private void ShowStatus(string? message = null)
    {
        int annotationCount = _document.State.Annotations.Count;
        _status.Text = message ?? $"{annotationCount} annotation{(annotationCount == 1 ? "" : "s")} · " + (_tool switch
        {
            ToolKind.Select => "Select an annotation to move or resize it",
            ToolKind.Crop => "Drag a crop area; release to apply · Esc cancels",
            ToolKind.Text => "Enter text above, then click to place it",
            ToolKind.Step => "Click to place the next numbered step",
            ToolKind.Redaction => "Drag to cover pixels with an opaque block",
            ToolKind.Pan => "Drag to pan the image",
            _ => "Drag to draw · Esc cancels · Ctrl+mouse wheel zooms"
        });
        _status.ToolTip = _status.Text;
    }

    private static bool IsEditingControl(DependencyObject? target)
    {
        while (target is not null)
        {
            if (target is TextBox or ComboBox or PasswordBox) return true;
            target = target is Visual ? VisualTreeHelper.GetParent(target) : LogicalTreeHelper.GetParent(target);
        }
        return false;
    }

    private void OnKeyDown(object sender, KeyEventArgs e)
    {
        if (IsEditingControl(e.OriginalSource as DependencyObject) || _busy) return;
        var modifiers = Keyboard.Modifiers;
        bool ctrl = (modifiers & ModifierKeys.Control) != 0;
        bool shift = (modifiers & ModifierKeys.Shift) != 0;
        if (e.Key == Key.Escape)
        {
            if (_gesture != Gesture.None) CancelGesture(); else { _selectedId = null; RefreshLayers(); RefreshProperties(); _surface.InvalidateVisual(); }
            ShowStatus();
        }
        else if (ctrl)
        {
            switch (e.Key)
            {
                case Key.S: _ = SaveAsync(shift); break;
                case Key.C: _ = CopyAsync(); break;
                case Key.Z: CancelGesture(); if (shift) _document.Redo(); else _document.Undo(); break;
                case Key.Y: CancelGesture(); _document.Redo(); break;
                case Key.D0: case Key.NumPad0: FitImage(); break;
                case Key.D1: case Key.NumPad1: ActualSize(); break;
                case Key.OemPlus: case Key.Add: SetZoom(_zoom * 1.25); break;
                case Key.OemMinus: case Key.Subtract: SetZoom(_zoom / 1.25); break;
                default: return;
            }
        }
        else if (e.Key == Key.Delete || e.Key == Key.Back) DeleteSelected();
        else if (e.Key is Key.Left or Key.Right or Key.Up or Key.Down)
        {
            if (Selected is null || _layers.IsKeyboardFocusWithin) return;
            double step = shift ? 10 : 1;
            Vector delta = e.Key switch { Key.Left => new Vector(-step, 0), Key.Right => new Vector(step, 0), Key.Up => new Vector(0, -step), _ => new Vector(0, step) };
            ChangeSelected(a => GeometryUtil.Move(a, delta));
        }
        else
        {
            ToolKind? tool = e.Key switch
            {
                Key.V => ToolKind.Select, Key.C => ToolKind.Crop, Key.A => ToolKind.Arrow,
                Key.L => ToolKind.Line, Key.R => ToolKind.Rectangle, Key.E => ToolKind.Ellipse,
                Key.P => ToolKind.Pen, Key.H => ToolKind.Highlighter, Key.T => ToolKind.Text,
                Key.N => ToolKind.Step, Key.B => ToolKind.Redaction, _ => null
            };
            if (tool is null) return;
            ChooseTool(tool.Value);
        }
        e.Handled = true;
    }

    private void SetBusy(bool busy)
    {
        _busy = busy;
        _saveButton.IsEnabled = _saveMore.IsEnabled = _copyButton.IsEnabled = _pinButton.IsEnabled = !busy;
        _surface.IsEnabled = _layers.IsEnabled = _hex.IsEnabled = _textOptions.IsEnabled = !busy;
        foreach (var button in _toolButtons.Values.Concat(_colorButtons)) button.IsEnabled = !busy;
        _resetCrop.IsEnabled = !busy && _document.State.Crop != ImageBounds;
        RefreshProperties();
        RefreshSaveState();
    }

    private void RefreshSaveState()
    {
        _saveButton.IsEnabled=!_busy&&!_document.IsSaved;
        if(_saveButton.Content is TextBlock label)label.Text=_document.IsSaved?"Saved":"Save";
        _saveButton.ToolTip=_document.IsSaved?"This version is already saved. Edit to enable Save, or choose Save a copy as.":"Save PNG to your SlipShot folder (Ctrl+S)";
        AutomationProperties.SetName(_saveButton,_document.IsSaved?"Image saved":"Save image");
    }

    private async Task<bool> SaveAsync(bool saveAs=false)
    {
        if (_busy) return false;
        CancelGesture();
        SetBusy(true);
        ShowStatus("Saving image…");
        try
        {
            bool saved = await (saveAs?_saveAs:_save)(_document);
            if (saved) _document.MarkSaved();
            ShowStatus(saved ? "Image saved." : "Image not saved. Your edits are still here.");
            return saved;
        }
        catch (Exception ex) { ShowStatus("Could not save the image. " + ex.Message); return false; }
        finally { SetBusy(false); }
    }

    private async Task CopyAsync()
    {
        if (_busy) return;
        CancelGesture();
        SetBusy(true);
        ShowStatus("Copying image…");
        try { await _copy(_document); ShowStatus(_document.IsSaved?"Image copied. This version is already saved.":"Flattened image copied. Save to keep a file."); }
        catch (Exception ex) { ShowStatus("Could not copy the image. " + ex.Message); }
        finally { SetBusy(false); }
    }

    private void Pin()
    {
        if (_busy) return;
        CancelGesture();
        try { _pin(DocumentRenderer.Render(_document)); ShowStatus("A flattened copy is pinned."); }
        catch (Exception ex) { ShowStatus("Could not pin the image. " + ex.Message); }
    }

    private async void OnClosing(object? sender, CancelEventArgs e)
    {
        if (_allowClose) return;
        if (_busy || _closing) { e.Cancel = true; return; }
        ImageFiles.RefreshSavedState(_document);
        CancelGesture();
        if (!_document.IsDirty) return;
        e.Cancel = true;
        _closing = true;
        try
        {
            var choice = ConfirmClose();
            if (choice == CloseChoice.Cancel) return;
            if (choice == CloseChoice.Save && !await SaveAsync()) return;
            _allowClose = true;
            // Defer even synchronous decisions until the current Closing event has returned.
            _ = Dispatcher.BeginInvoke(new Action(Close));
        }
        finally { _closing = false; }
    }

    private enum CloseChoice { Cancel, Save, Discard }
    private CloseChoice ConfirmClose()
    {
        var choice = CloseChoice.Cancel;
        var dialog = new Window { Owner = this, Title = "Save your edits?", Width = 430, SizeToContent = SizeToContent.Height, ResizeMode = ResizeMode.NoResize, WindowStartupLocation = WindowStartupLocation.CenterOwner, ShowInTaskbar = false };
        Theme.Apply(dialog);
        var content = new StackPanel { Margin = new Thickness(24) };
        content.Children.Add(Theme.Text("Save your edits?", 20));
        var description = Theme.Text("Your edits exist only in this window until you save an image file. Discarding closes this editor without saving.", 13, true);
        description.TextWrapping = TextWrapping.Wrap;
        description.Margin = new Thickness(0, 12, 0, 24);
        content.Children.Add(description);
        var actions = new WrapPanel { HorizontalAlignment = HorizontalAlignment.Right };
        var cancel = AddButton(actions, "Cancel", () => dialog.Close(), "Return to the editor");
        cancel.IsCancel = true;
        AddButton(actions, "Discard", () => { choice = CloseChoice.Discard; dialog.Close(); }, "Close without saving");
        var save = AddButton(actions, "Save", () => { choice = CloseChoice.Save; dialog.Close(); }, "Save to your SlipShot folder before closing", true);
        save.IsDefault = true;
        content.Children.Add(actions);
        dialog.Content = content;
        dialog.ShowDialog();
        return choice;
    }

    private sealed class DrawingSurface : FrameworkElement
    {
        private readonly EditorWindow _owner;
        public DrawingSurface(EditorWindow owner) => _owner = owner;
        protected override void OnRender(DrawingContext dc)
        {
            base.OnRender(dc);
            var crop = _owner._document.State.Crop;
            double zoom = _owner._zoom;
            dc.DrawRectangle(Brushes.Transparent, null, new Rect(0, 0, ActualWidth, ActualHeight));
            dc.PushClip(new RectangleGeometry(new Rect(0, 0, ActualWidth, ActualHeight)));
            dc.PushTransform(new ScaleTransform(zoom, zoom));
            dc.PushTransform(new TranslateTransform(-crop.X, -crop.Y));
            DocumentRenderer.Draw(dc, _owner._document.Source, _owner.VisibleState);
            if (_owner._cropPreview is { } selection)
            {
                var shade = new SolidColorBrush(Color.FromArgb(155, 0, 0, 0));
                var outer = new RectangleGeometry(crop);
                var inner = new RectangleGeometry(selection);
                dc.DrawGeometry(shade, null, new CombinedGeometry(GeometryCombineMode.Exclude, outer, inner));
                dc.DrawRectangle(null, new Pen(Brushes.White, 1.5 / zoom), selection);
            }
            else if (_owner.VisibleState.Annotations.FirstOrDefault(a => a.Id == _owner._selectedId) is { } selected)
            {
                var pen = new Pen(Theme.AccentBrush, 1.5 / zoom);
                dc.DrawRectangle(null, pen, selected.Bounds);
                if (_owner._tool == ToolKind.Select)
                {
                    double size = 7 / zoom;
                    foreach (var point in Handles(selected.Bounds))
                        dc.DrawRectangle(Theme.PanelBrush, pen, new Rect(point.X - size / 2, point.Y - size / 2, size, size));
                }
            }
            dc.Pop();
            dc.Pop();
            dc.Pop();
        }
    }

    /// <summary>Small authored geometry icons; labels carry the accessible names.</summary>
    private sealed class ToolIcon : FrameworkElement
    {
        private readonly ToolKind _kind;
        public ToolIcon(ToolKind kind) => _kind = kind;
        protected override void OnRender(DrawingContext dc)
        {
            var pen = new Pen(Theme.TextBrush, 1.4) { StartLineCap = PenLineCap.Round, EndLineCap = PenLineCap.Round, LineJoin = PenLineJoin.Round };
            switch (_kind)
            {
                case ToolKind.Rectangle: dc.DrawRectangle(null, pen, new Rect(2, 3, 12, 10)); break;
                case ToolKind.Redaction: dc.DrawRectangle(Theme.TextBrush, null, new Rect(2, 3, 12, 10)); break;
                case ToolKind.Ellipse: dc.DrawEllipse(null, pen, new Point(8, 8), 6, 5); break;
                case ToolKind.Step:
                    dc.DrawEllipse(null, pen, new Point(8, 8), 6, 6);
                    dc.DrawLine(pen, new Point(8, 5), new Point(8, 11));
                    dc.DrawLine(pen, new Point(6, 7), new Point(8, 5));
                    break;
                case ToolKind.Text:
                    dc.DrawLine(pen, new Point(2, 3), new Point(14, 3));
                    dc.DrawLine(pen, new Point(8, 3), new Point(8, 13));
                    dc.DrawLine(pen, new Point(5, 13), new Point(11, 13));
                    break;
                case ToolKind.Crop:
                    dc.DrawLine(pen, new Point(4, 1), new Point(4, 12));
                    dc.DrawLine(pen, new Point(4, 12), new Point(15, 12));
                    dc.DrawLine(pen, new Point(1, 4), new Point(12, 4));
                    dc.DrawLine(pen, new Point(12, 4), new Point(12, 15));
                    break;
                case ToolKind.Pan:
                    dc.DrawLine(pen, new Point(2, 8), new Point(14, 8));
                    dc.DrawLine(pen, new Point(8, 2), new Point(8, 14));
                    dc.DrawLine(pen, new Point(2, 8), new Point(4, 6));
                    dc.DrawLine(pen, new Point(2, 8), new Point(4, 10));
                    dc.DrawLine(pen, new Point(8, 2), new Point(6, 4));
                    dc.DrawLine(pen, new Point(8, 2), new Point(10, 4));
                    break;
                case ToolKind.Select:
                    var pointer = Geometry.Parse("M 3,2 L 13,8 L 8,9 L 6,14 Z");
                    dc.DrawGeometry(null, pen, pointer);
                    break;
                case ToolKind.Pen:
                case ToolKind.Highlighter:
                    dc.DrawLine(new Pen(Theme.TextBrush, _kind == ToolKind.Highlighter ? 5 : 2), new Point(3, 12), new Point(12, 3));
                    dc.DrawLine(pen, new Point(3, 12), new Point(2, 14));
                    break;
                default:
                    dc.DrawLine(pen, new Point(2, 13), new Point(13, 2));
                    if (_kind == ToolKind.Arrow)
                    {
                        dc.DrawLine(pen, new Point(7, 2), new Point(13, 2));
                        dc.DrawLine(pen, new Point(13, 2), new Point(13, 8));
                    }
                    break;
            }
        }
    }
}
