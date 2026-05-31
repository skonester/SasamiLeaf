using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Windows.System;
using FlyleafLib;
using FlyleafLib.MediaPlayer;
using SasamiLeaf.Uno.Controls;
using CommunityToolkit.Mvvm.Input;

namespace SasamiLeaf.Uno
{
    public sealed partial class MainPage : Page, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        private void Raise([CallerMemberName] string name = "") => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        private bool Set<T>(ref T field, T value, [CallerMemberName] string name = "")
        {
            if (EqualityComparer<T>.Default.Equals(field, value)) return false;
            field = value;
            Raise(name);
            return true;
        }

        public Player Player { get; set; }
        private Config playerConfig;
        private bool _reversePlaybackChecked;

        // OSD Msg Fields
        private const int MSG_TIMEOUT = 3500;
        private CancellationTokenSource cancelMsgToken = new();
        private string msg;
        public string Msg
        {
            get => msg;
            set
            {
                cancelMsgToken.Cancel();
                msg = value;
                Raise(nameof(Msg));
                cancelMsgToken = new CancellationTokenSource();
                Task.Run(() => FadeOutMsg(cancelMsgToken.Token));
            }
        }

        public string FlyleafLibVer => "FlyleafLib v" + System.Reflection.Assembly.GetAssembly(typeof(Engine)).GetName().Version;

        // Slide Show Fields
        public AppConfig.SlideShowConfig SlideShowConfig => App.AppConfig.SlideShow;
        public AppConfig.GeneralConfig GeneralConfig => App.AppConfig.General;

        private string imageTitle;
        public string ImageTitle { get => imageTitle; set => Set(ref imageTitle, value); }

        public List<string> ImageFiles { get; set; } = new();

        private int imageIndex = -1;
        public int ImageIndex
        {
            get => imageIndex;
            set
            {
                if (Set(ref imageIndex, value))
                {
                    UIImageIndex = value + 1;
                    Raise(nameof(ImageIndexTotalText));
                }
            }
        }

        private int uiImageIndex;
        public int UIImageIndex { get => uiImageIndex; set => Set(ref uiImageIndex, value); }

        private int totalFiles;
        public int TotalFiles { get => totalFiles; set { if (Set(ref totalFiles, value)) Raise(nameof(ImageIndexTotalText)); } }

        public string ImageIndexTotalText => $"{UIImageIndex} / {TotalFiles}";

        private bool slideShow;
        public bool SlideShow { get => slideShow; set => Set(ref slideShow, value); }

        private bool canNextImage;
        public bool CanNextImage { get => canNextImage; set => Set(ref canNextImage, value); }

        private bool canPrevImage;
        public bool CanPrevImage { get => canPrevImage; set => Set(ref canPrevImage, value); }

        private CancellationTokenSource slideShowCancel = new();
        private readonly object slideShowLock = new();
        private bool slideShowOpening;
        private int slideShowElapsedMs;
        private long slideShowStartedAt;
        private FileSystemWatcher slideShowWatcher;
        private bool userKeepRatioOnResize;
        private string imageFolder;
        private int folderId;

        private static readonly Regex RegexImages = new Regex(@"\.(apng|avif|bmp|jpg|jpeg|gif|ico|png|svg|tiff|webp|jfif)$", RegexOptions.IgnoreCase);

        // View Mode Property
        private MediaViewer mediaViewer = MediaViewer.Video;
        public MediaViewer MediaViewer
        {
            get => mediaViewer;
            set
            {
                if (Set(ref mediaViewer, value))
                {
                    SwitchMediaViewer();
                    Raise(nameof(IsVideoViewerVisible));
                    Raise(nameof(IsSlideViewerVisible));
                    Raise(nameof(IsImageViewerVisible));
                }
            }
        }

        public bool IsVideoViewerVisible => MediaViewer == MediaViewer.Video;
        public bool IsSlideViewerVisible => MediaViewer == MediaViewer.Slide;
        public bool IsImageViewerVisible => MediaViewer == MediaViewer.Image || MediaViewer == MediaViewer.Slide;

