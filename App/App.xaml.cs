using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;

namespace ElectricSketch
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public string Title
        {
            get
            {
                if (title == null)
                {
                    var attr = Assembly.GetExecutingAssembly().GetCustomAttribute<AssemblyTitleAttribute>();
                    title = attr.Title;
                }

                return title;
            }
        }

        string title;
    }
}
