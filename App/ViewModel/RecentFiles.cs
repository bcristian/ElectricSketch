using ElectricSketch.Properties;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;

namespace ElectricSketch.ViewModel
{
    public static class RecentFiles
    {
        public static void Load()
        {
            if (Settings.Default.RecentFiles != null)
            {
                foreach (var s in Settings.Default.RecentFiles)
                    List.Add(s);
            }
        }

        public static void Save()
        {
            Settings.Default.RecentFiles = [.. List];
            Settings.Default.Save();
        }

        public static ObservableCollection<string> List { get; set; } = [];
    }
}
