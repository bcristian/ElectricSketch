using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;

namespace ElectricLib
{
    public class Simulation
    {
        /// <summary>
        /// Create a simulation for the specified <see cref="ElectricLib.Schematic"/>. The circuit must not be modified while the simulation runs.
        /// </summary>
        /// <param name="schematic">the schematic to simulate</param>
        /// <param name="now">the time at the start of the simulation</param>
        /// <remarks>
        /// The simulation contains just three types of primitive devices: power sources, SPST switches, and consumers.
        /// The devices from the schematic create their simulations using one or more of these primitives and some logic that toggles switches based on
        /// the state of the consumers and/or pins.
        /// Of the primitive devices, only the switches can change state. This means that to model something that provides power based on some state, e.g.
        /// a transformer or a VFD, the device should add switches after the power supply and close them when it should be supplying power. This means that
        /// it is not possible to create continuously variable tensions. I don't think this is a significant limitation, given the scope of the program.
        /// Besides, we don't have analog inputs or proper load modeling, either.
        /// The simulation will detect connections between incompatible potentials or series connection of consumers. The logic of the simulated devices
        /// is responsible for detecting other errors, such as bad connections for motors.
        /// </remarks>
        public Simulation(Schematic schematic, DateTime now)
        {
            Now = now;
            Schematic = schematic;

            // Create the pins and the connections for all the devices in the schematic.
            // Doing it like this saves the effort of creating a lot of small connections (e.g. 1-1 mappings between internal and schematic pins), only
            // to merge most of them immediately after.
            // For best performance, device simulations should start connecting pins starting with the schematic ones - this way merging will only happen
            // if the devices internally connect schematic pins.
            var schematicPins = new PinSim[schematic.Devices.Count][];
            for (int i = 0; i < schematicPins.Length; i++)
            {
                var dev = schematic.Devices[i];
                var dp = new PinSim[dev.Pins.Count];
                schematicPins[i] = dp;
                for (int j = 0; j < dp.Length; j++)
                    dp[j] = new PinSim() { schPin = dev.Pins[j] };
            }

            designConnections = new List<List<PinSim>>(schematic.Connections.Count);
            foreach (var sc in schematic.Connections)
            {
                var pc = new List<PinSim>(sc.Pins.Count);
                designConnections.Add(pc);

                foreach (var pin in sc.Pins)
                {
                    var simPin = schematicPins[schematic.Devices.IndexOf(pin.Device)][pin.Device.Pins.IndexOf(pin)];
                    simPin.permConn = pc;
                    pc.Add(simPin);
                }
            }

            // Have the devices create their simulations, using the already created pins.
            devs = new List<DeviceSimulation>(schematic.Devices.Count);
            Devices = devs.AsReadOnly();
            // Put the simulations of the schematic devices at the same indices in our list.
            for (int i = 0; i < schematic.Devices.Count; i++)
                devs.Add(null);
            for (int i = 0; i < schematic.Devices.Count; i++)
            {
                // Track the created primitive devices and ensure that all their pins are connected.
                // There is no valid reason to create primitive devices with unconnected pins.
                // The converse is not true - it is valid to have unconnected device pins, e.g. junctions or models of real devices that are like that.
#if DEBUG
                var firstPS = powerSources.Count;
                var firstSW = switches.Count;
                var firstCS = consumers.Count;
#endif

                devs[i] = schematic.Devices[i].CreateSimulation(this, schematicPins[i]);

#if DEBUG
                for (int j = firstPS; j < powerSources.Count; j++)
                    if (powerSources[j].Pins.Any(p => p.permConn == null))
                        throw new InvalidProgramException("Device simulation left an unconnected pin on a primitive power source");
                for (int j = firstSW; j < switches.Count; j++)
                    if (switches[j].Pins.Any(p => p.permConn == null))
                        throw new InvalidProgramException("Device simulation left an unconnected pin on a primitive switch");
                for (int j = firstCS; j < consumers.Count; j++)
                    if (consumers[j].Pins.Any(p => p.permConn == null))
                        throw new InvalidProgramException("Device simulation left an unconnected pin on a primitive consumer");
#endif
            }

            // Delete permanent connections that contain exactly one internal pin and only schematic pins that are not connected in the
            // schematic. If there are several internal pins the connection might still be important, even if it is not directly connected to the exterior.
            // Such connections cannot affect the simulation.
            // This in itself seems a dubious optimization, since these connections would never be updated. But it makes the removal of useless switches
            // much easier, and that list is processed on every update.
            // This means that we cannot relate the internal pin to the device pin - should not be a problem, since no errors should be reported about
            // internal pins that have no function. The primitive devices don't do it, and schematic devices that report errors about unconnected pins
            // should do so using their own pins, not those of the primitive devices.
            for (int i = 0; i < designConnections.Count; i++)
            {
                var pc = designConnections[i];
                bool canBeDeleted = false;
                var numInternalPins = 0;
                foreach (var pin in pc)
                {
                    if (pin.IsInternal)
                    {
                        numInternalPins++;
                        // This works because there are no other integers between 0 and 1 :)
                        canBeDeleted = numInternalPins == 1;
                        if (!canBeDeleted)
                            break;
                    }
                    else if (schematic.IsConnected(SchematicPin(pin)))
                    {
                        canBeDeleted = false;
                        break;
                    }
                }
                if (canBeDeleted)
                {
                    designConnections.RemoveAt(i--);
                    foreach (var pin in pc)
                        pin.permConn = null;
                }
            }

            // TODO We could remove the schematic pins now. This would halve the size of the pin lists in the connections, so would
            // consume less memory and improve the performance of dynamic connection creation. Left for later if the program gets to
            // be used for schematics large enough that this is an issue.
            // Note that this would affect how we map between schematic and simulation pins.
            // We could also remove pins without permanent connections.

            // Leave in the switches list only those whose pins are connected to different, not null permanent connections.
            // This doesn't delete the switches, it just removes them from the list used for updates. They are still available for
            // the user to click all day long, it's just that we know to not even care. :)
            for (int i = 0; i < switches.Count; i++)
            {
                var sw = switches[i];
                if (sw.Pins[0].permConn == null || sw.Pins[1].permConn == null || sw.Pins[0].permConn == sw.Pins[1].permConn)
                {
                    switches.RemoveAt(i--);
                    sw.Useless = true;
                }
            }

            // Do the same for the consumers - leave only those with at least two pins in different not null permanent connections
            // that have either a power source or a not-useless switch.
            // Note that if we connect three lamps in series, this would remove the middle one from the list. But that connection is
            // illegal will be detected anyway.
            // It will also remove a lamp with one or both pins not connected. Not an issue, since it could never turn on.
            for (int i = 0; i < consumers.Count; i++)
            {
                var cons = consumers[i];
                List<PinSim> firstConn = null;
                bool gotSecond = false;
                foreach (var pin in cons.Pins)
                {
                    var isValidConn = pin.permConn != null
                        && pin.permConn != firstConn
                        && pin.permConn.Any(p =>
                        {
                            var t = p.dev.GetType();
                            return t == typeof(PowerSource) || (t == typeof(Switch) && switches.Contains(p.dev));
                        });
                    if (isValidConn)
                        if (firstConn == null)
                            firstConn = pin.permConn;
                        else
                        {
                            gotSecond = true;
                            break;
                        }
                }
                if (!gotSecond)
                    consumers.RemoveAt(i--);
            }

            // Don't do this, series detection will not work on static connections. And making it work is more effort than
            // it's worth, since this is one of those "almost useless" optimizations anyway.
            // Remove from the update list power sources that have no switches on their connections.
            //for (int i = 0; i < powerSources.Count; i++)
            //{
            //    var ps = powerSources[i];
            //    if (ps.Pins.All(p => p.permConn == null || !p.permConn.Any(q => q.dev.IsSwitch())))
            //        powerSources.RemoveAt(i--);
            //}

            // Create the connection states for the permanent connections that are not connected to any switch. These will not need to be rebuilt/updated.
            // Put those not connected to a power source in a special list, so that we can easily clear the visited mark when checking for series connections.
            // We also set their potentials at this time.
            staticConnections = new List<ConnectionState>();
            foreach (var pc in designConnections)
            {
                bool gotSwitch = false;
                Potential? pp = null;
                foreach (var pin in pc)
                {
                    var t = pin.dev.GetType();
                    if (t == typeof(Switch))
                    {
                        gotSwitch = true;
                        break;
                    }

                    if (t == typeof(PowerSource))
                    {
                        var ps = (PowerSource)pin.dev;
                        var psp = ps.Potentials[ps.Pins.IndexOf(pin)];
                        if (pp == null)
                            pp = psp;
                        else if (pp != psp)
                        {
                            SetError(ErrorCode.VoltageConflict, pc);
                            return;
                        }
                    }
                }

                if (gotSwitch)
                    continue;

                var dc = new ConnectionState() { pins = pc.ToArray(), potential = pp };
                foreach (var pin in dc.pins)
                    pin.connState = dc;

                if (pp == null)
                    staticConnections.Add(dc);
            }

            dynamicConnections = new List<ConnectionState>();

            // Compute the internal state.
            if (Error == null)
                ComputeState(Now);
        }

