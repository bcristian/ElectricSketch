using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace ElectricSketch
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        void RaisePropertyChanged([CallerMemberName] string property = "") => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(property));

        public MainWindow()
        {
            Instance = this;
            InitializeComponent();
            DataContext = this;

            Library = ViewModel.Library.Load();

            ViewModel.RecentFiles.Load();
            recentFiles.ItemsSource = ViewModel.RecentFiles.List;

            SetNewSchematic(null, null);
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            base.OnClosing(e);

            if (!e.Cancel && !IsItOKToDiscardCurrentData())
                e.Cancel = true;
        }

        public static MainWindow Instance { get; private set; }

        void OnHelp(object sender, ExecutedRoutedEventArgs e)
        {
            try { System.Diagnostics.Process.Start(Properties.Settings.Default.HelpLink); }
            catch { }
        }

        public static readonly RoutedUICommand OpenRecentFileCommand = new();

        bool IsItOKToDiscardCurrentData()
        {
            if (!DataHasChanged())
                return true;

            var response = MessageBox.Show("Save changes?", "Question", MessageBoxButton.YesNoCancel, MessageBoxImage.Question);
            return response switch
            {
                MessageBoxResult.Yes => Save(),
                MessageBoxResult.No => true,
                _ => false,
            };
        }

        private void OnOpen(object sender, ExecutedRoutedEventArgs e)
        {
            if (!IsItOKToDiscardCurrentData())
                return;

            var dlg = new Microsoft.Win32.OpenFileDialog
            {
                DefaultExt = ".esk",
                Filter = "Electric sketch files (*.esk)|*.esk|All files|*.*"
            };
            if (dlg.ShowDialog() == true)
                LoadFromFile(dlg.FileName);
        }

        private void OnOpenRecentFile(object sender, ExecutedRoutedEventArgs e)
        {
            if (!IsItOKToDiscardCurrentData())
                return;

            LoadFromFile((string)e.Parameter);
        }

        private void OnSave(object sender, ExecutedRoutedEventArgs e)
        {
            Save();
        }

        bool Save()
        {
            if (string.IsNullOrEmpty(currentFilePath))
                return SaveAs();
            return SaveToFile(currentFilePath);
        }

        void OnSaveAs(object sender, ExecutedRoutedEventArgs e)
        {
            SaveAs();
        }

        bool SaveAs()
        {
            var dlg = new Microsoft.Win32.SaveFileDialog
            {
                DefaultExt = ".esk",
                Filter = "Electric sketch files (*.esk)|*.esk|All files|*.*"
            };
            return dlg.ShowDialog() == true && SaveToFile(dlg.FileName);
        }

        private void OnNew(object sender, ExecutedRoutedEventArgs e)
        {
            if (!IsItOKToDiscardCurrentData())
                return;

            SetNewSchematic(null, null);
        }

        private void OnMainWindowClosing(object sender, CancelEventArgs e)
        {
            if (!IsItOKToDiscardCurrentData())
                e.Cancel = true;
        }

        public static readonly RoutedUICommand ImportCommand = new();

        private void OnImport(object sender, ExecutedRoutedEventArgs e)
        {
            var dlg = new Microsoft.Win32.OpenFileDialog
            {
                DefaultExt = ".esk",
                Filter = "Electric sketch files (*.esk)|*.esk|All files|*.*"
            };
            if (dlg.ShowDialog() == true)
                ImportFromFile(dlg.FileName);
        }


        public string CurrentFilePath
        {
            get => currentFilePath;
            set
            {
                if (currentFilePath == value)
                    return;
                currentFilePath = value;
                RaisePropertyChanged();
            }
        }
        string currentFilePath;

        public ViewModel.Schematic Schematic
        {
            get => schematic;
            set
            {
                if (schematic == value)
                    return;

                schematic = value;

                SetLastSavedAction();

                RaisePropertyChanged();
            }
        }

        ViewModel.Schematic schematic;

        Undo.UndoableAction lastSavedAction;

        void SetLastSavedAction()
        {
            lastSavedAction = GetCurrentAction();
        }

        Undo.UndoableAction GetCurrentAction()
        {
            if (schematic != null && schematic.UndoManager.LatestActionIndex >= 0)
                return schematic.UndoManager.History[schematic.UndoManager.LatestActionIndex];
            return null;
        }

        void LoadFromFile(string filePath)
        {
            Model.Schematic sch;
            try
            {
                sch = Model.Serialization.ReadFile(filePath);
            }
            catch (Exception)
            {
                MessageBox.Show($"Failed to read the file {filePath}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);

                // Remove the file from the recent list on failure to load.
                if (ViewModel.RecentFiles.List.Remove(filePath))
                    ViewModel.RecentFiles.Save();

                // Do nothing if we have a schematic. Otherwise fall through with null, which is equivalent to calling new.
                if (Schematic != null)
                    return;

                sch = null;
            }

            SetNewSchematic(sch, filePath);
        }

        void SetNewSchematic(Model.Schematic sch, string filePath)
        {
            if (sch == null)
            {
                sch = new Model.Schematic();
                filePath = null;
            }

            Schematic = new ViewModel.Schematic(sch);

            CurrentFilePath = filePath;

            if (!string.IsNullOrEmpty(filePath))
                AddCurrentSourceToRecentFiles();

            UpdateWindowTitle();
        }

        bool SaveToFile(string filePath)
        {
            if (!Model.Serialization.WriteFile(Schematic.CreateModel(), filePath))
                return false;

            SetLastSavedAction();

            if (filePath != currentFilePath)
            {
                currentFilePath = filePath;
                AddCurrentSourceToRecentFiles();
                UpdateWindowTitle();
            }

            return true;
        }

        void AddCurrentSourceToRecentFiles()
        {
            var i = ViewModel.RecentFiles.List.IndexOf(currentFilePath);
            if (i >= 0)
                ViewModel.RecentFiles.List.RemoveAt(i);
            ViewModel.RecentFiles.List.Insert(0, currentFilePath);
            ViewModel.RecentFiles.Save();
        }

        void UpdateWindowTitle()
        {
            if (currentFilePath == null)
                Title = $"{((App)Application.Current).Title} - new design";
            else
                Title = $"{((App)Application.Current).Title} - {currentFilePath}";
        }

        bool DataHasChanged()
        {
            return GetCurrentAction() != lastSavedAction;
        }

        void ImportFromFile(string filePath)
        {
            Model.Schematic sch;
            try
            {
                sch = Model.Serialization.ReadFile(filePath);
            }
            catch (Exception)
            {
                MessageBox.Show($"Failed to read the file {filePath}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);

                // Remove the file from the recent list on failure to load.
                if (ViewModel.RecentFiles.List.Remove(filePath))
                    ViewModel.RecentFiles.Save();

                return;
            }

            // If we don't have a schematic opened, just use the imported one.
            if (Schematic == null)
                SetNewSchematic(sch, null);
            else
                Schematic.Import(sch, filePath);
        }


        public ViewModel.Library Library
        {
            get => library;
            set
            {
                if (library == value)
                    return;
                library = value;
                RaisePropertyChanged();
            }
        }
        ViewModel.Library library = new();


        public string StatusText
        {
            get => statusText;
            set
            {
                if (statusText == value)
                    return;
                statusText = value;
                RaisePropertyChanged();
            }
        }
        string statusText;

        public ViewModel.Device DeviceToPlaceOnClick
        {
            get => deviceToPlaceOnClick;
            set
            {
                if (deviceToPlaceOnClick == value)
                    return;
                deviceToPlaceOnClick = value;
                RaisePropertyChanged();
                DeviceToPlaceOnClickChanged?.Invoke();
            }
        }
        ViewModel.Device deviceToPlaceOnClick;

        public event Action DeviceToPlaceOnClickChanged;

        public bool InSimulation => Schematic != null && Schematic.InSimulation;

        protected override void OnPreviewKeyDown(KeyEventArgs e)
        {
            base.OnPreviewKeyDown(e);

            if (e.Key == Key.Escape && DeviceToPlaceOnClick != null)
                DeviceToPlaceOnClick = null;
        }

        void CanPlay(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = Schematic != null && !Schematic.InSimulation;
            e.Handled = true;
        }

        void OnPlay(object sender, ExecutedRoutedEventArgs e)
        {
            Schematic.StartSimulation();
            RaisePropertyChanged(nameof(InSimulation));
        }

        void CanStop(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = Schematic != null && Schematic.InSimulation;
            e.Handled = true;
        }

        void OnStop(object sender, ExecutedRoutedEventArgs e)
        {
            Schematic.StopSimulation();
            RaisePropertyChanged(nameof(InSimulation));
        }
    }

    public class SimulationErrorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is not ElectricLib.CircuitError err)
                return string.Empty;

            return err.Code switch
            {
                ElectricLib.ErrorCode.IncompatiblePotentialsOnDevice => "Incompatible Potentials On Device",
                ElectricLib.ErrorCode.DangerousSwitch => "Dangerous Switch",
                ElectricLib.ErrorCode.InvalidSupplyVoltage => "Invalid Supply Voltage",
                ElectricLib.ErrorCode.InvalidConnections => "Invalid Device Connections",
                ElectricLib.ErrorCode.VoltageConflict => "Voltage Conflict",
                ElectricLib.ErrorCode.SeriesConnection => "Series Connection",
                ElectricLib.ErrorCode.Ringing => "Ringing",
                _ => "Unknown Error"
            };
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}
