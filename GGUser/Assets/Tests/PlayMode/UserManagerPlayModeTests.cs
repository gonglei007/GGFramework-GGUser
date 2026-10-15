using System;
using System.Collections;
using GGFramework.GGUser;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace GGFramework.GGUser.Tests
{
    /// <summary>
    /// UserManager 业务门面的 PlayMode 测试：验证其对 UserSystem 事件的订阅、
    /// 支付委托，以及收到登录成功事件后自身状态机推进到「已登录」。
    /// </summary>
    public class UserManagerPlayModeTests
    {
        [UnityTest]
        public IEnumerator Init_DoesNotThrow()
        {
            Assert.DoesNotThrow(() => UserManager.Instance.Init("um_channel"));
            yield return null;
        }

        [UnityTest]
        public IEnumerator Pay_DelegatesToUserSystem_RaisesPaySuccess()
        {
            UserManager um = UserManager.Instance;
            um.Init("um_channel");
            UserSystem us = UserSystem.Instance;

            bool gotPaySuccess = false;
            Action<UserSystem.States, UserSystem.Events, object> handler =
                (state, e, param) => { if (e == UserSystem.Events.SDKPaySuccess) gotPaySuccess = true; };

            us.onEvent += handler;
            um.Pay(new Order());
            yield return null;
            us.onEvent -= handler;

            Assert.IsTrue(gotPaySuccess, "UserManager.Pay 应委托到 UserSystem 并广播支付成功");
        }

        [UnityTest]
        public IEnumerator RepeatedInit_DoesNotDoubleSubscribe()
        {
            // P2 回归：多次调用 UserManager.Init 不应叠加 UserSystem.onEvent 订阅，
            // 否则一次 SDKLoginSuccess 事件会引发多次 fsm.ChangeState(States.Init)。
            UserManager um = UserManager.Instance;
            UserSystem us = UserSystem.Instance;

            // 先清空历史订阅（前序测试可能遗留），确保本用例从净状态开始。
            us.onEvent = null;

            um.Init("um_channel_a");
            int afterFirst = us.onEvent != null ? us.onEvent.GetInvocationList().Length : 0;
            um.Init("um_channel_b");
            um.Init("um_channel_c");
            int afterThird = us.onEvent != null ? us.onEvent.GetInvocationList().Length : 0;

            Assert.AreEqual(1, afterFirst, "首次 Init 应注册一个订阅");
            Assert.AreEqual(1, afterThird, "重复 Init 不应叠加订阅（P2 修复前会为 3）");

            // 验证事件仍能正常驱动状态机推进。
            us.TriggerEvent(UserSystem.States.SDKLogining, UserSystem.Events.SDKLoginSuccess, "u_probe");
            yield return null;
            Assert.IsTrue(um.HasLogined(), "单次订阅仍应能将 UserManager 推进到已登录");
        }

        [UnityTest]
        public IEnumerator AdvancesToLogined_OnSDKLoginSuccessEvent()
        {
            UserManager um = UserManager.Instance;
            um.Init("um_channel");
            UserSystem us = UserSystem.Instance;

            // 直接广播登录成功事件，触发 UserManager 内部状态机 Init→Authing→Logining→Logined
            us.TriggerEvent(UserSystem.States.SDKLogining, UserSystem.Events.SDKLoginSuccess, "u2");
            yield return null;

            Assert.IsTrue(um.HasLogined(), "收到 SDKLoginSuccess 后应推进到已登录（AutoEnterGame）");
        }
    }
}
