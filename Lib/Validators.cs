using System;
using System.Collections.Generic;
using System.Text;

namespace ElectricLib
{
    public static class Validators
    {
        /// <summary>
        /// Computes the voltage difference between two potentials. Returns false if they are not compatible (different sources or frequency).
        /// The voltage is null if one or both potentials are null.
        /// </summary>
        public static bool VoltageDifference(Potential? a, Potential? b, out Volt? volts)
        {
            if (!a.HasValue || !b.HasValue)
            {
                volts = null;
                return true;
            }
            if (a.Value.Source != b.Value.Source || a.Value.Frequency != b.Value.Frequency)
            {
                volts = null;
                return false;
            }

            // Sign matters only with DC.
            if (a.Value.Frequency.IsDC)
                volts = a.Value.Voltage - b.Value.Voltage;
            else if (a.Value.Voltage.IsZero)
                volts = b.Value.Voltage;
            else if (b.Value.Voltage.IsZero)
                volts = a.Value.Voltage;
            else if (a.Value.Phase == b.Value.Phase)
                volts = new Volt(Math.Abs(a.Value.Voltage.Value - b.Value.Voltage.Value));
            else
                volts = new Volt((float)Math.Abs(a.Value.Voltage.Value * 2 * Math.Sin((a.Value.Phase.Degrees - b.Value.Phase.Degrees) * Math.PI / 180 / 2)));
            return true;
        }

        /// <summary>
        /// Checks if the voltage is within the specified range.
        /// </summary>
        public static bool IsVoltageInRange(Volt volts, Volt nominal, float percentTolerance, bool polarityMatters)
        {
            if (!polarityMatters)
            {
                if (volts < Volt.Zero)
                    volts = -volts;
                if (nominal < Volt.Zero)
                    nominal = -nominal;
            }

            percentTolerance /= 100;
            if (nominal.Value < 0)
                return volts >= nominal * (1 + percentTolerance) && volts <= nominal * (1 - percentTolerance);
            else
                return volts >= nominal * (1 - percentTolerance) && volts <= nominal * (1 + percentTolerance);
        }

        /// <summary>
        /// Checks if the frequency is within the specified range.
        /// </summary>
        public static bool IsFrequencyInRange(Hz hz, Hz nominal, Hz absTolerance)
        {
            return Math.Abs(hz.Value - nominal.Value) <= absTolerance.Value;
        }
    }
}
