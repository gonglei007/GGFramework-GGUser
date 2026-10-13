using System.Collections.Generic;
using SimpleJson;

namespace GGFramework.GGUser
{
    /// <summary>购买状态。</summary>
    public enum PurchaseState
    {
        Failed = 0,
        Purchased = 1,
        Pending = 2,
        Restored = 3,
    }

    /// <summary>商品类型。</summary>
    public enum PayProductType
    {
        Consumable = 0,
        NonConsumable = 1,
        Subscription = 2,
    }

    /// <summary>业务发起支付的统一请求（归一化，屏蔽渠道差异）。</summary>
    public class PayRequest
    {
        public string unifiedProductId;                 // 平台统一商品ID
        public string developerOrderId;                 // 自己的订单号（贯穿对账）
        public string userId;                           // 平台账号
        public string routeHint;                        // 可选：强制指定渠道
        public Dictionary<string, string> extra = new Dictionary<string, string>();

        public string GetJsonString()
        {
            JsonObject json = new JsonObject();
            json["unifiedProductId"] = this.unifiedProductId;
            json["developerOrderId"] = this.developerOrderId;
            json["userId"] = this.userId;
            json["routeHint"] = this.routeHint;
            JsonObject ex = new JsonObject();
            if (this.extra != null)
            {
                foreach (KeyValuePair<string, string> kv in this.extra)
                {
                    ex[kv.Key] = kv.Value;
                }
            }
            json["extra"] = ex;
            return json.ToString();
        }
    }

    /// <summary>从商店拉取到的商品（含本地化价格）。</summary>
    public class StoreProduct
    {
        public string unifiedProductId;
        public string channelProductId;                 // 各商店真实商品ID
        public string title;
        public string description;
        public string priceText;                        // 本地化价格文案（展示用）
        public long priceMicros;
        public string currencyCode;
    }

    /// <summary>购买结果，携带票据信息，送网关校验。</summary>
    public class PurchaseResult
    {
        public string channel;                          // appleStore / googlePlay / custom / simulation
        public string unifiedProductId;
        public string developerOrderId;
        public string storeTransactionId;               // iOS transactionId / Android orderId
        public string purchaseToken;                    // Android 专有
        public string receipt;                          // iOS base64 收据 / Android 整包 JSON
        public string signature;                        // Android 签名
        public long purchaseTime;
        public PurchaseState state = PurchaseState.Failed;
        public string message;

        public string GetJsonString()
        {
            JsonObject json = new JsonObject();
            json["channel"] = this.channel;
            json["unifiedProductId"] = this.unifiedProductId;
            json["developerOrderId"] = this.developerOrderId;
            json["storeTransactionId"] = this.storeTransactionId;
            json["purchaseToken"] = this.purchaseToken;
            json["receipt"] = this.receipt;
            json["signature"] = this.signature;
            json["purchaseTime"] = this.purchaseTime;
            json["state"] = (int)this.state;
            json["message"] = this.message;
            return json.ToString();
        }
    }

    /// <summary>路由上下文。</summary>
    public class PayContext
    {
        public string platform = "editor";              // ios / android / editor
        public bool oversea = true;                     // Android 海外/国内
        public string region;
        public PayProductType productType = PayProductType.Consumable;
        public string userId;
        public string routeHint;                        // 强制指定渠道
        public string unifiedProductId;
    }

    /// <summary>网关票据校验结果。</summary>
    public class VerifyResponse
    {
        public bool delivered;
        public string orderId;
        public string items;                            // 发货内容（JSON 字符串）
        public string message;
    }

    /// <summary>返回给业务的支付最终结果。</summary>
    public class PayOutcome
    {
        public bool success;                            // 是否最终发货成功
        public PurchaseResult purchase;
        public VerifyResponse verify;
        public string message;
    }

    /// <summary>登录结果。</summary>
    public class LoginResult
    {
        public bool success;
        public string channel;
        public string channelUid;                       // 渠道账号ID
        public string channelToken;                     // 渠道凭证（送网关换平台 token）
        public string platformUid;                      // 平台账号（网关返回后填充）
        public string platformToken;
        public string message;
    }
}
