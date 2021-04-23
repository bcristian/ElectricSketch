using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Drawing;
using System.Text;

namespace ElectricSketch.ViewModel.Devices
{
    public class CamSwitch : TypedDevice<Model.Devices.CamSwitch, ElectricLib.Devices.CamSwitch, ElectricLib.Devices.CamSwitchSim>
    {
        public CamSwitch(Model.Devices.CamSwitch m) : base(m)
        {
            OriginOffset = new Point(25, 15);

            NumPositions = new NumPositions(this, "number of positions", () => functional.NumPositions, SetNumPositions); // Setting the value later, it builds related state
            NumContacts = new NumPoles(this, "number of contacts", () => functional.NumContacts, SetNumContacts); // Setting the value later, it builds related state
            SelectorPosition = new CurrentPosition(this, "selector position", GetSelectorPosition, SetSelectorPosition);
            AllowIncompatiblePotentials = new DesignOnlyBoolean(this, "allow incompatible potentials",
                () => functional.AllowIncompatiblePotentials,
                (v) => functional.AllowIncompatiblePotentials = v);

            contacts = new ObservableCollection<CamContact>();
            Contacts = new ReadOnlyObservableCollection<CamContact>(contacts);

            // We need to force the creation of the associated state, which will not happen if the value happens to be the one the functional was created with.
            SetNumPositions(m.NumPositions); // before contacts
            SetNumContacts(m.NumContacts);
            SetSelectorPosition(m.SelectorPosition);
            AllowIncompatiblePotentials.Value = m.AllowIncompatiblePotentials;

            for (int c = 0; c < m.NumContacts; c++)
            {
                var contact = contacts[c];
                for (int p = 0; p < m.NumPositions; p++)
                    contact.Positions[p].SetPatternNoUndo(m.Pattern[c, p]);
            }
        }

        protected override void FillModel(Model.Devices.CamSwitch m)
        {
            m.NumPositions = NumPositions.Value;
            m.NumContacts = NumContacts.Value;
            m.SelectorPosition = SelectorPosition.Value;
            m.AllowIncompatiblePotentials = AllowIncompatiblePotentials.Value;
            m.Pattern = new bool[m.NumContacts, m.NumPositions];
            for (int c = 0; c < m.NumContacts; c++)
            {
                var contact = contacts[c];
                for (int p = 0; p < m.NumPositions; p++)
                    m.Pattern[c, p] = contact.Positions[p].Pattern;
            }
        }

        public ReadOnlyObservableCollection<CamContact> Contacts { get; }
        readonly ObservableCollection<CamContact> contacts;


        public int CanvasWidth => 50;
        public int ContactHeight => 20;
        public int CanvasHeight => NumContacts.Value * ContactHeight + 10;
        public int PinHOffset => 30;

        void SetPinOffsets(int start, int num)
        {
            for (int i = start, end = start + num; i < end; i++)
            {
                var contact = i / 2;
                var left = i % 2 == 0;
                Pins[i].Offset = new Point(left ? -PinHOffset : PinHOffset, contact * ContactHeight);
            }
        }

        void SetNumPositions(int value)
        {
            if (Schematic != null && Schematic.UndoManager.State == Undo.UndoManagerState.Doing)
            {
                // In Undo/Redo mode, these have already been recorded.

                // So that we can restore the pattern.
                for (int p = value; p < NumPositions.Value; p++)
                    foreach (var contact in contacts)
                        contact.Positions[p].Pattern = false;
            }

            SelectorPosition.Value = Math.Min(SelectorPosition.Value, MaxPosition);
            functional.NumPositions = value;

            RaisePropertyChanged(nameof(MaxPosition));
        }

