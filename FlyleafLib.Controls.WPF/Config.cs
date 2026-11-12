using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Windows;
using System.Windows.Media;

using MaterialDesignThemes.Wpf;

namespace FlyleafLib.Controls.WPF;

public class UIConfig : NotifyPropertyChanged
{
    [JsonIgnore]
    public bool         Loaded              { get; private set; }

    public ObservableCollection<UITheme>
                        Themes              { get; set; }
    public string       SelectedTheme       { get => _SelectedTheme;        set => Set(ref _SelectedTheme, value); }
    string _SelectedTheme;

    public int          ActivityTimeout     { get; set; } // we just store it to file

    public string       SubsFontFamily      { get => _SubsFontFamily;       set => Set(ref _SubsFontFamily, value); }
    string _SubsFontFamily;
    public double       SubsFontSize        { get => _SubsFontSize;         set => Set(ref _SubsFontSize, value); }
    double _SubsFontSize;
    public Color        SubsFontColor       { get => _SubsFontColor;        set => Set(ref _SubsFontColor, value); }
    Color _SubsFontColor;
    public string       SubsFontStretch     { get => _SubsFontStretch;      set => Set(ref _SubsFontStretch, value); }
    string _SubsFontStretch;
    public string       SubsFontWeight      { get => _SubsFontWeight;       set => Set(ref _SubsFontWeight, value); }
    string _SubsFontWeight;
    public string       SubsFontStyle       { get => _SubsFontStyle;        set => Set(ref _SubsFontStyle, value); }
    string _SubsFontStyle;
    public Thickness    SubsMargin          { get => _SubsMargin;           set { Set(ref _SubsMargin, value); UpdateSubsMargin(); } }
    Thickness _SubsMargin;
    [JsonIgnore]
    public Thickness    SubsMargin2         { get => _SubsMargin2;          set => Set(ref _SubsMargin2, value); }
    Thickness _SubsMargin2;
    public double       SubsStrokeThickness { get => _SubsStrokeThickness;  set => Set(ref _SubsStrokeThickness, value); }
    double _SubsStrokeThickness;
    public bool SubsWithinViewport          { get => _SubsWithinViewport;   set => Set(ref _SubsWithinViewport, value); }
    bool _SubsWithinViewport;

    internal void UpdateSubsMargin()
    {
        Utils.UI(() =>
        {
            float vy = 0;
            if (SubsWithinViewport && flyleaf != null && flyleaf.Player != null)
            {
                var renderer = flyleaf.Player.Renderer;
                var viewport = renderer.Viewport;
                vy = Math.Max(flyleaf.Player.Renderer.ControlHeight - (viewport.Y + viewport.Height), 0);
            }

            Set(ref _SubsMargin2, new Thickness(SubsMargin.Left, SubsMargin.Top, SubsMargin.Right, SubsMargin.Bottom + vy), false, nameof(SubsMargin2));
        });
    }

    internal FlyleafME flyleaf;

    public UIConfig() { }
    public UIConfig(FlyleafME flyleaf) { this.flyleaf = flyleaf; }

    public static void Load(FlyleafME flyleaf, string path)
    {
        var config = JsonSerializer.Deserialize<UIConfig>(File.ReadAllText(path));
        if (config?.Themes == null || config.Themes.Count == 0 || config.Themes.Any(x => x == null))
            throw new InvalidDataException("The UI configuration does not contain any themes.");
        config.Loaded = true;
        config.flyleaf = flyleaf;
        foreach(var theme in config.Themes)
            theme.flyleaf = flyleaf;
        flyleaf.UIConfig = config;
        if (config.ActivityTimeout > 0)
            flyleaf.ActivityTimeout = config.ActivityTimeout;
    }

    public static void Save(FlyleafME flyleaf, string uiConfigPath, string configPath, string enginePath)
    {
        if (!string.IsNullOrEmpty(uiConfigPath))
        {
            flyleaf.UIConfig.ActivityTimeout = flyleaf.ActivityTimeout;
            File.WriteAllText(uiConfigPath, JsonSerializer.Serialize(flyleaf.UIConfig, Config.jsonOpts));
        }

        if (!string.IsNullOrEmpty(configPath) && flyleaf.Config != null)
            flyleaf.Config.Save(configPath);

        if (!string.IsNullOrEmpty(enginePath) && flyleaf.ConfigEngine != null)
            flyleaf.ConfigEngine.Save(enginePath);
    }
}

public class UITheme : NotifyPropertyChanged
{
    [JsonIgnore]
    public FlyleafME flyleaf;

    public UITheme() { }

    public UITheme(FlyleafME flyleaf, UITheme baseTheme)
    {
        this.flyleaf = flyleaf;

        if (baseTheme != null)
        {
            _PrimaryColor   = baseTheme.PrimaryColor;
            _SecondaryColor = baseTheme.SecondaryColor;
            _BackgroundColor= baseTheme.BackgroundColor;
            _SurfaceColor   = baseTheme.SurfaceColor;
        }
    }

    public string Name { get; set; }
    [JsonIgnore]
    public bool IsSelected => ReferenceEquals(flyleaf?.SelectedTheme, this);
    internal void RefreshSelection() => Raise(nameof(IsSelected));
    public Color PrimaryColor   {
        get => _PrimaryColor;
        set
        {
            if (!Set(ref _PrimaryColor, value)) return;
            if (flyleaf == null || flyleaf.SelectedTheme == null || flyleaf.SelectedTheme.Name != Name) return;

            flyleaf.ApplyTheme(this);
        }
    }
    Color _PrimaryColor;

    public Color SecondaryColor {
        get => _SecondaryColor;
        set
        {
            if (!Set(ref _SecondaryColor, value)) return;
            if (flyleaf == null || flyleaf.SelectedTheme == null || flyleaf.SelectedTheme.Name != Name) return;

            flyleaf.ApplyTheme(this);
        }
    }
    Color _SecondaryColor;

    public Color BackgroundColor {
        get => _BackgroundColor;
        set
        {
            if (!Set(ref _BackgroundColor, value)) return;
            if (flyleaf == null || flyleaf.SelectedTheme == null || flyleaf.SelectedTheme.Name != Name) return;

            flyleaf.ApplyTheme(this);
        }
    }
    Color _BackgroundColor;

    public Color SurfaceColor      {
        get => _SurfaceColor;
        set
        {
            if (!Set(ref _SurfaceColor, value)) return;
            if (flyleaf == null || flyleaf.SelectedTheme == null || flyleaf.SelectedTheme.Name != Name) return;

            flyleaf.ApplyTheme(this);
        }
    }
    Color _SurfaceColor;
}
