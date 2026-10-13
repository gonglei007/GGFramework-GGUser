using System.Collections.Generic;
using NUnit.Framework;

namespace GGFramework.GGUser.Tests
{
    /// <summary>
    /// 聚合门面 GGPlatform 的端到端（同步回调）测试：
    /// 支付全链路、可用性回退、校验未通过不结束事务（铁律）、登录换 token、启动补单。
    /// 每个用例都通过 Init 重建 Registry/Config/Gateway/Router 以保证隔离。
    /// </summary>
    public class GGPlatformEditModeTests
    {
        private GGPlatform gg;

        [SetUp]
        public void SetUp()
        {
            gg = GGPlatform.Instance;
        }

        // ---------------- 初始化 / 上下文 ----------------

        [Test]
        public void Init_Default_RegistersSimulationChannel()
        {
            gg.Init(null);
            Assert.IsTrue(gg.Registry.Contains("simulation"));
            Assert.IsNotNull(gg.Registry.GetPay("simulation"));
            Assert.IsNotNull(gg.Registry.GetLogin("simulation"));
        }

        [Test]
        public void CurrentPlatform_InEditor_IsEditor()
        {
            Assert.AreEqual("editor", GGPlatform.CurrentPlatform());
        }

        [Test]
        public void CreatePayContext_PopulatesFields()
        {
            gg.Init(null);
            PayContext ctx = gg.CreatePayContext("gem_60", PayProductType.NonConsumable, "appleStore");
            Assert.AreEqual("editor", ctx.platform);
            Assert.AreEqual("gem_60", ctx.unifiedProductId);
            Assert.AreEqual(PayProductType.NonConsumable, ctx.productType);
            Assert.AreEqual("appleStore", ctx.routeHint);
        }

        // ---------------- 支付全链路 ----------------

        [Test]
        public void Pay_Simulation_WithSimulatedGateway_Succeeds()
        {
            gg.Init(null); // 默认 payChannels=[simulation]，未注入 onPost → 模拟校验通过
            PayOutcome outcome = null;
            PayRequest req = new PayRequest { unifiedProductId = "gem_60", developerOrderId = "d1", userId = "u1" };

            gg.Pay(req, gg.CreatePayContext("gem_60"), o => outcome = o);

            Assert.IsNotNull(outcome);
            Assert.IsTrue(outcome.success);
            Assert.AreEqual("simulation", outcome.purchase.channel);
            Assert.IsTrue(outcome.verify.delivered);
        }

        [Test]
        public void Pay_WithInjectedGateway_Succeeds()
        {
            gg.Init(null);
            gg.Gateway.onPost = (url, body, onResponse) => onResponse("{\"delivered\":true,\"orderId\":\"srv1\"}");
            PayOutcome outcome = null;

            gg.Pay(new PayRequest { unifiedProductId = "gem_60", developerOrderId = "d1" },
                   gg.CreatePayContext("gem_60"), o => outcome = o);

            Assert.IsTrue(outcome.success);
            Assert.AreEqual("srv1", outcome.verify.orderId);
        }

        [Test]
        public void Pay_VerifySuccess_FinishesTransaction()
        {
            PlatformConfig cfg = new PlatformConfig();
            gg.Init(cfg);
            FakePayChannel fake = new FakePayChannel("fake");
            fake.purchaseState = PurchaseState.Purchased;
            gg.RegisterChannel("fake", () => fake);
            cfg.payChannels.Add("fake");
            gg.Gateway.onPost = (url, body, onResponse) => onResponse("{\"delivered\":true,\"orderId\":\"o1\"}");

            PayOutcome outcome = null;
            gg.Pay(new PayRequest { unifiedProductId = "gem_60", developerOrderId = "d1" },
                   gg.CreatePayContext("gem_60"), o => outcome = o);

            Assert.IsTrue(outcome.success);
            CollectionAssert.Contains(fake.finishedTransactions, "fake_tx_1");
        }

        [Test]
        public void Pay_VerifyFailed_KeepsTransactionUnfinished()
        {
            PlatformConfig cfg = new PlatformConfig();
            gg.Init(cfg);
            FakePayChannel fake = new FakePayChannel("fake");
            fake.purchaseState = PurchaseState.Purchased;
            gg.RegisterChannel("fake", () => fake);
            cfg.payChannels.Add("fake");
            gg.Gateway.onPost = (url, body, onResponse) => onResponse("{\"delivered\":false}");

            PayOutcome outcome = null;
            gg.Pay(new PayRequest { unifiedProductId = "gem_60", developerOrderId = "d1" },
                   gg.CreatePayContext("gem_60"), o => outcome = o);

            Assert.IsFalse(outcome.success, "校验未通过不应判定成功");
            Assert.AreEqual(0, fake.finishedTransactions.Count, "铁律：未确认发货不得结束事务");
        }

