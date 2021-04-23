using ElectricLib;
using ElectricLib.Devices;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Text;

namespace Electric_Sketch_Tests
{
    public class RelayTests_Timed
    {
        void Setup(RelayFunction func)
        {
            sch = new Schematic();

            sch.AddDevice(ps = new SinglePhaseSupply());
            sch.AddDevice(rl = new Relay());
            sch.AddDevice(l = new Lamp());
            sch.AddDevice(sw = new NpstSwitch());
            sch.AddDevice(masterSw = new NpstSwitch());

            rl.Function = func;
            rl.Interval = TimeSpan.FromSeconds(1);

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

        void Update(double deltaT)
        {
            sim.Update(TimeSpan.FromSeconds(deltaT));
            Assert.IsNull(sim.Error);
        }


        [Test]
        public void OnDelay()
        {
            Setup(RelayFunction.OnDelay);

            Assert.IsFalse(lSim.IsOn);

            Update(0.5);
            Assert.IsFalse(lSim.IsOn);

            Update(0.51);
            Assert.IsTrue(lSim.IsOn);

            Update(0.51);
            Assert.IsTrue(lSim.IsOn);

            masterSwSim.Closed = false;
            Update(0.1);
            Assert.IsFalse(lSim.IsOn);

            masterSwSim.Closed = true;
            Update(0.1);
            Assert.IsFalse(lSim.IsOn);

            Update(0.51);
            Assert.IsFalse(lSim.IsOn);

            Update(0.51);
            Assert.IsTrue(lSim.IsOn);
        }

        [Test]
        public void OnDelaySignal()
        {
            Setup(RelayFunction.OnDelayBySignal);

            Assert.IsFalse(lSim.IsOn);

            Update(0.5);
            Assert.IsFalse(lSim.IsOn);

            Update(0.51);
            Assert.IsFalse(lSim.IsOn);

            swSim.Closed = true;
            Update(0.5);
            Assert.IsFalse(lSim.IsOn);

            Update(0.51);
            Assert.IsTrue(lSim.IsOn);

            swSim.Closed = false;
            Update(0);
            Assert.IsFalse(lSim.IsOn);

            Update(10);
            Assert.IsFalse(lSim.IsOn);

            swSim.Closed = true;
            Update(0.99);
            Assert.IsFalse(lSim.IsOn);

            Update(0.02);
            Assert.IsTrue(lSim.IsOn);

            swSim.Closed = false;
            Update(0.0);
            Assert.IsFalse(lSim.IsOn);

            masterSwSim.Closed = false;
            Update(0.0);
            Assert.IsFalse(lSim.IsOn);

            masterSwSim.Closed = true;
            swSim.Closed = true;
            Update(0.0);
            Assert.IsFalse(lSim.IsOn);

            Update(0.5);
            Assert.IsFalse(lSim.IsOn);

            Update(0.51);
            Assert.IsTrue(lSim.IsOn);
        }

        [Test]
        public void OffDelay()
        {
            Setup(RelayFunction.OffDelay);

            Assert.IsFalse(lSim.IsOn);

            swSim.Closed = true;
            Update(0.01);
            Assert.IsTrue(lSim.IsOn);

            Update(0.51);
            Assert.IsTrue(lSim.IsOn);

            Update(10);
            Assert.IsTrue(lSim.IsOn);

            swSim.Closed = false;
            Update(0.01);
            Assert.IsTrue(lSim.IsOn);

            Update(0.51);
            Assert.IsTrue(lSim.IsOn);

            Update(0.51);
            Assert.IsFalse(lSim.IsOn);

            swSim.Closed = true;
            Update(0.01);
            Assert.IsTrue(lSim.IsOn);

            masterSwSim.Closed = false;
            Update(0.01);
            Assert.IsFalse(lSim.IsOn);

            masterSwSim.Closed = true;
            swSim.Closed = true;
            Update(0.01);
            Assert.IsTrue(lSim.IsOn);
        }

        [Test]
        public void SingleShotLeadingEdge()
        {
            Setup(RelayFunction.SingleShotLeadingEdge);

            Assert.IsFalse(lSim.IsOn);

            // Signal control mode.
            swSim.Closed = true;
            Update(0.1);
            Assert.IsTrue(lSim.IsOn);

            Update(0.5);
            Assert.IsTrue(lSim.IsOn);

            Update(0.5);
            Assert.IsFalse(lSim.IsOn);

            swSim.Closed = false;
            Update(0.1);
            Assert.IsFalse(lSim.IsOn);

            swSim.Closed = true;
            Update(0.1);
            Assert.IsTrue(lSim.IsOn);

            swSim.Closed = false;
            Update(0.1);
            Assert.IsTrue(lSim.IsOn);

            swSim.Closed = true;
            Update(0.1);
            Assert.IsTrue(lSim.IsOn);

            Update(0.4);
            Assert.IsTrue(lSim.IsOn);

            Update(0.5);
            Assert.IsFalse(lSim.IsOn);

            // Power control mode (keep signal connected, toggle power). Will energize when powered, for the duration of the interval or until powered off.
            masterSwSim.Closed = false;
            swSim.Closed = true;
            Update(0.1);
            Assert.IsFalse(lSim.IsOn);

            masterSwSim.Closed = true;
            Update(0.1);
            Assert.IsTrue(lSim.IsOn);

            Update(1.1);
            Assert.IsFalse(lSim.IsOn);

            masterSwSim.Closed = false;
            Update(0.1);
            Assert.IsFalse(lSim.IsOn);

            masterSwSim.Closed = true;
            Update(0.1);
            Assert.IsTrue(lSim.IsOn);

            Update(1.1);
            Assert.IsFalse(lSim.IsOn);
        }

        [Test]
        public void SingleShotTrailingEdge()
        {
            Setup(RelayFunction.SingleShotTrailingEdge);

            Assert.IsFalse(lSim.IsOn);

            swSim.Closed = true;
            Update(0);
            Assert.IsFalse(lSim.IsOn);

            swSim.Closed = false;
            Update(0.05);
            Assert.IsTrue(lSim.IsOn);

            Update(0.9);
            Assert.IsTrue(lSim.IsOn);

            Update(0.1);
            Assert.IsFalse(lSim.IsOn);

            swSim.Closed = true;
            Update(0.0);
            Assert.IsFalse(lSim.IsOn);

            swSim.Closed = false;
            Update(0.0);
            Assert.IsTrue(lSim.IsOn);
        }

        [Test]
        public void FlasherPauseFirst()
        {
            Setup(RelayFunction.FlasherPauseFirst);

            Assert.IsFalse(lSim.IsOn);

            Update(1.1);
            Assert.IsTrue(lSim.IsOn);

            Update(1.0);
            Assert.IsFalse(lSim.IsOn);

            Update(1.0);
            Assert.IsTrue(lSim.IsOn);
        }

        [Test]
        public void FlasherPulseFirst()
        {
            Setup(RelayFunction.FlasherPulseFirst);

            Assert.IsTrue(lSim.IsOn);

            Update(1.1);
            Assert.IsFalse(lSim.IsOn);

            Update(1.0);
            Assert.IsTrue(lSim.IsOn);

            Update(1.0);
            Assert.IsFalse(lSim.IsOn);
        }
    }
}