        // Commands
        public IRelayCommand ShowNextImage { get; set; }
        public IRelayCommand ShowPrevImage { get; set; }
        public IRelayCommand SlideShowToggle { get; set; }
        public IRelayCommand SlideShowRestart { get; set; }

        public MainPage()
        {
            this.InitializeComponent();
            MyFlyleafME.Tag = this;

            ShowPrevImage = new RelayCommand(() => SlideShowGoTo(SlidePosition.Prev));
            ShowNextImage = new RelayCommand(() => SlideShowGoTo(SlidePosition.Next));
            SlideShowToggle = new RelayCommand(SlideShowToggleAction);
            SlideShowRestart = new RelayCommand(() => { SlideShowStart(false, false); SlideShowGoTo(SlidePosition.Start); });

            this.Loaded += MainPage_Loaded;
            this.KeyDown += MainPage_KeyDown;
        }

        private void MainPage_Loaded(object sender, RoutedEventArgs e)
        {
            // Set the overlay window for window operations (e.g. Always on Top, Minimize)
            MyFlyleafME.OverlayWindow = ((App)Application.Current).MainWindow;

            if (Engine.IsLoaded)
                PageAndEngineLoaded();
            else
                Engine.Loaded += (o, args) => PageAndEngineLoaded();
        }

        private void PageAndEngineLoaded()
        {
            this.DispatcherQueue.TryEnqueue(() =>
            {
                // Initialize Player
                LoadPlayer();

                // Assign DataContext to expose player, settings commands, etc. to XAML and FlyleafBar
                this.DataContext = MyFlyleafME;
            });
        }

