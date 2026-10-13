using NUnit.Framework;

namespace GGFramework.GGUser.Tests
{
    /// <summary>渠道注册中心：懒创建、缓存、能力探测测试。</summary>
    public class ChannelRegistryTests
    {
        [Test]
        public void Get_CreatesLazilyAndCachesInstance()
        {
            ChannelRegistry reg = new ChannelRegistry();
            FakePlainChannel created = null;
            reg.Register("plain", () => { created = new FakePlainChannel(); return created; });

            IChannel c1 = reg.Get("plain");
            Assert.IsNotNull(c1);
            Assert.IsTrue(created.initCalled, "首次获取应调用 Init");

            IChannel c2 = reg.Get("plain");
            Assert.AreSame(c1, c2, "第二次获取应返回缓存实例，不再重新创建");
        }

        [Test]
        public void Get_UnknownId_ReturnsNull()
        {
            ChannelRegistry reg = new ChannelRegistry();
            Assert.IsNull(reg.Get("nope"));
            Assert.IsNull(reg.Get(null));
            Assert.IsNull(reg.Get(""));
        }

        [Test]
        public void Register_NullArgs_Ignored()
        {
            ChannelRegistry reg = new ChannelRegistry();
            reg.Register(null, () => new FakePlainChannel());
            reg.Register("x", null);
            Assert.IsFalse(reg.Contains("x"));
        }

        [Test]
        public void CapabilityCast_ReturnsNullWhenNotImplemented()
        {
            ChannelRegistry reg = new ChannelRegistry();
            reg.Register("pay", () => new FakePayChannel("pay"));
            reg.Register("login", () => new FakeLoginChannel("login"));
            reg.Register("plain", () => new FakePlainChannel());

            // 支付渠道不具备登录能力
            Assert.IsNotNull(reg.GetPay("pay"));
            Assert.IsNull(reg.GetLogin("pay"));

            // 登录渠道不具备支付能力
            Assert.IsNotNull(reg.GetLogin("login"));
            Assert.IsNull(reg.GetPay("login"));

            // 纯渠道两者都不具备
            Assert.IsNull(reg.GetPay("plain"));
            Assert.IsNull(reg.GetLogin("plain"));
        }

        [Test]
        public void Contains_ReflectsRegistration()
        {
            ChannelRegistry reg = new ChannelRegistry();
            reg.Register("pay", () => new FakePayChannel("pay"));
            Assert.IsTrue(reg.Contains("pay"));
            Assert.IsFalse(reg.Contains("other"));
            Assert.IsFalse(reg.Contains(null));
        }
    }
}
