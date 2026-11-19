using System;
using NUnit.Framework;
using UnityEngine;

namespace GGFramework.GGUser.Tests
{
    public class SDKInjectionTests
    {
        private GameObject host;
        private UserSystem owner;

        private sealed class FakeSDK : ISDK
        {
            private readonly UserSystem owner;
            public int InitCalls;
            public FakeSDK(UserSystem owner) { this.owner = owner; }
            public void Init() { InitCalls++; owner.CallSDKInitSuccess("fake"); }
            public void Login() { }
            public void Logout() { }
            public void Quit() { }
            public void Pay(Order order, UserSystem.UserData data = null) { owner.CallSDKPayCancel("fake"); }
            public void SubmitData(string data) { }
        }

        [SetUp]
        public void SetUp()
        {
            SingletonLifetime.IsQuitting = false;
            host = new GameObject("SDK injection test");
            owner = host.AddComponent<UserSystem>();
            // EditMode does not run MonoBehaviour.Awake automatically.
            typeof(UserSystem).GetMethod("Awake", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
                .Invoke(owner, null);
        }

        [TearDown]
        public void TearDown()
        {
            if (owner.SDK is NativeSDK native) NativeLuaBridge.onNativeCallUnity -= native.HandleSDKCallback;
            UnityEngine.Object.DestroyImmediate(host);
            SingletonLifetime.IsQuitting = false;
        }

        [Test]
        public void ConfigureSDK_ReplacesFactoryDefault_AndRejectsNull()
        {
            Assert.Throws<ArgumentNullException>(() => owner.ConfigureSDK(null));
            var fake = new FakeSDK(owner);
            owner.ConfigureSDK(fake);
            Assert.That(owner.SDK, Is.SameAs(fake));
        }

        [Test]
        public void SDKInit_ReportsInitializationBeforeAnyLoginTransition_ExactlyOnce()
        {
            var fake = new FakeSDK(owner);
            owner.ConfigureSDK(fake);
            int events = 0;
            owner.onEvent += (state, evt, value) => { if (evt == UserSystem.Events.SDKInitSuccess) events++; };
            owner.SDKInit();
            Assert.That(fake.InitCalls, Is.EqualTo(1));
            Assert.That(events, Is.EqualTo(1), "Initialization is broadcast directly even when no login follows.");
        }

        [Test]
        public void SDKPay_InjectedCancellation_DoesNotAlsoEmitEditorSuccess()
        {
            owner.ConfigureSDK(new FakeSDK(owner));
            int canceled = 0, success = 0;
            owner.onEvent += (state, evt, value) =>
            {
                if (evt == UserSystem.Events.SDKPayCancel) canceled++;
                if (evt == UserSystem.Events.SDKPaySuccess) success++;
            };
            owner.SDKPay(new Order());
            Assert.That(canceled, Is.EqualTo(1));
            Assert.That(success, Is.Zero);
        }
    }
}
