using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using Undo;

namespace ElectricSketch.View
{
    public sealed class SchematicCanvas : Canvas
    {
        public SchematicCanvas()
        {
            // Because if nothing is set we don't get mouse input.
            // It's easy to forget that the default is null, not transparent.
            SetCurrentValue(BackgroundProperty, Brushes.Transparent);

            SetCurrentValue(AllowDropProperty, true);

            DataContextChanged += OnDataContextChanged;
            Loaded += OnLoaded;

            Focusable = true; // so that we receive commands

            CommandBindings.Add(new CommandBinding(ApplicationCommands.Undo, OnUndo, CanUndo));
            CommandBindings.Add(new CommandBinding(ApplicationCommands.Redo, OnRedo, CanRedo));
            CommandBindings.Add(new CommandBinding(ApplicationCommands.Copy, OnCopy, CanCopy));
            CommandBindings.Add(new CommandBinding(ApplicationCommands.Cut, OnCut, CanCut));
            CommandBindings.Add(new CommandBinding(ApplicationCommands.Paste, OnPaste, CanPaste));
            CommandBindings.Add(new CommandBinding(ApplicationCommands.Delete, OnDelete, CanDelete));
            CommandBindings.Add(new CommandBinding(ApplicationCommands.SelectAll, OnSelectAll));

            // Used for deleting just the object, without propagating along the connections.
            ApplicationCommands.Delete.InputGestures.Add(new KeyGesture(Key.Delete, ModifierKeys.Control));
        }

        void OnLoaded(object sender, RoutedEventArgs e)
        {
            scrollViewer = this.FindAncestor<ScrollViewer>();
        }

        void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            Schematic = DataContext as ViewModel.Schematic;
        }

        public ViewModel.Schematic Schematic { get; private set; }

        ScrollViewer scrollViewer;


        protected override void OnMouseDown(MouseButtonEventArgs e)
        {
            base.OnMouseDown(e);

            Focus();

            if (e.ChangedButton == MouseButton.Left)
            {
                if (!MainWindow.Instance.InSimulation && MainWindow.Instance.DeviceToPlaceOnClick != null)
                {
                    AddDevice_OnMouseDown(e);
                    lmbDragStart = null;
                }
                else
                {
                    lmbDragStart = e.GetPosition(this);
                    lmbDragStartDevice = null;
                    lmbDragStartPin = null;
                    if (e.OriginalSource is FrameworkElement fe)
                    {
                        lmbDragStartDevice = DeviceContainer.GetDevice(fe);
                        lmbDragStartPin = DeviceContainer.GetPin(fe);
                    }

                    if (MainWindow.Instance.InSimulation)
                    {
                        if (lmbDragStartPin == null)
                            Selection_OnMouseDown(e);
                    }
                    else if (lmbDragStartPin != null || addConnPins.Count > 0)
                        AddConnection_OnMouseDown(e);
                    else
                    {
                        Selection_OnMouseDown(e);

                        if (lmbDragStartDevice != null)
                            DragDevices_OnMouseDown(e);
                    }
                }
            }
            else if (e.ChangedButton == MouseButton.Middle)
            {
                PanZoom_OnMouseDown(e);
            }
        }

