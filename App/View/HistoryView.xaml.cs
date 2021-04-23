using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
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
using Undo;

namespace ElectricSketch.View
{
    /// <summary>
    /// Interaction logic for HistoryView.xaml
    /// </summary>
    public partial class HistoryView : UserControl, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        void RaisePropertyChanged([System.Runtime.CompilerServices.CallerMemberName] string property = "") => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(property));

        public HistoryView()
        {
            InitializeComponent();

            DataContextChanged += OnDataContextChanged;
        }

        private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            // We want to change the appearance of history items depending on them being in the past or in the future.
            // Doing that with a converter that takes the item and determines its position relative to the current
            // does not work, because it will not update, as neither the action nor the history are changed when the
            // current position changes. Note that since these are objects, the binding could only detect changes if
            // another instance was assigned, not if the contents of the instance change.

            Schematic = DataContext as ViewModel.Schematic;

            if (Schematic != null)
            {
                history = new ReadOnlyObservableCollectionTransform<UndoableAction, UndoableActionWrapper>(
                    Schematic.UndoManager.History, (h) => new UndoableActionWrapper(Schematic.UndoManager, h), true);
                history.CollectionChanged += OnCollectionChanged;
                RaisePropertyChanged(nameof(History));
            }
        }

        ViewModel.Schematic Schematic { get; set; }

        public IReadOnlyObservableCollection<UndoableActionWrapper> History => history;
        ReadOnlyObservableCollectionTransform<UndoableAction, UndoableActionWrapper> history;

        void OnCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            sv ??= list.FindFirstChild<ScrollViewer>();
            sv?.ScrollToBottom();
            list.SelectedIndex = list.Items.Count - 1;
        }

        ScrollViewer sv;

        void OnMouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (Schematic != null)
                Schematic.UndoManager.GoToStateAfterAction(((UndoableActionWrapper)list.SelectedItem).Action);
        }
    }

    public class UndoableActionWrapper : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        void RaisePropertyChanged([System.Runtime.CompilerServices.CallerMemberName] string property = "") => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(property));

        public UndoableActionWrapper(UndoManager undoManager, UndoableAction action)
        {
            UndoManager = undoManager;
            Action = action;
            // This works because items cannot be removed from the middle of history.
            Index = undoManager.History.IndexOf(action);
            isFuture = ComputeIsFuture();

            PropertyChangedEventManager.AddHandler(undoManager, (s, e) => IsFuture = ComputeIsFuture(), nameof(Undo.UndoManager.LatestActionIndex));
            PropertyChangedEventManager.AddHandler(action, (s, e) => RaisePropertyChanged(nameof(Description)), nameof(UndoableAction.Description));
        }

        bool ComputeIsFuture() => Index > UndoManager.LatestActionIndex;

        public UndoManager UndoManager { get; }
        public UndoableAction Action { get; }
        public int Index { get; }

        public string Description => Action.Description;
        public bool IsFuture
        {
            get => isFuture;
            set
            {
                if (isFuture == value)
                    return;
                isFuture = value;
                RaisePropertyChanged();
            }
        }
        bool isFuture;
    }

    public class IsFutureConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            var sch = (ViewModel.Schematic)values[1];
            var act = (UndoableAction)values[0];
            return sch.UndoManager.History.IndexOf(act) > sch.UndoManager.LatestActionIndex;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
