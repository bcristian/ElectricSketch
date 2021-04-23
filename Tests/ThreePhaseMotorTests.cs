using ElectricLib;
using ElectricLib.Devices;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Text;

namespace Electric_Sketch_Tests
{
    public class ThreePhaseMotorTests
    {
        [SetUp]
        public void Setup()
        {
            sch = new Schematic();

            sch.AddDevice(ps = new ThreePhaseSupply());
            sch.AddDevice(m = new ThreePhaseMotor());
        }

        Schematic sch;
        ThreePhaseSupply ps;
        ThreePhaseMotor m;

        [Test]
        public void Simplest()
        {
            sch.Connect(ps.R, m.U1);
            sch.Connect(ps.S, m.V1);
            sch.Connect(ps.T, m.W1);

            var sim = new Simulation(sch, DateTime.UtcNow);
            Assert.IsNull(sim.Error);

            var ms = m.Sim(sim);
            Assert.AreEqual(TurnDirection.CW, ms.TurnDirection);
        }

        [Test]
        public void MissingPhase()
        {
            sch.Connect(ps.R, m.U1);
            sch.Connect(ps.S, m.V1);
            //sch.Connect(ps.T, m.W1);

            var sim = new Simulation(sch, DateTime.UtcNow);
            Assert.IsNotNull(sim.Error);
            Assert.AreEqual(ErrorCode.InvalidConnections, sim.Error.Code);
        }

        [Test]
        public void DuplicatePhase()
        {
            sch.Connect(ps.R, m.U1);
            sch.Connect(ps.S, m.V1);
            //sch.Connect(ps.T, m.W1);
            sch.Connect(ps.S, m.W1);

            var sim = new Simulation(sch, DateTime.UtcNow);
            Assert.IsNotNull(sim.Error);
            Assert.AreEqual(ErrorCode.InvalidConnections, sim.Error.Code);
        }

        [Test]
        public void InvalidLamp()
        {
            var l = new Lamp();
            sch.AddDevice(l);

            sch.Connect(ps.R, m.U1);
            sch.Connect(ps.S, m.V1);
            sch.Connect(ps.T, l.A1);
            sch.Connect(l.A2, m.W1);

            var sim = new Simulation(sch, DateTime.UtcNow);
            Assert.IsNotNull(sim.Error);
            Assert.AreEqual(ErrorCode.SeriesConnection, sim.Error.Code);
        }

        [Test]
        public void ThreeInvalidLamps()
        {
            var l1 = new Lamp();
            sch.AddDevice(l1);
            var l2 = new Lamp();
            sch.AddDevice(l2);
            var l3 = new Lamp();
            sch.AddDevice(l3);

            sch.Connect(ps.R, l1.A1);
            sch.Connect(l1.A2, m.U1);

            sch.Connect(ps.S, l2.A1);
            sch.Connect(l2.A2, m.V1);

            sch.Connect(ps.T, l3.A1);
            sch.Connect(l3.A2, m.W1);

            var sim = new Simulation(sch, DateTime.UtcNow);
            Assert.IsNotNull(sim.Error);
            Assert.AreEqual(ErrorCode.SeriesConnection, sim.Error.Code);
        }

        [Test]
        public void ThreeSwitchShuntedInvalidLamps()
        {
            var l1 = new Lamp();
            sch.AddDevice(l1);
            var l2 = new Lamp();
            sch.AddDevice(l2);
            var l3 = new Lamp();
            sch.AddDevice(l3);

            sch.Connect(ps.R, l1.A1);
            sch.Connect(l1.A2, m.U1);

            sch.Connect(ps.S, l2.A1);
            sch.Connect(l2.A2, m.V1);

            sch.Connect(ps.T, l3.A1);
            sch.Connect(l3.A2, m.W1);

            var sw = new NpstSwitch(3);
            sch.AddDevice(sw);
            sch.Connect(l1.A1, sw.A(0));
            sch.Connect(l1.A2, sw.B(0));
            sch.Connect(l2.A1, sw.A(1));
            sch.Connect(l2.A2, sw.B(1));
            sch.Connect(l3.A1, sw.A(2));
            sch.Connect(l3.A2, sw.B(2));
            sw.Closed = true;

            var sim = new Simulation(sch, DateTime.UtcNow);
            Assert.IsNull(sim.Error);

            var ms = m.Sim(sim);
            Assert.AreEqual(TurnDirection.CW, ms.TurnDirection);

            var sws = sw.Sim(sim);
            sws.Closed = false;
            sim.Update(TimeSpan.FromMilliseconds(1));

            Assert.IsNotNull(sim.Error);
            Assert.AreEqual(ErrorCode.SeriesConnection, sim.Error.Code);
        }

