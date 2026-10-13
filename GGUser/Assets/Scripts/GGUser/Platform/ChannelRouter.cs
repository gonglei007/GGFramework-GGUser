using System.Collections.Generic;
using UnityEngine;

namespace GGFramework.GGUser
{
    /// <summary>
    /// 支付渠道路由：按“平台硬约束 + 配置优先级”产出有序候选队列。
    /// 可用性探测（Connect / QueryProducts）与 fallback 由门面 GGPlatform 驱动。
    /// </summary>
    public interface IPayChannelRouter
    {
        List<IPayChannel> ResolveCandidates(PayContext ctx);
    }

    public class DefaultPayChannelRouter : IPayChannelRouter
    {
        private readonly ChannelRegistry registry;
        private readonly PlatformConfig config;

        public DefaultPayChannelRouter(ChannelRegistry registry, PlatformConfig config)
        {
            this.registry = registry;
            this.config = config;
        }

        public List<IPayChannel> ResolveCandidates(PayContext ctx)
        {
            List<IPayChannel> result = new List<IPayChannel>();
            if (ctx == null) ctx = new PayContext();

            // routeHint 强制指定渠道
            if (!string.IsNullOrEmpty(ctx.routeHint))
            {
                IPayChannel forced = registry.GetPay(ctx.routeHint);
                if (forced != null) result.Add(forced);
                else Debug.LogWarning("[GGPlatform] routeHint 指定渠道不可用: " + ctx.routeHint);
                return result;
            }

            List<string> ids = new List<string>();
            if (ctx.platform == "ios")
            {
                // iOS 虚拟商品强制走 AppStore（合规），其后追加配置兜底
                ids.Add("appleStore");
                ids.AddRange(config.GetPriority("ios"));
            }
            else if (ctx.platform == "android")
            {
                if (ctx.oversea)
                {
                    ids.Add("googlePlay");
                    ids.AddRange(config.GetPriority("android_oversea"));
                }
                else
                {
                    ids.AddRange(config.GetPriority("android_cn"));
                }
            }
            else
            {
                // 编辑器 / 其它：使用配置的支付渠道顺序
                ids.AddRange(config.payChannels);
            }

            // 去重并解析为已注册的渠道实例
            HashSet<string> seen = new HashSet<string>();
            foreach (string id in ids)
            {
                if (string.IsNullOrEmpty(id) || seen.Contains(id)) continue;
                seen.Add(id);
                IPayChannel ch = registry.GetPay(id);
                if (ch != null) result.Add(ch);
                else Debug.LogWarning("[GGPlatform] 支付渠道未注册: " + id);
            }
            return result;
        }
    }
}
