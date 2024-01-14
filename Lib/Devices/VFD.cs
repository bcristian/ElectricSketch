using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ElectricLib.Devices
{
    /// <summary>
    /// Covers transformers and PSU's. Something that supplies power if itself powered.
    /// </summary>
    public class VFD : Device<VFDSim>, ISinglePhaseConsumer
    {
        public override string DefaultNamePrefix => "VFD";

        public VFD() : this(null) { }
        public VFD(string name = null) : base(name)
        {
            pins.Add(new Pin(this, "R"));
            pins.Add(new Pin(this, "S"));
            pins.Add(new Pin(this, "T"));

            pins.Add(new Pin(this, "U"));
            pins.Add(new Pin(this, "V"));
            pins.Add(new Pin(this, "W"));

            pins.Add(new Pin(this, "Com"));
            pins.Add(new Pin(this, "Fwd"));
            pins.Add(new Pin(this, "Rev"));
            pins.Add(new Pin(this, "Stop"));

            pins.Add(new Pin(this, "KA"));
            pins.Add(new Pin(this, "KB"));
            pins.Add(new Pin(this, "KC"));

            pins.Add(new Pin(this, "FA"));
            pins.Add(new Pin(this, "FB"));
            pins.Add(new Pin(this, "FC"));
        }

        public Pin Pin(VFDPin pin) => Pins[(int)pin];

        public Pin R => Pin(VFDPin.R);
        public Pin S => Pin(VFDPin.S);
        public Pin T => Pin(VFDPin.T);

        public Pin U => Pin(VFDPin.U);
        public Pin V => Pin(VFDPin.V);
        public Pin W => Pin(VFDPin.W);

        public Pin DICom => Pin(VFDPin.DICom);
        public Pin DIFwd => Pin(VFDPin.DIFwd);
        public Pin DIRev => Pin(VFDPin.DIRev);
        public Pin DIStop => Pin(VFDPin.DIStop);

        public Pin RunCom => Pin(VFDPin.RunCom);
        public Pin RunNO => Pin(VFDPin.RunNO);
        public Pin RunNC => Pin(VFDPin.RunNC);

        public Pin FaultCom => Pin(VFDPin.FaultCom);
        public Pin FaultNO => Pin(VFDPin.FaultNO);
        public Pin FaultNC => Pin(VFDPin.FaultNC);


        /// <summary>
        /// Power supply mode.
        /// </summary>
        public VFDSupply PowerSupply { get; set; }

        /// <summary>
        /// Nominal supply voltage. Null means automatic detection.
        /// </summary>
        public Volt? InVoltage { get; set; }
        Volt? ISinglePhaseConsumer.Voltage => InVoltage;

        /// <summary>
        /// Acceptable supply voltage deviation, in percents. E.g. +/- 20% of nominal.
        /// </summary>
        public float VoltageTolerance { get; set; } = 20;

        /// <summary>
        /// Nominal supply frequency. Null means automatic detection. Zero means DC. To define a device that can accept both AC and DC
        /// set this to zero and the frequency tolerance to the maximum allowable AC frequency.
        /// </summary>
        public Hz? InFrequency { get; set; }
        Hz? ISinglePhaseConsumer.Frequency => InFrequency;

        /// <summary>
        /// Acceptable supply frequency deviation, in Hz. E.g. +/- 15Hz.
        /// </summary>
        public Hz FrequencyTolerance { get; set; } = new Hz(15);

        bool ISinglePhaseConsumer.PolarityMatters => false;



        /// <summary>
        /// Output voltage.
        /// </summary>
        public Volt OutVoltage { get; set; } = new Volt(230);

        /// <summary>
        /// Output frequency.
        /// </summary>
        public Hz OutFrequency { get; set; } = new Hz(400);


        internal override VFDSim CreateSimulation(Simulation sim, ArraySegment<PinSim> pins) => new(this, sim, pins);
    }

    public enum VFDPin
    {
        /// <summary>
        /// Power input. Single phase between R and S.
        /// </summary>
        R, S, T,

        /// <summary>
        /// Motor output.
        /// </summary>
        U, V, W,

        // Digital command pins.

        /// <summary>
        /// DI common. Connect the pins to this to sent a command.
        /// </summary>
        DICom,

        /// <summary>
        /// Forward run.
        /// </summary>
        DIFwd,

        /// <summary>
        /// Reverse run.
        /// </summary>
        DIRev,

        /// <summary>
        /// Stop/reset.
        /// </summary>
        DIStop,

        /// <summary>
        /// Common pin of a relay energized when driving the motor.
        /// </summary>
        RunCom,

        /// <summary>
        /// NO pin of a relay energized when driving the motor.
        /// </summary>
        RunNO,

        /// <summary>
        /// NC pin of a relay energized when driving the motor.
        /// </summary>
        RunNC,

        /// <summary>
        /// Common pin of a relay energized when the VFD is in the faulted state.
        /// </summary>
        FaultCom,

        /// <summary>
        /// NO pin of a relay energized when the VFD is in the faulted state.
        /// </summary>
        FaultNO,

        /// <summary>
        /// NC pin of a relay energized when the VFD is in the faulted state.
        /// </summary>
        FaultNC,

        _Num
    }

    public class VFDSim : DeviceSimulation
    {
        internal VFDSim(VFD device, Simulation simulation, ArraySegment<PinSim> pins) : base(device, simulation, pins)
        {
            ps = simulation.AddPowerSource(device,
                new Potential(device.OutVoltage / 1.732f, device.OutFrequency, Phase.R, device),
                new Potential(device.OutVoltage / 1.732f, device.OutFrequency, Phase.S, device),
                new Potential(device.OutVoltage / 1.732f, device.OutFrequency, Phase.T, device));

            if (device.PowerSupply == VFDSupply.SinglePhase)
            {
                singlePhaseCons = new SinglePhaseConsumerSim(device, device, simulation, pins.Slice((int)VFDPin.R, 2));
            }
            else
            {
                threePhaseCons = simulation.AddConsumer(device, 3);
                for (int i = 0; i < 3; i++)
                    simulation.Connect(pins[i + (int)VFDPin.R], threePhaseCons.Pins[i]);
            }

            // So that we can connect fwd (UVW) and rev (UWV)
            sw = new Simulation.Switch[5];
            for (int i = 0; i < sw.Length; i++)
                sw[i] = simulation.AddSwitch(device, false, true);
            simulation.Connect(ps.Pins[0], sw[0].A);
            simulation.Connect(ps.Pins[1], sw[1].A);
            simulation.Connect(ps.Pins[2], sw[2].A);
            simulation.Connect(ps.Pins[1], sw[3].A);
            simulation.Connect(ps.Pins[2], sw[4].A);
            simulation.Connect(sw[0].B, pins[(int)VFDPin.U]);
            simulation.Connect(sw[1].B, pins[(int)VFDPin.V]);
            simulation.Connect(sw[2].B, pins[(int)VFDPin.W]);
            simulation.Connect(sw[3].B, pins[(int)VFDPin.W]);
            simulation.Connect(sw[4].B, pins[(int)VFDPin.V]);


            var psDISrcObj = new object();
            psDI = simulation.AddPowerSource(device,
                new Potential(Volt.Zero, Hz.DC, Phase.Zero, psDISrcObj),
                new Potential(CmdCons.CmdVoltage, Hz.DC, Phase.Zero, psDISrcObj));
            simulation.Connect(psDI.Pins[1], Pins[(int)VFDPin.DICom]);

            fwdCmd = new SinglePhaseConsumerSim(new CmdCons(), device, simulation, new PinSim[] { Pins[(int)VFDPin.DIFwd], psDI.Pins[0] });
            revCmd = new SinglePhaseConsumerSim(new CmdCons(), device, simulation, new PinSim[] { Pins[(int)VFDPin.DIRev], psDI.Pins[0] });
            stopCmd = new SinglePhaseConsumerSim(new CmdCons(), device, simulation, new PinSim[] { Pins[(int)VFDPin.DIStop], psDI.Pins[0] });

            runNO = simulation.AddSwitch(device, false, false);
            runNC = simulation.AddSwitch(device, true, false);
            simulation.Connect(runNO.A, pins[(int)VFDPin.RunCom]);
            simulation.Connect(runNO.B, pins[(int)VFDPin.RunNO]);
            simulation.Connect(runNC.A, pins[(int)VFDPin.RunCom]);
            simulation.Connect(runNC.B, pins[(int)VFDPin.RunNC]);

            faultNO = simulation.AddSwitch(device, false, false);
            faultNC = simulation.AddSwitch(device, true, false);
            simulation.Connect(faultNO.A, pins[(int)VFDPin.FaultCom]);
            simulation.Connect(faultNO.B, pins[(int)VFDPin.FaultNO]);
            simulation.Connect(faultNC.A, pins[(int)VFDPin.FaultCom]);
            simulation.Connect(faultNC.B, pins[(int)VFDPin.FaultNC]);
        }

        class CmdCons : ISinglePhaseConsumer
        {
            public static readonly Volt CmdVoltage = new(24);
            public Volt? Voltage => CmdVoltage;
            public float VoltageTolerance => 0;
            public Hz? Frequency => Hz.DC;
            public Hz FrequencyTolerance => Hz.Zero;
            public bool PolarityMatters => true;
        }

        readonly Simulation.PowerSource ps;
        readonly Simulation.Switch[] sw;
        readonly SinglePhaseConsumerSim singlePhaseCons;
        readonly Simulation.Consumer threePhaseCons;
        readonly Simulation.PowerSource psDI;
        readonly SinglePhaseConsumerSim fwdCmd;
        readonly SinglePhaseConsumerSim revCmd;
        readonly SinglePhaseConsumerSim stopCmd;
        readonly Simulation.Switch runNO, runNC;
        readonly Simulation.Switch faultNO, faultNC;

        internal override void Update()
        {
            if (singlePhaseCons != null)
            {
                singlePhaseCons.Update();
                IsPowered = singlePhaseCons.IsOn;
            }
            else
                UpdateThreePhaseSupply();

            psDI.Update();
            fwdCmd.Update();
            revCmd.Update();
            stopCmd.Update();

            if (IsPowered)
            {
                var fwd = fwdCmd.IsOn;
                var rev = revCmd.IsOn;
                var stop = stopCmd.IsOn;

                if (fwd)
                {
                    if (rev || stop)
                        throw new SimException(ErrorCode.InvalidConnections, Device);
                    if (!Faulted)
                        Output = TurnDirection.CW;
                }
                if (rev)
                {
                    if (fwd || stop)
                        throw new SimException(ErrorCode.InvalidConnections, Device);
                    if (!Faulted)
                        Output = TurnDirection.CCW;
                }
                if (stop)
                {
                    if (fwd || rev)
                        throw new SimException(ErrorCode.InvalidConnections, Device);
                    Output = TurnDirection.None;
                    Faulted = false;
                }

                if (Faulted)
                    Output = TurnDirection.None;
            }
            else
            {
                Output = TurnDirection.None;
                runNC.Closed = true;
                runNO.Closed = false;
                faultNC.Closed = true;
                faultNO.Closed = false;
                Faulted = false;
            }

            switch (Output)
            {
                case TurnDirection.CW:
                    sw[0].Closed = true;
                    sw[1].Closed = true;
                    sw[2].Closed = true;
                    sw[3].Closed = false;
                    sw[4].Closed = false;
                    break;

                case TurnDirection.CCW:
                    sw[0].Closed = true;
                    sw[1].Closed = false;
                    sw[2].Closed = false;
                    sw[3].Closed = true;
                    sw[4].Closed = true;
                    break;

                case TurnDirection.None:
                    sw[0].Closed = false;
                    sw[1].Closed = false;
                    sw[2].Closed = false;
                    sw[3].Closed = false;
                    sw[4].Closed = false;
                    break;

                default:
                    throw new NotImplementedException();
            }

            for (int i = 0; i < sw.Length; i++)
                sw[i].Update();

            runNC.Closed = Output == TurnDirection.None;
            runNO.Closed = !runNC.Closed;

            faultNO.Closed = Faulted;
            faultNC.Closed = !Faulted;

            runNC.Update();
            runNO.Update();
            faultNC.Update();
            faultNO.Update();

            ps.Update();
        }

        void UpdateThreePhaseSupply()
        {
            // Get potentials and compute voltages between the input pins.
            var potentials = new Potential?[3];
            for (int i = 0; i < 3; i++)
                potentials[i] = Simulation.GetPotential(Pins[(int)VFDPin.R + i]);

            // Check all pins not connected or at the same potential.
            var allNull = potentials.All(p => !p.HasValue || p == potentials[0]);
            if (allNull)
            {
                IsPowered = false;
                return;
            }

            var voltages = new Volt[3];
            for (int i = 0; i < 3; i++)
            {
                if (!Validators.VoltageDifference(potentials[i], potentials[(i + 1) % 3], out Volt? v))
                    throw new SimException(ErrorCode.IncompatiblePotentialsOnDevice, Device);
                if (!v.HasValue)
                    throw new SimException(ErrorCode.InvalidConnections, Device);
                voltages[i] = v.Value;
            }
            if (voltages[0] != voltages[1] || voltages[1] != voltages[2])
                throw new SimException(ErrorCode.InvalidConnections, Device);

            var vfd = (VFD)Device;

            // Check voltage. Zero is always acceptable, but don't set it as the automatically detected value.
            var volts = voltages[0];
            if (!volts.IsZero)
            {
                if (vfd.InVoltage.HasValue)
                {
                    // If a nominal voltage has been defined, check that the supply matches.
                    if (!Validators.IsVoltageInRange(volts, vfd.InVoltage.Value, vfd.VoltageTolerance, false))
                        throw new SimException(ErrorCode.InvalidSupplyVoltage, Device);
                }
                else
                {
                    // Check that the supply matches the automatically detected one, or store the current value.
                    if (DetectedVoltage.HasValue && !Validators.IsVoltageInRange(volts, DetectedVoltage.Value, vfd.VoltageTolerance, false))
                        throw new SimException(ErrorCode.InvalidSupplyVoltage, Device);
                    DetectedVoltage = volts;
                }

                // Check phases.
                var phase = new Phase[]
                {
                    potentials[0].Value.Phase,
                    potentials[1].Value.Phase,
                    potentials[2].Value.Phase,
                };
                if (phase[0] == phase[1] && phase[0] == phase[2])
                {
                    // All connected to the same phase.
                    IsPowered = false;
                }
                else if (phase[0] == phase[1] || phase[1] == phase[2] || phase[2] == phase[0])
                {
                    // Duplicate phase.
                    throw new SimException(ErrorCode.InvalidConnections, Device);
                }
                else
                    IsPowered = true;
            }
            else
            {
                IsPowered = false;
            }

            // Check frequency. Frequency always matters, even if the voltage is zero (but not null).
            if (vfd.InFrequency.HasValue)
            {
                // If a nominal frequency has been defined, check that the supply matches.
                if (!Validators.IsFrequencyInRange(potentials[0].Value.Frequency, vfd.InFrequency.Value, vfd.FrequencyTolerance))
                    throw new SimException(ErrorCode.InvalidSupplyVoltage, Device);
            }
            else
            {
                // Check that the supply matches the automatically detected one, or store the current value.
                if (!DetectedFrequency.HasValue)
                    DetectedFrequency = potentials[0].Value.Frequency;
                else if (!Validators.IsFrequencyInRange(potentials[0].Value.Frequency, DetectedFrequency.Value, vfd.FrequencyTolerance))
                    throw new SimException(ErrorCode.InvalidSupplyVoltage, Device);
            }
        }

        internal override void Validate()
        {
            if (singlePhaseCons != null)
                singlePhaseCons.Validate();
            else
                threePhaseCons.Validate();

            for (int i = 0; i < sw.Length; i++)
                sw[i].Validate();

            ps.Validate();

            psDI.Validate();
            fwdCmd.Validate();
            revCmd.Validate();
            stopCmd.Validate();

            runNC.Update();
            runNO.Update();
            faultNC.Update();
            faultNO.Update();
        }


        /// <summary>
        /// Simulate a fault. To clear connect DI stop.
        /// </summary>
        public void Fault() => Faulted = true;

        /// <summary>
        /// True if the VFD is in the faulted state.
        /// </summary>
        public bool Faulted { get; protected set; }

        /// <summary>
        /// True if the VFD is powered.
        /// </summary>
        public bool IsPowered { get; protected set; }

        /// <summary>
        /// Output power direction (CW = fwd, CCW = rev).
        /// </summary>
        public TurnDirection Output { get; protected set; }

        /// <summary>
        /// Detected supply voltage, if the device is set to automatically detect it.
        /// We store it so that we can detect if the device is being powered by e.g. 220V at some point and 380V at another.
        /// Such devices do exist, but that is not generally the case.
        /// </summary>
        public Volt? DetectedVoltage { get; protected set; }

        /// <summary>
        /// Detected supply frequency, if the device is set to automatically detect it.
        /// We store it so that we can detect if the device is being powered by e.g. AC at some point and DC at another.
        /// Such devices do exist, but that is not generally the case.
        /// </summary>
        public Hz? DetectedFrequency { get; protected set; }
    }

    /// <summary>
    /// VFD power supply mode.
    /// </summary>
    public enum VFDSupply
    {
        SinglePhase,
        ThreePhase
    }
}
