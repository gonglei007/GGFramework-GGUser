using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GGFramework.GGUser
{
    public class SDKFactory
    {
        public enum SDKTarget
        {
            QuickSDK,
            ZhiShangSDK,
            NativeSDK,
        };

        public static void Init()
        {
        }

        public static ISDK CreateSDK(SDKTarget target)
        {
            ISDK sdk = null;
            switch (target)
            {
                case SDKTarget.QuickSDK:
#if QUICK_SDK
                    sdk = new QuickSDKImpl();
#else
                    Debug.LogWarning("[SDKFactory] QuickSDK 未启用（需在 Player Settings 中定义 QUICK_SDK 宏）。");
#endif
                    break;
                case SDKTarget.ZhiShangSDK:
                    sdk = new ZhiShangSDK();
                    break;
                case SDKTarget.NativeSDK:
                    sdk = new NativeSDK();
                    break;
                default:
                    Debug.LogWarning("[SDKFactory] 不支持的 SDKTarget: " + target);
                    break;
            }
            return sdk;
        }
    }
}
