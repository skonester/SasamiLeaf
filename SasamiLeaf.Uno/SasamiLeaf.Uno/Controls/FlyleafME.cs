using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using FlyleafLib;
using FlyleafLib.MediaPlayer;
using CommunityToolkit.Mvvm.Input;

namespace SasamiLeaf.Uno.Controls
{
    public class FlyleafME : FlyleafHost, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;
        protected void Raise([CallerMemberName] string propertyName = "") => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        protected bool Set<T>(ref T field, T value, [CallerMemberName] string propertyName = "")
        {
            if (EqualityComparer<T>.Default.Equals(field, value)) return false;
            field = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            return true;
        }

        public Window? OverlayWindow { get; set; }

        public FlyleafME() : this(null) { }

        public FlyleafME(Window? overlayWindow)
        {
            DefaultStyleKey = typeof(FlyleafHost);
            OverlayWindow = overlayWindow;
            RegisterCommands();
        }

        #region Properties
        public string? UIConfigPath { get; set; }
        public string? ConfigPath { get; set; }
        public string? EnginePath { get; set; }

        public Config? Config => Player?.Config;
        
        private UIConfig? _UIConfig;
        public UIConfig? UIConfig { get => _UIConfig; set => Set(ref _UIConfig, value); }

        public AudioEngine? AudioEngine => Engine.Audio;
        public EngineConfig? ConfigEngine => Engine.Config;

        public bool ShowDebug { get => _ShowDebug; set { Set(ref _ShowDebug, value); if (Config != null) Config.Player.Stats = value; } }
        private bool _ShowDebug;

        private UITheme? _SelectedTheme;
        public UITheme? SelectedTheme
        {
            get => _SelectedTheme;
            set
            {
                if (Set(ref _SelectedTheme, value))
                {
                    if (Config != null && Config.Video != null && value != null)
                        Config.Video.BackColor = System.Windows.Media.Color.FromArgb(value.SurfaceColor.A, value.SurfaceColor.R, value.SurfaceColor.G, value.SurfaceColor.B);
                    
                    ThemeLoaded?.Invoke(this, EventArgs.Empty);
                }
            }
        }

        public bool IsFullScreen
        {
            get => FSC != null && FSC.IsFullScreen;
            set
            {
                if (FSC != null)
                {
                    FSC.IsFullScreen = value;
                    Raise();
                }
            }
        }

        private bool _IsMinimized;
        public bool IsMinimized
        {
            get => _IsMinimized;
            set
            {
                if (Set(ref _IsMinimized, value))
                {
                    ApplyMinimized(value);
                }
            }
        }

        private bool _DetachedTopMost;
        public bool DetachedTopMost
        {
            get => _DetachedTopMost;
            set
            {
                if (Set(ref _DetachedTopMost, value))
                {
                    ApplyAlwaysOnTop(value);
                }
            }
        }

        public new object? Tag { get; set; }
        public int ActivityTimeout { get; set; }
        public AvailableWindows KeyBindingsEnum { get; set; }
        public AvailableWindows DetachedResize { get; set; }
        public AvailableWindows DetachedDragMove { get; set; }
        public AvailableWindows ToggleFullScreenOnDoubleClick { get; set; }
        public bool KeepRatioOnResize { get; set; }
        public AvailableWindows OpenOnDrop { get; set; }
        public int PreferredLandscapeWidth { get; set; }
        public int PreferredPortraitHeight { get; set; }

        public Brush VideoBackground
        {
            get => Background;
            set => Background = value;
        }

        public event EventHandler? ThemeLoaded;
        #endregion

        protected override void PlayerChanged(Player? oldPlayer)
        {
            if (oldPlayer != null)
                oldPlayer.Renderer.ViewportChanged -= ViewportChanged;

            base.PlayerChanged(oldPlayer);

            if (Player == null)
                return;

            if (SelectedTheme != null)
                Player.Config.Video.BackColor = System.Windows.Media.Color.FromArgb(SelectedTheme.SurfaceColor.A, SelectedTheme.SurfaceColor.R, SelectedTheme.SurfaceColor.G, SelectedTheme.SurfaceColor.B);

            // Additional Key Bindings for Subtitles
            var keys = Player.Config.Player.KeyBindings;
            bool SubsYUp = false, SubsYDown = false, SubsFontIncrease = false, SubsFontDecrease = false;
            void aSubsYUp()         { if (UIConfig != null) { Thickness t = UIConfig.SubsMargin; t.Bottom += 2; UIConfig.SubsMargin = t; } }
            void aSubsYDown()       { if (UIConfig != null) { Thickness t = UIConfig.SubsMargin; t.Bottom -= 2; UIConfig.SubsMargin = t; } }
            void aSubsFontIncrease() { if (UIConfig != null) UIConfig.SubsFontSize += 2; }
            void aSubsFontDecrease() { if (UIConfig != null) UIConfig.SubsFontSize -= 2; }

            foreach (var binding in keys.Keys)
            {
                if (binding.Action != KeyBindingAction.Custom || string.IsNullOrEmpty(binding.ActionName))
                    continue;

                switch (binding.ActionName)
                {
                    case "SubsYUp":
                        binding.SetAction(aSubsYUp, false);
                        SubsYUp = true;
                        break;

                    case "SubsYDown":
                        binding.SetAction(aSubsYDown, false);
                        SubsYDown = true;
                        break;

                    case "SubsFontIncrease":
                        binding.SetAction(aSubsFontIncrease, false);
                        SubsFontIncrease = true;
                        break;

                    case "SubsFontDecrease":
                        binding.SetAction(aSubsFontDecrease, false);
                        SubsFontDecrease = true;
                        break;
                }
            }

            if (!Player.Config.Loaded)
            {
                if (!SubsYUp)
                    keys.AddCustom(System.Windows.Input.Key.Up,     false, aSubsYUp,           "SubsYUp",          true);
                if (!SubsYDown)
                    keys.AddCustom(System.Windows.Input.Key.Down,   false, aSubsYDown,         "SubsYDown",        true);
                if (!SubsFontIncrease)
                    keys.AddCustom(System.Windows.Input.Key.Right,  false, aSubsFontIncrease,  "SubsFontIncrease", true);
                if (!SubsFontDecrease)
                    keys.AddCustom(System.Windows.Input.Key.Left,   false, aSubsFontDecrease,  "SubsFontDecrease", true);
            }

            Player.Renderer.ViewportChanged += ViewportChanged;
            ViewportChanged(null, EventArgs.Empty);
        }

