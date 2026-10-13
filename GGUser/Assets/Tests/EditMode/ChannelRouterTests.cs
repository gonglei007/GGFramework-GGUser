using System.Collections.Generic;
using NUnit.Framework;

namespace GGFramework.GGUser.Tests
{
    /// <summary>支付渠道路由：平台硬约束、优先级链、routeHint、去重与未注册跳过。</summary>
    public class ChannelRouterTests
    {
        private ChannelRegistry registry;
        private PlatformConfig config;
        private DefaultPayChannelRouter router;

        private void Setup(params string[] payChannelIds)
        {
            registry = new ChannelRegistry();
            config = new PlatformConfig();
            foreach (string id in payChannelIds)
            {
                string captured = id;
                registry.Register(captured, () => new FakePayChannel(captured));
                config.payChannels.Add(captured);
            }
            router = new DefaultPayChannelRouter(registry, config);
        }

        private static List<string> Ids(List<IPayChannel> channels)
        {
            List<string> ids = new List<string>();
            foreach (IPayChannel c in channels) ids.Add(c.ChannelId);
            return ids;
        }

        [Test]
        public void RouteHint_ForcesSingleChannel()
        {
            Setup("appleStore", "custom");
            PayContext ctx = new PayContext { platform = "editor", routeHint = "custom" };

            List<IPayChannel> result = router.ResolveCandidates(ctx);

            Assert.AreEqual(1, result.Count);
            Assert.AreEqual("custom", result[0].ChannelId);
        }

        [Test]
        public void RouteHint_UnregisteredChannel_ReturnsEmpty()
        {
            Setup("custom");
            PayContext ctx = new PayContext { platform = "editor", routeHint = "ghost" };

            List<IPayChannel> result = router.ResolveCandidates(ctx);

            Assert.AreEqual(0, result.Count);
        }

        [Test]
        public void IOS_PrependsAppleStoreBeforePriority()
        {
            Setup("appleStore", "custom");
            config.priority["ios"] = new List<string> { "custom" };
            PayContext ctx = new PayContext { platform = "ios" };

            List<string> ids = Ids(router.ResolveCandidates(ctx));

            CollectionAssert.AreEqual(new[] { "appleStore", "custom" }, ids);
        }

        [Test]
        public void AndroidOversea_PrependsGooglePlay()
        {
            Setup("googlePlay", "custom");
            config.priority["android_oversea"] = new List<string> { "custom" };
            PayContext ctx = new PayContext { platform = "android", oversea = true };

            List<string> ids = Ids(router.ResolveCandidates(ctx));

            CollectionAssert.AreEqual(new[] { "googlePlay", "custom" }, ids);
        }

        [Test]
        public void AndroidCN_UsesPriorityWithoutHardConstraint()
        {
            Setup("custom", "thirdparty");
            config.priority["android_cn"] = new List<string> { "custom", "thirdparty" };
            PayContext ctx = new PayContext { platform = "android", oversea = false };

            List<string> ids = Ids(router.ResolveCandidates(ctx));

            CollectionAssert.AreEqual(new[] { "custom", "thirdparty" }, ids);
        }

        [Test]
        public void Editor_UsesConfiguredPayChannels()
        {
            Setup("a", "b", "c");
            PayContext ctx = new PayContext { platform = "editor" };

            List<string> ids = Ids(router.ResolveCandidates(ctx));

            CollectionAssert.AreEqual(new[] { "a", "b", "c" }, ids);
        }

        [Test]
        public void Duplicates_AreRemoved()
        {
            Setup("appleStore", "custom");
            // 硬约束已加入 appleStore，优先级链再次包含它 → 应去重
            config.priority["ios"] = new List<string> { "appleStore", "custom" };
            PayContext ctx = new PayContext { platform = "ios" };

            List<string> ids = Ids(router.ResolveCandidates(ctx));

            CollectionAssert.AreEqual(new[] { "appleStore", "custom" }, ids);
        }

        [Test]
        public void UnregisteredChannels_AreSkipped()
        {
            Setup("a");
            config.payChannels.Add("missing");   // 未注册
            PayContext ctx = new PayContext { platform = "editor" };

            List<string> ids = Ids(router.ResolveCandidates(ctx));

            CollectionAssert.AreEqual(new[] { "a" }, ids);
        }

        [Test]
        public void NullContext_DoesNotThrow()
        {
            Setup("a");
            List<IPayChannel> result = router.ResolveCandidates(null);
            Assert.IsNotNull(result);
        }
    }
}