        internal Pin SchematicPin(PinSim pin)
        {
            return Schematic.Devices[devs.IndexOf(pin.dev)].Pins[pin.dev.Pins.IndexOf(pin)];
        }

        /// <summary>
        /// Permanent connections, i.e. those that cannot be affected by the state of the switches.
        /// These include the connections from the schematic and those between the internal pins.
        /// </summary>
        /// <remarks>
        /// Do not assume that the lists starts with the correspondents of the connections in the schematic, because it is possible
        /// to have devices with permanent internal connections between their pins, e.g. because a device was modeled to replicate a
        /// real one that has that configuration. And the optimizations might delete some connections that are present in the schematic.
        /// </remarks>
        readonly List<List<PinSim>> designConnections;

        /// <summary>
        /// Connections made by design and switches in the current state of the simulation. Only those that can change are included in this list.
        /// </summary>
        readonly List<ConnectionState> dynamicConnections;

        /// <summary>
        /// State for permanent connections that have no switches, thus cannot change state (in our model, which requires that consumers cannot be
        /// connected in series). And detecting such connections is exactly what we need this list for.
        /// </summary>
        readonly List<ConnectionState> staticConnections;


        /// <summary>
        /// Called by the device simulations to connect the pins.
        /// </summary>
        internal void Connect(PinSim a, PinSim b)
        {
            if (dynamicConnections != null)
                throw new InvalidOperationException("This method may only be called while constructing the device simulations");

            if (a.dev == b.dev)
                throw new InvalidOperationException("Why shunting a primitive device?");

            if (a.permConn == null)
            {
                if (b.permConn == null)
                {
                    var pc = new List<PinSim>(2) { a, b };
                    a.permConn = b.permConn = pc;
                    designConnections.Add(pc);
                }
                else
                {
                    a.permConn = b.permConn;
                    a.permConn.Add(a);
                }
            }
            else
            {
                if (b.permConn == null)
                {
                    b.permConn = a.permConn;
                    b.permConn.Add(b);
                }
                else if (a.permConn != b.permConn)
                {
                    // Merge the two lists. Add the shorter to the longer.
                    List<PinSim> shorter, longer;
                    if (a.permConn.Count > b.permConn.Count)
                    {
                        shorter = b.permConn;
                        longer = a.permConn;
                    }
                    else
                    {
                        shorter = a.permConn;
                        longer = b.permConn;
                    }

                    longer.AddRange(shorter);
                    foreach (var pin in shorter)
                        pin.permConn = longer;

                    designConnections.Remove(shorter);
                }
            }
        }

