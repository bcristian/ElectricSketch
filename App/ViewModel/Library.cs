using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Text;
using System.Windows.Media.Imaging;

namespace ElectricSketch.ViewModel
{
    // Not really worth it to implement a proper model.

    public class Library
    {
        public LibraryFolder Root { get; } = new LibraryFolder();

        public static Library Load()
        {
            var lib = new Library();
            System.Diagnostics.Debug.Assert(lib.Root.Components.Count == 0);

            // Create structure for known types.
            // Add user-created presets.
            AddSampleData(lib);

            return lib;
        }

        public Library()
        {
#if DEBUG
            if (DependencyObjectExt.InDesignMode)
                AddSampleData(this);
#endif
        }

        static void AddSampleData(Library lib)
        {
            var switches = new LibraryFolder() { Name = "Switches & Buttons" };
            lib.Root.Folders.Add(switches);
            switches.Components.Add(Device.FromModel(new Model.Devices.NpstSwitch() { Name = "Single Throw" }));
            switches.Components.Add(Device.FromModel(new Model.Devices.RotarySwitch() { Name = "Multi Throw" }));
            switches.Components.Add(Device.FromModel(new Model.Devices.PairSwitch() { Name = "Pair" }));
            switches.Components.Add(Device.FromModel(new Model.Devices.CamSwitch() { Name = "Cam Switch", NumContacts = 2, Pattern = new bool[,] { { false, true }, { true, false } } }));
            var swPresets = new LibraryFolder() { Name = "Switch Presets" };
            switches.Folders.Add(swPresets);
            swPresets.Components.Add(Device.FromModel(new Model.Devices.NpstSwitch() { Name = "SPST Switch" }));
            swPresets.Components.Add(Device.FromModel(new Model.Devices.RotarySwitch() { Name = "SPDT Switch" }));
            swPresets.Components.Add(Device.FromModel(new Model.Devices.NpstSwitch() { Name = "DPST Switch", NumPoles = 2 }));
            swPresets.Components.Add(Device.FromModel(new Model.Devices.RotarySwitch() { Name = "DPDT Switch", NumPoles = 2 }));
            swPresets.Components.Add(Device.FromModel(new Model.Devices.PairSwitch() { Name = "Pair Switch" }));
            var btPresets = new LibraryFolder() { Name = "Button Presets" };
            switches.Folders.Add(btPresets);
            btPresets.Components.Add(Device.FromModel(new Model.Devices.NpstSwitch() { Name = "SPST Button", Momentary = true }));
            btPresets.Components.Add(Device.FromModel(new Model.Devices.RotarySwitch() { Name = "SPDT Button", MomentaryLastPosition = true }));
            btPresets.Components.Add(Device.FromModel(new Model.Devices.NpstSwitch() { Name = "DPST Button", Momentary = true, NumPoles = 2 }));
            btPresets.Components.Add(Device.FromModel(new Model.Devices.RotarySwitch() { Name = "DPDT Button", NumPoles = 2, MomentaryLastPosition = true }));
            btPresets.Components.Add(Device.FromModel(new Model.Devices.RotarySwitch() { Name = "Left-Right Button", NumPoles = 1, NumPositions = 3, CurrentPosition = 1, MomentaryFirstPosition = true, MomentaryLastPosition = true }));
            btPresets.Components.Add(Device.FromModel(new Model.Devices.PairSwitch() { Name = "Pair Button", Momentary = true }));
            var camPresets = new LibraryFolder() { Name = "Cam Presets" };
            switches.Folders.Add(camPresets);
            camPresets.Components.Add(Device.FromModel(new Model.Devices.CamSwitch()
            {
                Name = "2P 1-0-2",
                NumPositions = 3,
                NumContacts = 4,
                Pattern =
                new bool[,]
                {
                    { true, false, false },
                    { false, false, true },
                    { true, false, false },
                    { false, false, true },
                }
            }));
            camPresets.Components.Add(Device.FromModel(new Model.Devices.CamSwitch() { Name = "Dahlander 1-0-2", NumPositions = 3, NumContacts = 8, Pattern =
                new bool[,]
                {
                    { true, false, false },
                    { false, false, true },
                    { false, false, true },
                    { false, false, true },
                    { true, false, false },
                    { true, false, false },
                    { false, false, true },
                    { false, false, true },
                }
            }));

            var relays = new LibraryFolder() { Name = "Relays" };
            lib.Root.Folders.Add(relays);
            relays.Components.Add(Device.FromModel(new Model.Devices.Relay() { Name = "Relay" }));

            var motors = new LibraryFolder() { Name = "Motors" };
            lib.Root.Folders.Add(motors);
            motors.Components.Add(Device.FromModel(new Model.Devices.SimpleMotor() { Name = "Simple Motor" }));
            motors.Components.Add(Device.FromModel(new Model.Devices.ThreePhaseMotor() { Name = "Three Phase Motor" }));

            var sources = new LibraryFolder() { Name = "Power Sources" };
            lib.Root.Folders.Add(sources);
            sources.Components.Add(Device.FromModel(new Model.Devices.SinglePhaseSupply() { Name = "Single Phase AC", Frequency = 50, Voltage = 230 }));
            sources.Components.Add(Device.FromModel(new Model.Devices.SinglePhaseSupply() { Name = "DC", Frequency = 0, Voltage = 12 }));
            sources.Components.Add(Device.FromModel(new Model.Devices.ThreePhaseSupply() { Name = "Three Phase", Frequency = 50, Voltage = 400 }));
            sources.Components.Add(Device.FromModel(new Model.Devices.Transformer() { Name = "Transformer" }));
            sources.Components.Add(Device.FromModel(new Model.Devices.VFD() { Name = "VFD" }));

            var misc = new LibraryFolder() { Name = "Miscellaneous" };
            lib.Root.Folders.Add(misc);
            misc.Components.Add(Device.FromModel(new Model.Devices.Lamp() { Name = "Lamp" }));
            misc.Components.Add(Device.FromModel(new Model.Devices.Junction() { Name = "Junction" }));
        }
    }

    public class LibraryFolder
    {
        public string Name { get; set; }
        //public BitmapImage Icon { get; set; }

        public ObservableCollection<LibraryFolder> Folders { get; } = [];
        public ObservableCollection<Device> Components { get; } = [];
    }
}