        private void ViewportChanged(object? sender, EventArgs e) => UIConfig?.UpdateSubsMargin();

        #region Helper WinUI Actions
        private Microsoft.UI.Windowing.AppWindow? GetAppWindow()
        {
            if (OverlayWindow == null) return null;
            var windowHandle = WinRT.Interop.WindowNative.GetWindowHandle(OverlayWindow);
            var windowId = Microsoft.UI.Win32Interop.GetWindowIdFromWindow(windowHandle);
            return Microsoft.UI.Windowing.AppWindow.GetFromWindowId(windowId);
        }

        private void ApplyAlwaysOnTop(bool value)
        {
            var appWindow = GetAppWindow();
            if (appWindow != null && appWindow.Presenter is Microsoft.UI.Windowing.OverlappedPresenter overlappedPresenter)
            {
                overlappedPresenter.IsAlwaysOnTop = value;
            }
        }

        private void ApplyMinimized(bool value)
        {
            if (OverlayWindow == null) return;
            var windowHandle = WinRT.Interop.WindowNative.GetWindowHandle(OverlayWindow);
            const int SW_MINIMIZE = 6;
            const int SW_RESTORE = 9;
            NativeMethods.ShowWindow(windowHandle, value ? SW_MINIMIZE : SW_RESTORE);
        }

        private static class NativeMethods
        {
            [System.Runtime.InteropServices.DllImport("user32.dll")]
            public static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);
        }
        #endregion

        #region Commands
        private void RegisterCommands()
        {
            OpenFileDialogCmd = new CommunityToolkit.Mvvm.Input.RelayCommand(OpenFileDialogAction);
            ExitApplicationCmd = new CommunityToolkit.Mvvm.Input.RelayCommand(ExitApplicationAction);
            OpenSettingsCmd = new CommunityToolkit.Mvvm.Input.RelayCommand(OpenSettingsAction);
            ChangeAspectRatioCmd = new CommunityToolkit.Mvvm.Input.RelayCommand<object>(ChangeAspectRatioAction);
            ResetSubsPositionYCmd = new CommunityToolkit.Mvvm.Input.RelayCommand(ResetSubsPositionYAction);
            SetSubsPositionYCmd = new CommunityToolkit.Mvvm.Input.RelayCommand<object>(SetSubsPositionYAction);
        }

        public IRelayCommand OpenFileDialogCmd { get; set; } = null!;
        public void OpenFileDialogAction() => Player?.OpenFromFileDialog();

        public IRelayCommand ExitApplicationCmd { get; set; } = null!;
        public void ExitApplicationAction() => Microsoft.UI.Xaml.Application.Current.Exit();

        public IRelayCommand OpenSettingsCmd { get; set; } = null!;
        public async void OpenSettingsAction()
        {
            if (Config == null || this.XamlRoot == null) return;

            var settingsView = new SettingsView(this);
            var dialog = new ContentDialog()
            {
                Content = settingsView,
                Title = "Settings",
                PrimaryButtonText = "Save",
                SecondaryButtonText = "Cancel",
                CloseButtonText = "Close",
                XamlRoot = this.XamlRoot
            };

            var prevConfig = Config.Video.Clone();

            var result = await dialog.ShowAsync();
            if (result == ContentDialogResult.Secondary)
            {
                Config.Video.HDRtoSDRMethod = prevConfig.HDRtoSDRMethod;
                Config.Video.SDRDisplayNitsCustom = prevConfig.SDRDisplayNitsCustom;
            }
            else if (result == ContentDialogResult.Primary)
            {
                settingsView.ApplySettings();
                if (UIConfigPath != null)
                {
                    SasamiLeaf.Uno.Controls.UIConfig.Save(this, UIConfigPath, ConfigPath, EnginePath);
                }
            }
        }

        public IRelayCommand<object> ChangeAspectRatioCmd { get; set; } = null!;
        public void ChangeAspectRatioAction(object? obj)
        {
            if (Config == null) return;

            if (obj is string ratio)
            {
                if (ratio == "Keep")
                    Config.Video.AspectRatio = AspectRatio.Keep;
                else if (ratio == "Custom")
                    Config.Video.AspectRatio = AspectRatio.Custom;
                else
                    Config.Video.AspectRatio = ratio;
            }
        }

        public IRelayCommand ResetSubsPositionYCmd { get; set; } = null!;
        public void ResetSubsPositionYAction()
        {
            if (UIConfig != null)
                UIConfig.SubsMargin = new Thickness(0, 0, 0, 48); // default
        }

        public IRelayCommand<object> SetSubsPositionYCmd { get; set; } = null!;
        public void SetSubsPositionYAction(object? y)
        {
            if (UIConfig != null && y != null)
            {
                Thickness t = UIConfig.SubsMargin;
                t.Bottom += double.Parse(y.ToString()!);
                UIConfig.SubsMargin = t;
            }
        }

        public void CloseCanceled() { }
        #endregion
    }

    public enum AvailableWindows
    {
        None,
        Surface,
        Overlay,
        Both
    }
}
