using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace ElectricSketch.View
{
    public partial class Library : UserControl
    {
        public Library()
        {
            InitializeComponent();

            // Because if nothing is set we don't get mouse input.
            // It's easy to forget that the default is null, not transparent.
            SetCurrentValue(BackgroundProperty, Brushes.Transparent);
        }

        protected override void OnPreviewMouseMove(MouseEventArgs e)
        {
            base.OnPreviewMouseMove(e);

            if (MainWindow.Instance.DeviceToPlaceOnClick == null)
                MainWindow.Instance.StatusText = "Drag components into schematic. Double click to place several instances.";
        }
    }
}
