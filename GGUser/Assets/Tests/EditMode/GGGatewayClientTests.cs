using NUnit.Framework;

namespace GGFramework.GGUser.Tests
{
    /// <summary>网关客户端：模拟校验、注入传输后的解析、登录换 token。</summary>
    public class GGGatewayClientTests
    {
        private static PurchaseResult Purchased()
        {
            return new PurchaseResult
            {
                channel = "appleStore",
                unifiedProductId = "gem_60",
                developerOrderId = "order_1",
                storeTransactionId = "tx_1",
                state = PurchaseState.Purchased,
            };
        }

        [Test]
        public void Verify_NullPurchase_ReturnsNotDelivered()
        {
            GGGatewayClient gw = new GGGatewayClient();
            VerifyResponse resp = null;
            gw.Verify(null, r => resp = r);

            Assert.IsNotNull(resp);
            Assert.IsFalse(resp.delivered);
        }

        [Test]
        public void Verify_NoOnPost_SimulatesDeliveredForPurchased()
        {
            GGGatewayClient gw = new GGGatewayClient();
            VerifyResponse resp = null;
            gw.Verify(Purchased(), r => resp = r);

            Assert.IsNotNull(resp);
            Assert.IsTrue(resp.delivered);
            Assert.AreEqual("order_1", resp.orderId);
        }

        [Test]
        public void Verify_NoOnPost_SimulatesNotDeliveredForFailed()
        {
            GGGatewayClient gw = new GGGatewayClient();
            PurchaseResult p = Purchased();
            p.state = PurchaseState.Failed;

            VerifyResponse resp = null;
            gw.Verify(p, r => resp = r);

            Assert.IsNotNull(resp);
            Assert.IsFalse(resp.delivered);
        }

        [Test]
        public void Verify_WithOnPost_PostsToConfiguredUrlAndParsesResponse()
        {
            GGGatewayClient gw = new GGGatewayClient();
            gw.baseUrl = "https://pay.example.com";
            gw.verifyPath = "/v1/verify";

            string capturedUrl = null;
            string capturedBody = null;
            gw.onPost = (url, body, onResponse) =>
            {
                capturedUrl = url;
                capturedBody = body;
                onResponse("{\"delivered\":true,\"orderId\":\"srv_1\",\"message\":\"ok\"}");
            };

            VerifyResponse resp = null;
            gw.Verify(Purchased(), r => resp = r);

            Assert.AreEqual("https://pay.example.com/v1/verify", capturedUrl);
            StringAssert.Contains("order_1", capturedBody);
            Assert.IsNotNull(resp);
            Assert.IsTrue(resp.delivered);
            Assert.AreEqual("srv_1", resp.orderId);
        }

        [Test]
        public void ExchangeToken_NoOnPost_SimulatesPlatformCredentials()
        {
            GGGatewayClient gw = new GGGatewayClient();
            LoginResult login = new LoginResult
            {
                success = true,
                channel = "simulation",
                channelUid = "sim_uid",
                channelToken = "sim_token",
            };

            LoginResult result = null;
            gw.ExchangeToken(login, r => result = r);

            Assert.IsNotNull(result);
            Assert.AreEqual("sim_uid", result.platformUid);
            Assert.AreEqual("platform_sim_token", result.platformToken);
        }

        [Test]
        public void ExchangeToken_FailedLogin_ReturnsUnchangedWithoutCallingGateway()
        {
            GGGatewayClient gw = new GGGatewayClient();
            bool posted = false;
            gw.onPost = (url, body, onResponse) => { posted = true; onResponse("{}"); };

            LoginResult login = new LoginResult { success = false, channel = "x" };
            LoginResult result = null;
            gw.ExchangeToken(login, r => result = r);

            Assert.IsFalse(posted, "登录失败不应请求网关");
            Assert.IsNotNull(result);
            Assert.IsFalse(result.success);
            Assert.IsNull(result.platformToken);
        }

        [Test]
        public void ExchangeToken_WithOnPost_ParsesPlatformUidAndToken()
        {
            GGGatewayClient gw = new GGGatewayClient();
            gw.onPost = (url, body, onResponse) =>
                onResponse("{\"platformUid\":\"p_1\",\"platformToken\":\"pt_1\"}");

            LoginResult login = new LoginResult
            {
                success = true,
                channel = "apple",
                channelUid = "a_uid",
                channelToken = "a_token",
            };

            LoginResult result = null;
            gw.ExchangeToken(login, r => result = r);

            Assert.IsNotNull(result);
            Assert.AreEqual("p_1", result.platformUid);
            Assert.AreEqual("pt_1", result.platformToken);
        }
    }
}
