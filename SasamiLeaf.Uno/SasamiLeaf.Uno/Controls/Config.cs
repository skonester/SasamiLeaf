using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.UI.Xaml;
using Windows.UI;
using FlyleafLib;

namespace SasamiLeaf.Uno.Controls
{
    public class UIConfig : NotifyPropertyChanged
    {
        [JsonIgnore]
        public bool Loaded { get; private set; }

        public ObservableCollection<UITheme> Themes { get; set; }
        
        public string SelectedTheme
        {
            get => _SelectedTheme;
            set
            {
                if (Set(ref _SelectedTheme, value))
                {
                    if (flyleaf != null && Themes != null)
                    {
                        foreach (var theme in Themes)
                        {
                            if (theme.Name == value)
                            {
                                flyleaf.SelectedTheme = theme;
                                break;
                            }
                        }
                    }
                }
            }
        }
        private string _SelectedTheme;

        public int ActivityTimeout { get; set; }

        public string SubsFontFamily { get => _SubsFontFamily; set => Set(ref _SubsFontFamily, value); }
        private string _SubsFontFamily;

        public double SubsFontSize { get => _SubsFontSize; set => Set(ref _SubsFontSize, value); }
        private double _SubsFontSize;

        public Color SubsFontColor { get => _SubsFontColor; set => Set(ref _SubsFontColor, value); }
        private Color _SubsFontColor;

        public string SubsFontStretch { get => _SubsFontStretch; set => Set(ref _SubsFontStretch, value); }
        private string _SubsFontStretch;

        public string SubsFontWeight { get => _SubsFontWeight; set => Set(ref _SubsFontWeight, value); }
        private string _SubsFontWeight;

        public string SubsFontStyle { get => _SubsFontStyle; set => Set(ref _SubsFontStyle, value); }
        private string _SubsFontStyle;

        public Thickness SubsMargin { get => _SubsMargin; set { Set(ref _SubsMargin, value); UpdateSubsMargin(); } }
        private Thickness _SubsMargin;

        [JsonIgnore]
        public Thickness SubsMargin2 { get => _SubsMargin2; set => Set(ref _SubsMargin2, value); }
        private Thickness _SubsMargin2;

        public double SubsStrokeThickness { get => _SubsStrokeThickness; set => Set(ref _SubsStrokeThickness, value); }
        private double _SubsStrokeThickness;

        public bool SubsWithinViewport { get => _SubsWithinViewport; set => Set(ref _SubsWithinViewport, value); }
        private bool _SubsWithinViewport;

        internal void UpdateSubsMargin()
        {
            float vy = 0;
            if (SubsWithinViewport && flyleaf != null && flyleaf.Player != null)
            {
                var renderer = flyleaf.Player.Renderer;
                var viewport = renderer.Viewport;
                vy = Math.Max(flyleaf.Player.Renderer.ControlHeight - (viewport.Y + viewport.Height), 0);
            }

            Set(ref _SubsMargin2, new Thickness(SubsMargin.Left, SubsMargin.Top, SubsMargin.Right, SubsMargin.Bottom + vy), false, nameof(SubsMargin2));
        }

        [JsonIgnore]
        internal FlyleafME flyleaf;

        public UIConfig() { }
        public UIConfig(FlyleafME flyleaf) { this.flyleaf = flyleaf; }

        private static readonly JsonSerializerOptions jsonOpts = new() { WriteIndented = true };

        public static void Load(FlyleafME flyleaf, string path)
        {
            if (!File.Exists(path)) return;
            try
            {
                flyleaf.UIConfig = JsonSerializer.Deserialize<UIConfig>(File.ReadAllText(path));
                flyleaf.ActivityTimeout = flyleaf.UIConfig.ActivityTimeout;
                flyleaf.UIConfig.Loaded = true;
                flyleaf.UIConfig.flyleaf = flyleaf;
                foreach (var theme in flyleaf.UIConfig.Themes)
                    theme.flyleaf = flyleaf;
            }
            catch { }
        }

        public static void Save(FlyleafME flyleaf, string uiConfigPath, string configPath, string enginePath)
        {
            try
            {
                if (!string.IsNullOrEmpty(uiConfigPath) && flyleaf.UIConfig != null)
                {
                    flyleaf.UIConfig.ActivityTimeout = flyleaf.ActivityTimeout;
                    File.WriteAllText(uiConfigPath, JsonSerializer.Serialize(flyleaf.UIConfig, jsonOpts));
                }

                if (!string.IsNullOrEmpty(configPath) && flyleaf.Config != null)
                    flyleaf.Config.Save(configPath);

                if (!string.IsNullOrEmpty(enginePath) && flyleaf.ConfigEngine != null)
                    flyleaf.ConfigEngine.Save(enginePath);
            }
            catch { }
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
                _PrimaryColor = baseTheme.PrimaryColor;
                _SecondaryColor = baseTheme.SecondaryColor;
                _BackgroundColor = baseTheme.BackgroundColor;
                _SurfaceColor = baseTheme.SurfaceColor;
            }
        }

        public string Name { get; set; }
        
        public Color PrimaryColor
        {
            get => _PrimaryColor;
            set
            {
                if (Set(ref _PrimaryColor, value))
                {
                    ApplyColors();
                }
            }
        }
        private Color _PrimaryColor;

        public Color SecondaryColor
        {
            get => _SecondaryColor;
            set
            {
                if (Set(ref _SecondaryColor, value))
                {
                    ApplyColors();
                }
            }
        }
        private Color _SecondaryColor;

        public Color BackgroundColor
        {
            get => _BackgroundColor;
            set
            {
                if (Set(ref _BackgroundColor, value))
                {
                    ApplyColors();
                }
            }
        }
        private Color _BackgroundColor;

        public Color SurfaceColor
        {
            get => _SurfaceColor;
            set
            {
                if (Set(ref _SurfaceColor, value))
                {
                    ApplyColors();
                }
            }
        }
        private Color _SurfaceColor;

        private void ApplyColors()
        {
            if (flyleaf == null || flyleaf.SelectedTheme == null || flyleaf.SelectedTheme.Name != Name) return;
            
            // Sync with Player settings
            if (flyleaf.Config != null && flyleaf.Config.Video != null)
            {
                flyleaf.Config.Video.BackColor = System.Windows.Media.Color.FromArgb(SurfaceColor.A, SurfaceColor.R, SurfaceColor.G, SurfaceColor.B);
            }

            // We can also trigger global resources reload or event notifications if needed
        }
    }
}
