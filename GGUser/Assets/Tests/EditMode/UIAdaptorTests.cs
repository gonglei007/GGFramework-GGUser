using System;
using NUnit.Framework;

namespace GGFramework.GGUser.Tests
{
    /// <summary>
    /// UIAdaptor / LogAdaptor 依赖倒置契约测试。
    /// 重点覆盖 P0 修复：未注入实现时不应因 callback 为 null 抛 NullReferenceException。
    /// </summary>
    public class UIAdaptorTests
    {
        [Test]
        public void ShowDialog_NoOnDialog_NullCallback_DoesNotThrow()
        {
            UIAdaptor ui = new UIAdaptor();
            Assert.DoesNotThrow(() => ui.ShowDialog("title", "msg", true, null));
        }

        [Test]
        public void ShowDialog_NoOnDialog_WithCallback_InvokesWithTrue()
        {
            UIAdaptor ui = new UIAdaptor();
            bool? result = null;
            ui.ShowDialog("title", "msg", true, b => result = b);
            Assert.IsNotNull(result);
            Assert.IsTrue(result.Value);
        }

        [Test]
        public void ShowDialog_WithOnDialog_PassesLocalizedTexts()
        {
            UIAdaptor ui = new UIAdaptor();
            ui.onGetText = key => "loc_" + key;
            string gotTitle = null;
            string gotMessage = null;
            ui.onDialog = (title, message, confirm, cb) =>
            {
                gotTitle = title;
                gotMessage = message;
                if (cb != null) cb(true);
            };

            bool callbackFired = false;
            ui.ShowDialog("t", "m", true, b => callbackFired = true);

            Assert.AreEqual("loc_t", gotTitle);
            Assert.AreEqual("loc_m", gotMessage);
            Assert.IsTrue(callbackFired);
        }

        [Test]
        public void ShowWaiting_NoOnWaiting_DoesNotThrow()
        {
            UIAdaptor ui = new UIAdaptor();
            Assert.DoesNotThrow(() => ui.ShowWaiting(true));
        }

        [Test]
        public void LogAdaptor_NoHooks_DoesNotThrow()
        {
            LogAdaptor log = new LogAdaptor();
            Assert.DoesNotThrow(() => log.PostError("id", "info"));
        }

        [Test]
        public void LogAdaptor_PostError_InvokesHook()
        {
            LogAdaptor log = new LogAdaptor();
            string capturedId = null;
            string capturedInfo = null;
            log.onPostError = (id, info) => { capturedId = id; capturedInfo = info; };

            log.PostError("pay_verify_failed", "order_1");

            Assert.AreEqual("pay_verify_failed", capturedId);
            Assert.AreEqual("order_1", capturedInfo);
        }
    }
}