        [Test]
        public void Pay_PurchaseFailed_FinishesWithoutVerify()
        {
            PlatformConfig cfg = new PlatformConfig();
            gg.Init(cfg);
            FakePayChannel fake = new FakePayChannel("fake");
            fake.purchaseState = PurchaseState.Failed;
            gg.RegisterChannel("fake", () => fake);
            cfg.payChannels.Add("fake");
            bool verified = false;
            gg.Gateway.onPost = (url, body, onResponse) => { verified = true; onResponse("{\"delivered\":true}"); };

            PayOutcome outcome = null;
            gg.Pay(new PayRequest { unifiedProductId = "gem_60", developerOrderId = "d1" },
                   gg.CreatePayContext("gem_60"), o => outcome = o);

            Assert.IsFalse(outcome.success);
            Assert.IsFalse(verified, "购买失败不应送网关校验");
            CollectionAssert.Contains(fake.finishedTransactions, "fake_tx_1");
        }

        [Test]
        public void Pay_PurchasePending_DoesNotFinish()
        {
            PlatformConfig cfg = new PlatformConfig();
            gg.Init(cfg);
            FakePayChannel fake = new FakePayChannel("fake");
            fake.purchaseState = PurchaseState.Pending;
            gg.RegisterChannel("fake", () => fake);
            cfg.payChannels.Add("fake");

            PayOutcome outcome = null;
            gg.Pay(new PayRequest { unifiedProductId = "gem_60", developerOrderId = "d1" },
                   gg.CreatePayContext("gem_60"), o => outcome = o);

            Assert.IsFalse(outcome.success);
            Assert.AreEqual(PurchaseState.Pending, outcome.purchase.state);
            Assert.AreEqual(0, fake.finishedTransactions.Count, "挂起事务应保留等待补单");
        }

        // ---------------- 路由与回退 ----------------

        [Test]
        public void Pay_FallsBack_WhenFirstChannelConnectFails()
        {
            PlatformConfig cfg = new PlatformConfig();
            gg.Init(cfg);
            FakePayChannel bad = new FakePayChannel("bad");
            bad.connectOk = false;
            FakePayChannel good = new FakePayChannel("good");
            gg.RegisterChannel("bad", () => bad);
            gg.RegisterChannel("good", () => good);
            cfg.payChannels.Add("bad");
            cfg.payChannels.Add("good");
            gg.Gateway.onPost = (url, body, onResponse) => onResponse("{\"delivered\":true}");

            PayOutcome outcome = null;
            gg.Pay(new PayRequest { unifiedProductId = "gem_60", developerOrderId = "d1" },
                   gg.CreatePayContext("gem_60"), o => outcome = o);

            Assert.IsTrue(outcome.success);
            Assert.AreEqual("good", outcome.purchase.channel);
            Assert.AreEqual(1, bad.connectCalls);
            Assert.AreEqual(0, bad.purchaseCalls, "连接失败的渠道不应发起购买");
        }

        [Test]
        public void Pay_FallsBack_WhenProductUnavailable()
        {
            PlatformConfig cfg = new PlatformConfig();
            gg.Init(cfg);
            FakePayChannel bad = new FakePayChannel("bad");
            bad.productsAvailable = false;
            FakePayChannel good = new FakePayChannel("good");
            gg.RegisterChannel("bad", () => bad);
            gg.RegisterChannel("good", () => good);
            cfg.payChannels.Add("bad");
            cfg.payChannels.Add("good");
            gg.Gateway.onPost = (url, body, onResponse) => onResponse("{\"delivered\":true}");

            PayOutcome outcome = null;
            gg.Pay(new PayRequest { unifiedProductId = "gem_60", developerOrderId = "d1" },
                   gg.CreatePayContext("gem_60"), o => outcome = o);

            Assert.IsTrue(outcome.success);
            Assert.AreEqual("good", outcome.purchase.channel);
        }

        [Test]
        public void Pay_AllChannelsUnavailable_ReturnsFailure()
        {
            PlatformConfig cfg = new PlatformConfig();
            gg.Init(cfg);
            FakePayChannel bad = new FakePayChannel("bad");
            bad.connectOk = false;
            gg.RegisterChannel("bad", () => bad);
            cfg.payChannels.Add("bad");

            PayOutcome outcome = null;
            gg.Pay(new PayRequest { unifiedProductId = "gem_60", developerOrderId = "d1" },
                   gg.CreatePayContext("gem_60"), o => outcome = o);

            Assert.IsFalse(outcome.success);
        }

