using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GGFramework.GGUser
{
    public class InternalSDK : ISDK
    {
        public InternalSDK()
        {
        }
        public void Init()
        {
            NativeLuaBridge.Instance.UnityCallNative("Init", null);
        }

        public void Login()
        {
            NativeLuaBridge.Instance.UnityCallNative("Login", null);
        }

        public void Logout()
        {
            NativeLuaBridge.Instance.UnityCallNative("Logout", null);
        }

        public void Quit()
        {
            NativeLuaBridge.Instance.UnityCallNative("SdkQuit", null);
        }

        public void Pay(Order order, UserSystem.UserData userData = null)
        {
            NativeLuaBridge.Instance.UnityCallNative("Pay", order.GetJsonString());
        }

        public void SubmitData(string jsonData)
        {
#if SIMULATION_MODE_SDK
#else
            NativeLuaBridge.Instance.UnityCallNative("SubmitData", jsonData);
#endif
        }
    }
}

