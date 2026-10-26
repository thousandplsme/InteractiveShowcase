using System;
using NUnit.Framework;

namespace InteractiveShowcase.Prototype1.Tests
{
    public sealed class ExperienceTests
    {
        private ExperienceStateMachine NewGreeting()
        {
            var m = new ExperienceStateMachine(); m.Tick(0, true, true); m.Tick(1, true, true); return m;
        }
        [Test] public void S01_NoFaceNeverStarts() { var m=new ExperienceStateMachine(); m.Tick(0,true,false); m.Tick(100,true,false); Assert.AreEqual(ExperienceState.Ready,m.State); Assert.Zero(m.StartCount); }
        [Test] public void S02_StartBoundary() { var m=new ExperienceStateMachine(); m.Tick(0,true,true); Assert.False(m.Tick(.999,true,true)); Assert.True(m.Tick(1,true,true)); Assert.False(m.Tick(1.001,true,true)); Assert.AreEqual(1,m.StartCount); }
        [Test] public void S03_InterruptedQualification() { var m=new ExperienceStateMachine(); m.Tick(0,true,true); m.Tick(.99,true,false); m.Tick(1,true,true); Assert.False(m.Tick(1.999,true,true)); Assert.True(m.Tick(2,true,true)); }
        [Test] public void S04_IntermittentFacesDoNotAccumulate() { var m=new ExperienceStateMachine(); for(int i=0;i<20;i++){m.Tick(i,true,true);m.Tick(i+.9,true,false);} Assert.Zero(m.StartCount); }
        [Test] public void S05_GreetingBoundary() {var m=NewGreeting();m.Tick(2.999,true,true);Assert.True(m.IsGreeting);m.Tick(3,true,true);Assert.AreEqual(ExperienceState.AwaitExit,m.State);m.Tick(3.001,true,true);Assert.AreEqual(1,m.StartCount);}
        [Test] public void S06_NoRepeatedGreeting(){var m=NewGreeting();m.Tick(100,true,true);m.Tick(200,true,true);Assert.AreEqual(1,m.StartCount);Assert.AreEqual(ExperienceState.AwaitExit,m.State);}
        [Test] public void S07_LeaveButFinishGreeting(){var m=NewGreeting();m.Tick(1.25,true,false);m.Tick(2.25,true,false);Assert.True(m.IsGreeting);m.Tick(3,true,false);Assert.AreEqual(ExperienceState.Ready,m.State);}
        [Test] public void S08_LateExitWaits(){var m=NewGreeting();m.Tick(2.75,true,false);m.Tick(3,true,false);Assert.AreEqual(ExperienceState.AwaitExit,m.State);m.Tick(3.75,true,false);Assert.AreEqual(ExperienceState.Ready,m.State);}
        [Test] public void S09_ReturnDoesNotExtend(){var m=NewGreeting();m.Tick(1.25,true,false);m.Tick(1.5,true,true);m.Tick(3,true,true);Assert.False(m.IsGreeting);Assert.AreEqual(1,m.StartCount);}
        [Test] public void S10_QualifiedExitIsNotLatched(){var m=NewGreeting();m.Tick(1.25,true,false);m.Tick(2.25,true,false);m.Tick(2.5,true,true);m.Tick(3,true,true);Assert.AreEqual(ExperienceState.AwaitExit,m.State);}
        [Test] public void S11_ShortExitResets(){var m=NewGreeting();m.Tick(3,true,false);m.Tick(3.75,true,true);m.Tick(4,true,false);m.Tick(4.5,true,false);Assert.AreEqual(ExperienceState.AwaitExit,m.State);m.Tick(5,true,false);Assert.AreEqual(ExperienceState.Ready,m.State);}
        [Test] public void S12_ExitBoundary(){var m=NewGreeting();m.Tick(3,true,false);m.Tick(3.999,true,false);Assert.AreEqual(ExperienceState.AwaitExit,m.State);m.Tick(4,true,false);Assert.AreEqual(ExperienceState.Ready,m.State);m.Tick(4.001,true,false);Assert.AreEqual(1,m.StartCount);}
        [Test] public void S13_SecondVisit(){var m=NewGreeting();m.Tick(3,true,false);m.Tick(4,true,false);m.Tick(5,true,true);Assert.False(m.Tick(5.99,true,true));Assert.True(m.Tick(6,true,true));Assert.AreEqual(2,m.StartCount);}
        [Test] public void S14_SimultaneousCompletion(){var m=NewGreeting();m.Tick(2,true,false);Assert.False(m.Tick(3,true,false));Assert.AreEqual(ExperienceState.Ready,m.State);}
        [TestCase(.5)] [TestCase(1.5)] [TestCase(4)]
        public void S15_DisconnectResetsEveryState(double at){var m=new ExperienceStateMachine();m.Tick(0,true,true);if(at>=1)m.Tick(1,true,true);if(at>=3)m.Tick(3,true,true);m.Tick(at,false,true);Assert.AreEqual(ExperienceState.Offline,m.State);m.Tick(10,true,true);Assert.False(m.Tick(10.99,true,true));Assert.True(m.Tick(11,true,true));}
        [TestCase(1)] [TestCase(3)] public void S16_DisconnectHasPriority(double at){var m=new ExperienceStateMachine();m.Tick(0,true,true);if(at==3)m.Tick(1,true,true);Assert.False(m.Tick(at,false,true));Assert.False(m.IsGreeting);Assert.AreEqual(at==3?1:0,m.StartCount);}
        [Test] public void S17_RecoverWithFace(){var m=NewGreeting();m.Tick(1.5,false,false);m.Tick(100,true,true);Assert.False(m.Tick(100.9,true,true));Assert.True(m.Tick(101,true,true));}
        [Test] public void S18_RecoverNoFace(){var m=NewGreeting();m.Tick(1.5,false,false);m.Tick(100,true,false);Assert.AreEqual(ExperienceState.Ready,m.State);Assert.False(m.IsGreeting);}
        [TestCase(.5)] [TestCase(1.5)] [TestCase(4)] public void S19_Reset(double at){var m=new ExperienceStateMachine();m.Tick(0,true,true);if(at>=1)m.Tick(1,true,true);m.Tick(at,true,true);m.Reset();Assert.Zero(m.StartCount);Assert.AreEqual(ExperienceState.Offline,m.State);m.Tick(10,true,true);Assert.False(m.Tick(10.9,true,true));}
        [Test] public void S20_CustomDurations(){var m=new ExperienceStateMachine(.5,1,.25);m.Tick(0,true,true);Assert.True(m.Tick(.5,true,true));m.Tick(1.25,true,false);m.Tick(1.499,true,false);Assert.True(m.IsGreeting);m.Tick(1.5,true,false);Assert.AreEqual(ExperienceState.Ready,m.State);}
        [TestCase(.01)] [TestCase(.25)] public void S21_TickFrequencyAndDuplicateTime(double interval){var m=new ExperienceStateMachine();int starts=0;for(int i=0;i<1000;i++){double t=i*interval;if(m.Tick(t,true,true))starts++;Assert.False(m.Tick(t,true,true));}Assert.AreEqual(1,starts);}

