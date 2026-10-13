using System;
using System.Collections;
using GGFramework.GGUser;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace GGFramework.GGUser.Tests
{
    /// <summary>
    /// UserSystem 遗留登录/支付链路的 PlayMode 测试。
    /// 通过公开的 SDK 回调入口（CallSDKLoginSuccess / SDKPay / TriggerEvent）驱动，
    /// 覆盖「SDK 回调 → 状态机 → 事件广播」核心机制；不依赖原生桥接。
    /// </summary>
    public class UserSystemPlayModeTests
    {
        [UnityTest]
        public IEnumerator Init_StoresChannel()
        {
            UserSystem us = UserSystem.Instance;
            us.Init("play_channel");
            Assert.AreEqual("play_channel", us.Channel);
            yield return null;
        }

        [UnityTest]
        public IEnumerator CallSDKLoginSuccess_SetsUserAndReachesLogined()
        {
            UserSystem us = UserSystem.Instance;
            us.CallSDKLoginSuccess(new User("u1", "t1", "a1", "play_channel"));

            // user 同步赋值；状态切换经协程延迟，等待若干帧
            Assert.IsNotNull(us.UserInfo);
            Assert.AreEqual("u1", us.UserInfo.uid);

            yield return new WaitForSeconds(0.3f);
            Assert.IsTrue(us.HasLogined(), "登录成功回调后应进入已登录状态");
        }

        [UnityTest]
        public IEnumerator SDKPay_InEditor_RaisesPaySuccessEvent()
        {
            UserSystem us = UserSystem.Instance;
            bool gotPaySuccess = false;
            Action<UserSystem.States, UserSystem.Events, object> handler =
                (state, e, param) => { if (e == UserSystem.Events.SDKPaySuccess) gotPaySuccess = true; };

            us.onEvent += handler;
            us.SDKPay(new Order());
            yield return null;
            us.onEvent -= handler;

            Assert.IsTrue(gotPaySuccess, "编辑器下模拟支付应广播 SDKPaySuccess");
        }

        [UnityTest]
        public IEnumerator CallSDKLoginFailed_RaisesLoginFailedEvent()
        {
            UserSystem us = UserSystem.Instance;
            bool gotFailed = false;
            Action<UserSystem.States, UserSystem.Events, object> handler =
                (state, e, param) => { if (e == UserSystem.Events.SDKLoginFailed) gotFailed = true; };

            us.onEvent += handler;
            us.CallSDKLoginFailed("boom");
            yield return null;
            us.onEvent -= handler;

            Assert.IsTrue(gotFailed, "登录失败应直接广播 SDKLoginFailed（不依赖状态跃迁）");
        }

        [UnityTest]
        public IEnumerator TriggerEvent_InvokesSubscriber()
        {
            UserSystem us = UserSystem.Instance;
            object received = null;
            Action<UserSystem.States, UserSystem.Events, object> handler =
                (state, e, param) => { if (e == UserSystem.Events.SDKInitSuccess) received = param; };

            us.onEvent += handler;
            us.TriggerEvent(UserSystem.States.None, UserSystem.Events.SDKInitSuccess, "payload");
            yield return null;
            us.onEvent -= handler;

            Assert.AreEqual("payload", received);
        }
    }
}
