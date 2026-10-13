using System;
using System.Collections.Generic;
using UnityEngine;

namespace GGFramework.GGUser
{
    /// <summary>
    /// 聚合平台门面：业务唯一入口。
    /// 负责渠道路由、支付全链路（购买 → 网关校验 → 发货 → finish）、登录换 token、掉单补单。
    /// 与现有 UserSystem/UserManager 平行，不破坏既有登录/渠道支付逻辑。
    /// </summary>
    public class GGPlatform
    {
        private static GGPlatform _instance;
        public static GGPlatform Instance
        {
            get
            {
                if (_instance == null) _instance = new GGPlatform();
                return _instance;
            }
        }

        public ChannelRegistry Registry { get; private set; }
        public PlatformConfig Config { get; private set; }
        public GGGatewayClient Gateway { get; private set; }
        public IPayChannelRouter Router { get; private set; }
        public UIAdaptor UIAdaptor { get; private set; }
        public LogAdaptor LogAdaptor { get; private set; }

        private bool inited = false;

        private GGPlatform()
        {
            UIAdaptor = new UIAdaptor();
            LogAdaptor = new LogAdaptor();
        }

        /// <summary>初始化平台。传入 null 使用默认配置（仅模拟渠道）。</summary>
        public void Init(PlatformConfig config = null)
        {
            Config = config != null ? config : PlatformConfig.Default();
            Registry = new ChannelRegistry();
            Gateway = new GGGatewayClient();
            Gateway.baseUrl = Config.gatewayBaseUrl;
            Gateway.verifyPath = Config.verifyPath;
            Gateway.loginPath = Config.loginPath;
            Router = new DefaultPayChannelRouter(Registry, Config);

            // 默认注册模拟渠道（编辑器可跑通全链路）。真实渠道由宿主 RegisterChannel 注入。
            Registry.Register("simulation", delegate { return new SimulationChannel(); });

            inited = true;
            Debug.Log("[GGPlatform] 初始化完成。");
        }

        private void EnsureInit()
        {
            if (!inited) Init(null);
        }

        /// <summary>插件式注册渠道（AppleStore / GooglePlay / Custom / Legacy 适配器）。</summary>
        public void RegisterChannel(string id, Func<IChannel> creator)
        {
            EnsureInit();
            Registry.Register(id, creator);
        }

        public static string CurrentPlatform()
        {
#if UNITY_IOS && !UNITY_EDITOR
            return "ios";
#elif UNITY_ANDROID && !UNITY_EDITOR
            return "android";
#else
            return "editor";
#endif
        }

        /// <summary>按当前运行平台构建默认支付上下文。</summary>
        public PayContext CreatePayContext(string unifiedProductId, PayProductType type = PayProductType.Consumable, string routeHint = null)
        {
            PayContext ctx = new PayContext();
            ctx.unifiedProductId = unifiedProductId;
            ctx.productType = type;
            ctx.routeHint = routeHint;
            ctx.platform = CurrentPlatform();
            ctx.oversea = Config != null ? Config.androidOversea : true;
            return ctx;
        }

        // ---------------- 登录 ----------------
        public void Login(Action<LoginResult> callback)
        {
            Login(null, callback);
        }

        public void Login(string channelId, Action<LoginResult> callback)
        {
            EnsureInit();
            ILoginChannel ch = null;
            if (!string.IsNullOrEmpty(channelId))
            {
                ch = Registry.GetLogin(channelId);
            }
            else
            {
                foreach (string id in Config.loginChannels)
                {
                    ch = Registry.GetLogin(id);
                    if (ch != null) break;
                }
            }

            if (ch == null)
            {
                if (callback != null) callback(new LoginResult { success = false, message = "无可用登录渠道" });
                return;
            }

            ch.Login(delegate(LoginResult result)
            {
                // 渠道登录成功后送网关换平台 token
                Gateway.ExchangeToken(result, callback);
            });
        }

        public void Logout(string channelId)
        {
            EnsureInit();
            ILoginChannel ch = Registry.GetLogin(channelId);
            if (ch != null) ch.Logout();
        }

        // ---------------- 支付 ----------------
        public void Pay(PayRequest request, PayContext ctx, Action<PayOutcome> callback)
        {
            EnsureInit();
            if (request == null)
            {
                if (callback != null) callback(new PayOutcome { success = false, message = "空的支付请求" });
                return;
            }
            if (ctx == null) ctx = CreatePayContext(request.unifiedProductId, PayProductType.Consumable, request.routeHint);

            List<IPayChannel> candidates = Router.ResolveCandidates(ctx);
            if (candidates.Count == 0)
            {
                NotifyUnavailable();
                if (callback != null) callback(new PayOutcome { success = false, message = "无可用支付渠道" });
                return;
            }
            TryCandidate(candidates, 0, request, ctx, callback);
        }

