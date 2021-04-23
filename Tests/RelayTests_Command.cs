using ElectricLib;
using ElectricLib.Devices;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Text;

namespace Electric_Sketch_Tests
{
    public class RelayTests_Command
    {
        [SetUp]
        public void Setup()
        {
            sch = new Schematic();

            sch.AddDevice(ps = new SinglePhaseSupply());
            sch.AddDevice(rl = new Relay());
            sch.AddDevice(l = new Lamp());
            sch.AddDevice(sw = new NpstSwitch());
            sch.AddDevice(masterSw = new NpstSwitch());

            rl.Function = RelayFunction.Command;

            sch.Connect(ps.L, masterSw.A(0));
            sch.Connect(masterSw.B(0), rl.A1);
            sch.Connect(ps.N, rl.A2);
            sch.Connect(masterSw.B(0), rl.Common(0));
            sch.Connect(rl.A1, sw.A(0));
            sch.Connect(rl.B1, sw.B(0));
            sch.Connect(ps.N, l.A2);
            sch.Connect(rl.NO(0), l.A1);

            masterSw.Closed = true;

            sim = new Simulation(sch, DateTime.UtcNow);
            Assert.IsNull(sim.Error);

            rlSim = rl.Sim(sim);
            lSim = l.Sim(sim);
            swSim = sw.Sim(sim);
            masterSwSim = masterSw.Sim(sim);
        }

        Schematic sch;
        SinglePhaseSupply ps;
        Relay rl;
        Lamp l;
        NpstSwitch sw, masterSw;
        Simulation sim;
        RelaySim rlSim;
        SinglePhaseConsumerSim lSim;
        NpstSwitchSim swSim;
        NpstSwitchSim masterSwSim;


        [Test]
        public void NormalOperation()
        {
            Assert.IsFalse(lSim.IsOn);

            swSim.Closed = true;
            sim.Update(TimeSpan.FromSeconds(1));
            Assert.IsNull(sim.Error);
            Assert.IsTrue(lSim.IsOn);

            swSim.Closed = false;
            sim.Update(TimeSpan.FromSeconds(1));
            Assert.IsNull(sim.Error);
            Assert.IsFalse(lSim.IsOn);
        }

        [Test]
        public void CutPower()
        {
            Assert.IsFalse(lSim.IsOn);

            swSim.Closed = true;
            sim.Update(TimeSpan.FromSeconds(1));
            Assert.IsNull(sim.Error);
            Assert.IsTrue(lSim.IsOn);

            masterSwSim.Closed = false;
            sim.Update(TimeSpan.FromSeconds(1));
            Assert.IsNull(sim.Error);
            Assert.IsFalse(lSim.IsOn);
        }
    }
}
