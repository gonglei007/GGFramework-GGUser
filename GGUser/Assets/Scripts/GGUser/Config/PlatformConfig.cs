using System.Collections.Generic;
using SimpleJson;
using UnityEngine;

namespace GGFramework.GGUser
{
    /// <summary>
    /// 平台配置：渠道开关、优先级、商品映射、网关地址。
    /// 可由本地文件或服务端远程下发（JSON），支持不发版调整灰度/开关/优先级。
    /// </summary>
    public class PlatformConfig
    {
        public List<string> payChannels = new List<string>();
        public List<string> loginChannels = new List<string>();

        // 路由优先级链：key 如 "ios" / "android_oversea" / "android_cn"
        public Dictionary<string, List<string>> priority = new Dictionary<string, List<string>>();

        // 商品映射：unifiedProductId -> (channelId -> channelProductId)
        public Dictionary<string, Dictionary<string, string>> productMap = new Dictionary<string, Dictionary<string, string>>();

        public string gatewayBaseUrl = "";
        public string verifyPath = "/ggpay/verify";
        public string loginPath = "/ggaccount/token";
        public bool androidOversea = true;

        public List<string> GetPriority(string key)
        {
            List<string> list;
            if (priority != null && priority.TryGetValue(key, out list) && list != null)
            {
                return list;
            }
            return new List<string>();
        }

        /// <summary>取某统一商品在指定渠道下的真实商品ID；未配置则回退为统一ID。</summary>
        public string GetChannelProductId(string unifiedProductId, string channelId)
        {
            Dictionary<string, string> map;
            if (productMap != null && productMap.TryGetValue(unifiedProductId, out map) && map != null)
            {
                string pid;
                if (map.TryGetValue(channelId, out pid))
                {
                    return pid;
                }
            }
            return unifiedProductId;
        }

        public static PlatformConfig Default()
        {
            PlatformConfig c = new PlatformConfig();
            c.payChannels.Add("simulation");
            c.loginChannels.Add("simulation");
            return c;
        }

        public static PlatformConfig FromJson(string json)
        {
            PlatformConfig c = Default();
            if (string.IsNullOrEmpty(json))
            {
                return c;
            }
            try
            {
                JsonObject root = SimpleJson.SimpleJson.DeserializeObject<JsonObject>(json);
                if (root == null)
                {
                    return c;
                }

                List<string> pay = ReadStringList(root, "payChannels");
                if (pay != null) c.payChannels = pay;
                List<string> login = ReadStringList(root, "loginChannels");
                if (login != null) c.loginChannels = login;

                if (root.ContainsKey("androidOversea"))
                {
                    bool.TryParse(root["androidOversea"].ToString(), out c.androidOversea);
                }

                if (root.ContainsKey("gateway"))
                {
                    JsonObject gw = root["gateway"] as JsonObject;
                    if (gw != null)
                    {
                        if (gw.ContainsKey("baseUrl")) c.gatewayBaseUrl = gw["baseUrl"].ToString();
                        if (gw.ContainsKey("verifyPath")) c.verifyPath = gw["verifyPath"].ToString();
                        if (gw.ContainsKey("loginPath")) c.loginPath = gw["loginPath"].ToString();
                    }
                }

                if (root.ContainsKey("priority"))
                {
                    JsonObject p = root["priority"] as JsonObject;
                    if (p != null)
                    {
                        c.priority = new Dictionary<string, List<string>>();
                        foreach (KeyValuePair<string, object> kv in p)
                        {
                            c.priority[kv.Key] = ReadArray(kv.Value as JsonArray);
                        }
                    }
                }

                if (root.ContainsKey("productMap"))
                {
                    JsonObject pm = root["productMap"] as JsonObject;
                    if (pm != null)
                    {
                        c.productMap = new Dictionary<string, Dictionary<string, string>>();
                        foreach (KeyValuePair<string, object> kv in pm)
                        {
                            JsonObject inner = kv.Value as JsonObject;
                            Dictionary<string, string> map = new Dictionary<string, string>();
                            if (inner != null)
                            {
                                foreach (KeyValuePair<string, object> ikv in inner)
                                {
                                    if (ikv.Value == null) continue;
                                    if (ikv.Key == "amount") continue; // 忽略非渠道字段
                                    map[ikv.Key] = ikv.Value.ToString();
                                }
                            }
                            c.productMap[kv.Key] = map;
                        }
                    }
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError("[PlatformConfig] 解析失败: " + e);
            }
            return c;
        }

        private static List<string> ReadStringList(JsonObject root, string key)
        {
            if (!root.ContainsKey(key)) return null;
            return ReadArray(root[key] as JsonArray);
        }

        private static List<string> ReadArray(JsonArray arr)
        {
            List<string> list = new List<string>();
            if (arr != null)
            {
                foreach (object o in arr)
                {
                    if (o != null) list.Add(o.ToString());
                }
            }
            return list;
        }
    }
}