        void BuildDynamicConnections(bool checkErrors = true)
        {
            // Remove existing.
            foreach (var dc in dynamicConnections)
                foreach (var pin in dc.pins)
                    pin.connState = null;
            dynamicConnections.Clear();

            // Create new ones, based on the state of the switches.
            // Doing the closed switches first is potentially faster, because we allocate fewer larger lists and reduce the number of merges.
            // This is most significant when we have circuits with similar connections made by multiple switches, e.g. a circuit with 3 states,
            // a portion is powered on in 2 of them.
            foreach (var sw in switches)
            {
                if (!sw.Closed)
                    continue;

                var a = sw.Pins[0];
                var b = sw.Pins[1];
                if (a.connState == null)
                {
                    if (b.connState == null)
                    {
                        // Create a new dynamic connection that contains both sides.
                        var dc = new ConnectionState() { pins = new PinSim[a.permConn.Count + b.permConn.Count] };
                        a.permConn.CopyTo(dc.pins);
                        b.permConn.CopyTo(dc.pins, a.permConn.Count);
                        foreach (var pin in dc.pins)
                            pin.connState = dc;
                        dynamicConnections.Add(dc);
                    }
                    else
                    {
                        // Add to the existing one.
                        var pins = new PinSim[b.connState.pins.Length + a.permConn.Count];
                        Array.Copy(b.connState.pins, pins, b.connState.pins.Length);
                        a.permConn.CopyTo(pins, b.connState.pins.Length);
                        b.connState.pins = pins;
                        foreach (var pin in a.permConn)
                            pin.connState = b.connState;
                    }
                }
                else
                {
                    if (b.connState == null)
                    {
                        // Add to the existing one.
                        var pins = new PinSim[a.connState.pins.Length + b.permConn.Count];
                        Array.Copy(a.connState.pins, pins, a.connState.pins.Length);
                        b.permConn.CopyTo(pins, a.connState.pins.Length);
                        a.connState.pins = pins;
                        foreach (var pin in b.permConn)
                            pin.connState = a.connState;
                    }
                    else if (a.connState != b.connState)
                    {
                        // Merge the existing connections, by moving the items in the shorter to the longer of them.
                        ConnectionState shorter, longer;
                        if (a.connState.pins.Length < b.connState.pins.Length)
                        {
                            shorter = a.connState;
                            longer = b.connState;
                        }
                        else
                        {
                            shorter = b.connState;
                            longer = a.connState;
                        }

                        var pins = new PinSim[shorter.pins.Length + longer.pins.Length];
                        Array.Copy(shorter.pins, pins, shorter.pins.Length);
                        Array.Copy(longer.pins, 0, pins, shorter.pins.Length, longer.pins.Length);
                        longer.pins = pins;
                        dynamicConnections.Remove(shorter);
                        foreach (var pin in shorter.pins)
                            pin.connState = longer;
                    }
                }
            }

            // Now the open switches. Create a dynamic connection for each side, unless one exists already.
            foreach (var sw in switches)
            {
                if (sw.Closed)
                    continue;

                foreach (var pin in sw.Pins)
                {
                    if (pin.connState == null)
                    {
                        var dc = new ConnectionState() { pins = new PinSim[pin.permConn.Count] };
                        pin.permConn.CopyTo(dc.pins);
                        foreach (var p in dc.pins)
                            p.connState = dc;
                        dynamicConnections.Add(dc);
                    }
                }
            }

            // Set the potentials.
            foreach (var ps in powerSources)
            {
                for (int i = 0; i < ps.Pins.Count; i++)
                {
                    var pin = ps.Pins[i];
                    var pp = ps.Potentials[i];
                    if (pin.connState == null)
                        continue;
                    if (pin.connState.potential == null)
                        pin.connState.potential = pp;
                    else if (pin.connState.potential != pp)
                    {
                        if (checkErrors)
                            SetError(ErrorCode.VoltageConflict, pin.connState.pins);
                        return;
                    }
                }
            }

            if (!checkErrors)
                return;

            // Check if we have consumers connected in series between different potentials.
            // Start from each potential, i.e. the pins of the power sources.
            // Go through each connected consumer and for each pin on the other side:
            //  if it's at a known potential or not connected, stop
            //  if the connection is marked as having been visited from another potential (see below), set the error
            //  mark that we've visited the connection from this potential
            //  recursively repeat the above
            // Just looking connections at null potential with consumers attached to different potentials is not enough, think three lamp in series.
            foreach (var cs in staticConnections)
            {
                cs.visitedBy = null;
                cs.visitedThrough = null;
            }
            // The dynamic ones have just been created, so they are at null.
            foreach (var ps in powerSources)
            {
                for (int i = 0; i < ps.Pins.Count; i++)
                {
                    if (ps.Pins[i].connState == null)
                        continue;
                    RecursiveConsumerVisit(ps.Pins[i].connState, ps.Pins[i], ps.Potentials[i]);
                    if (Error != null)
                        return;
                }
            }

            void RecursiveConsumerVisit(ConnectionState conn, PinSim from, Potential pp)
            {
                foreach (var pin in conn.pins)
                {
                    if (pin == from || pin.dev == from.dev || !pin.dev.IsConsumer())
                        continue;
                    foreach (var consumerPin in pin.dev.Pins)
                    {
                        if (consumerPin == pin)
                            continue;
                        var connAfterConsumer = consumerPin.connState;
                        if (connAfterConsumer == null || connAfterConsumer.potential.HasValue)
                            continue;
                        if (connAfterConsumer.visitedBy.HasValue && connAfterConsumer.visitedBy != pp)
                        {
                            var trace = new List<PinSim>();
                            for (var p = consumerPin; p != null; p = p.connState.visitedThrough)
                                trace.Add(p);
                            for (var p = connAfterConsumer.visitedThrough; p != null; p = p.connState.visitedThrough)
                                trace.Add(p);
                            SetError(ErrorCode.SeriesConnection, trace);
                            return;
                        }

                        connAfterConsumer.visitedBy = pp;
                        connAfterConsumer.visitedThrough = from;

                        RecursiveConsumerVisit(connAfterConsumer, consumerPin, pp);

                        if (Error != null)
                            return;
                    }
                }
            }
        }


