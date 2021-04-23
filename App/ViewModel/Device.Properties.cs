using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using Undo;

namespace ElectricSketch.ViewModel
{
    // We want to have specific data templates for voltage, frequency, etc.
    // Two way binding does not work with value types, so we cannot use the approach from the electric library.
    // Using data templates for value types has the same problem (the data context would be the boxed value, and updating it doesn't help).

    // Bonus, this approach conveniently handles Undo support.


    public class TypedDeviceProperty<T> : INotifyPropertyChanged, IConvertible
    {
        public event PropertyChangedEventHandler PropertyChanged;
        protected void RaisePropertyChanged([System.Runtime.CompilerServices.CallerMemberName] string property = null)
        {
#if DEBUG
            var pi = GetType().GetProperty(property);
            if (pi == null)
                throw new ArgumentException($"Property {property} not found on {this}");
#endif
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(property));
        }

        public TypedDeviceProperty(Device device, string property, bool designOnly, Func<T> get, Action<T> set, bool supportsUndo = true)
        {
            Device = device;
            Property = property;
            DesignOnly = designOnly;
            SupportsUndo = supportsUndo;

            if (!DesignOnly)
                Device.InSimulationChanged += OnDeviceInSimulationChanged;

            this.get = get;
            this.set = set;
        }

        public string Property { get; }
        public Device Device { get; }
        public bool DesignOnly { get; }
        public bool SupportsUndo { get; }

        public T Value
        {
            get => get();
            set
            {
                if (get().Equals(value))
                    return;

                if (Device.Schematic != null)
                    if (Device.InSimulation)
                        if (DesignOnly)
                            throw new CannotChangeStateInSimulationException();
                        else
                            SetValueNoUndo(value);
                    else if (SupportsUndo)
                        Device.Schematic.UndoManager.Do(new ChangeAction(this, value));
                    else
                        SetValueNoUndo(value);
                else
                    SetValueNoUndo(value);
            }
        }
        protected readonly Func<T> get;
        protected readonly Action<T> set;

        public event Action ValueChanged;

        protected virtual void SetValueNoUndo(T value)
        {
            set(value);
            ValueChanged?.Invoke();
            RaisePropertyChanged(nameof(Value));
        }

        protected virtual void OnDeviceInSimulationChanged()
        {
            // Just in case the value changed on entering or exiting the simulation.
            ValueChanged?.Invoke();
            RaisePropertyChanged(nameof(Value));
        }

        // Don't, because it's a trap. Bindings (or code) will/might listen to PropertyChanged messages for the object itself,
        // but they will only happen for the Value property.
        //public static implicit operator T (TypedDeviceProperty<T> prop) => prop.value;

        public TypeCode GetTypeCode() => ((IConvertible)get()).GetTypeCode();
        public bool ToBoolean(IFormatProvider provider) => ((IConvertible)get()).ToBoolean(provider);
        public byte ToByte(IFormatProvider provider) => ((IConvertible)get()).ToByte(provider);
        public char ToChar(IFormatProvider provider) => ((IConvertible)get()).ToChar(provider);
        public DateTime ToDateTime(IFormatProvider provider) => ((IConvertible)get()).ToDateTime(provider);
        public decimal ToDecimal(IFormatProvider provider) => ((IConvertible)get()).ToDecimal(provider);
        public double ToDouble(IFormatProvider provider) => ((IConvertible)get()).ToDouble(provider);
        public short ToInt16(IFormatProvider provider) => ((IConvertible)get()).ToInt16(provider);
        public int ToInt32(IFormatProvider provider) => ((IConvertible)get()).ToInt32(provider);
        public long ToInt64(IFormatProvider provider) => ((IConvertible)get()).ToInt64(provider);
        public sbyte ToSByte(IFormatProvider provider) => ((IConvertible)get()).ToSByte(provider);
        public float ToSingle(IFormatProvider provider) => ((IConvertible)get()).ToSingle(provider);
        public string ToString(IFormatProvider provider) => ((IConvertible)get()).ToString(provider);
        public object ToType(Type conversionType, IFormatProvider provider) => ((IConvertible)get()).ToType(conversionType, provider);
        public ushort ToUInt16(IFormatProvider provider) => ((IConvertible)get()).ToUInt16(provider);
        public uint ToUInt32(IFormatProvider provider) => ((IConvertible)get()).ToUInt32(provider);
        public ulong ToUInt64(IFormatProvider provider) => ((IConvertible)get()).ToUInt64(provider);

