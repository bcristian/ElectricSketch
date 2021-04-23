using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
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
    // The ViewModel.Schematic is the DataContext
    public partial class Schematic : UserControl
    {
        public Schematic()
        {
            InitializeComponent();

            Loaded += OnLoaded;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            var canvas = items.FindFirstChild<SchematicCanvas>();
            minimap.Target = canvas;
            canvas.Focus();
        }
    }

    public class ElementStyleSelector : StyleSelector
    {
        public Style DeviceStyle { get; set; }
        public Style ConnectionStyle { get; set; }

        public override Style SelectStyle(object item, DependencyObject container)
        {
            return item switch
            {
                ViewModel.Device _ => DeviceStyle,
                ViewModel.Connection _ => ConnectionStyle,
                _ => base.SelectStyle(item, container)
            };
        }
    }
}
