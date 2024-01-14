using System;
using System.Collections.Generic;
using System.Text;

namespace ElectricLib.Devices
{
    /// <summary>
    /// A two-wire motor, AC or DC. In AC mode it turns clockwise. In DC mode the direction depends on polarity.
    /// </summary>
    public class SimpleMotor(string name) : SinglePhaseConsumer<SimpleMotorSim>((device, simulation, pins) => new SimpleMotorSim((SimpleMotor)device, simulation, pins), name)
    {
        public override string DefaultNamePrefix => "M";

        public SimpleMotor() : this(null) { }
    }

    public class SimpleMotorSim(SimpleMotor device, Simulation simulation, ArraySegment<PinSim> pins) : SinglePhaseConsumerSim(device, device, simulation, pins)
    {
        public TurnDirection TurnDirection
        {
            get
            {
                if (!IsOn)
                    return TurnDirection.None;
                if (DetectedFrequency == Hz.DC)
                    return DetectedVoltage.Value.Value > 0 ? TurnDirection.CW : TurnDirection.CCW;
                return TurnDirection.CW;
            }
        }
    }

    /// <summary>
    /// The direction a motor is turning.
    /// </summary>
    public enum TurnDirection
    {
        /// <summary>
        /// Not turning.
        /// </summary>
        None,

        /// <summary>
        /// Turning clockwise.
        /// </summary>
        CW,

        /// <summary>
        /// Turning counter-clockwise.
        /// </summary>
        CCW
    }
}