        // 候选渠道可用性探测（Connect + QueryProducts）：不可用则回退到下一个候选。
        private void TryCandidate(List<IPayChannel> candidates, int index, PayRequest request, PayContext ctx, Action<PayOutcome> callback)
        {
            if (index >= candidates.Count)
            {
                NotifyUnavailable();
                if (callback != null) callback(new PayOutcome { success = false, message = "所有支付渠道均不可用" });
                return;
            }

            IPayChannel channel = candidates[index];
            channel.Connect(delegate(bool ok)
            {
                if (!ok)
                {
                    Debug.LogWarning("[GGPlatform] 渠道连接失败，回退: " + channel.ChannelId);
                    TryCandidate(candidates, index + 1, request, ctx, callback);
                    return;
                }

                List<string> ids = new List<string>();
                ids.Add(request.unifiedProductId);
                channel.QueryProducts(ids, delegate(List<StoreProduct> products)
                {
                    if (products == null || products.Count == 0)
                    {
                        Debug.LogWarning("[GGPlatform] 商品不可用，回退: " + channel.ChannelId + " / " + request.unifiedProductId);
                        TryCandidate(candidates, index + 1, request, ctx, callback);
                        return;
                    }
                    DoPurchase(channel, request, callback);
                });
            });
        }

        // 命中渠道后执行购买；购买失败/取消不再回退（用户已交互，避免重复扣款）。
        private void DoPurchase(IPayChannel channel, PayRequest request, Action<PayOutcome> callback)
        {
            channel.Purchase(request, delegate(PurchaseResult result)
            {
                if (result == null)
                {
                    if (callback != null) callback(new PayOutcome { success = false, message = "购买无返回" });
                    return;
                }
                result.channel = channel.ChannelId;

                if (result.state == PurchaseState.Pending)
                {
                    if (callback != null) callback(new PayOutcome { success = false, purchase = result, message = "支付挂起(Pending)" });
                    return;
                }
                if (result.state != PurchaseState.Purchased)
                {
                    channel.FinishTransaction(result.storeTransactionId);
                    if (callback != null) callback(new PayOutcome { success = false, purchase = result, message = result.message });
                    return;
                }
                VerifyAndDeliver(channel, result, callback);
            });
        }

        // 送网关校验；确认发货后才 FinishTransaction（铁律）。
        private void VerifyAndDeliver(IPayChannel channel, PurchaseResult result, Action<PayOutcome> callback)
        {
            Gateway.Verify(result, delegate(VerifyResponse verify)
            {
                if (verify != null && verify.delivered)
                {
                    channel.FinishTransaction(result.storeTransactionId);
                    if (callback != null) callback(new PayOutcome { success = true, purchase = result, verify = verify });
                }
                else
                {
                    // 不 finish，保留事务等待启动补单
                    LogAdaptor.PostError("pay_verify_failed", result.developerOrderId);
                    if (callback != null) callback(new PayOutcome { success = false, purchase = result, verify = verify, message = "票据校验未通过" });
                }
            });
        }

        private void NotifyUnavailable()
        {
            UIAdaptor.ShowDialog("pay_unavailable", "pay_unavailable_desc", true, delegate(bool b) { });
        }

        // ---------------- 补单 / 恢复 ----------------
        /// <summary>启动补单：拉取未完成事务，走同样的校验/发货/finish 流程。</summary>
        public void ProcessUnfinished(string channelId, Action<PayOutcome> callback)
        {
            EnsureInit();
            IPayChannel channel = Registry.GetPay(channelId);
            if (channel == null) return;

            channel.QueryUnfinished(delegate(List<PurchaseResult> list)
            {
                if (list == null) return;
                foreach (PurchaseResult r in list)
                {
                    if (r == null || r.state != PurchaseState.Purchased) continue;
                    r.channel = channel.ChannelId;
                    VerifyAndDeliver(channel, r, callback);
                }
            });
        }

        public void Restore(string channelId, Action<List<PurchaseResult>> callback)
        {
            EnsureInit();
            IPayChannel channel = Registry.GetPay(channelId);
            if (channel == null)
            {
                if (callback != null) callback(null);
                return;
            }
            channel.Restore(callback);
        }
    }
}
