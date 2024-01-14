using System;
using System.Collections.Generic;
using System.Text;

namespace ElectricLib
{
    public class CircuitError
    {
        /// <summary>
        /// The type of error.
        /// </summary>
        public ErrorCode Code { get; }

        /// <summary>
        /// The pins related to the error.
        /// </summary>
        /// <remarks>
        /// Some of them might be on internal pins of the device. With strange devices, it might even be possible that all of them are internal.
        /// </remarks>
        public List<Pin> Pins { get; }


        internal CircuitError(ErrorCode code)
        {
            Code = code;
            Pins = [];
        }
        internal CircuitError(ErrorCode code, IEnumerable<Pin> pins)
        {
            Code = code;
            Pins = new List<Pin>(pins);
        }
    }

    public class SimException(ErrorCode errorCode, IDevice device) : ApplicationException
    {
        public ErrorCode ErrorCode { get; } = errorCode;
        public IDevice Device { get; } = device;
    }

    public enum ErrorCode
    {
        /// <summary>
        /// A device has related pins connected to incompatible potentials, e.g. one side of a relay is connected to AC mains and the other to a VFD output.
        /// </summary>
        IncompatiblePotentialsOnDevice = 1,

        /// <summary>
        /// A user-operated switch can connect incompatible potentials by a single action, e.g. by pressing a button or rotating to the next position.
        /// Doing so would be detected by the simulation, but it is not a safe design. This detection can be disabled on each switch.
        /// </summary>
        DangerousSwitch,

        /// <summary>
        /// The voltage supplied to a device does not match its specifications. E.g. voltage too low or too high, wrong polarity, etc.
        /// </summary>
        InvalidSupplyVoltage,

        /// <summary>
        /// Invalid connections to a device. E.g. duplicate phase connected to a three phase consumer, invalid motor connections.
        /// </summary>
        InvalidConnections,

        /// <summary>
        /// A connection has been detected between different potentials. E.g. a short circuit, connecting a DC line to an AC one, a VFD to mains, etc.
        /// </summary>
        VoltageConflict,

        /// <summary>
        /// A series connection of consumers has been detected - this is not legal in our model.
        /// </summary>
        SeriesConnection,

        /// <summary>
        /// The circuit is in an unstable, repeating state. E.g. a relay that disconnects its own coil. No pins will be given, this is not a localized
        /// fault. Use the <see cref="Simulation.UpdateTrace"/> to diagnose the problem.
        /// </summary>
        Ringing
    }
}
