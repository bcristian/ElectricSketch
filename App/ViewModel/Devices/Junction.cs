using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;
using System.Windows.Media;
using System.Linq;
using System.Collections.Specialized;
using System.Runtime.CompilerServices;

namespace ElectricSketch.ViewModel.Devices
{
    public sealed class Junction : TypedDevice<Model.Devices.Junction, ElectricLib.Devices.Junction, ElectricLib.Devices.JunctionSim>
    {
        public Junction(Point pos, string name = null) : this(new Model.Devices.Junction() { Name = name, Position = pos }) { }
        public Junction(Model.Devices.Junction m) : base(m)
        {
            // The pin must have zero offset, the code related to connections relies on this.
            OriginOffset = new Point(15, 15);
        }

        protected override void FillModel(Model.Devices.Junction m) { }

        /// <summary>
        /// Show the junction dot.
        /// </summary>
        public bool Show
        {
            get => show;
            set
            {
                if (show == value)
                    return;
                show = value;
                RaisePropertyChanged();
            }
        }
        bool show = true;

        protected override void OnSchematicChanged(Schematic old)
        {
            base.OnSchematicChanged(old);

            if (old != null)
            {
                ((INotifyCollectionChanged)old.Elements).CollectionChanged -= OnSchematicElementsChanged;
                UnwatchPosition();
            }

            if (Schematic != null)
                ((INotifyCollectionChanged)Schematic.Elements).CollectionChanged += OnSchematicElementsChanged;

            Show = CheckShouldShow();
        }

        protected override void RaisePropertyChanged([CallerMemberName] string property = null)
        {
            base.RaisePropertyChanged(property);

            if (property == nameof(Name))
                Show = CheckShouldShow();
        }

        void OnSchematicElementsChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.NewItems != null && e.NewItems.Count > 0 && (e.NewItems[0] is Connection) ||
                e.OldItems != null && e.OldItems.Count > 0 && (e.OldItems[0] is Connection))
            {
                Show = CheckShouldShow();
            }
        }

        bool CheckShouldShow()
        {
            UnwatchPosition();

            // Show if not in schematic or if the name is set.
            if (Schematic == null || !string.IsNullOrEmpty(Name))
                return true;

            // Show if between more or less than two connections, or if the connections are at the same or opposite angles.
            var conns = Schematic.ConnectionsToPin(Pins[0]).ToList();
            if (conns.Count != 2)
                return true;

            watchPosition[0] = conns[0].Other(Pins[0]);
            watchPosition[0].Device.PositionChanged += OnDevicePositionChanged;
            watchPosition[1] = conns[1].Other(Pins[0]);
            watchPosition[1].Device.PositionChanged += OnDevicePositionChanged;
            return CheckShouldShowBecauseOfAngle();
        }

        void UnwatchPosition()
        {
            if (watchPosition[0] != null)
            {
                watchPosition[0].Device.PositionChanged -= OnDevicePositionChanged;
                watchPosition[1].Device.PositionChanged -= OnDevicePositionChanged;
                watchPosition[0] = null;
                watchPosition[1] = null;
            }
        }

        void OnDevicePositionChanged(object sender, DevicePositionChangedEventArgs args)
        {
            Show = CheckShouldShowBecauseOfAngle();
        }

        protected override void OnPositionChanged(Point oldPos)
        {
            base.OnPositionChanged(oldPos);

            if (watchPosition[0] != null)
                Show = CheckShouldShowBecauseOfAngle();

        }

        readonly Pin[] watchPosition = new Pin[2];

        bool CheckShouldShowBecauseOfAngle()
        {
            var d0 = watchPosition[0].Position - (Size)Pins[0].Position;
            var d1 = watchPosition[1].Position - (Size)Pins[0].Position;
            var a0 = Math.Atan2(d0.Y, d0.X);
            var a1 = Math.Atan2(d1.Y, d1.X);
            var da = Math.Abs(a0 - a1);
            if (Math.Abs(da) < 0.087 || Math.Abs(da - Math.PI) < 0.087) // 5 deg
                return true;
            return false;
        }
    }
}