        sealed class ChangeAction : PropertyChangeAction<TypedDeviceProperty<T>>
        {
            public ChangeAction(TypedDeviceProperty<T> device, T newValue) : base(device)
            {
                value = new OneOrMore<T>(device.Value, newValue, (dev, value) => dev.SetValueNoUndo(value));
                UpdateDescription();
            }

            readonly OneOrMore<T> value;

            public override bool Merge(UndoableAction action)
            {
                var merged = base.Merge(action);
                if (merged)
                    UpdateDescription();
                return merged;
            }

            void UpdateDescription()
            {
                if (device != null)
                    Description = $"Change {device.Property} of {device.Device} from {value.oneOld} to {value.oneNew}";
                else
                    Description = $"Change {devices[0].Property} of {devices.Count} devices";
            }
        }
    }

    public sealed class Voltage : TypedDeviceProperty<float>
    {
        public Voltage(Device device, string property, Func<float> get, Action<float> set) : base(device, property, true, get, set) { }
    }

    public sealed class NullableVoltage : TypedDeviceProperty<float?>
    {
        public NullableVoltage(Device device, string property, Func<float?> get, Action<float?> set) : base(device, property, true, get, set) { }

        public static float? Convert(ElectricLib.Volt? v) => v.HasValue ? v.Value.Value : (float?)null;
        public static ElectricLib.Volt? Convert(float? v) => v.HasValue ? new ElectricLib.Volt(v.Value) : (ElectricLib.Volt?)null;

        public bool HasValue
        {
            get => Value.HasValue;
        }

        public float? Detected
        {
            get => detected;
            set
            {
                if (detected == value)
                    return;
                detected = value;
                RaisePropertyChanged();
            }
        }
        float? detected;

        protected override void SetValueNoUndo(float? value)
        {
            var hadValue = get().HasValue;
            base.SetValueNoUndo(value);
            if (value.HasValue != hadValue)
                RaisePropertyChanged(nameof(HasValue));
        }
    }

    public sealed class Frequency : TypedDeviceProperty<float>
    {
        public Frequency(Device device, string property, Func<float> get, Action<float> set) : base(device, property, true, get, set) { }
    }

    public sealed class NullableFrequency : TypedDeviceProperty<float?>
    {
        public NullableFrequency(Device device, string property, Func<float?> get, Action<float?> set) : base(device, property, true, get, set) { }

        public static float? Convert(ElectricLib.Hz? v) => v.HasValue ? v.Value.Value : (float?)null;
        public static ElectricLib.Hz? Convert(float? v) => v.HasValue ? new ElectricLib.Hz(v.Value) : (ElectricLib.Hz?)null;

        public bool HasValue
        {
            get => Value.HasValue;
        }

        public float? Detected
        {
            get => detected;
            set
            {
                if (detected == value)
                    return;
                detected = value;
                RaisePropertyChanged();
            }
        }
        float? detected;

        protected override void SetValueNoUndo(float? value)
        {
            var hadValue = get().HasValue;
            base.SetValueNoUndo(value);
            if (value.HasValue != hadValue)
                RaisePropertyChanged(nameof(HasValue));
        }
    }

    public sealed class NumPoles : TypedDeviceProperty<int>
    {
        public NumPoles(Device device, string property, Func<int> get, Action<int> set) : base(device, property, true, get, set) { }
    }

    public sealed class NumPositions : TypedDeviceProperty<int>
    {
        public NumPositions(Device device, string property, Func<int> get, Action<int> set) : base(device, property, true, get, set) { }
    }

    public sealed class CurrentPosition : TypedDeviceProperty<int>
    {
        public CurrentPosition(Device device, string property, Func<int> get, Action<int> set) : base(device, property, false, get, set) { }
    }

    public sealed class DesignOnlyBoolean : TypedDeviceProperty<bool>
    {
        public DesignOnlyBoolean(Device device, string property, Func<bool> get, Action<bool> set) : base(device, property, true, get, set) { }
    }

    public sealed class Boolean : TypedDeviceProperty<bool>
    {
        public Boolean(Device device, string property, Func<bool> get, Action<bool> set, bool supportsUndo = true) : base(device, property, false, get, set, supportsUndo) { }
    }

    public sealed class Percent : TypedDeviceProperty<float>
    {
        public Percent(Device device, string property, Func<float> get, Action<float> set) : base(device, property, true, get, set) { }
    }

    public sealed class Duration : TypedDeviceProperty<TimeSpan>
    {
        public Duration(Device device, string property, Func<TimeSpan> get, Action<TimeSpan> set) : base(device, property, true, get, set) { }
    }
}