        /// <summary>
        /// The circuit being simulated.
        /// </summary>
        public Schematic Schematic { get; }

        /// <summary>
        /// Maps schematic devices to their simulations - the are in the same order as in the schematic.
        /// </summary>
        public ReadOnlyCollection<DeviceSimulation> Devices { get; }
        readonly List<DeviceSimulation> devs;

        public TSim Device<TSim>(IDevice schDev) where TSim : DeviceSimulation => (TSim)Devices[Schematic.Devices.IndexOf(schDev)];

        /// <summary>
        /// The simulation time.
        /// </summary>
        public DateTime Now { get; private set; }

        /// <summary>
        /// Updates the simulation.
        /// </summary>
        /// <param name="now">the simulated time</param>
        public void Update(DateTime now)
        {
            if (Error != null)
                return;
            if (now < Now)
                throw new ArgumentException("Cannot simulate backwards in time");

            ComputeState(now);
        }
        public void Update(TimeSpan deltaT) => Update(Now + deltaT);

        /// <summary>
        /// Raised during update, each time the state changes, which might be several times during a single update.
        /// The state has been computed, so it can be used for example to display an animation of the changes.
        /// </summary>
        public event Action Updated;

        /// <summary>
        /// Detected error, if any. The simulation stops when an error is detected.
        /// </summary>
        public CircuitError Error { get; private set; }

