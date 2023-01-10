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
                    //sdk = new QuickSDKImpl();
                    break;
                case SDKTarget.ZhiShangSDK:
                    //sdk = new ZhiShangSDK();
                    break;
                case SDKTarget.NativeSDK:
                    //sdk = new NativeSDK();
                    break;
                default:
                    break;
            }
            return sdk;
        }
    }
}