        void SetNumContacts(int value)
        {
            if (Schematic != null && Schematic.UndoManager.State == Undo.UndoManagerState.Doing)
            {
                // In Undo/Redo mode, these have already been recorded.
                for (int p = value; p < functional.NumContacts; p++)
                    for (int i = 0; i < 2; i++)
                        Schematic.RemoveConnections(Pins[2 * p + i], true);

                // So that we can restore the pattern.
                for (int c = value; c < contacts.Count; c++)
                {
                    var contact = contacts[c];
                    for (int p = 0; p < NumPositions.Value; p++)
                        contact.Positions[p].Pattern = false;
                }
            }

            functional.NumContacts = value;
            RaisePropertyChanged(nameof(CanvasHeight));

            if (contacts.Count < value)
            {
                var fp = 2 * contacts.Count;
                SetPinOffsets(fp, Pins.Count - fp);

                for (int c = contacts.Count; c < value; c++)
                    contacts.Add(new CamContact(this, c));

            }
            else
            {
                // Works because the functional removes poles from the end.
                while (contacts.Count > value)
                    contacts.RemoveAt(contacts.Count - 1);
            }
        }

        /// <summary>
        /// Number of contacts.
        /// </summary>
        public NumPoles NumContacts { get; }

        /// <summary>
        /// Number of positions.
        /// </summary>
        public NumPositions NumPositions { get; }

        public int MaxPosition { get => NumPositions.Value - 1; }

        /// <summary>
        /// Selector position, 0 to <see cref="NumPositions"/> - 1.
        /// </summary>
        public CurrentPosition SelectorPosition { get; }

        int GetSelectorPosition()
        {
            if (InSimulation)
                return simulation.Position;
            else
                return functional.Position;
        }

        void SetSelectorPosition(int value)
        {
            if (InSimulation)
                simulation.Position = value;
            else
                functional.Position = value;
        }

        /// <summary>
        /// <see cref="ElectricLib.ErrorCode.DangerousSwitch"/>
        /// </summary>
        public DesignOnlyBoolean AllowIncompatiblePotentials { get; }


        public override void PrepareForSimulation()
        {
            base.PrepareForSimulation();

            functional.Pattern = new bool[NumContacts.Value, NumPositions.Value];
            for (int c = 0; c < NumContacts.Value; c++)
            {
                var contact = contacts[c];
                for (int p = 0; p < NumPositions.Value; p++)
                    functional.Pattern[c, p] = contact.Positions[p].Pattern;
            }
        }

        public override void ActionPress()
        {
            if (NumPositions.Value == 2)
                SelectorPosition.Value = 1 - SelectorPosition.Value;
        }

        public override void NextPress()
        {
            SelectorPosition.Value = Math.Min(SelectorPosition.Value + 1, MaxPosition);
        }

        public override void PrevPress()
        {
            SelectorPosition.Value = Math.Max(SelectorPosition.Value - 1, 0);
        }
    }

    /// <summary>
    /// A contact of a cam switch.
    /// </summary>
    public class CamContact : INotifyPropertyChanged
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

        public CamContact(CamSwitch sw, int index)
        {
            Switch = sw;
            Index = index;

            positions = new ObservableCollection<CamPosition>();
            Positions = new ReadOnlyObservableCollection<CamPosition>(positions);
            OnNumPositionsChanged();

            sw.NumPositions.ValueChanged += OnNumPositionsChanged;
            sw.SelectorPosition.ValueChanged += () => RaisePropertyChanged(nameof(Closed));
        }

        public CamSwitch Switch { get; }
        public int Index { get; }

        public bool Closed => positions[Switch.SelectorPosition.Value].Pattern;

        public ReadOnlyObservableCollection<CamPosition> Positions { get; }
        readonly ObservableCollection<CamPosition> positions;

        void OnNumPositionsChanged()
        {
            if (positions.Count < Switch.NumPositions.Value)
            {
                for (int i = positions.Count; i < Switch.NumPositions.Value; i++)
                {
                    var cp = new CamPosition(this, i);
                    cp.PatternChanged += OnPatternChanged;
                    positions.Add(cp);
                }
            }
            else
            {
                while (positions.Count > Switch.NumPositions.Value)
                {
                    var cp = positions[^1];
                    cp.PatternChanged -= OnPatternChanged;
                    positions.RemoveAt(positions.Count - 1);
                }
            }
        }