        private void LoadPlayer()
        {
            string configPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "SasamiLeaf", "Flyleaf.Config.json");
            string uiConfigPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "SasamiLeaf", "Flyleaf.UIConfig.json");
            string enginePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "SasamiLeaf", "Flyleaf.Engine.json");

            MyFlyleafME.ConfigPath = configPath;
            MyFlyleafME.UIConfigPath = uiConfigPath;
            MyFlyleafME.EnginePath = enginePath;

            if (File.Exists(configPath))
            {
                try { playerConfig = Config.Load(configPath); } catch { playerConfig = DefaultConfig(); }
            }
            else
            {
                playerConfig = DefaultConfig();
            }

            userClearScreen = playerConfig.Video.ClearScreen;

            Player = new Player(playerConfig);
            MyFlyleafME.Player = Player;

            Player.Opening += Player_Opening;
            Player.OpenCompleted += Player_OpenCompleted;

            // Property and config monitoring
            Player.PropertyChanged += (o, e) =>
            {
                this.DispatcherQueue.TryEnqueue(() =>
                {
                    if (e.PropertyName == "ReversePlayback" && !_reversePlaybackChecked)
                    {
                        if (playerConfig.Decoder.MaxVideoFrames < 80)
                            playerConfig.Decoder.MaxVideoFrames = 80;
                        _reversePlaybackChecked = true;
                    }
                    else if (e.PropertyName == nameof(Player.Speed))
                        Msg = $"Speed x{Player.Speed}";
                    else if (e.PropertyName == nameof(Player.Status) && Player.Activity.Mode == ActivityMode.Idle)
                        Player.Activity.ForceActive();
                });
            };

            Player.Config.Video.PropertyChanged += (o, e) =>
            {
                this.DispatcherQueue.TryEnqueue(() =>
                {
                    if (e.PropertyName == nameof(Player.Config.Video.Rotation))
                        Msg = $"Rotation {Player.Config.Video.Rotation}°";
                    else if (e.PropertyName == nameof(Player.Config.Video.Zoom))
                        Msg = $"Zoom {Player.Config.Video.Zoom}%";
                });
            };

            Player.Audio.PropertyChanged += (o, e) =>
            {
                this.DispatcherQueue.TryEnqueue(() =>
                {
                    if (e.PropertyName == nameof(Player.Audio.Volume))
                        Msg = $"Volume {Player.Audio.Volume}%";
                    else if (e.PropertyName == nameof(Player.Audio.Mute))
                        Msg = Player.Audio.Mute ? "Muted" : "Unmuted";
                });
            };

            Player.Config.Audio.PropertyChanged += (o, e) =>
            {
                this.DispatcherQueue.TryEnqueue(() =>
                {
                    if (e.PropertyName == nameof(Player.Config.Audio.Delay))
                        Msg = $"Audio Delay {Player.Config.Audio.Delay / 10000}ms";
                });
            };

            Player.Config.Subtitles.PropertyChanged += (o, e) =>
            {
                this.DispatcherQueue.TryEnqueue(() =>
                {
                    if (e.PropertyName == nameof(Player.Config.Subtitles.Delay))
                        Msg = $"Subs Delay {Player.Config.Subtitles.Delay / 10000}ms";
                });
            };

            // Load UIConfig
            if (File.Exists(uiConfigPath))
            {
                UIConfig.Load(MyFlyleafME, uiConfigPath);
            }
            else
            {
                var uiConf = new UIConfig(MyFlyleafME)
                {
                    SubsMargin = new Thickness(0, 0, 0, 48),
                    SubsFontFamily = "Segoe UI",
                    SubsFontWeight = "Bold",
                    SubsFontStyle = "Normal",
                    SubsFontSize = 48,
                    SubsStrokeThickness = 3,
                    SubsFontColor = Windows.UI.Color.FromArgb(255, 255, 255, 255), // White
                    SubsWithinViewport = true
                };

                // Add themes
                var defaultTheme = new UITheme(MyFlyleafME, null)
                {
                    Name = "Default",
                    PrimaryColor = Windows.UI.Color.FromArgb(255, 0, 132, 208),
                    SecondaryColor = Windows.UI.Color.FromArgb(255, 255, 255, 255),
                    BackgroundColor = Windows.UI.Color.FromArgb(255, 0, 0, 0),
                    SurfaceColor = Windows.UI.Color.FromArgb(255, 0, 0, 0)
                };

                uiConf.Themes = new System.Collections.ObjectModel.ObservableCollection<UITheme>()
                {
                    new UITheme(MyFlyleafME, defaultTheme) { Name = "SasamiLeaf Pink", PrimaryColor = Windows.UI.Color.FromArgb(255, 255, 136, 187) },
                    new UITheme(MyFlyleafME, defaultTheme) { Name = "Black & White", PrimaryColor = Windows.UI.Color.FromArgb(255, 255, 255, 255) },
                    new UITheme(MyFlyleafME, defaultTheme) { Name = "Blue & Pink", PrimaryColor = Windows.UI.Color.FromArgb(255, 30, 144, 255), SecondaryColor = Windows.UI.Color.FromArgb(255, 255, 136, 187) },
                    new UITheme(MyFlyleafME, defaultTheme) { Name = "Orange", PrimaryColor = Windows.UI.Color.FromArgb(255, 255, 131, 0) },
                    new UITheme(MyFlyleafME, defaultTheme) { Name = "SasamiLeaf Dark", PrimaryColor = Windows.UI.Color.FromArgb(255, 255, 136, 187) },
                    new UITheme(MyFlyleafME, defaultTheme) { Name = "Custom", PrimaryColor = Windows.UI.Color.FromArgb(255, 255, 165, 0) }
                };

                uiConf.SelectedTheme = "SasamiLeaf Pink";
                MyFlyleafME.UIConfig = uiConf;

                UIConfig.Save(MyFlyleafME, uiConfigPath, configPath, enginePath);
            }

            foreach (var theme in MyFlyleafME.UIConfig.Themes)
            {
                if (theme.Name == MyFlyleafME.UIConfig.SelectedTheme)
                {
                    MyFlyleafME.SelectedTheme = theme;
                    break;
                }
            }
        }

        private Config DefaultConfig()
        {
            Config config = new();
            config.Demuxer.FormatOptToUnderlying = true;
            config.Video.GPUAdapter = "";
            config.Subtitles.SearchLocal = true;
            return config;
        }

        // Custom Key Handling for Player and SlideShow
        private void MainPage_KeyDown(object sender, KeyRoutedEventArgs e)
        {
            var ctrl = (Microsoft.UI.Input.InputKeyboardSource.GetKeyStateForCurrentThread(VirtualKey.Control) & Windows.UI.Core.CoreVirtualKeyStates.Down) == Windows.UI.Core.CoreVirtualKeyStates.Down;
            HandleKeyDown(e.Key, ctrl);
        }

        private void HandleKeyDown(VirtualKey key, bool ctrl)
        {
            if (MediaViewer == MediaViewer.Video)
            {
                switch (key)
                {
                    case VirtualKey.Space:
                        ExecuteCommand(Player?.Commands?.TogglePlayPause);
                        break;
                    case VirtualKey.Left:
                        ExecuteCommand(Player?.Commands?.SeekBackward);
                        break;
                    case VirtualKey.Right:
                        ExecuteCommand(Player?.Commands?.SeekForward);
                        break;
                    case VirtualKey.PageUp:
                        ExecuteCommand(Player?.Commands?.SeekBackward3);
                        break;
                    case VirtualKey.PageDown:
                        ExecuteCommand(Player?.Commands?.SeekForward3);
                        break;
                    case VirtualKey.Home:
                        Player?.SeekToStart();
                        break;
                    case VirtualKey.End:
                        Player?.SeekToEnd();
                        break;
                    case VirtualKey.Q:
                        if (ctrl) ExecuteCommand(Player?.Commands?.Stop);
                        break;
                    case VirtualKey.C:
                        if (ctrl) Player?.CopyToClipboard();
                        break;
                    case VirtualKey.X:
                        if (ctrl) Player?.Flush();
                        break;
                }
            }
            else if (MediaViewer == MediaViewer.Slide)
            {
                switch (key)
                {
                    case VirtualKey.Right:
                        SlideShowGoTo(SlidePosition.Next);
                        break;
                    case VirtualKey.Left:
                        SlideShowGoTo(SlidePosition.Prev);
                        break;
                    case VirtualKey.Space:
                        SlideShowToggleAction();
                        break;
                    case VirtualKey.F5:
                        SlideShowStart(true);
                        break;
                    case VirtualKey.PageDown:
                        SlideShowGoTo(SlidePosition.NextPage);
                        break;
                    case VirtualKey.PageUp:
                        SlideShowGoTo(SlidePosition.PrevPage);
                        break;
                    case VirtualKey.Home:
                        SlideShowGoTo(SlidePosition.Start);
                        break;
                    case VirtualKey.End:
                        SlideShowGoTo(SlidePosition.End);
                        break;
                    case VirtualKey.Delete:
                        ImageDelete();
                        break;
                    case VirtualKey.C:
                        if (ctrl) ImageCutCopy(true);
                        break;
                    case VirtualKey.X:
                        if (ctrl) ImageCutCopy(false);
                        break;
                    case VirtualKey.Q:
                        if (ctrl) { Player?.Stop(); MediaViewer = MediaViewer.Video; }
                        break;
                }
            }
            else if (MediaViewer == MediaViewer.Image)
            {
                switch (key)
                {
                    case VirtualKey.Delete:
                        ImageDelete();
                        break;
                    case VirtualKey.C:
                        if (ctrl) ImageCutCopy(true);
                        break;
                    case VirtualKey.X:
                        if (ctrl) ImageCutCopy(false);
                        break;
                    case VirtualKey.Q:
                        if (ctrl) { Player?.Stop(); MediaViewer = MediaViewer.Video; }
                        break;
                }
            }
        }

        private void ExecuteCommand(ICommand command)
        {
            if (command != null && command.CanExecute(null))
            {
                command.Execute(null);
            }
        }

        // Native Drag and Drop Implementation
        private void Page_DragOver(object sender, DragEventArgs e)
        {
            e.AcceptedOperation = Windows.ApplicationModel.DataTransfer.DataPackageOperation.Copy;
        }

        private async void Page_Drop(object sender, DragEventArgs e)
        {
            if (e.DataView.Contains(Windows.ApplicationModel.DataTransfer.StandardDataFormats.StorageItems))
            {
                var items = await e.DataView.GetStorageItemsAsync();
                if (items.Count > 0)
                {
                    var file = items[0] as Windows.Storage.StorageFile;
                    if (file != null)
                    {
                        Player?.OpenAsync(file.Path);
                    }
                }
            }
        }

        // Win32 Alphanumeric Sort P/Invoke
        [DllImport("shlwapi.dll", EntryPoint = "StrCmpLogicalW", ExactSpelling = true, CharSet = CharSet.Unicode)]
        [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
        public static extern int StrCmpLogical(string psz1, string psz2);

        // Slide Show Logic
        private void Player_Opening(object sender, OpeningArgs e)
        {
            this.DispatcherQueue.TryEnqueue(() =>
            {
                if (slideShowOpening)
                {
                    ImageTitle = ImageFiles[ImageIndex];
                }
                else if (!RegexImages.IsMatch(e.Url))
                {
                    if (MediaViewer != MediaViewer.Video)
                        MediaViewer = MediaViewer.Video;
                }
                else
                {
                    try
                    {
                        var url = e.Url.AsSpan();
                        string curFolder = null;
                        bool folderFound = false;

                        for (int i = url.Length - 1; i >= 0; i--)
                        {
                            if (url[i] == '/' || url[i] == '\\')
                            {
                                ImageTitle = url[(i + 1)..].ToString();
                                curFolder = url[..i].ToString();
                                folderFound = true;
                                break;
                            }
                        }

                        if (!folderFound)
                        {
                            MediaViewer = MediaViewer.Image;
                        }
                        else if (curFolder.Equals(imageFolder, StringComparison.OrdinalIgnoreCase) && MediaViewer == MediaViewer.Slide)
                        {
                            lock (slideShowLock)
                            {
                                int idx = -1;
                                for (int i = 0; i < ImageFiles.Count; i++)
                                {
                                    if (ImageFiles[i].Equals(ImageTitle, StringComparison.OrdinalIgnoreCase))
                                    {
                                        idx = i;
                                        break;
                                    }
                                }

                                ImageIndex = idx;
                                CanPrevImage = idx > 0;
                                CanNextImage = idx < ImageFiles.Count - 1;
                            }
                        }
                        else
                        {
                            imageFolder = curFolder;
                            RefreshFolder(true);
                        }
                    }
                    catch
                    {
                        MediaViewer = MediaViewer.Image;
                    }
                }
            });
        }

        private void Player_OpenCompleted(object sender, OpenCompletedArgs e)
        {
            this.DispatcherQueue.TryEnqueue(() =>
            {
                if (MediaViewer == MediaViewer.Video || e.IsSubtitles)
                    return;
                
                if (MediaViewer == MediaViewer.Image && imageTitle == null)
                {
                    ImageTitle = Player.Playlist.Selected.Title;
                }
                else
                {
                    if (!e.Success && e.Error != "Cancelled")
                        Player.Renderer?.ClearScreen(true);

                    lock (slideShowLock)
                    {
                        if (SlideShow)
                        {
                            slideShowCancel.Cancel();
                            slideShowCancel = new CancellationTokenSource();
                            slideShowStartedAt = DateTime.UtcNow.Ticks;
                            slideShowElapsedMs = 0;
                            Task.Run(() => SlideShowTask(slideShowCancel.Token));
                        }
                    }
                }
            });
        }

        private bool userClearScreen;
        private void SwitchMediaViewer()
        {
            switch (MediaViewer)
            {
                case MediaViewer.Video:
                    imageFolder = null;
                    if (Player != null && Player.Config != null)
                        Player.Config.Video.ClearScreen = userClearScreen;
                    MyFlyleafME.KeepRatioOnResize = userKeepRatioOnResize;
                    break;

                case MediaViewer.Image:
                    if (Player != null && Player.Config != null)
                        Player.Config.Video.ClearScreen = userClearScreen;
                    MyFlyleafME.KeepRatioOnResize = userKeepRatioOnResize;
                    break;

                case MediaViewer.Slide:
                    if (Player != null && Player.Config != null)
                    {
                        userClearScreen = Player.Config.Video.ClearScreen;
                        Player.Config.Video.ClearScreen = false;
                    }
                    MyFlyleafME.KeepRatioOnResize = false;
                    break;
            }
        }

        private void RefreshFolder(bool init)
        {
            Task.Run(() =>
            {
                int curId = Interlocked.Increment(ref folderId);

                var files = Directory.EnumerateFiles(imageFolder);
                List<string> imageFiles = new List<string>();
                foreach (var file in files)
                {
                    if (curId != folderId) return;

                    if (!RegexImages.IsMatch(file))
                        continue;

                    imageFiles.Add(Path.GetFileName(file));
                    if (imageFiles.Count > SlideShowConfig.MaxFiles)
                        break;
                }

                imageFiles.Sort(StrCmpLogical);

                this.DispatcherQueue.TryEnqueue(() =>
                {
                    lock (slideShowLock)
                    {
                        if (curId != folderId) return;

                        if (imageFiles.Count == 0)
                        {
                            MediaViewer = MediaViewer.Video;
                        }
                        else if (imageFiles.Count == 1)
                        {
                            if (!init)
                                Player.OpenAsync(Path.Combine(imageFolder, imageFiles[0]));
                            MediaViewer = MediaViewer.Image;
                        }
                        else
                        {
                            if (init)
                                MediaViewer = MediaViewer.Slide;

                            int idx = -1;
                            for (int i = 0; i < imageFiles.Count; i++)
                            {
                                if (imageFiles[i].Equals(ImageTitle, StringComparison.OrdinalIgnoreCase))
                                {
                                    idx = i;
                                    break;
                                }
                            }

                            ImageFiles = imageFiles;
                            TotalFiles = imageFiles.Count;
                            CanPrevImage = idx > 0;
                            CanNextImage = idx < ImageFiles.Count - 1;

                            if (SlideShow && !CanNextImage)
                                SlideShow = false;

                            if (init)
                            {
                                if (slideShowWatcher != null)
                                {
                                    slideShowWatcher.Dispose();
                                }
                                slideShowWatcher = new FileSystemWatcher(imageFolder)
                                {
                                    EnableRaisingEvents = true,
                                    NotifyFilter = NotifyFilters.FileName
                                };

                                slideShowWatcher.Renamed += (o, ev) =>
                                {
                                    if (RegexImages.IsMatch(ev.Name))
                                        this.DispatcherQueue.TryEnqueue(() => RefreshFolder(false));
                                };

                                slideShowWatcher.Deleted += (o, ev) =>
                                {
                                    if (RegexImages.IsMatch(ev.Name))
                                        this.DispatcherQueue.TryEnqueue(() => RefreshFolder(false));
                                };

                                slideShowWatcher.Created += (o, ev) =>
                                {
                                    if (RegexImages.IsMatch(ev.Name))
                                        this.DispatcherQueue.TryEnqueue(() => RefreshFolder(false));
                                };
                            }

                            if (idx == -1)
                                SlideShowGoTo(SlidePosition.Refresh);
                            else
                                ImageIndex = idx;
                        }
                    }
                });
            });
        }

        private void SlideShowDeInit()
        {
            lock (slideShowLock)
            {
                slideShowCancel.Cancel();
                slideShowCancel = new CancellationTokenSource();
                imageFolder = null;
                ImageFiles.Clear();
                ImageIndex = -1;
                TotalFiles = 0;
                slideShowElapsedMs = 0;
                SlideShow = CanPrevImage = CanNextImage = false;
                if (slideShowWatcher != null)
                {
                    slideShowWatcher.Dispose();
                    slideShowWatcher = null;
                }
            }
        }

        public void SlideShowGoTo(SlidePosition pos, bool fromSlideShow = false)
        {
            lock (slideShowLock)
            {
                if (MediaViewer != MediaViewer.Slide)
                    return;

                if (SlideShow && !fromSlideShow)
                {
                    slideShowCancel.Cancel();
                    slideShowCancel = new CancellationTokenSource();
                }

                if ((ImageIndex < 1 && (pos == SlidePosition.Prev || pos == SlidePosition.PrevPage)) ||
                    (ImageIndex >= ImageFiles.Count - 1 && (pos == SlidePosition.Next || pos == SlidePosition.NextPage)))
                    return;

                if (pos == SlidePosition.Start || ImageFiles.Count < 2)
                    ImageIndex = 0;
                else if (pos == SlidePosition.End)
                    ImageIndex = ImageFiles.Count - 1;
                else if (pos == SlidePosition.Prev)
                    ImageIndex--;
                else if (pos == SlidePosition.Next)
                    ImageIndex++;
                else if (pos == SlidePosition.PrevPage)
                    ImageIndex = Math.Max(0, ImageIndex - SlideShowConfig.PageStep);
                else if (pos == SlidePosition.NextPage)
                    ImageIndex = Math.Min(ImageFiles.Count - 1, ImageIndex + SlideShowConfig.PageStep);

                CanPrevImage = ImageIndex > 0;
                CanNextImage = ImageIndex < ImageFiles.Count - 1;

                if (SlideShow && !CanNextImage)
                    SlideShow = false;

                slideShowElapsedMs = 0;
                slideShowOpening = true;
                Player.OpenAsync(Path.Combine(imageFolder, ImageFiles[ImageIndex]));
                slideShowOpening = false;
            }
        }

        private void SlideShowStop()
        {
            lock (slideShowLock)
            {
                slideShowCancel.Cancel();
                slideShowCancel = new CancellationTokenSource();
                SlideShow = false;
                slideShowElapsedMs += (int)(DateTime.UtcNow.Ticks - slideShowStartedAt) / 10000;
                SlidePlayIcon.Glyph = "\uF5B0"; // Play Icon
            }
        }

        private void SlideShowStart(bool inFullScreen = false, bool andTask = true)
        {
            lock (slideShowLock)
            {
                slideShowCancel.Cancel();
                slideShowCancel = new CancellationTokenSource();
                SlideShow = true;
                if (inFullScreen)
                    Player.FullScreen();

                Player.Activity.ForceIdle();
                SlidePlayIcon.Glyph = "\uE71A"; // Pause Icon

                if (andTask)
                {
                    slideShowStartedAt = DateTime.UtcNow.Ticks;
                    Task.Run(() => SlideShowTask(slideShowCancel.Token));
                }
            }
        }

        private void SlideShowToggleAction()
        {
            if (SlideShow)
                SlideShowStop();
            else if (canNextImage)
                SlideShowStart();
        }

        private async Task SlideShowTask(CancellationToken token)
        {
            try
            {
                if (Player.CanPlay && slideShowElapsedMs < SlideShowConfig.SlideShowTimer)
                    await Task.Delay(SlideShowConfig.SlideShowTimer - slideShowElapsedMs, token);

                while (Player.IsPlaying)
                {
                    await Task.Delay(100, token);
                }

                if (token.IsCancellationRequested || !SlideShow)
                    return;

                this.DispatcherQueue.TryEnqueue(() => SlideShowGoTo(SlidePosition.Next, true));
            }
            catch { }
        }

        private async void ImageDelete()
        {
            if (SlideShowConfig.DeleteConfirmation)
            {
                var dialog = new ContentDialog()
                {
                    Title = "Delete Confirmation",
                    Content = "Are you sure you want to delete this image?",
                    PrimaryButtonText = "Yes",
                    SecondaryButtonText = "No",
                    XamlRoot = this.XamlRoot
                };
                var result = await dialog.ShowAsync();
                if (result != ContentDialogResult.Primary)
                    return;
            }

            Player.Stop();
            try
            {
                File.Delete(Path.Combine(imageFolder, ImageFiles[ImageIndex]));
            }
            catch (Exception)
            {
                return;
            }
        }

        private async void ImageCutCopy(bool copy = true)
        {
            try
            {
                var filePath = ImageFiles.Count > 0 ? Path.Combine(imageFolder, ImageFiles[ImageIndex]) : Player.Playlist.Selected.Url;
                var storageFile = await Windows.Storage.StorageFile.GetFileFromPathAsync(filePath);
                
                var dataPackage = new Windows.ApplicationModel.DataTransfer.DataPackage();
                dataPackage.SetStorageItems(new List<Windows.Storage.IStorageItem> { storageFile });
                dataPackage.RequestedOperation = copy ? Windows.ApplicationModel.DataTransfer.DataPackageOperation.Copy : Windows.ApplicationModel.DataTransfer.DataPackageOperation.Move;
                
                Windows.ApplicationModel.DataTransfer.Clipboard.SetContent(dataPackage);

                if (!copy)
                {
                    if (ImageFiles.Count == 1 || MediaViewer == MediaViewer.Image)
                    {
                        Player.Stop();
                        MediaViewer = MediaViewer.Video;
                    }
                    else
                    {
                        SlideShowGoTo(canNextImage ? SlidePosition.Next : (canPrevImage ? SlidePosition.Prev : SlidePosition.Start));
                    }
                }
            }
            catch (Exception) { }
        }

        private async Task FadeOutMsg(CancellationToken token)
        {
            try
            {
                await Task.Delay(MSG_TIMEOUT, token);
                this.DispatcherQueue.TryEnqueue(() =>
                {
                    if (!token.IsCancellationRequested)
                    {
                        msg = "";
                        Raise(nameof(Msg));
                    }
                });
            }
            catch { }
        }

        private async void BtnSlideShowSettings_Click(object sender, RoutedEventArgs e)
        {
            var stack = new StackPanel { Spacing = 10, Padding = new Thickness(10) };
            
            var timerPanel = new StackPanel { Orientation = Orientation.Horizontal };
            timerPanel.Children.Add(new TextBlock { Text = "Timer (ms): ", Width = 120, VerticalAlignment = VerticalAlignment.Center });
            var timerInput = new TextBox { Text = SlideShowConfig.SlideShowTimer.ToString(), Width = 100 };
            timerPanel.Children.Add(timerInput);
            stack.Children.Add(timerPanel);

            var stepPanel = new StackPanel { Orientation = Orientation.Horizontal };
            stepPanel.Children.Add(new TextBlock { Text = "Page Step: ", Width = 120, VerticalAlignment = VerticalAlignment.Center });
            var stepInput = new TextBox { Text = SlideShowConfig.PageStep.ToString(), Width = 100 };
            stepPanel.Children.Add(stepInput);
            stack.Children.Add(stepPanel);

            var maxPanel = new StackPanel { Orientation = Orientation.Horizontal };
            maxPanel.Children.Add(new TextBlock { Text = "Max Images: ", Width = 120, VerticalAlignment = VerticalAlignment.Center });
            var maxInput = new TextBox { Text = SlideShowConfig.MaxFiles.ToString(), Width = 100 };
            maxPanel.Children.Add(maxInput);
            stack.Children.Add(maxPanel);

            var deletePanel = new StackPanel { Orientation = Orientation.Horizontal };
            deletePanel.Children.Add(new TextBlock { Text = "Delete Confirmation: ", Width = 150, VerticalAlignment = VerticalAlignment.Center });
            var deleteToggle = new ToggleSwitch { IsOn = SlideShowConfig.DeleteConfirmation };
            deletePanel.Children.Add(deleteToggle);
            stack.Children.Add(deletePanel);

            var dialog = new ContentDialog()
            {
                Title = "SlideShow Settings",
                Content = stack,
                PrimaryButtonText = "Save",
                CloseButtonText = "Cancel",
                XamlRoot = this.XamlRoot
            };

            var result = await dialog.ShowAsync();
            if (result == ContentDialogResult.Primary)
            {
                if (int.TryParse(timerInput.Text, out var t)) SlideShowConfig.SlideShowTimer = t;
                if (int.TryParse(stepInput.Text, out var s)) SlideShowConfig.PageStep = s;
                if (int.TryParse(maxInput.Text, out var m)) SlideShowConfig.MaxFiles = m;
                SlideShowConfig.DeleteConfirmation = deleteToggle.IsOn;
                
                App.AppConfig.Save();
            }
        }

        private void BtnMinimize_Click(object sender, RoutedEventArgs e)
        {
            MyFlyleafME.IsMinimized = true;
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e)
        {
            SlideShowDeInit();
            ((App)Application.Current).MainWindow?.Close();
        }
    }

    public enum MediaViewer
    {
        Image,
        Video,
        Slide
    }

    public enum SlidePosition
    {
        Start,
        Prev,
        Next,
        Refresh,
        PrevPage,
        NextPage,
        End
    }
}
