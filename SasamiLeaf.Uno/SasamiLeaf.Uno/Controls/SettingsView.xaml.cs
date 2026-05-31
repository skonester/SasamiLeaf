using System;
using Microsoft.UI.Xaml.Controls;
using FlyleafLib;

namespace SasamiLeaf.Uno.Controls
{
    public sealed partial class SettingsView : UserControl
    {
        private FlyleafME flyleaf;

        public SettingsView(FlyleafME flyleaf)
        {
            this.InitializeComponent();
            this.flyleaf = flyleaf;
            this.DataContext = flyleaf;

            CmbLogLevel.ItemsSource = Enum.GetValues(typeof(FlyleafLib.LogLevel));
            CmbAspectRatio.ItemsSource = FlyleafLib.AspectRatio.AspectRatios;
        }

        public void ApplySettings()
        {
            // Settings are automatically applied via TwoWay bindings,
            // but you can add explicit validations or updates here.
        }
    }
}