        void OnPatternChanged(CamPosition camPosition)
        {
            var index = positions.IndexOf(camPosition);
            if (index == Switch.SelectorPosition.Value)
                RaisePropertyChanged(nameof(Closed));
        }
    }


    /// <summary>
    /// An abstract device position. Used so that the view can enumerate them in order to create visuals for each.
    /// </summary>
    public class CamPosition : INotifyPropertyChanged
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

        public CamPosition(CamContact contact, int index)
        {
            Contact = contact;
            Index = index;
        }

        public CamContact Contact { get; }
        public int Index { get; }

        public bool Pattern
        {
            get => pattern;
            set
            {
                if (pattern == value)
                    return;

                if (Contact.Switch?.Schematic?.UndoManager != null)
                    Contact.Switch.Schematic.UndoManager.Do(new ChangeCamSwitchPatternAction(Contact.Switch, new CamSwitchPatternChange() { contact = Contact.Index, position = Index, value = value }));
                else
                    SetPatternNoUndo(value);
            }
        }
        bool pattern;

        public event Action<CamPosition> PatternChanged;

        internal void SetPatternNoUndo(bool value)
        {
            pattern = value;
            PatternChanged?.Invoke(this);
            RaisePropertyChanged(nameof(Pattern));
        }
    }

    public struct CamSwitchPatternChange
    {
        public int contact;
        public int position;
        public bool value;
    }

    public struct ContactAndPosition
    {
        public int contact;
        public int position;

        public override bool Equals(object obj) => (obj is ContactAndPosition other) && Equals(other);
        public bool Equals([System.Diagnostics.CodeAnalysis.AllowNull] ContactAndPosition other) => contact == other.contact && position == other.position;
        public override int GetHashCode() => (contact, position).GetHashCode();
        public static bool operator ==(ContactAndPosition a, ContactAndPosition b) => Equals(a, b);
        public static bool operator !=(ContactAndPosition a, ContactAndPosition b) => !Equals(a, b);

        public override string ToString() => $"{contact} {position}";
    }

    internal sealed class ChangeCamSwitchPatternAction : PropertyChangeAction<CamSwitch>
    {
        public ChangeCamSwitchPatternAction(CamSwitch device, CamSwitchPatternChange change) : base(device)
        {
            singleChange = change;
            Description = $"Change cam pattern {device}";
        }

        CamSwitchPatternChange singleChange;
        Dictionary<ContactAndPosition, bool> singleDeviceChanges;
        List<Dictionary<ContactAndPosition, bool>> multipleDeviceChanges;


        protected override void Redo(CamSwitch device, int index)
        {
            base.Redo(device, index);

            if (index < 0)
                if (singleDeviceChanges == null)
                    device.Contacts[singleChange.contact].Positions[singleChange.position].SetPatternNoUndo(singleChange.value);
                else
                    Redo(device, singleDeviceChanges);
            else
                Redo(devices[index], multipleDeviceChanges[index]);
        }

        static void Redo(CamSwitch device, Dictionary<ContactAndPosition, bool> changes)
        {
            foreach (var change in changes)
                device.Contacts[change.Key.contact].Positions[change.Key.position].SetPatternNoUndo(change.Value);
        }

        protected override void Undo(CamSwitch device, int index)
        {
            base.Undo(device, index);

            if (index < 0)
                if (singleDeviceChanges == null)
                    device.Contacts[singleChange.contact].Positions[singleChange.position].SetPatternNoUndo(!singleChange.value);
                else
                    Undo(device, singleDeviceChanges);
            else
                Undo(devices[index], multipleDeviceChanges[index]);
        }

        static void Undo(CamSwitch device, Dictionary<ContactAndPosition, bool> changes)
        {
            foreach (var change in changes)
                device.Contacts[change.Key.contact].Positions[change.Key.position].SetPatternNoUndo(!change.Value);
        }

        // Merge a new change to the single device.
        protected override void MergeOneNewChange(PropertyChangeAction<CamSwitch> _other)
        {
            base.MergeOneNewChange(_other);

            System.Diagnostics.Debug.Assert(multipleDeviceChanges == null);

            var other = (ChangeCamSwitchPatternAction)_other;
            System.Diagnostics.Debug.Assert(other.singleDeviceChanges == null);
            System.Diagnostics.Debug.Assert(other.multipleDeviceChanges == null);

            if (singleDeviceChanges == null)
                if (other.singleChange.contact == singleChange.contact && other.singleChange.position == singleChange.position)
                    singleChange.value = other.singleChange.value;
                else
                    singleDeviceChanges = new Dictionary<ContactAndPosition, bool>
                    {
                        { new ContactAndPosition() { contact = singleChange.contact, position = singleChange.position }, singleChange.value },
                        { new ContactAndPosition() { contact = other.singleChange.contact, position = other.singleChange.position }, other.singleChange.value }
                    };
            else
                singleDeviceChanges[new ContactAndPosition() { contact = other.singleChange.contact, position = other.singleChange.position }] = other.singleChange.value;
        }

        // Change from single device to many.
        protected override void SwitchOneToMany(PropertyChangeAction<CamSwitch> _other)
        {
            base.SwitchOneToMany(_other);

            System.Diagnostics.Debug.Assert(multipleDeviceChanges == null);

            var other = (ChangeCamSwitchPatternAction)_other;
            System.Diagnostics.Debug.Assert(other.singleDeviceChanges == null);
            System.Diagnostics.Debug.Assert(other.multipleDeviceChanges == null);

            if (singleDeviceChanges == null)
                multipleDeviceChanges = new List<Dictionary<ContactAndPosition, bool>>()
                {
                    new Dictionary<ContactAndPosition, bool>() { { new ContactAndPosition() { contact = singleChange.contact, position = singleChange.position }, singleChange.value } },
                    new Dictionary<ContactAndPosition, bool>() { { new ContactAndPosition() { contact = other.singleChange.contact, position = other.singleChange.position }, other.singleChange.value } }
                };
            else
            {
                multipleDeviceChanges = new List<Dictionary<ContactAndPosition, bool>>()
                {
                    singleDeviceChanges,
                    new Dictionary<ContactAndPosition, bool>() { { new ContactAndPosition() { contact = other.singleChange.contact, position = other.singleChange.position }, other.singleChange.value } }
                };

                singleDeviceChanges = null;
            }

            Description = $"Change cam pattern on {devices.Count} switches";
        }

        // Merge a change to a new device.
        protected override void MergeNewDevice(PropertyChangeAction<CamSwitch> _other)
        {
            base.MergeNewDevice(_other);

            System.Diagnostics.Debug.Assert(singleDeviceChanges == null);
            System.Diagnostics.Debug.Assert(multipleDeviceChanges != null);

            var other = (ChangeCamSwitchPatternAction)_other;
            System.Diagnostics.Debug.Assert(other.singleDeviceChanges == null);
            System.Diagnostics.Debug.Assert(other.multipleDeviceChanges == null);

            multipleDeviceChanges.Add(new Dictionary<ContactAndPosition, bool>() { { new ContactAndPosition() { contact = other.singleChange.contact, position = other.singleChange.position }, other.singleChange.value } });
            System.Diagnostics.Debug.Assert(multipleDeviceChanges.Count == devices.Count);
        }

        // Merge a change to an existing device.
        protected override void MergeNewChange(int index, PropertyChangeAction<CamSwitch> _other)
        {
            base.MergeNewChange(index, _other);

            System.Diagnostics.Debug.Assert(singleDeviceChanges == null);
            System.Diagnostics.Debug.Assert(multipleDeviceChanges != null);

            var other = (ChangeCamSwitchPatternAction)_other;
            System.Diagnostics.Debug.Assert(other.singleDeviceChanges == null);
            System.Diagnostics.Debug.Assert(other.multipleDeviceChanges == null);

            var changes = multipleDeviceChanges[index];
            changes[new ContactAndPosition() { contact = other.singleChange.contact, position = other.singleChange.position }] = other.singleChange.value;
        }
    }
}
