using ElectricLib;
using ElectricLib.Devices;
using NUnit.Framework;
using System;

namespace Electric_Sketch_Tests
{
    public class SchematicsTests
    {
        [Test]
        public void AddingDevices()
        {
            var s = new Schematic();
            var a = new Lamp();
            var b = new Lamp("Another lamp");
            var c = new Lamp();

            s.AddDevice(a);
            Assert.IsTrue(a.Name == "L1");
            s.AddDevice(b);
            Assert.IsTrue(b.Name == "Another lamp");
            s.AddDevice(c);
            Assert.IsTrue(c.Name == "L3");
            Assert.Catch(() => s.AddDevice(a), "Not again");
        }

        [Test]
        public void ConnectingAndDisconnecting()
        {
            var s = new Schematic();
            var a = new Lamp();
            var b = new Lamp();

            s.AddDevice(a);
            s.AddDevice(b);

            Assert.Catch(() => s.Connect(a.A1), "Should not be able to connect a single pin");

            s.Connect(a.A1, a.A2);
            Assert.IsTrue(s.Connections.Count == 1);

            s.Connect(b.A1, b.A2);
            Assert.IsTrue(s.Connections.Count == 2);

            s.Connect(a.A1, b.A2);
            Assert.IsTrue(s.Connections.Count == 1);

            s.Disconnect(a.A1);
            s.Disconnect(b.A1);
            Assert.IsTrue(s.Connections.Count == 1);

            s.Connect(a.A1, b.A1);
            Assert.IsTrue(s.Connections.Count == 2);
        }

        [Test]
        public void RemovingDevice()
        {
            var s = new Schematic();
            var a = new Lamp();
            var b = new Lamp();

            s.AddDevice(a);
            s.AddDevice(b);

            s.Connect(a.A1, a.A2);
            s.Connect(b.A1, b.A2);
            Assert.IsTrue(s.Connections.Count == 2);

            s.RemoveDevice(b);
            Assert.IsTrue(s.Connections.Count == 1);

            s.AddDevice(b);
            s.Connect(b.A1, b.A2);
            Assert.IsTrue(s.Connections.Count == 2);
        }

        [Test]
        public void TestConnections()
        {
            var sch = new Schematic();

            var l1 = new Lamp();
            sch.AddDevice(l1);

            var l2 = new Lamp();
            sch.AddDevice(l2);

            var l3 = new Lamp();
            sch.AddDevice(l3);

            var sw = new NpstSwitch();
            sch.AddDevice(sw);

            sw.Closed = true;

            sch.Connect(l1.A1, l2.A1);
            sch.Connect(l1.A2, l2.A2);
            sch.Connect(l1.A2, sw.A(0));
            sch.Connect(sw.B(0), l3.A1);

            var sim = new Simulation(sch, DateTime.UtcNow);
            Assert.IsNull(sim.Error);

            var sl1 = l1.Sim(sim);
            var sl2 = l2.Sim(sim);
            var sl3 = l3.Sim(sim);
            var ssw = sw.Sim(sim);

            Assert.IsTrue(sim.AreConnected(sl1.Pins[0], sl2.Pins[0]));
            Assert.IsTrue(sim.AreConnected(sl1.Pins[1], ssw.Pins[0]));
            Assert.IsTrue(sim.AreConnected(sl2.Pins[1], ssw.Pins[0]));
            Assert.IsTrue(sim.AreConnected(sl2.Pins[1], ssw.Pins[1]));
            Assert.IsTrue(sim.AreConnected(sl1.Pins[1], sl3.Pins[0]));
        }
    }
}