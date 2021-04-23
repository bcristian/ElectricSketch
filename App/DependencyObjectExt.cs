using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using System.Windows;
using System.Windows.Media;

namespace ElectricSketch
{
    public static class DependencyObjectExt
    {
        public static T FindAncestor<T>(this DependencyObject @this)
        {
            do
            {
                var parent = VisualTreeHelper.GetParent(@this);
                if (parent is T ofType)
                    return ofType;
                @this = parent;
            }
            while (@this != null);
            return default;
        }

        public static DependencyObject FindAncestorWithParent<T>(this DependencyObject @this)
        {
            do
            {
                var parent = VisualTreeHelper.GetParent(@this);
                if (parent is T)
                    return @this;
                @this = parent;
            }
            while (@this != null);
            return default;
        }

        public static T FindFirstChild<T>(this DependencyObject @this)
        {
            var numChildren = VisualTreeHelper.GetChildrenCount(@this);
            for (var i = 0; i < numChildren; i++)
            {
                var child = VisualTreeHelper.GetChild(@this, i);
                if (child is T result || (result = child.FindFirstChild<T>()) != null)
                    return result;
            }
            return default;
        }

        /// <summary>
        /// Enumerates, depth-first, the descendants of the object that are of the specified type.
        /// </summary>
        /// <param name="topLevelOnly">if true, it will not search below objects that are of the type</param>
        public static void EnumerateDescendants<T>(this DependencyObject @this, IList<T> list, bool topLevelOnly)
        {
            var numChildren = VisualTreeHelper.GetChildrenCount(@this);
            for (var i = 0; i < numChildren; i++)
            {
                var child = VisualTreeHelper.GetChild(@this, i);
                if (child is T result)
                {
                    list.Add(result);
                    if (topLevelOnly)
                        continue;
                }

                child.EnumerateDescendants(list, topLevelOnly);
            }
        }

        /// <summary>
        /// Detects if we're running in Design Mode.
        /// </summary>
        public static bool InDesignMode
        {
            get
            {
                if (!inDesignMode.HasValue)
                {
#if SILVERLIGHT
                    _isInDesignMode = DesignerProperties.IsInDesignTool;
#else
                    var prop = DesignerProperties.IsInDesignModeProperty;
                    inDesignMode = (bool)DependencyPropertyDescriptor.FromProperty(prop, typeof(FrameworkElement)).Metadata.DefaultValue;

                    // Just to be sure
                    if (!inDesignMode.Value && System.Diagnostics.Process.GetCurrentProcess().ProcessName.StartsWith("devenv", StringComparison.Ordinal))
                        inDesignMode = true;
#endif
                }

                return inDesignMode.Value;
            }
        }
        static bool? inDesignMode;
    }
}
