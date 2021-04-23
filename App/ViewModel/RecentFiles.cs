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
            Settings.Default.RecentFiles = new System.Collections.Specialized.StringCollection();
            foreach (var s in List)
                Settings.Default.RecentFiles.Add(s);
            Settings.Default.Save();
        }

        public static ObservableCollection<string> List { get; set; } = new ObservableCollection<string>();
    }
}
