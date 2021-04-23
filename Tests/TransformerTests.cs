using ElectricLib;
using ElectricLib.Devices;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Text;

namespace Electric_Sketch_Tests
{
    public class TransformerTests
    {
        [Test]
        public void Simple()
        {
            var sch = new Schematic();

            var ps = new SinglePhaseSupply();
            sch.AddDevice(ps);

            var tr = new Transformer();
            sch.AddDevice(tr);

            var l = new Lamp();
            sch.AddDevice(l);

            sch.Connect(ps.L, tr.A1);
            sch.Connect(ps.N, tr.A2);
            sch.Connect(tr.B1, l.A1);
            sch.Connect(tr.B2, l.A2);

            var sim = new Simulation(sch, DateTime.UtcNow);
            Assert.IsNull(sim.Error);

            var ls = l.Sim(sim);
            Assert.IsTrue(ls.IsOn);
        }

        [Test]
        public void Error()
        {
            var sch = new Schematic();

            var ps = new SinglePhaseSupply();
            sch.AddDevice(ps);

            var tr = new Transformer();
            sch.AddDevice(tr);

            sch.Connect(ps.L, tr.A1);
            sch.Connect(ps.N, tr.A2);

            sch.Connect(ps.N, tr.B2);

            var sim = new Simulation(sch, DateTime.UtcNow);
            Assert.IsNotNull(sim.Error);
            Assert.AreEqual(ErrorCode.VoltageConflict, sim.Error.Code);
        }

        [Test]
        public void Relay()
        {
            var sch = new Schematic();

            var ps = new SinglePhaseSupply();
            sch.AddDevice(ps);

            var tr = new Transformer();
            sch.AddDevice(tr);
            tr.OutFrequency = Hz.DC;

            var l = new Lamp();
            sch.AddDevice(l);
            l.Voltage = ps.Voltage;
            l.Frequency = ps.Frequency;

            var rl = new Relay();
            sch.AddDevice(rl);
            rl.Voltage = tr.OutVoltage;
            Assert.AreNotEqual(ps.Voltage, tr.OutVoltage);
            Assert.IsFalse(Validators.IsVoltageInRange(tr.OutVoltage, l.Voltage.Value, l.VoltageTolerance, l.PolarityMatters));

            sch.Connect(ps.L, tr.A1);
            sch.Connect(ps.N, tr.A2);
            sch.Connect(tr.B1, rl.A2); // B1 = +, A2 = +
            sch.Connect(tr.B2, rl.A1);
            sch.Connect(ps.L, rl.Common(0));
            sch.Connect(rl.NO(0), l.A1);
            sch.Connect(ps.N, l.A2);

            var sim = new Simulation(sch, DateTime.UtcNow);
            Assert.IsNull(sim.Error);

            var ls = l.Sim(sim);
            Assert.IsTrue(ls.IsOn);
        }
    }
}
