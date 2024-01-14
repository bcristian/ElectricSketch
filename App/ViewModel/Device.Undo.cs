using System;
using System.Collections.Generic;
using System.Text;
using Undo;

namespace ElectricSketch.ViewModel
{
    public abstract class PropertyChangeAction<T>(T device) : UndoableAction where T : class
    {

        // We want to group changes that affect several devices, e.g. a batch rename or moving several selected devices.
        // But since the merging only happens after the new action is created, it's better if we don't allocate lists with
        // one element only to merge them immediately after.

        // Only set if this is a single-device change.
        public T device = device;

        // Only created if this change affects several devices. The original will be added to the list and the field set to null.
        public List<T> devices;

        public override void RedoAfterChildren()
        {
            if (device != null)
                Redo(device, -1); // this can be overloaded
            else
                for (int i = 0; i < devices.Count; i++)
                    Redo(devices[i], i);
        }

        protected void SetFieldsNewValues()
        {
            if (device != null)
                SetFieldsNewValues(device, -1); // this cannot be overloaded
            else
                for (int i = 0; i < devices.Count; i++)
                    SetFieldsNewValues(devices[i], i);
        }

        protected virtual void Redo(T device, int index)
        {
            SetFieldsNewValues(device, index);
        }

        protected void SetFieldsNewValues(T device, int index)
        {
            foreach (var f in OneOrMoreFields)
            {
                var fi = f.fi.GetValue(this);
                f.Set.Invoke(fi, new object[] { device, f.New.Invoke(fi, new object[] { index }) });
            }
        }

        public override void UndoBeforeChildren()
        {
            if (device != null)
                Undo(device, -1);
            else
                for (int i = 0; i < devices.Count; i++)
                    Undo(devices[i], i);
        }

        protected virtual void Undo(T device, int index)
        {
            foreach (var f in OneOrMoreFields)
            {
                var fi = f.fi.GetValue(this);
                f.Set.Invoke(fi, new object[] { device, f.Old.Invoke(fi, new object[] { index }) });
            }
        }

        protected static Q Value<Q>(int index, Q ifSingle, IReadOnlyList<Q> ifMany) => index < 0 ? ifSingle : ifMany[index];

        public override bool Merge(UndoableAction action)
        {
            if (action.GetType() != GetType())
                return false;

            var change = (PropertyChangeAction<T>)action;

            if (device != null)
            {
                if (change.device == device)
                    MergeOneNewChange(change);
                else
                {
                    devices = new List<T>(2) { device, change.device };
                    device = null;
                    SwitchOneToMany(change);
                }
            }
            else
            {
                var index = devices.IndexOf(change.device);
                if (index < 0)
                {
                    devices.Add(change.device);
                    MergeNewDevice(change);
                }
                else
                    MergeNewChange(index, change);
            }

            SetFieldsNewValues();

            Time = action.Time;

            return true;
        }

        // Use this in derived types for storing the old/new values.
        // This class uses reflection to automatically merge them.
        protected class OneOrMore<SomeType>(SomeType oldValue, SomeType newValue, Action<T, SomeType> setter)
        {
            public SomeType oneOld = oldValue;
            public SomeType oneNew = newValue;
            public List<SomeType> moreOld;
            public List<SomeType> moreNew;
            public Action<T, SomeType> setter = setter;

            public SomeType New(int index) => index < 0 ? oneNew : moreNew[index];
            public SomeType Old(int index) => index < 0 ? oneOld : moreOld[index];

            public void Set(T device, SomeType value) => setter?.Invoke(device, value);

            public void MergeOneNewChange(OneOrMore<SomeType> other)
            {
                oneNew = other.oneNew;
            }

            public void SwitchOneToMany(OneOrMore<SomeType> other)
            {
                moreOld = [oneOld, other.oneOld];
                oneOld = default;
                moreNew = [oneNew, other.oneNew];
                oneNew = default;
            }

            public void MergeNewDevice(OneOrMore<SomeType> other)
            {
                moreOld.Add(other.oneOld);
                moreNew.Add(other.oneNew);
            }

            public void MergeNewChange(int index, OneOrMore<SomeType> other)
            {
                moreNew[index] = other.oneNew;
            }
        }

        protected struct OneOrModeField
        {
            public System.Reflection.FieldInfo fi;
            public System.Reflection.MethodInfo MergeOneNewChange;
            public System.Reflection.MethodInfo SwitchOneToMany;
            public System.Reflection.MethodInfo MergeNewDevice;
            public System.Reflection.MethodInfo MergeNewChange;
            public System.Reflection.MethodInfo Set;
            public System.Reflection.MethodInfo New;
            public System.Reflection.MethodInfo Old;

            public void Invoke(System.Reflection.MethodInfo method, object @this, object other)
            {
                var thisFieldInst = fi.GetValue(@this);
                var otherFieldInst = fi.GetValue(other);
                method.Invoke(thisFieldInst, new object[] { otherFieldInst });
            }

            public void Invoke(System.Reflection.MethodInfo method, object @this, object other, int index)
            {
                var thisFieldInst = fi.GetValue(@this);
                var otherFieldInst = fi.GetValue(other);
                method.Invoke(thisFieldInst, new object[] { index, otherFieldInst });
            }
        }

        static List<OneOrModeField> oneOrMoreFields;

        protected List<OneOrModeField> OneOrMoreFields
        {
            get
            {
                if (oneOrMoreFields == null)
                {
                    oneOrMoreFields = [];

                    var fields = GetType().GetFields(
                        System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public);
                    foreach (var field in fields)
                    {
                        var ft = field.FieldType;
                        if (ft.IsConstructedGenericType && ft.GetGenericTypeDefinition() == typeof(OneOrMore<>))
                        {
                            oneOrMoreFields.Add(new OneOrModeField()
                            {
                                fi = field,
                                MergeOneNewChange = ft.GetMethod("MergeOneNewChange"),
                                SwitchOneToMany = ft.GetMethod("SwitchOneToMany"),
                                MergeNewDevice = ft.GetMethod("MergeNewDevice"),
                                MergeNewChange = ft.GetMethod("MergeNewChange"),
                                Set = ft.GetMethod("Set"),
                                New = ft.GetMethod("New"),
                                Old = ft.GetMethod("Old")
                            });
                        }
                    }
                }

                return oneOrMoreFields;
            }
        }

        // Merge a new change to the single device.
        protected virtual void MergeOneNewChange(PropertyChangeAction<T> other)
        {
            foreach (var f in OneOrMoreFields)
                f.Invoke(f.MergeOneNewChange, this, other);
        }

        // Change from single device to many.
        protected virtual void SwitchOneToMany(PropertyChangeAction<T> other)
        {
            foreach (var f in OneOrMoreFields)
                f.Invoke(f.SwitchOneToMany, this, other);
        }

        // Merge a change to a new device.
        protected virtual void MergeNewDevice(PropertyChangeAction<T> other)
        {
            foreach (var f in OneOrMoreFields)
                f.Invoke(f.MergeNewDevice, this, other);
        }

        // Merge a change to an existing device.
        protected virtual void MergeNewChange(int index, PropertyChangeAction<T> other)
        {
            foreach (var f in OneOrMoreFields)
                f.Invoke(f.MergeNewChange, this, other, index);
        }
    }
}
