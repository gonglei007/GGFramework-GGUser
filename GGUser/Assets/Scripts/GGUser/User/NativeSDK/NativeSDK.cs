#if UNITY_EDITOR
#define SIMULATION_MODE_SDK
#endif
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using SimpleJson;

namespace GGFramework.GGUser
{
    public class NativeSDK : ISDK
    {
        public NativeSDK()
        {
            Debug.Log("NativeSDK-事件监听");
            NativeLuaBridge.onNativeCallUnity += HandleSDKCallback;
        }

        public void HandleSDKCallback(string methodName, string jsonString)
        {
            Debug.LogFormat("HandleSDKCallback => method:{0}  param:{1}", methodName, jsonString);
            //UISystem.Instance.PopDialog(methodName, jsonString);
            switch (methodName)
            {
                case "InitSuccess":
                    UserSystem.Instance.CallSDKInitSuccess("");
                    break;
                case "InitFailed":
                    UserSystem.Instance.CallSDKInitFailed("");
                    break;
                case "LoginSuccess":
                    //GL - 解析参数，填入uid，传给后端。
                    try
                    {
                        JsonObject jsonObject = SimpleJson.SimpleJson.DeserializeObject<JsonObject>(jsonString);
                        User user = new User();
                        user.uid = jsonObject["accountid"].ToString();
                        user.token = jsonObject["token"].ToString();
                        user.channel = UserSystem.Instance.Channel;
                        UserSystem.Instance.CallSDKLoginSuccess(user);
                    }
                    catch (Exception e)
                    {
                        Debug.LogError("[NativeSDK] LoginSuccess 解析异常: " + e);
                        UserSystem.Instance.UIAdaptor.ShowDialog("login_parse_error", "login_parse_error_desc", true, (ret) => {
                        });
                        throw;
                    }
                    break;
                case "LoginFailed":
                    UserSystem.Instance.CallSDKLoginFailed("");
                    break;
                case "PaySuccess":
                    UserSystem.Instance.CallSDKPaySuccess("");
                    break;
                case "PayFailed":
                    try
                    {
                        JsonObject jsonObject = SimpleJson.SimpleJson.DeserializeObject<JsonObject>(jsonString);
                        UserSystem.Instance.CallSDKPayFailed(jsonObject["customorgerid"].ToString());
                    }
                    catch (Exception e)
                    {
                        Debug.LogError("[NativeSDK] PayFailed 解析异常: " + e);
                        UserSystem.Instance.UIAdaptor.ShowDialog("pay_parse_error", "pay_parse_error_desc", true, (ret) => {
                        });
                        throw;
                    }
                    break;
                case "PayCancel":
                    {
                        JsonObject jsonObject = SimpleJson.SimpleJson.DeserializeObject<JsonObject>(jsonString);
                        UserSystem.Instance.CallSDKPayCancel(jsonObject["customorgerid"].ToString());
                    }

                    break;
                case "LogoutSuccess":
                    {
                        Application.Quit();
                    }
                    break;
                case "QuitGame":
                    {
                        UserSystem.Instance.UIAdaptor.ShowDialog("quit_dlg_title", "quit_dlg_desc", true, (ret) => {
                            if (ret)
                            {
                                Application.Quit();
                            }
                        });
                    }
                    break;
                default:
                    break;
            }
        }

        public void Init()
        {
#if SIMULATION_MODE_SDK
            HandleSDKCallback("InitSuccess", "");
#else
        NativeLuaBridge.Instance.UnityCallNative("Init", null);
#endif
        }

        public void Login()
        {
#if SIMULATION_MODE_SDK
            // 编辑器模拟：构造包含必要字段的登录回包，避免 HandleSDKCallback 中因缺少键而抛异常。
            JsonObject msg = new JsonObject();
            msg["accountid"] = "sim_" + UnityEngine.Random.Range(100000, 999999);
            msg["token"] = "sim_token";
            HandleSDKCallback("LoginSuccess", msg.ToString());
#else
        NativeLuaBridge.Instance.UnityCallNative("Login", null);
#endif
        }

        public void Logout()
        {
#if SIMULATION_MODE_SDK
#else
        NativeLuaBridge.Instance.UnityCallNative("Logout", null);
#endif
        }

        public void Quit()
        {
#if SIMULATION_MODE_SDK
#else
        NativeLuaBridge.Instance.UnityCallNative("SdkQuit", null);
#endif
        }

        public void Pay(Order order, UserSystem.UserData userData = null)
        {
#if SIMULATION_MODE_SDK
            HandleSDKCallback("PaySuccess", "");
#else
        if(userData == null){
            NativeLuaBridge.Instance.UnityCallNative("Pay", order.GetJsonString());
        }
        else{
            JsonObject orderJsonObject = SimpleJson.SimpleJson.DeserializeObject<JsonObject>(order.GetJsonString());
            JsonObject userJsonObject = SimpleJson.SimpleJson.DeserializeObject<JsonObject>(userData.GetJsonString());
            foreach (string jKey in userJsonObject.Keys)
            {
                if (!orderJsonObject.ContainsKey(jKey)) {
                    orderJsonObject[jKey] = userJsonObject[jKey];
                }
            }
            
            NativeLuaBridge.Instance.UnityCallNative("Pay", orderJsonObject.ToString());
        }
#endif
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
