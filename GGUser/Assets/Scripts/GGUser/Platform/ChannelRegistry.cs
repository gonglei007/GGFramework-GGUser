using System;
using System.Collections.Generic;

namespace GGFramework.GGUser
{
    /// <summary>
    /// 渠道注册中心：配置驱动、插件式注册、懒创建并缓存实例。
    /// 取代 SDKFactory 的硬编码 switch，支持多渠道路由与动态增删。
    /// </summary>
    public class ChannelRegistry
    {
        private readonly Dictionary<string, Func<IChannel>> creators = new Dictionary<string, Func<IChannel>>();
        private readonly Dictionary<string, IChannel> instances = new Dictionary<string, IChannel>();

        /// <summary>注册渠道创建器（插件式）。</summary>
        public void Register(string id, Func<IChannel> creator)
        {
            if (string.IsNullOrEmpty(id) || creator == null) return;
            creators[id] = creator;
        }

        /// <summary>按 id 获取渠道实例（懒创建 + 缓存 + 首次 Init）。</summary>
        public IChannel Get(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;

            IChannel cached;
            if (instances.TryGetValue(id, out cached)) return cached;

            Func<IChannel> creator;
            if (!creators.TryGetValue(id, out creator)) return null;

            IChannel channel = creator();
            if (channel != null)
            {
                channel.Init();
                instances[id] = channel;
            }
            return channel;
        }

        public ILoginChannel GetLogin(string id) { return Get(id) as ILoginChannel; }

        public IPayChannel GetPay(string id) { return Get(id) as IPayChannel; }

        public bool Contains(string id) { return !string.IsNullOrEmpty(id) && creators.ContainsKey(id); }
    }
}
