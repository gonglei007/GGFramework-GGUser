using System;
using System.Collections.Generic;

namespace GGFramework.GGUser
{
    /// <summary>
    /// 所有渠道的公共契约。渠道按“能力”实现下面的子接口，
    /// 一个渠道可实现其中任意组合（如 Apple/Google 只有支付、自定义渠道两者都有）。
    /// </summary>
    public interface IChannel
    {
        string ChannelId { get; }
        void Init();
    }

    /// <summary>
    /// 登录能力。渠道登录返回渠道凭证，再由网关换取平台 token。
    /// </summary>
    public interface ILoginChannel : IChannel
    {
        void Login(Action<LoginResult> callback);
        void Logout();
    }

    /// <summary>
    /// 支付能力。对接 AppStore / GooglePlay / 自定义支付。
    /// 铁律：只有服务端确认发货后，才调用 <see cref="FinishTransaction"/>。
    /// </summary>
    public interface IPayChannel : IChannel
    {
        /// <summary>连接商店服务（Google Billing 需先连上）。</summary>
        void Connect(Action<bool> callback);

        /// <summary>按统一商品ID查询商店商品详情（含本地化价格）。</summary>
        void QueryProducts(IList<string> unifiedProductIds, Action<List<StoreProduct>> callback);

        /// <summary>发起购买，异步回调携带票据的购买结果。</summary>
        void Purchase(PayRequest request, Action<PurchaseResult> callback);

        /// <summary>恢复购买（iOS 必需 / Android queryPastPurchases）。</summary>
        void Restore(Action<List<PurchaseResult>> callback);

        /// <summary>发货确认后结束事务（iOS finishTransaction / Android consume）。</summary>
        void FinishTransaction(string transactionId);

        /// <summary>启动时查询未完成事务，用于掉单补单。</summary>
        void QueryUnfinished(Action<List<PurchaseResult>> callback);
    }
}
