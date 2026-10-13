using System;
using SimpleJson;
using UnityEngine;

namespace GGFramework.GGUser
{
    /// <summary>
    /// 自建统一服务端网关客户端。
    /// 传输实现通过 <see cref="onPost"/> 委托注入（框架不绑定任何网络库，风格同 UIAdaptor/LogAdaptor）。
    /// 未注入时给出模拟结果，便于编辑器联调。
    /// </summary>
    public class GGGatewayClient
    {
        // (url, jsonBody, onResponse)
        public Action<string, string, Action<string>> onPost = null;

        public string baseUrl = "";
        public string verifyPath = "/ggpay/verify";
        public string loginPath = "/ggaccount/token";

        /// <summary>票据校验：把购买结果送网关，返回是否可发货。</summary>
        public void Verify(PurchaseResult purchase, Action<VerifyResponse> callback)
        {
            if (purchase == null)
            {
                if (callback != null) callback(new VerifyResponse { delivered = false, message = "空的购买结果" });
                return;
            }

            string url = baseUrl + verifyPath;
            string body = purchase.GetJsonString();

            if (onPost == null)
            {
                Debug.LogWarning("[GGGatewayClient] 未注入 onPost，返回模拟校验结果。");
                if (callback != null) callback(SimulateVerify(purchase));
                return;
            }

            onPost(url, body, delegate(string resp)
            {
                if (callback != null) callback(ParseVerify(resp));
            });
        }

        /// <summary>渠道登录凭证换平台 token。</summary>
        public void ExchangeToken(LoginResult login, Action<LoginResult> callback)
        {
            if (login == null || !login.success)
            {
                if (callback != null) callback(login);
                return;
            }

            string url = baseUrl + loginPath;

            if (onPost == null)
            {
                Debug.LogWarning("[GGGatewayClient] 未注入 onPost，返回模拟平台 token。");
                login.platformUid = login.channelUid;
                login.platformToken = "platform_" + login.channelToken;
                if (callback != null) callback(login);
                return;
            }

            JsonObject body = new JsonObject();
            body["channel"] = login.channel;
            body["channelUid"] = login.channelUid;
            body["channelToken"] = login.channelToken;

            onPost(url, body.ToString(), delegate(string resp)
            {
                if (!string.IsNullOrEmpty(resp))
                {
                    try
                    {
                        JsonObject json = SimpleJson.SimpleJson.DeserializeObject<JsonObject>(resp);
                        if (json != null)
                        {
                            if (json.ContainsKey("platformUid")) login.platformUid = json["platformUid"].ToString();
                            if (json.ContainsKey("platformToken")) login.platformToken = json["platformToken"].ToString();
                        }
                    }
                    catch (Exception e)
                    {
                        Debug.LogError("[GGGatewayClient] ExchangeToken 解析失败: " + e);
                    }
                }
                if (callback != null) callback(login);
            });
        }

        private VerifyResponse SimulateVerify(PurchaseResult p)
        {
            VerifyResponse r = new VerifyResponse();
            r.orderId = p.developerOrderId;
            r.delivered = p.state == PurchaseState.Purchased;
            r.message = r.delivered ? "模拟校验通过" : "模拟校验未通过";
            return r;
        }

        private VerifyResponse ParseVerify(string json)
        {
            VerifyResponse r = new VerifyResponse();
            if (string.IsNullOrEmpty(json))
            {
                r.message = "空响应";
                return r;
            }
            try
            {
                JsonObject o = SimpleJson.SimpleJson.DeserializeObject<JsonObject>(json);
                if (o != null)
                {
                    if (o.ContainsKey("delivered")) bool.TryParse(o["delivered"].ToString(), out r.delivered);
                    if (o.ContainsKey("orderId")) r.orderId = o["orderId"].ToString();
                    if (o.ContainsKey("items")) r.items = o["items"].ToString();
                    if (o.ContainsKey("message")) r.message = o["message"].ToString();
                }
            }
            catch (Exception e)
            {
                Debug.LogError("[GGGatewayClient] ParseVerify 失败: " + e);
            }
            return r;
        }
    }
}