        const string Good="{\"detectionHoldSeconds\":1,\"greetingSeconds\":2,\"exitHoldSeconds\":1}";
        [TestCase(.1f)] [TestCase(10f)] [TestCase(1.5f)]
        public void C01_ValidSettings(float value){string n=value.ToString(System.Globalization.CultureInfo.InvariantCulture);var s=ExperienceSettings.Parse(Good.Replace(":1",":"+n));Assert.AreEqual(value,s.detectionHoldSeconds);}
        [TestCase("{}")] [TestCase("bad")] [TestCase("{\"other\":1}")]
        public void C02_MissingMalformedUnknown(string json){Assert.Throws<FormatException>(()=>ExperienceSettings.Parse(json));}
        [Test] public void C02_Duplicate(){Assert.Throws<FormatException>(()=>ExperienceSettings.Parse(Good.Replace("{","{\"greetingSeconds\":1,")));}
        [TestCase("\"1\"")] [TestCase("true")] [TestCase("null")] [TestCase("NaN")] [TestCase("Infinity")] [TestCase("0")] [TestCase("-1")] [TestCase("11")] [TestCase("1e999")]
        public void C03_InvalidValues(string value){Assert.Throws<FormatException>(()=>ExperienceSettings.Parse(Good.Replace(":1",":"+value)));}
        [TestCase(UdpVisionReceiver.ReceptionState.Tracking,true)] [TestCase(UdpVisionReceiver.ReceptionState.NoFace,true)]
        [TestCase(UdpVisionReceiver.ReceptionState.Waiting,false)] [TestCase(UdpVisionReceiver.ReceptionState.TimedOut,false)]
        [TestCase(UdpVisionReceiver.ReceptionState.Stopped,false)] [TestCase(UdpVisionReceiver.ReceptionState.Error,false)]
        public void U09_ReceptionMapping(UdpVisionReceiver.ReceptionState state,bool connected){Assert.AreEqual(connected,ExperienceController.IsConnected(state));}
    }
}
