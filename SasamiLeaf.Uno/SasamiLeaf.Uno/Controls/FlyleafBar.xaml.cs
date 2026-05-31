using System;
using System.ComponentModel;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using FlyleafLib.MediaPlayer;

namespace SasamiLeaf.Uno.Controls
{
    public sealed partial class FlyleafBar : UserControl
    {
        public static readonly DependencyProperty PlayerProperty =
            DependencyProperty.Register(nameof(Player), typeof(Player), typeof(FlyleafBar), new PropertyMetadata(null, OnPlayerChanged));

        public Player Player
        {
            get => (Player)GetValue(PlayerProperty);
            set => SetValue(PlayerProperty, value);
        }

        public FlyleafBar()
        {
            this.InitializeComponent();
            this.DataContextChanged += (s, e) =>
            {
                if (DataContext is FlyleafME flyleaf)
                {
                    this.Player = flyleaf.Player;
                }
            };
        }

        private static void OnPlayerChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is FlyleafBar bar)
            {
                if (e.OldValue is Player oldPlayer)
                {
                    oldPlayer.PropertyChanged -= bar.OnPlayerPropertyChanged;
                    if (oldPlayer.Audio != null)
                        oldPlayer.Audio.PropertyChanged -= bar.OnAudioPropertyChanged;
                }

                if (e.NewValue is Player newPlayer)
                {
                    newPlayer.PropertyChanged += bar.OnPlayerPropertyChanged;
                    if (newPlayer.Audio != null)
                        newPlayer.Audio.PropertyChanged += bar.OnAudioPropertyChanged;
                    bar.UpdatePlayPauseIcon(newPlayer.Status);
                    if (newPlayer.Audio != null)
                        bar.UpdateVolumeIcon(newPlayer.Audio.Mute, newPlayer.Audio.Volume);
                }
            }
        }

        private void OnPlayerPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (sender is Player player)
            {
                if (e.PropertyName == nameof(Player.Status))
                {
                    this.DispatcherQueue.TryEnqueue(() => UpdatePlayPauseIcon(player.Status));
                }
            }
        }

        private void OnAudioPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (sender is Audio audio)
            {
                if (e.PropertyName == nameof(Audio.Mute) || e.PropertyName == nameof(Audio.Volume))
                {
                    this.DispatcherQueue.TryEnqueue(() => UpdateVolumeIcon(audio.Mute, audio.Volume));
                }
            }
        }

        private void UpdatePlayPauseIcon(Status status)
        {
            if (status == Status.Playing)
            {
                PlayIcon.Symbol = Symbol.Pause;
            }
            else
            {
                PlayIcon.Symbol = Symbol.Play;
            }
        }

        private void UpdateVolumeIcon(bool mute, int volume)
        {
            if (mute || volume == 0)
            {
                VolumeIcon.Symbol = Symbol.Mute;
            }
            else if (volume < 33)
            {
                VolumeIcon.Symbol = Symbol.Volume; // or Symbol.VolumeLow if available
            }
            else
            {
                VolumeIcon.Symbol = Symbol.Volume;
            }
        }
    }
}