        /// <summary>
        /// The states the simulation went through on the latest update. Mostly useful for diagnosing ringing.
        /// </summary>
        public List<bool[]> UpdateTrace { get; } = new List<bool[]>();



        void ComputeState(DateTime newTime)
        {
            Debug.Assert(Error == null);

            // Clear the update trace, but keep the last state, so that on error we know where we were.
            if (UpdateTrace.Count > 1)
            {
                var stateBefore = UpdateTrace[^1];
                UpdateTrace.Clear();
                UpdateTrace.Add(stateBefore);
            }

            // Because device logic responds to potentials on the pins, we need to repeat these steps until there are no more changes, or we detect ringing.
            // Ringing means that the circuit keeps changing state forever, e.g. a relay with its coil fed through a NC contact on itself.
            // If switches have been changed (by the user since the latest Update), do a pass without updating the time first.
            // This gives a better representation of actions in time.
            // For example:
            // sw.Close();
            // sim.Update(1.0);
            // The expected result is that the simulation is in the state corresponding to one second after closing the switch.
            // But if we update the time before updating the devices, the state would actually correspond to 1 second passing and then closing the switch.
            var wereChanges = false;
            var passNum = 0;
            var repeatAfterUpdatingTime = false;
            do
            {
                passNum++;

                // Reconfigure the circuit according to the state of the switches, if they were changed (either by the user since the last update,
                // or by updating the devices).
                // Not doing it before updating the devices not only means an useless update pass, but it could result in ringing or other strange
                // behavior, because on the first Update the devices would still see the circuit as it was the last time, without the switch changes.
                if (switchesChanged)
                {
                    switchesChanged = false;

                    var state = switches.Select(sw => sw.Closed).ToArray();

                    // switchesChanged might be a false positive, if the user flicked switches back and forth, ending in the same config
                    if (!(UpdateTrace.Count == 1 && UpdateTrace[0].SequenceEqual(state)))
                    {
                        wereChanges = true;

                        // Log state and detect ringing, i.e. getting to the same state again.
                        if (UpdateTrace.Any(s => s.SequenceEqual(state)))
                        {
                            SetError(ErrorCode.Ringing, new PinSim[0]);
                            return;
                        }
                        else
                            UpdateTrace.Add(state);

                        BuildDynamicConnections();
                        if (Error != null)
                            return;
                    }
                }

                // If there were no switch changes, we can safely update the time before updating the devices.
                if (passNum == 1 && !wereChanges)
                {
                    repeatAfterUpdatingTime = false;
                    Now = newTime;
                }

                // Apply device logic.
                try
                {
                    for (int i = 0; i < devs.Count; i++)
                        devs[i].Update();
                }
                catch (SimException ex)
                {
                    SetError(ex.ErrorCode, ex.Device);
                    return;
                }

                Updated?.Invoke();

                // If there were switch changes, update the time now and do another pass.
                if (passNum == 1 && wereChanges)
                {
                    repeatAfterUpdatingTime = true;
                    Now = newTime;
                }
            }
            while (switchesChanged || (passNum == 1 && repeatAfterUpdatingTime));

            if (wereChanges)
            {
                try
                {
                    for (int i = 0; i < devs.Count; i++)
                        devs[i].Validate();
                }
                catch (SimException ex)
                {
                    SetError(ex.ErrorCode, ex.Device);
                }
            }
        }

