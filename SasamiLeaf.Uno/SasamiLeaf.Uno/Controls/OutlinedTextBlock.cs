using System;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Text;
using Windows.UI;

namespace SasamiLeaf.Uno.Controls
{
    public class OutlinedTextBlock : ContentControl
    {
        private Grid grid;
        private TextBlock foregroundTextBlock;
        private TextBlock[] outlineTextBlocks;

        public OutlinedTextBlock()
        {
            this.IsTabStop = false;
            this.HorizontalContentAlignment = HorizontalAlignment.Center;
            this.VerticalContentAlignment = VerticalAlignment.Center;
            
            grid = new Grid();
            this.Content = grid;

            // 8 directions for the outline
            outlineTextBlocks = new TextBlock[8];
            for (int i = 0; i < 8; i++)
            {
                outlineTextBlocks[i] = new TextBlock();
                grid.Children.Add(outlineTextBlocks[i]);
            }

            foregroundTextBlock = new TextBlock();
            grid.Children.Add(foregroundTextBlock);

            UpdateText();
            UpdateLayoutProperties();
        }

        #region Dependency Properties

        public static readonly DependencyProperty TextProperty = DependencyProperty.Register(
            nameof(Text), typeof(string), typeof(OutlinedTextBlock), new PropertyMetadata(string.Empty, OnPropertyChanged));

        public static readonly DependencyProperty FillProperty = DependencyProperty.Register(
            nameof(Fill), typeof(Brush), typeof(OutlinedTextBlock), new PropertyMetadata(new SolidColorBrush(Colors.White), OnPropertyChanged));

        public static readonly DependencyProperty StrokeProperty = DependencyProperty.Register(
            nameof(Stroke), typeof(Brush), typeof(OutlinedTextBlock), new PropertyMetadata(new SolidColorBrush(Colors.Black), OnPropertyChanged));

        public static readonly DependencyProperty StrokeThicknessProperty = DependencyProperty.Register(
            nameof(StrokeThickness), typeof(double), typeof(OutlinedTextBlock), new PropertyMetadata(2.0, OnPropertyChanged));

        public static readonly DependencyProperty StrokeThicknessInitialProperty = DependencyProperty.Register(
            nameof(StrokeThicknessInitial), typeof(double), typeof(OutlinedTextBlock), new PropertyMetadata(2.0, OnPropertyChanged));

        public static readonly DependencyProperty FontSizeInitialProperty = DependencyProperty.Register(
            nameof(FontSizeInitial), typeof(double), typeof(OutlinedTextBlock), new PropertyMetadata(32.0, OnPropertyChanged));

        public static readonly DependencyProperty TextWrappingProperty = DependencyProperty.Register(
            nameof(TextWrapping), typeof(TextWrapping), typeof(OutlinedTextBlock), new PropertyMetadata(TextWrapping.NoWrap, OnPropertyChanged));

        public static readonly DependencyProperty TextAlignmentProperty = DependencyProperty.Register(
            nameof(TextAlignment), typeof(TextAlignment), typeof(OutlinedTextBlock), new PropertyMetadata(TextAlignment.Left, OnPropertyChanged));

        public string Text
        {
            get => (string)GetValue(TextProperty);
            set => SetValue(TextProperty, value);
        }

        public Brush Fill
        {
            get => (Brush)GetValue(FillProperty);
            set => SetValue(FillProperty, value);
        }

        public Brush Stroke
        {
            get => (Brush)GetValue(StrokeProperty);
            set => SetValue(StrokeProperty, value);
        }

        public double StrokeThickness
        {
            get => (double)GetValue(StrokeThicknessProperty);
            set => SetValue(StrokeThicknessProperty, value);
        }

        public double StrokeThicknessInitial
        {
            get => (double)GetValue(StrokeThicknessInitialProperty);
            set => SetValue(StrokeThicknessInitialProperty, value);
        }

        public double FontSizeInitial
        {
            get => (double)GetValue(FontSizeInitialProperty);
            set => SetValue(FontSizeInitialProperty, value);
        }

        public TextWrapping TextWrapping
        {
            get => (TextWrapping)GetValue(TextWrappingProperty);
            set => SetValue(TextWrappingProperty, value);
        }

        public TextAlignment TextAlignment
        {
            get => (TextAlignment)GetValue(TextAlignmentProperty);
            set => SetValue(TextAlignmentProperty, value);
        }

        #endregion

        private static void OnPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is OutlinedTextBlock otb)
            {
                if (e.Property == TextProperty)
                {
                    otb.UpdateText();
                }
                else
                {
                    otb.UpdateLayoutProperties();
                }
            }
        }

        private void UpdateText()
        {
            var text = Text;
            foregroundTextBlock.Text = text ?? string.Empty;
            for (int i = 0; i < 8; i++)
            {
                outlineTextBlocks[i].Text = text ?? string.Empty;
            }
        }

        private void UpdateLayoutProperties()
        {
            var fill = Fill;
            var stroke = Stroke;
            var thickness = StrokeThickness;
            var fontFamily = FontFamily;
            var fontSize = FontSize;
            var fontWeight = FontWeight;
            var fontStyle = FontStyle;
            var textWrapping = TextWrapping;
            var textAlignment = TextAlignment;

            // Apply to foreground
            foregroundTextBlock.Foreground = fill;
            foregroundTextBlock.FontFamily = fontFamily;
            foregroundTextBlock.FontSize = fontSize;
            foregroundTextBlock.FontWeight = fontWeight;
            foregroundTextBlock.FontStyle = fontStyle;
            foregroundTextBlock.TextWrapping = textWrapping;
            foregroundTextBlock.TextAlignment = textAlignment;

            // Shift positions for outline
            double[] dx = { -1,  0,  1, -1, 1, -1, 0, 1 };
            double[] dy = { -1, -1, -1,  0, 0,  1, 1, 1 };

            for (int i = 0; i < 8; i++)
            {
                var tb = outlineTextBlocks[i];
                tb.Foreground = stroke;
                tb.FontFamily = fontFamily;
                tb.FontSize = fontSize;
                tb.FontWeight = fontWeight;
                tb.FontStyle = fontStyle;
                tb.TextWrapping = textWrapping;
                tb.TextAlignment = textAlignment;
                tb.Margin = new Thickness(dx[i] * thickness, dy[i] * thickness, -dx[i] * thickness, -dy[i] * thickness);
            }
        }
    }
}
