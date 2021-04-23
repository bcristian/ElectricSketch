using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;

namespace ElectricSketch.View
{
    /// <summary>
    /// Wraps all items in ContentControl's.
    /// </summary>
    /// <remarks>
    /// We need this so that we can use data templates that create the graphics for the devices, and
    /// then wrap those in the controls we need, e.g. in the schematic view and in the library view.
    /// </remarks>
    public class MyItemsControl : ItemsControl
    {
        protected override bool IsItemItsOwnContainerOverride(object item) => false;

        // The default is ContentPresenter, but we want something with a template.
        protected override DependencyObject GetContainerForItemOverride() => new ContentControl();
    }
}