        [Test]
        public void Pay_NoCandidates_ReturnsFailure()
        {
            PlatformConfig cfg = new PlatformConfig(); // 空 payChannels
            gg.Init(cfg);

            PayOutcome outcome = null;
            gg.Pay(new PayRequest { unifiedProductId = "gem_60", developerOrderId = "d1" },
                   gg.CreatePayContext("gem_60"), o => outcome = o);

            Assert.IsFalse(outcome.success);
        }

        [Test]
        public void Pay_NullRequest_ReturnsFailure()
        {
            gg.Init(null);
            PayOutcome outcome = null;
            gg.Pay(null, null, o => outcome = o);
            Assert.IsFalse(outcome.success);
        }

        // ---------------- 登录 ----------------

        [Test]
        public void Login_Simulation_ExchangesPlatformToken()
        {
            gg.Init(null); // loginChannels=[simulation]，未注入 onPost → 模拟换 token
            LoginResult result = null;
            gg.Login(r => result = r);

            Assert.IsNotNull(result);
            Assert.IsTrue(result.success);
            Assert.AreEqual("simulation", result.channel);
            Assert.AreEqual("platform_sim_token", result.platformToken);
        }

        [Test]
        public void Login_SpecificChannel_UsesThatChannel()
        {
            PlatformConfig cfg = new PlatformConfig();
            gg.Init(cfg);
            FakeLoginChannel fake = new FakeLoginChannel("fakeLogin");
            gg.RegisterChannel("fakeLogin", () => fake);
            cfg.loginChannels.Add("fakeLogin");

            LoginResult result = null;
            gg.Login("fakeLogin", r => result = r);

            Assert.IsTrue(result.success);
            Assert.AreEqual("fakeLogin", result.channel);
            Assert.AreEqual(1, fake.loginCalls);
        }

        [Test]
        public void Login_NoAvailableChannel_ReturnsFailure()
        {
            PlatformConfig cfg = new PlatformConfig(); // 空 loginChannels
            gg.Init(cfg);

            LoginResult result = null;
            gg.Login(r => result = r);

            Assert.IsNotNull(result);
            Assert.IsFalse(result.success);
        }

        // ---------------- 补单 / 恢复 ----------------

        [Test]
        public void ProcessUnfinished_VerifiesAndFinishes()
        {
            PlatformConfig cfg = new PlatformConfig();
            gg.Init(cfg);
            FakePayChannel fake = new FakePayChannel("fake");
            fake.unfinished.Add(new PurchaseResult
            {
                state = PurchaseState.Purchased,
                storeTransactionId = "u_tx",
                developerOrderId = "d9",
            });
            gg.RegisterChannel("fake", () => fake);
            gg.Gateway.onPost = (url, body, onResponse) => onResponse("{\"delivered\":true,\"orderId\":\"o9\"}");

            PayOutcome outcome = null;
            gg.ProcessUnfinished("fake", o => outcome = o);

            Assert.IsNotNull(outcome);
            Assert.IsTrue(outcome.success);
            CollectionAssert.Contains(fake.finishedTransactions, "u_tx");
        }

        [Test]
        public void ProcessUnfinished_UnknownChannel_DoesNotThrow()
        {
            gg.Init(null);
            Assert.DoesNotThrow(() => gg.ProcessUnfinished("ghost", o => { }));
        }

        [Test]
        public void Restore_ReturnsChannelList()
        {
            PlatformConfig cfg = new PlatformConfig();
            gg.Init(cfg);
            FakePayChannel fake = new FakePayChannel("fake");
            fake.restore.Add(new PurchaseResult { state = PurchaseState.Restored, storeTransactionId = "r1" });
            gg.RegisterChannel("fake", () => fake);

            List<PurchaseResult> restored = null;
            gg.Restore("fake", list => restored = list);

            Assert.IsNotNull(restored);
            Assert.AreEqual(1, restored.Count);
        }

        [Test]
        public void Restore_UnknownChannel_ReturnsNull()
        {
            gg.Init(null);
            bool called = false;
            List<PurchaseResult> restored = new List<PurchaseResult>();
            gg.Restore("ghost", list => { called = true; restored = list; });
            Assert.IsTrue(called);
            Assert.IsNull(restored);
        }
    }
}
