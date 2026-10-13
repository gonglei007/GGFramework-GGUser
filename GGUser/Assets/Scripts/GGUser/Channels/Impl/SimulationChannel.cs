using System;
using System.Collections.Generic;
using UnityEngine;

namespace GGFramework.GGUser
{
    /// <summary>
    /// 模拟渠道：编辑器内跑通「登录 / 查询 / 购买 / 校验 / 补单」全链路，无需真机与商店。
    /// 延续现有 SIMULATION_MODE_SDK 的离线调试理念。
    /// </summary>
    public class SimulationChannel : ILoginChannel, IPayChannel
    {
        public string ChannelId { get { return "simulation"; } }

        public void Init()
        {
            Debug.Log("[SimulationChannel] Init");
        }

        public void Login(Action<LoginResult> callback)
        {
            LoginResult r = new LoginResult();
            r.success = true;
            r.channel = ChannelId;
            r.channelUid = "sim_" + UnityEngine.Random.Range(100000, 999999);
            r.channelToken = "sim_token";
            if (callback != null) callback(r);
        }

        public void Logout()
        {
            Debug.Log("[SimulationChannel] Logout");
        }

        public void Connect(Action<bool> callback)
        {
            if (callback != null) callback(true);
        }

        public void QueryProducts(IList<string> unifiedProductIds, Action<List<StoreProduct>> callback)
        {
            List<StoreProduct> list = new List<StoreProduct>();
            if (unifiedProductIds != null)
            {
                foreach (string id in unifiedProductIds)
                {
                    StoreProduct p = new StoreProduct();
                    p.unifiedProductId = id;
                    p.channelProductId = id;
                    p.title = "商品 " + id;
                    p.description = "模拟商品";
                    p.priceText = "$0.99";
                    p.priceMicros = 990000;
                    p.currencyCode = "USD";
                    list.Add(p);
                }
            }
            if (callback != null) callback(list);
        }

        public void Purchase(PayRequest request, Action<PurchaseResult> callback)
        {
            PurchaseResult r = new PurchaseResult();
            r.channel = ChannelId;
            r.state = PurchaseState.Purchased;
            if (request != null)
            {
                r.unifiedProductId = request.unifiedProductId;
                r.developerOrderId = request.developerOrderId;
            }
            r.storeTransactionId = "sim_tx_" + DateTime.Now.Ticks;
            r.receipt = "sim_receipt";
            r.purchaseTime = DateTime.Now.Ticks;
            r.message = "模拟支付成功";
            Debug.Log("[SimulationChannel] Purchase: " + r.storeTransactionId);
            if (callback != null) callback(r);
        }

        public void Restore(Action<List<PurchaseResult>> callback)
        {
            if (callback != null) callback(new List<PurchaseResult>());
        }

        public void FinishTransaction(string transactionId)
        {
            Debug.Log("[SimulationChannel] FinishTransaction: " + transactionId);
        }

        public void QueryUnfinished(Action<List<PurchaseResult>> callback)
        {
            if (callback != null) callback(new List<PurchaseResult>());
        }
    }
}
