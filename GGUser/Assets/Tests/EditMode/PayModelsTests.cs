using System;
using NUnit.Framework;
using SimpleJson;

namespace GGFramework.GGUser.Tests
{
    /// <summary>归一化数据模型的 JSON 序列化测试。</summary>
    public class PayModelsTests
    {
        [Test]
        public void PayRequest_GetJsonString_ContainsCoreFields()
        {
            PayRequest req = new PayRequest
            {
                unifiedProductId = "gem_60",
                developerOrderId = "order_1001",
                userId = "u_1",
                routeHint = "appleStore",
            };
            req.extra["region"] = "cn";

            string json = req.GetJsonString();
            JsonObject o = SimpleJson.SimpleJson.DeserializeObject<JsonObject>(json);

            Assert.AreEqual("gem_60", o["unifiedProductId"].ToString());
            Assert.AreEqual("order_1001", o["developerOrderId"].ToString());
            Assert.AreEqual("u_1", o["userId"].ToString());
            Assert.AreEqual("appleStore", o["routeHint"].ToString());
            Assert.IsTrue(o.ContainsKey("extra"));
        }

        [Test]
        public void PurchaseResult_GetJsonString_SerializesStateAsInt()
        {
            PurchaseResult r = new PurchaseResult
            {
                channel = "appleStore",
                unifiedProductId = "gem_60",
                developerOrderId = "order_1001",
                storeTransactionId = "tx_1",
                state = PurchaseState.Purchased,
            };

            string json = r.GetJsonString();
            JsonObject o = SimpleJson.SimpleJson.DeserializeObject<JsonObject>(json);

            Assert.AreEqual("appleStore", o["channel"].ToString());
            Assert.AreEqual("tx_1", o["storeTransactionId"].ToString());
            Assert.AreEqual((int)PurchaseState.Purchased, Convert.ToInt32(o["state"]));
        }

        [Test]
        public void PurchaseState_HasExpectedNumericOrder()
        {
            Assert.AreEqual(0, (int)PurchaseState.Failed);
            Assert.AreEqual(1, (int)PurchaseState.Purchased);
            Assert.AreEqual(2, (int)PurchaseState.Pending);
            Assert.AreEqual(3, (int)PurchaseState.Restored);
        }
    }
}