        [Test]
        public void Reverse()
        {
            var sw = new RotarySwitch(3, 3);
            sch.AddDevice(sw);

            sch.Connect(ps.R, sw.CommonPin(0));
            sch.Connect(ps.S, sw.CommonPin(1));
            sch.Connect(ps.T, sw.CommonPin(2));
            sch.Connect(m.U1, sw.PositionPin(0, 0));
            sch.Connect(m.V1, sw.PositionPin(1, 0));
            sch.Connect(m.W1, sw.PositionPin(2, 0));
            sch.Connect(m.U1, sw.PositionPin(0, 2));
            sch.Connect(m.V1, sw.PositionPin(2, 2));
            sch.Connect(m.W1, sw.PositionPin(1, 2));

            var sim = new Simulation(sch, DateTime.UtcNow);
            Assert.IsNull(sim.Error);

            var ms = m.Sim(sim);
            Assert.AreEqual(TurnDirection.CW, ms.TurnDirection);

            var sws = sw.Sim(sim);
            sws.Position = 1;
            sim.Update(TimeSpan.FromSeconds(1));
            Assert.IsNull(sim.Error);
            Assert.AreEqual(TurnDirection.None, ms.TurnDirection);

            sws.Position = 2;
            sim.Update(TimeSpan.FromSeconds(1));
            Assert.IsNull(sim.Error);
            Assert.AreEqual(TurnDirection.CCW, ms.TurnDirection);
        }

        [Test]
        public void DahlanderHiLo()
        {
            m.Configuration = ThreePhaseMotorConfig.Dahlander;

            // 1-0-2 Dahlander cam switch
            var sw = new CamSwitch(3, 8, new bool[,]
                {
                    { true, false, false },
                    { false, false, true },
                    { false, false, true },
                    { false, false, true },
                    { true, false, false },
                    { true, false, false },
                    { false, false, true },
                    { false, false, true },
                });
            sch.AddDevice(sw);

            sch.Connect(sw.Pins[0], sw.Pins[2]);
            sch.Connect(sw.Pins[4], sw.Pins[6]);
            sch.Connect(sw.Pins[4], sw.Pins[8]);
            sch.Connect(sw.Pins[10], sw.Pins[14]);
            sch.Connect(sw.Pins[1], sw.Pins[5]);
            sch.Connect(sw.Pins[7], sw.Pins[11]);
            sch.Connect(sw.Pins[9], sw.Pins[13]);

            sch.Connect(ps.R, sw.Pins[0]);
            sch.Connect(ps.S, sw.Pins[9]);
            sch.Connect(ps.T, sw.Pins[14]);
            sch.Connect(m.U1, sw.Pins[1]);
            sch.Connect(m.V1, sw.Pins[4]);
            sch.Connect(m.W1, sw.Pins[7]);
            sch.Connect(m.U2, sw.Pins[3]);
            sch.Connect(m.V2, sw.Pins[12]);
            sch.Connect(m.W2, sw.Pins[15]);

            var sim = new Simulation(sch, DateTime.UtcNow);
            Assert.IsNull(sim.Error);

            var ms = m.Sim(sim);
            Assert.AreEqual(TurnDirection.CW, ms.TurnDirection);
            Assert.IsFalse(ms.HiSpeed);

            var sws = sw.Sim(sim);
            sws.Position = 1;
            sim.Update(TimeSpan.FromSeconds(1));
            Assert.IsNull(sim.Error);
            Assert.AreEqual(TurnDirection.None, ms.TurnDirection);

            sws.Position = 2;
            sim.Update(TimeSpan.FromSeconds(1));
            Assert.IsNull(sim.Error);
            Assert.AreEqual(TurnDirection.CW, ms.TurnDirection);
            Assert.IsTrue(ms.HiSpeed);
        }
    }
}