        bool switchesChanged = true; // because we must compute the state initially, when calling UpdateState from the constructor

        /// <summary>
        /// Sets the simulation in the specified state. Meaningful result are only guaranteed when the state is one from the trace of the
        /// most recent update. Put the simulation back in the latest state before making other changes or calling Update, or strange things
        /// might happen.
        /// </summary>
        public void SetState(bool[] state)
        {
            // Do not call into the device simulations, just set the switches and compute state
            for (int i = 0; i < switches.Count; i++)
            {
                var sw = switches[i];
                sw.Closed = state[i];
            }

            BuildDynamicConnections(false);
        }

        /// <summary>
        /// Returns the potential on the specified pin.
        /// </summary>
        public Potential? GetPotential(PinSim pin)
        {
            // The potential of unconnected pins is null, unless they are on a power source.
            // The distinction is irrelevant to the simulation itself, but returning null on a power source pin would be weird for humans.
            if (pin.connState != null)
                return pin.connState.potential;
            if (pin.dev is PowerSource ps)
                return ps.Potentials[ps.Pins.IndexOf(pin)];
            return null;
        }

        /// <summary>
        /// Tests if the specified pins are currently connected.
        /// </summary>
        public bool AreConnected(PinSim a, PinSim b)
        {
            if (a.permConn != null && a.permConn == b.permConn)
                return true;
            if (a.connState != null && a.connState == b.connState)
                return true;
            return false;
        }

        void SetError(ErrorCode code, IEnumerable<PinSim> pins)
        {
            Error = new CircuitError(code);

            // Replace the simulation pins with the corresponding schematic pins.
            foreach (var pin in pins)
            {
                // The obvious.
                if (pin.schPin != null)
                {
                    Error.Pins.Add(pin.schPin);
                    continue;
                }

                // Schematic pins of the device that it is permanently connected to.
                if (pin.permConn == null)
                    continue; // cannot be dynamically connected either, skip
                foreach (var otherPin in pin.permConn)
                {
                    if (otherPin.dev.Device == pin.dev.Device && otherPin.schPin != null)
                        Error.Pins.Add(otherPin.schPin);
                }
                if (Error.Pins.Count > 0)
                    continue;

                // Schematic pins of the device that it is currently connected to.
                if (pin.connState == null)
                    continue;
                foreach (var otherPin in pin.connState.pins)
                {
                    if (otherPin.dev.Device == pin.dev.Device && otherPin.schPin != null)
                        Error.Pins.Add(otherPin.schPin);
                }
            }
        }

