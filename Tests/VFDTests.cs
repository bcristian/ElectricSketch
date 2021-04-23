using ElectricLib;
using ElectricLib.Devices;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Text;

namespace Electric_Sketch_Tests
{
    public class VFDTests
    {
        [Test]
        public void SimpleSinglePhase()
        {
            Simple(VFDSupply.SinglePhase);
        }

        [Test]
        public void SimpleThreePhase()
        {
            Simple(VFDSupply.ThreePhase);
        }

        void Simple(VFDSupply supplyType)
        {
            var sch = new Schematic();

            var ps = new ThreePhaseSupply();
            sch.AddDevice(ps);

            var vfd = new VFD();
            sch.AddDevice(vfd);
            vfd.PowerSupply = supplyType;

            var fwd = new NpstSwitch();
            sch.AddDevice(fwd);

            var rev = new NpstSwitch();
            sch.AddDevice(rev);

            var stop = new NpstSwitch();
            sch.AddDevice(stop);

            var m = new ThreePhaseMotor();
            sch.AddDevice(m);

            var rl = new Lamp();
            sch.AddDevice(rl);

            var fl = new Lamp();
            sch.AddDevice(fl);

            sch.Connect(ps.R, vfd.R);
            sch.Connect(ps.S, vfd.S);
            sch.Connect(ps.T, vfd.T);

            sch.Connect(vfd.U, m.U1);
            sch.Connect(vfd.V, m.V1);
            sch.Connect(vfd.W, m.W1);

            sch.Connect(fwd.A(0), vfd.DICom);
            sch.Connect(rev.A(0), vfd.DICom);
            sch.Connect(stop.A(0), vfd.DICom);

            sch.Connect(fwd.B(0), vfd.DIFwd);
            sch.Connect(rev.B(0), vfd.DIRev);
            sch.Connect(stop.B(0), vfd.DIStop);

            sch.Connect(vfd.RunCom, ps.R);
            sch.Connect(vfd.RunNO, rl.A1);
            sch.Connect(rl.A2, ps.N);

            sch.Connect(vfd.FaultCom, ps.R);
            sch.Connect(vfd.FaultNO, fl.A1);
            sch.Connect(fl.A2, ps.N);

            var sim = new Simulation(sch, DateTime.UtcNow);
            Assert.IsNull(sim.Error);

            var fwdS = fwd.Sim(sim);
            var revS = rev.Sim(sim);
            var stopS = stop.Sim(sim);

            var mS = m.Sim(sim);

            var rls = rl.Sim(sim);
            var fls = fl.Sim(sim);

            Assert.AreEqual(TurnDirection.None, mS.TurnDirection);
            Assert.IsFalse(rls.IsOn);
            Assert.IsFalse(fls.IsOn);

            fwdS.Closed = true;
            sim.Update(TimeSpan.FromSeconds(1));
            Assert.IsNull(sim.Error);
            Assert.AreEqual(TurnDirection.CW, mS.TurnDirection);
            Assert.IsTrue(rls.IsOn);
            Assert.IsFalse(fls.IsOn);

            fwdS.Closed = false;
            sim.Update(TimeSpan.FromSeconds(1));
            Assert.IsNull(sim.Error);
            Assert.AreEqual(TurnDirection.CW, mS.TurnDirection);
            Assert.IsTrue(rls.IsOn);
            Assert.IsFalse(fls.IsOn);

            sim.Update(TimeSpan.FromSeconds(1));
            Assert.IsNull(sim.Error);
            Assert.AreEqual(TurnDirection.CW, mS.TurnDirection);
            Assert.IsTrue(rls.IsOn);
            Assert.IsFalse(fls.IsOn);

            revS.Closed = true;
            sim.Update(TimeSpan.FromSeconds(1));
            Assert.IsNull(sim.Error);
            Assert.AreEqual(TurnDirection.CCW, mS.TurnDirection);
            Assert.IsTrue(rls.IsOn);
            Assert.IsFalse(fls.IsOn);

            revS.Closed = false;
            stopS.Closed = true;
            sim.Update(TimeSpan.FromSeconds(1));
            Assert.IsNull(sim.Error);
            Assert.AreEqual(TurnDirection.None, mS.TurnDirection);
            Assert.IsFalse(rls.IsOn);
            Assert.IsFalse(fls.IsOn);

            stopS.Closed = false;
            sim.Update(TimeSpan.FromSeconds(1));
            Assert.IsNull(sim.Error);
            Assert.AreEqual(TurnDirection.None, mS.TurnDirection);
            Assert.IsFalse(rls.IsOn);
            Assert.IsFalse(fls.IsOn);

            fwdS.Closed = true;
            sim.Update(TimeSpan.FromSeconds(1));
            Assert.IsNull(sim.Error);
            Assert.AreEqual(TurnDirection.CW, mS.TurnDirection);
            Assert.IsTrue(rls.IsOn);
            Assert.IsFalse(fls.IsOn);

            var vfds = vfd.Sim(sim);
            vfds.Fault();
            sim.Update(TimeSpan.FromSeconds(1));
            Assert.IsNull(sim.Error);
            Assert.AreEqual(TurnDirection.None, mS.TurnDirection);
            Assert.IsFalse(rls.IsOn);
            Assert.IsTrue(fls.IsOn);

            fwdS.Closed = false;
            sim.Update(TimeSpan.FromSeconds(1));
            Assert.IsNull(sim.Error);
            Assert.AreEqual(TurnDirection.None, mS.TurnDirection);
            Assert.IsFalse(rls.IsOn);
            Assert.IsTrue(fls.IsOn);

            fwdS.Closed = true;
            sim.Update(TimeSpan.FromSeconds(1));
            Assert.IsNull(sim.Error);
            Assert.AreEqual(TurnDirection.None, mS.TurnDirection);
            Assert.IsFalse(rls.IsOn);
            Assert.IsTrue(fls.IsOn);

            fwdS.Closed = false;
            stopS.Closed = true;
            sim.Update(TimeSpan.FromSeconds(1));
            Assert.IsNull(sim.Error);
            Assert.AreEqual(TurnDirection.None, mS.TurnDirection);
            Assert.IsFalse(rls.IsOn);
            Assert.IsFalse(fls.IsOn);

            stopS.Closed = false;
            fwdS.Closed = true;
            sim.Update(TimeSpan.FromSeconds(1));
            Assert.IsNull(sim.Error);
            Assert.AreEqual(TurnDirection.CW, mS.TurnDirection);
            Assert.IsTrue(rls.IsOn);
            Assert.IsFalse(fls.IsOn);
        }
    }
}
