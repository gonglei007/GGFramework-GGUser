using System.Collections.Generic;
using NUnit.Framework;

namespace GGFramework.GGUser.Tests
{
    /// <summary>模拟渠道：编辑器内跑通全链路的行为契约。</summary>
    public class SimulationChannelTests
    {
        [Test]
        public void Implements_BothLoginAndPayCapabilities()
        {
            SimulationChannel ch = new SimulationChannel();
            Assert.AreEqual("simulation", ch.ChannelId);
            Assert.IsInstanceOf<ILoginChannel>(ch);
            Assert.IsInstanceOf<IPayChannel>(ch);
        }

        [Test]
        public void Login_ReturnsSuccessWithChannelCredentials()
        {
            SimulationChannel ch = new SimulationChannel();
            LoginResult result = null;
            ch.Login(r => result = r);

            Assert.IsNotNull(result);
            Assert.IsTrue(result.success);
            Assert.AreEqual("simulation", result.channel);
            StringAssert.StartsWith("sim_", result.channelUid);
            Assert.AreEqual("sim_token", result.channelToken);
        }

        [Test]
        public void Connect_ReturnsTrue()
        {
            SimulationChannel ch = new SimulationChannel();
            bool ok = false;
            ch.Connect(b => ok = b);
            Assert.IsTrue(ok);
        }

        [Test]
        public void QueryProducts_ReturnsOneProductPerRequestedId()
        {
            SimulationChannel ch = new SimulationChannel();
            List<StoreProduct> products = null;
            ch.QueryProducts(new List<string> { "gem_60", "gem_300" }, p => products = p);

            Assert.IsNotNull(products);
            Assert.AreEqual(2, products.Count);
            Assert.AreEqual("gem_60", products[0].unifiedProductId);
            Assert.AreEqual("$0.99", products[0].priceText);
        }

        [Test]
        public void Purchase_ReturnsPurchasedWithTransactionId()
        {
            SimulationChannel ch = new SimulationChannel();
            PurchaseResult result = null;
            ch.Purchase(new PayRequest { unifiedProductId = "gem_60", developerOrderId = "d1" }, r => result = r);

            Assert.IsNotNull(result);
            Assert.AreEqual(PurchaseState.Purchased, result.state);
            StringAssert.StartsWith("sim_tx_", result.storeTransactionId);
            Assert.AreEqual("gem_60", result.unifiedProductId);
            Assert.AreEqual("d1", result.developerOrderId);
        }

        [Test]
        public void Restore_And_QueryUnfinished_ReturnEmptyNotNull()
        {
            SimulationChannel ch = new SimulationChannel();

            List<PurchaseResult> restored = null;
            ch.Restore(r => restored = r);
            Assert.IsNotNull(restored);
            Assert.AreEqual(0, restored.Count);

            List<PurchaseResult> unfinished = null;
            ch.QueryUnfinished(u => unfinished = u);
            Assert.IsNotNull(unfinished);
            Assert.AreEqual(0, unfinished.Count);
        }

        [Test]
        public void FinishTransaction_DoesNotThrow()
        {
            SimulationChannel ch = new SimulationChannel();
            Assert.DoesNotThrow(() => ch.FinishTransaction("sim_tx_1"));
        }
    }
}