        void SetError(ErrorCode code, IDevice device)
        {
            Error = new CircuitError(code);
            Error.Pins.AddRange(device.Pins);
        }


        // NOTE: Primitive types are sealed, so that we can safely use the faster .GetType() == typeof(XXX) instead of is operator

        /// <summary>
        /// Creates potentials in the circuit.
        /// </summary>
        internal sealed class PowerSource : DeviceSimulation
        {
            internal PowerSource(IDevice device, Simulation sim, Potential[] potentials) : base(device, sim, potentials.Length)
            {
                Potentials = potentials;
            }

            internal Potential[] Potentials { get; }

            internal override void Update() { }
            internal override void Validate() { }
        }

        readonly List<PowerSource> powerSources = new List<PowerSource>();

        /// <summary>
        /// Creates a power source device. The pins will correspond to the given potentials.
        /// </summary>
        internal PowerSource AddPowerSource(IDevice device, params Potential[] potentials)
        {
            if (potentials.Length < 2)
                throw new ArgumentException("There must be at least two potentials");
            var ps = new PowerSource(device, this, potentials);
            powerSources.Add(ps);
            devs.Add(ps);
            return ps;
        }

        /// <summary>
        /// Opens or closes a contact between the two pins.
        /// </summary>
        internal sealed class Switch : DeviceSimulation
        {
            public Switch(IDevice device, Simulation sim, bool closed, bool allowIncompatiblePotentials) : base(device, sim, 2)
            {
                Closed = closed;
                AllowIncompatiblePotentials = allowIncompatiblePotentials;
            }

            public PinSim A => Pins[0];
            public PinSim B => Pins[1];

            // Set by the simulation if the switch cannot affect the state.
            internal bool Useless { get; set; }

            public bool Closed
            {
                get => closed;
                set
                {
                    if (closed == value)
                        return;
                    closed = value;
                    if (!Useless)
                        Simulation.switchesChanged = true;
                }
            }
            bool closed;

            /// <summary>
            /// If false, <see cref="Validate"/> will fail if incompatible potentials are present across the pins when the switch is open
            /// (it would result in an error sooner if it is closed). Manual switches should not allow this, as it should not be possible to connect
            /// incompatible potentials by the press of a button. The error would be detected if the button is pressed in the simulation, but the error might
            /// go unnoticed if it is not. Forewarned and all that.
            /// But when the switch is used in complex switches or contactors such connections should be allowed, as they are unavoidable in many common scenarios.
            /// </summary>
            public bool AllowIncompatiblePotentials { get; set; } = false;

            internal override void Update() { }
            internal override void Validate()
            {
                if (!Closed && !AllowIncompatiblePotentials)
                {
                    var pa = Simulation.GetPotential(A);
                    if (pa != null)
                    {
                        var pb = Simulation.GetPotential(B);
                        if (pb != null && pa != pb)
                            Simulation.SetError(ErrorCode.DangerousSwitch, Pins);
                    }
                }
            }
        }

        readonly List<Switch> switches = new List<Switch>();

        internal Switch AddSwitch(IDevice device, bool closed, bool allowIncompatiblePotentials)
        {
            var sw = new Switch(device, this, closed, allowIncompatiblePotentials);
            switches.Add(sw);
            devs.Add(sw);
            return sw;
        }


        /// <summary>
        /// Something that consumes energy.
        /// </summary>
        internal sealed class Consumer : DeviceSimulation
        {
            public Consumer(IDevice device, Simulation sim, int numPins) : base(device, sim, numPins)
            {
            }

            internal override void Update() { }
            internal override void Validate() { }

