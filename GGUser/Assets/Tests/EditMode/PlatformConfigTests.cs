using System.Collections.Generic;
using NUnit.Framework;

namespace GGFramework.GGUser.Tests
{
    /// <summary>PlatformConfig 的默认值、JSON 解析与映射查询测试。</summary>
    public class PlatformConfigTests
    {
        [Test]
        public void Default_ContainsSimulationChannel()
        {
            PlatformConfig cfg = PlatformConfig.Default();
            CollectionAssert.Contains(cfg.payChannels, "simulation");
            CollectionAssert.Contains(cfg.loginChannels, "simulation");
            Assert.IsTrue(cfg.androidOversea);
        }

        [Test]
        public void FromJson_NullOrEmpty_FallsBackToDefault()
        {
            PlatformConfig cfg = PlatformConfig.FromJson(null);
            CollectionAssert.Contains(cfg.payChannels, "simulation");

            PlatformConfig cfg2 = PlatformConfig.FromJson("");
            CollectionAssert.Contains(cfg2.payChannels, "simulation");
        }

        [Test]
        public void FromJson_ParsesChannelsGatewayPriorityAndProductMap()
        {
            string json = "{"
                + "\"payChannels\":[\"appleStore\",\"googlePlay\",\"custom\"],"
                + "\"loginChannels\":[\"custom\",\"guest\"],"
                + "\"androidOversea\":false,"
                + "\"priority\":{\"ios\":[\"appleStore\"],\"android_cn\":[\"custom\",\"thirdparty\"]},"
                + "\"productMap\":{\"gem_60\":{\"appleStore\":\"com.x.gem60\",\"googlePlay\":\"gp_60\",\"amount\":60}},"
                + "\"gateway\":{\"baseUrl\":\"https://pay.example.com\",\"verifyPath\":\"/v1/verify\",\"loginPath\":\"/v1/token\"}"
                + "}";

            PlatformConfig cfg = PlatformConfig.FromJson(json);

            CollectionAssert.AreEqual(new[] { "appleStore", "googlePlay", "custom" }, cfg.payChannels);
            CollectionAssert.AreEqual(new[] { "custom", "guest" }, cfg.loginChannels);
            Assert.IsFalse(cfg.androidOversea);
            Assert.AreEqual("https://pay.example.com", cfg.gatewayBaseUrl);
            Assert.AreEqual("/v1/verify", cfg.verifyPath);
            Assert.AreEqual("/v1/token", cfg.loginPath);

            CollectionAssert.AreEqual(new[] { "custom", "thirdparty" }, cfg.GetPriority("android_cn"));
        }

        [Test]
        public void GetPriority_UnknownKey_ReturnsEmptyNotNull()
        {
            PlatformConfig cfg = PlatformConfig.Default();
            List<string> list = cfg.GetPriority("does_not_exist");
            Assert.IsNotNull(list);
            Assert.AreEqual(0, list.Count);
        }

        [Test]
        public void GetChannelProductId_Mapped_ReturnsChannelId()
        {
            PlatformConfig cfg = PlatformConfig.Default();
            cfg.productMap["gem_60"] = new Dictionary<string, string> { { "appleStore", "com.x.gem60" } };

            Assert.AreEqual("com.x.gem60", cfg.GetChannelProductId("gem_60", "appleStore"));
        }

        [Test]
        public void GetChannelProductId_Unmapped_FallsBackToUnifiedId()
        {
            PlatformConfig cfg = PlatformConfig.Default();
            cfg.productMap["gem_60"] = new Dictionary<string, string> { { "appleStore", "com.x.gem60" } };

            // 渠道未配置映射 → 回退统一ID
            Assert.AreEqual("gem_60", cfg.GetChannelProductId("gem_60", "custom"));
            // 商品完全未配置 → 回退统一ID
            Assert.AreEqual("sword", cfg.GetChannelProductId("sword", "appleStore"));
        }
    }
}
