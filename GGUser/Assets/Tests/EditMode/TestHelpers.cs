using System;
using System.Collections.Generic;

namespace GGFramework.GGUser.Tests
{
    /// <summary>
    /// 可配置的假支付渠道，用于测试路由、可用性回退（fallback）、校验/发货与补单链路，
    /// 所有回调均同步触发，便于 EditMode 断言。
    /// </summary>
    public class FakePayChannel : IPayChannel
    {
        private readonly string id;

        public FakePayChannel(string id)
        {
            this.id = id;
        }

        // 可配置行为
        public bool connectOk = true;
        public bool productsAvailable = true;
        public PurchaseState purchaseState = PurchaseState.Purchased;
        public List<PurchaseResult> unfinished = new List<PurchaseResult>();
        public List<PurchaseResult> restore = new List<PurchaseResult>();

        // 调用计数 / 记录
        public int connectCalls;
        public int queryProductsCalls;
        public int purchaseCalls;
        public List<string> finishedTransactions = new List<string>();

        public string ChannelId { get { return id; } }

        public void Init() { }

        public void Connect(Action<bool> callback)
        {
            connectCalls++;
            if (callback != null) callback(connectOk);
        }

        public void QueryProducts(IList<string> unifiedProductIds, Action<List<StoreProduct>> callback)
        {
            queryProductsCalls++;
            List<StoreProduct> list = new List<StoreProduct>();
            if (productsAvailable && unifiedProductIds != null)
            {
                foreach (string pid in unifiedProductIds)
                {
                    list.Add(new StoreProduct
                    {
                        unifiedProductId = pid,
                        channelProductId = pid,
                        title = "fake_" + pid,
                        priceText = "$1.99",
                        priceMicros = 1990000,
                        currencyCode = "USD",
                    });
                }
            }
            if (callback != null) callback(list);
        }

        public void Purchase(PayRequest request, Action<PurchaseResult> callback)
        {
            purchaseCalls++;
            PurchaseResult r = new PurchaseResult();
            r.channel = id;
            r.state = purchaseState;
            if (request != null)
            {
                r.unifiedProductId = request.unifiedProductId;
                r.developerOrderId = request.developerOrderId;
            }
            r.storeTransactionId = id + "_tx_1";
            r.purchaseTime = 0;
            r.message = purchaseState == PurchaseState.Purchased ? "fake ok" : "fake not purchased";
            if (callback != null) callback(r);
        }

        public void Restore(Action<List<PurchaseResult>> callback)
        {
            if (callback != null) callback(restore);
        }

        public void FinishTransaction(string transactionId)
        {
            finishedTransactions.Add(transactionId);
        }

        public void QueryUnfinished(Action<List<PurchaseResult>> callback)
        {
            if (callback != null) callback(unfinished);
        }
    }

    /// <summary>可配置的假登录渠道。</summary>
    public class FakeLoginChannel : ILoginChannel
    {
        private readonly string id;

        public FakeLoginChannel(string id)
        {
            this.id = id;
        }

        public bool loginSuccess = true;
        public int loginCalls;
        public int logoutCalls;

        public string ChannelId { get { return id; } }

        public void Init() { }

        public void Login(Action<LoginResult> callback)
        {
            loginCalls++;
            LoginResult r = new LoginResult();
            r.success = loginSuccess;
            r.channel = id;
            r.channelUid = id + "_uid";
            r.channelToken = id + "_token";
            r.message = loginSuccess ? "ok" : "failed";
            if (callback != null) callback(r);
        }

        public void Logout()
        {
            logoutCalls++;
        }
    }

    /// <summary>只实现 IChannel（不具备登录/支付能力），用于验证能力探测返回 null。</summary>
    public class FakePlainChannel : IChannel
    {
        public bool initCalled;

        public string ChannelId { get { return "plain"; } }

        public void Init() { initCalled = true; }
    }
}