            // TODO add utility functions for checking the usual cases - single phase, motor, etc.
        }

        readonly List<Consumer> consumers = new List<Consumer>();

        internal Consumer AddConsumer(IDevice device, int numPins)
        {
            var cons = new Consumer(device, this, numPins);
            consumers.Add(cons);
            devs.Add(cons);
            return cons;
        }
    }

    internal static class DevSimExt
    {
        // Shortcut for writing dev.GetType() == typeof(XXX)
        internal static bool IsPowerSource(this DeviceSimulation dev) => dev.GetType() == typeof(Simulation.PowerSource);
        internal static bool IsSwitch(this DeviceSimulation dev) => dev.GetType() == typeof(Simulation.Switch);
        internal static bool IsConsumer(this DeviceSimulation dev) => dev.GetType() == typeof(Simulation.Consumer);
    }


    /// <summary>
    /// Base class for device simulations.
    /// </summary>
    public abstract class DeviceSimulation
    {
        internal DeviceSimulation(IDevice device, Simulation simulation, IList<PinSim> pins)
        {
            if (pins == null || pins.Count == 0)
                throw new ArgumentException("At least one pin must be given");

            Device = device;
            Simulation = simulation;
            Pins = pins;
            for (int i = 0; i < Pins.Count; i++)
                if (Pins[i].dev == null) // i.e. this is not contained in another device simulation
                    Pins[i].dev = this;
        }

        internal DeviceSimulation(IDevice device, Simulation simulation, int numPins)
        {
            if (numPins < 1)
                throw new ArgumentException("At least one pin must be given");

            Device = device;
            Simulation = simulation;
            Pins = new PinSim[numPins];
            for (int i = 0; i < Pins.Count; i++)
                Pins[i] = new PinSim() { dev = this };
        }

        /// <summary>
        /// The schematic device this is part of.
        /// </summary>
        public IDevice Device { get; }

        /// <summary>
        /// The simulation this is part of.
        /// </summary>
        public Simulation Simulation { get; }

        /// <summary>
        /// Same order as the pins on the device.
        /// </summary>
        public IList<PinSim> Pins { get; } // Because ArraySegment must be cast to IList (boxing) for IndexOf, element assignment, etc.

        /// <summary>
        /// Called one or more times during each simulation update. The device should update the switches according to its logic.
        /// </summary>
        internal abstract void Update();

        /// <summary>
        /// Called at the end of each simulation update, after computing the state. The device should verify that the state is valid.
        /// </summary>
        internal abstract void Validate();
    }

    /// <summary>
    /// A pin on a simulated device.
    /// </summary>
    public class PinSim
    {
        /// <summary>
        /// Pins this one is always connected to by design, so cannot be disconnected by switches.
        /// Includes pins of the schematic devices and pins of the internal primitive devices.
        /// pin.permConn == c <=> c.Contains(pin)
        /// </summary>
        internal List<PinSim> permConn;

        /// <summary>
        /// The state of the connection the pin is currently part of.
        /// </summary>
        internal ConnectionState connState;

        /// <summary>
        /// The simulation device containing this pin.
        /// </summary>
        internal DeviceSimulation dev;

        /// <summary>
        /// The corresponding schematic pin, if it exits.
        /// </summary>
        internal Pin schPin;


        internal bool IsInternal
        {
            get
            {
                var t = dev.GetType();
                return t == typeof(Simulation.PowerSource) || t == typeof(Simulation.Switch) || t == typeof(Simulation.Consumer);
            }
        }

        public override string ToString() => schPin?.ToString() ?? "internal";
    }

    internal class ConnectionState
    {
        /// <summary>
        /// Current potential. Null means not set - either not computed yet or wire in the air or open circuit.
        /// </summary>
        public Potential? potential;

        /// <summary>
        /// Used when checking for series connection of consumers.
        /// </summary>
        public Potential? visitedBy;

        /// <summary>
        /// So that we can easily show the user the series-connected devices.
        /// </summary>
        public PinSim visitedThrough;

        /// <summary>
        /// Pins connected here. pin.crtConn == c <=> c.pins.Contains(pin)
        /// </summary>
        public PinSim[] pins;
    }
}