        Point? lmbDragStart;
        ViewModel.Device lmbDragStartDevice;
        ViewModel.Pin lmbDragStartPin;

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);

            Focus();

            if (e.LeftButton != MouseButtonState.Pressed)
            {
                lmbDragStart = null;
                lmbDragStartDevice = null;
                lmbDragStartPin = null;
            }

            if (MainWindow.Instance.InSimulation)
            {
                Cursor = Cursors.Arrow;
                if (lmbDragStart.HasValue && lmbDragStartDevice == null)
                    Selection_OnMouseMove(e);
            }
            else
            {
                if (MainWindow.Instance.DeviceToPlaceOnClick != null)
                {
                    Cursor = e.MiddleButton == MouseButtonState.Pressed ? Cursors.ScrollAll : Cursors.UpArrow;
                }
                else
                {
                    MainWindow.Instance.StatusText = "Drag to select devices. Ctrl toggles selection. Shift adds to selection. Alt removes.";
                    Cursor = e.MiddleButton == MouseButtonState.Pressed ? Cursors.ScrollAll : Cursors.Arrow;
                }

                if (addConnPins.Count > 0)
                    AddConnection_OnMouseMove(e);

                if (lmbDragStart.HasValue && addConnPins.Count == 0)
                {
                    if (lmbDragStartDevice != null)
                        DragDevices_OnMouseMove(e);
                    else
                        Selection_OnMouseMove(e);
                }
            }

            PanZoom_OnMouseMove(e);
        }

        protected override void OnMouseUp(MouseButtonEventArgs e)
        {
            base.OnMouseUp(e);

            if (!MainWindow.Instance.InSimulation)
            {
                if (addConnPins.Count > 0)
                    AddConnection_OnMouseUp(e);
                else
                    DragDevices_OnMouseUp(e);
            }

            lmbDragStart = null;
            lmbDragStartDevice = null;
            lmbDragStartPin = null;

            ReleaseMouseCapture();
        }

        protected override void OnPreviewMouseWheel(MouseWheelEventArgs e)
        {
            base.OnPreviewMouseWheel(e);

            PanZoom_OnPreviewMouseWheel(e);
        }

        protected override void OnPreviewKeyDown(KeyEventArgs e)
        {
            base.OnPreviewKeyDown(e);

            e.Handled = e.IsRepeat;
            if (e.IsRepeat)
                return;

            if (!MainWindow.Instance.InSimulation)
                AddConnection_OnPreviewKeyDown(e);

            switch (e.Key)
            {
                case Key.Space:
                    foreach (var dev in Schematic.SelectedDevices)
                        dev.ActionPress();
                    e.Handled = true;
                    break;
                case Key.Right:
                    foreach (var dev in Schematic.SelectedDevices)
                        dev.NextPress();
                    e.Handled = true;
                    break;
                case Key.Left:
                    foreach (var dev in Schematic.SelectedDevices)
                        dev.PrevPress();
                    e.Handled = true;
                    break;
            }
        }

        protected override void OnPreviewKeyUp(KeyEventArgs e)
        {
            base.OnPreviewKeyUp(e);

            e.Handled = e.IsRepeat;
            if (e.IsRepeat)
                return;

            switch (e.Key)
            {
                case Key.Space:
                    foreach (var dev in Schematic.SelectedDevices)
                        dev.ActionRelease();
                    e.Handled = true;
                    break;
                case Key.Right:
                    foreach (var dev in Schematic.SelectedDevices)
                        dev.NextRelease();
                    e.Handled = true;
                    break;
                case Key.Left:
                    foreach (var dev in Schematic.SelectedDevices)
                        dev.PrevRelease();
                    e.Handled = true;
                    break;
            }
        }

        protected override void OnLostFocus(RoutedEventArgs e)
        {
            base.OnLostFocus(e);
            AbortAddingConnection();
        }


        #region Adding connections

        void AddConnection_OnMouseDown(MouseButtonEventArgs e)
        {
            // Two ways to add connections:
            //  - drag a pin over another or a connection
            //  - click a pin, click empty spaces to add intermediary junctions (hit backspace to remove the most recent) click a pin or connection to finish.
            // Either way, hitting ESC cancels (and removes the connections).

            // Ignore if we're already adding a connection, will be handled by mouse up.
            if (addConnPins.Count > 0)
                return;

            addConnPins.Add(lmbDragStartPin);
            connectByDragging = true; // We'll change it if we detect a click

            e.Handled = true;
        }

        readonly List<ViewModel.Pin> addConnPins = [];
        bool connectByDragging;
        UndoableAction addConnAction;

        void AddConnection_OnMouseUp(MouseButtonEventArgs e)
        {
            if (connectByDragging)
            {
                if (addConnPins.Count == 1) // so we haven't moved from the initial pin yet
                {
                    connectByDragging = false;
                    AddConnectionSegment(e.GetPosition(this));
                }
                else
                {
                    // If we're over another pin or connection, connect to it. Otherwise (i.e. original pin or empty space) abort.
                    var pin = GetPinAtCursor();
                    if (pin == addConnPins[0])
                        AbortAddingConnection();
                    else if (pin != null)
                        FinishAddingConnection(pin);
                    else
                    {
                        var conn = GetConnectionAtCursor();
                        if (conn != null)
                            FinishAddingConnection(conn);
                        else
                            AbortAddingConnection();
                    }
                }
            }
            else
            {
                // Ignore clicking on the last 2 pins (the last because we'd be connecting to itself, the previous because we'd be adding two overlapping connections).
                System.Diagnostics.Debug.Assert(addConnPins.Count >= 2);
                var pin = GetPinAtCursor();
                if (pin == addConnPins[^2] || (addConnPins.Count > 2 && pin == addConnPins[^3]))
                    return;
                if (pin != null)
                    FinishAddingConnection(pin);
                else
                {
                    var conn = GetConnectionAtCursor();
                    if (conn != null)
                        FinishAddingConnection(conn);
                    else
                        AddConnectionSegment(e.GetPosition(this));
                }
            }
        }

        void AddConnectionSegment(Point pos)
        {
            if (addConnAction == null)
            {
                addConnAction = new UndoableAction() { Description = $"Add connection from {addConnPins[0]}" };
                Schematic.UndoManager.Do(addConnAction, true);
            }

            // Create a junction at the current mouse position and connect the latest pin to it.
            // Then add its pin to the list.
            var iPos = ViewModel.Schematic.SnapToGrid(pos);
            var j = new ViewModel.Devices.Junction(iPos);
            Schematic.AddDevice(j, false);
            Schematic.AddConnection(addConnPins[^1], j.Pins[0]);
            addConnPins.Add(j.Pins[0]);
        }

        void AddConnection_OnMouseMove(MouseEventArgs e)
        {
            if ((lmbDragStartPin == null && connectByDragging)) // safety catches
            {
                AbortAddingConnection();
                return;
            }

            if (addConnPins.Count == 1)
                AddConnectionSegment(e.GetPosition(this));
            else
                // Keep the last junction attached to the cursor.
                addConnPins[^1].Device.Position = ViewModel.Schematic.SnapToGrid(e.GetPosition(this));
        }

        void AddConnection_OnPreviewKeyDown(KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                AbortAddingConnection();
                e.Handled = true;
            }
            else if (e.Key == Key.Back)
            {
                if (connectByDragging)
                    AbortAddingConnection();
                else
                    RemoveLatestJunction();
                e.Handled = true;
            }
        }

        ViewModel.Pin GetPinAtCursor()
        {
            ViewModel.Pin result = null;
            HitTestResultBehavior HitTestResultCallback(HitTestResult ht)
            {
                if (ht.VisualHit is FrameworkElement fe)
                {
                    var pin = DeviceContainer.GetPin(fe);
                    if (pin != null && (addConnPins.Count == 0 || pin != addConnPins[^1]))
                    {
                        result = pin;
                        return HitTestResultBehavior.Stop;
                    }
                }
                return HitTestResultBehavior.Continue;
            };

            var pos = Mouse.GetPosition(this);
            VisualTreeHelper.HitTest(this, null, HitTestResultCallback, new PointHitTestParameters(pos));
            if (result == null)
                VisualTreeHelper.HitTest(this, null, HitTestResultCallback, new PointHitTestParameters(Converters.PointConverter.Convert(ViewModel.Schematic.SnapToGrid(pos))));

            return result;
        }

        ViewModel.Connection GetConnectionAtCursor()
        {
            ViewModel.Connection result = null;
            HitTestResultBehavior HitTestResultCallback(HitTestResult ht)
            {
                if (ht.VisualHit is FrameworkElement fe)
                {
                    var conn = DeviceContainer.GetConnection(fe);
                    if (conn != null && (addConnPins.Count == 0 || !conn.Pins.Contains(addConnPins[^1])))
                    {
                        result = conn;
                        return HitTestResultBehavior.Stop;
                    }
                }
                return HitTestResultBehavior.Continue;
            };

            VisualTreeHelper.HitTest(this, null, HitTestResultCallback, new PointHitTestParameters(Mouse.GetPosition(this)));
            return result;
        }

        ViewModel.Device GetDeviceAtCursor()
        {
            ViewModel.Device result = null;
            HitTestResultBehavior HitTestResultCallback(HitTestResult ht)
            {
                if (ht.VisualHit is FrameworkElement fe)
                {
                    var dev = DeviceContainer.GetDevice(fe);
                    if (dev != null)
                    {
                        result = dev;
                        return HitTestResultBehavior.Stop;
                    }
                }
                return HitTestResultBehavior.Continue;
            };

            VisualTreeHelper.HitTest(this, null, HitTestResultCallback, new PointHitTestParameters(Mouse.GetPosition(this)));
            return result;
        }

        void RemoveLatestJunction()
        {
            if (addConnPins.Count < 3)
                return;
            Schematic.AddConnection(addConnPins[^3], addConnPins[^1]);
            Schematic.RemoveDevice(addConnPins[^2].Device, false);
            addConnPins[^2] = addConnPins[^1];
            addConnPins.RemoveAt(addConnPins.Count - 1);
        }

        void FinishAddingConnection(ViewModel.Pin pin)
        {
            Schematic.AddConnection(addConnPins[^2], pin);
            Schematic.RemoveDevice(addConnPins[^1].Device, false);

            Schematic.UndoManager.Pop(addConnAction);
            addConnAction.Description = $"Connected {addConnPins[0]} to {pin}";

            addConnAction = null;
            addConnPins.Clear();
        }

        void FinishAddingConnection(ViewModel.Connection conn)
        {
            Schematic.AddConnection(addConnPins[^1], conn.Pins[0]);
            Schematic.AddConnection(addConnPins[^1], conn.Pins[1]);
            Schematic.RemoveConnection(conn, false);

            Schematic.UndoManager.Pop(addConnAction);
            addConnAction.Description = $"Connected {addConnPins[0]} between {conn.Pins[0]} and {conn.Pins[1]}";

            addConnAction = null;
            addConnPins.Clear();
        }

        void AbortAddingConnection()
        {
            if (addConnAction == null)
                return;

            Schematic.UndoManager.Pop(addConnAction);
            Schematic.UndoManager.Undo();
            Schematic.UndoManager.PurgeFuture();

            for (int i = addConnPins.Count - 1; i > 0; i--) // not the first one
                System.Diagnostics.Debug.Assert(addConnPins[i].Device.Schematic == null);

            addConnAction = null;
            addConnPins.Clear();
        }

        #endregion


        #region Dragging devices

        void DragDevices_OnMouseDown(MouseButtonEventArgs _)
        {
            CaptureMouse();

            deviceDragPos.Clear();

            // Don't allow moving to negative positions.
            minDragDevPos = new System.Drawing.Point(int.MaxValue, int.MaxValue);
            foreach (var dev in Schematic.SelectedDevices)
            {
                deviceDragPos[dev] = dev.Position;
                minDragDevPos.X = Math.Min(minDragDevPos.X, dev.Position.X);
                minDragDevPos.Y = Math.Min(minDragDevPos.Y, dev.Position.Y);
            }

            // Set the undo manager to merge all subsequent modifications until releasing the mouse.
            // Without this multiple changes are recorded if the user stops dragging for more than the
            // regular merging interval, even if the button is still pressed.
            // HACK safeguard against debug breaks and other reasons for not putting the original back
            // and getting stuck with infinite for ever.
            if (!normalMergeTimeLimit.HasValue)
                normalMergeTimeLimit = Schematic.UndoManager.MergeTimeLimit;
            Schematic.UndoManager.MergeTimeLimit = TimeSpan.MaxValue;
            Schematic.UndoManager.DontMergeNextChange = true;
        }

        System.Drawing.Point minDragDevPos;
        TimeSpan? normalMergeTimeLimit;

        readonly Dictionary<ViewModel.Device, System.Drawing.Point> deviceDragPos = [];

        void DragDevices_OnMouseMove(MouseEventArgs e)
        {
            var mousePos = e.GetPosition(this);
            var delta = mousePos - lmbDragStart.Value;
            delta.X = Math.Max(-minDragDevPos.X, delta.X);
            delta.Y = Math.Max(-minDragDevPos.Y, delta.Y);

            foreach (var d in deviceDragPos.Keys)
                d.PositionWillChange = true;

            foreach (var kvp in deviceDragPos)
                kvp.Key.Position = ViewModel.Schematic.SnapToGrid(kvp.Value.X + delta.X, kvp.Value.Y + delta.Y);
        }

        void DragDevices_OnMouseUp(MouseButtonEventArgs _)
        {
            deviceDragPos.Clear();
            if (normalMergeTimeLimit.HasValue)
                Schematic.UndoManager.MergeTimeLimit = normalMergeTimeLimit.Value;
        }

        #endregion


        #region Selection

        void OnSelectAll(object sender, ExecutedRoutedEventArgs e)
        {
            Schematic.SelectAll();
        }

        void Selection_OnMouseDown(MouseButtonEventArgs e)
        {
            if (lmbDragStartDevice != null)
            {
                if (Keyboard.Modifiers.HasFlag(ModifierKeys.Control))
                    lmbDragStartDevice.IsSelected = !lmbDragStartDevice.IsSelected;
                else if (Keyboard.Modifiers.HasFlag(ModifierKeys.Shift))
                    lmbDragStartDevice.IsSelected = true;
                else if (Keyboard.Modifiers.HasFlag(ModifierKeys.Alt))
                    lmbDragStartDevice.IsSelected = false;
                else if (!lmbDragStartDevice.IsSelected)
                {
                    Schematic.UnselectAll();
                    lmbDragStartDevice.IsSelected = true;
                }
            }
            else if (!Keyboard.Modifiers.HasFlag(ModifierKeys.Shift)
                && !Keyboard.Modifiers.HasFlag(ModifierKeys.Control)
                && !Keyboard.Modifiers.HasFlag(ModifierKeys.Alt))
                Schematic.UnselectAll();

            e.Handled = true;
        }

        void Selection_OnMouseMove(MouseEventArgs e)
        {
            AdornerLayer.GetAdornerLayer(this)?.Add(new SelectionAdorner(this, lmbDragStart.Value));
            e.Handled = true;
        }

        #endregion


        #region Pan and Zoom

        void PanZoom_OnMouseDown(MouseButtonEventArgs e)
        {
            if (scrollViewer == null)
                return;
            prevCanvasDragPoint = e.GetPosition(scrollViewer);
            Cursor = Cursors.ScrollAll;
            e.Handled = true;
        }

        void PanZoom_OnMouseMove(MouseEventArgs e)
        {
            if (scrollViewer == null)
                return;

            if (e.MiddleButton == MouseButtonState.Pressed)
            {
                if (!prevCanvasDragPoint.HasValue)
                    prevCanvasDragPoint = e.GetPosition(scrollViewer);
                else
                {
                    var pos = e.GetPosition(scrollViewer);
                    var d = pos - prevCanvasDragPoint.Value;
                    scrollViewer.ScrollToHorizontalOffset(scrollViewer.HorizontalOffset + d.X);
                    scrollViewer.ScrollToVerticalOffset(scrollViewer.VerticalOffset + d.Y);
                    prevCanvasDragPoint = pos;
                }
            }
            else
            {
                prevCanvasDragPoint = null;
            }
        }

        void PanZoom_OnPreviewMouseWheel(MouseWheelEventArgs e)
        {
            if (!Keyboard.Modifiers.HasFlag(ModifierKeys.Control) || cannotZoom)
                return;

            if (scaler == null)
            {
                scaler = LayoutTransform as ScaleTransform;
                if (scaler == null)
                {
                    if (LayoutTransform != Transform.Identity)
                    {
                        cannotZoom = true;
                        return;
                    }
                    LayoutTransform = scaler = new ScaleTransform();
                }
            }

            var s = Math.Clamp(scaler.ScaleX + .1 * e.Delta / 120, .20, 2);
            if (s != scaler.ScaleX)
                scaler.ScaleX = scaler.ScaleY = s;

            e.Handled = true;
        }

        Point? prevCanvasDragPoint;

        bool cannotZoom = false;
        ScaleTransform scaler;

        protected override Size MeasureOverride(Size _)
        {
            var childConstraint = new Size(double.PositiveInfinity, double.PositiveInfinity);

            var size = new Size();
            foreach (UIElement child in Children)
            {
                if (child == null)
                    continue;

                var left = GetLeft(child);
                if (double.IsNaN(left))
                    left = 0;
                var top = GetTop(child);
                if (double.IsNaN(top))
                    top = 0;

                child.Measure(childConstraint);
                if (child.IsMeasureValid)
                {
                    var desiredSize = child.DesiredSize;

                    size.Width = Math.Max(size.Width, left + desiredSize.Width);
                    size.Height = Math.Max(size.Height, top + desiredSize.Height);
                }
            }

            // Add some margin.
            size.Width += 30;
            size.Height += 30;
            return size;
        }

        protected override Size ArrangeOverride(Size arrangeSize)
        {
            base.ArrangeOverride(arrangeSize);

            var size = arrangeSize;
            var childConstraint = new Size(double.PositiveInfinity, double.PositiveInfinity);
            foreach (UIElement child in Children)
            {
                if (child == null)
                    continue;

                var left = GetLeft(child);
                if (double.IsNaN(left))
                    left = 0;
                var top = GetTop(child);
                if (double.IsNaN(top))
                    top = 0;

                child.Measure(childConstraint);
                if (child.IsMeasureValid)
                {
                    var desiredSize = child.DesiredSize;

                    size.Width = Math.Max(size.Width, left + desiredSize.Width);
                    size.Height = Math.Max(size.Height, top + desiredSize.Height);
                }
            }

            if (size != arrangeSize)
                InvalidateMeasure();
            return size;
        }

        #endregion


        #region Adding devices

        protected override void OnDrop(DragEventArgs e)
        {
            base.OnDrop(e);

            if (e.Data.GetData("Electrical Device") is not ViewModel.Device dev)
                return;

            Schematic.UnselectAll();
            AddDevice(dev.Clone(), e.GetPosition(this));

            e.Handled = true;
        }

        void AddDevice_OnMouseDown(MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed
                && e.RightButton == MouseButtonState.Released
                && e.MiddleButton == MouseButtonState.Released)
            {
                Schematic.UnselectAll();
                AddDevice(MainWindow.Instance.DeviceToPlaceOnClick.Clone(), e.GetPosition(this));
                e.Handled = true;
            }
        }

        ViewModel.Device AddDevice(ViewModel.Device dev, Point pos)
        {
            dev.Position = ViewModel.Schematic.SnapToGrid(pos);
            Schematic.AddDevice(dev, true);
            Schematic.Select(dev);
            return dev;
        }

        #endregion


        #region Copy, Paste, etc

        void OnUndo(object sender, ExecutedRoutedEventArgs e)
        {
            Schematic.UndoManager.Undo();
        }

        void CanUndo(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = DependencyObjectExt.InDesignMode || (Schematic != null && Schematic.Simulation == null && Schematic.UndoManager.CanUndo);
            e.Handled = true;
        }

        void OnRedo(object sender, ExecutedRoutedEventArgs e)
        {
            Schematic.UndoManager.Redo();
        }

        void CanRedo(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = DependencyObjectExt.InDesignMode || (Schematic != null && Schematic.Simulation == null && Schematic.UndoManager.CanRedo);
            e.Handled = true;
        }

        void OnCopy(object sender, ExecutedRoutedEventArgs e)
        {
            CopySelectedDevicesToClipBoard();
        }

        void CopySelectedDevicesToClipBoard()
        {
            // We must put serializable data in the clipboard.
            var copiedDevices = Schematic.SelectedDevices.Select(d => d.Model()).ToList();

            var delta = (System.Drawing.Size)copiedDevices[0].Position;
            foreach (var dev in copiedDevices)
                dev.Position -= delta;

            // TODO copy connections

            Clipboard.SetData("Electrical Device List", copiedDevices);
        }

        void CanCopy(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = DependencyObjectExt.InDesignMode || (Schematic != null && Schematic.SelectedDevices.Any());
            e.Handled = true;
        }

        void OnCut(object sender, ExecutedRoutedEventArgs e)
        {
            CopySelectedDevicesToClipBoard();
            foreach (var dev in Schematic.SelectedDevices.ToList())
                Schematic.RemoveDevice(dev, true);
        }

        void CanCut(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = DependencyObjectExt.InDesignMode || (Schematic != null && Schematic.Simulation == null && Schematic.SelectedDevices.Any());
            e.Handled = true;
        }

        void OnPaste(object sender, ExecutedRoutedEventArgs e)
        {
            if (Clipboard.GetData("Electrical Device List") is not List<Model.Device> devs)
                return;

            Schematic.UnselectAll();

            // TODO paste connections

            Schematic.UnselectAll();
            var delta = IsMouseOver ? (Vector)Mouse.GetPosition(this) : default;
            if (devs.Count > 0)
                using (Schematic.UndoManager.CreateBatch("Paste devices"))
                {
                    foreach (var dev in devs)
                        AddDevice(ViewModel.Device.FromModel(dev), new Point(dev.Position.X, dev.Position.Y) + delta).IsSelected = true;
                }
            else
                AddDevice(ViewModel.Device.FromModel(devs[0]), new Point(devs[0].Position.X, devs[0].Position.Y) + delta).IsSelected = true;
        }

        void CanPaste(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = DependencyObjectExt.InDesignMode || (Schematic != null && Schematic.Simulation == null && Clipboard.ContainsData("Electrical Device List"));
            e.Handled = true;
        }

        void OnDelete(object sender, ExecutedRoutedEventArgs e)
        {
            // If the cursor is over a connection or an unselected device, delete that.
            // Otherwise delete the selected devices.
            var connAtCursor = GetConnectionAtCursor();
            if (connAtCursor != null)
            {
                Schematic.RemoveConnection(connAtCursor, !Keyboard.Modifiers.HasFlag(ModifierKeys.Control));
                return;
            }

            var devAtCursor = GetDeviceAtCursor();
            if (devAtCursor != null && !devAtCursor.IsSelected)
            {
                Schematic.RemoveDevice(devAtCursor, !Keyboard.Modifiers.HasFlag(ModifierKeys.Control));
                return;
            }

            // If multiple devices are selected, wrap the deletion in a single operation.
            if (Schematic.SelectedDevices.Count > 1)
            {
                using (Schematic.UndoManager.CreateBatch($"Delete {Schematic.SelectedDevices.Count} devices"))
                    for (int i = 0; i < Schematic.Elements.Count; i++)
                    {
                        if (Schematic.Elements[i] is ViewModel.Device dev && dev.IsSelected)
                        {
                            Schematic.RemoveDevice(dev, true);
                            i--;
                        }
                    }
            }
            else if (Schematic.SelectedDevices.Count != 0)
                Schematic.RemoveDevice(Schematic.SelectedDevices.First(), true);
        }

        void CanDelete(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = DependencyObjectExt.InDesignMode || (Schematic != null && Schematic.Simulation == null);
            e.Handled = true;
        }

        #endregion
    }
}
