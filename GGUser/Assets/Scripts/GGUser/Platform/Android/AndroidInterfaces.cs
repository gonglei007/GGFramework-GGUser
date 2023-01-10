using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

public class AndroidInterfaces
{

#if (UNITY_ANDROID && !UNITY_EDITOR)
        //private static string androidPackageName = "com.go.SSDuel";
        //private static AndroidJavaClass activity = new AndroidJavaClass(string.Format("{0}.UnityPlayerActivity",androidPackageName));
        private static AndroidJavaClass activity = null;
#endif

    #region Game API


    public static void CallSdkLogin ()
	{
#if UNITY_ANDROID && !UNITY_EDITOR
		if (Application.platform == RuntimePlatform.Android)
		{
			activity.CallStatic("SdkLogin");
		}
#endif
	}

	public static void CallSdkQuit ()
	{
#if UNITY_ANDROID && !UNITY_EDITOR
		if (Application.platform == RuntimePlatform.Android)
		{
			activity.CallStatic("SdkQuit");
		}
#endif
	}

    /// <summary>
    /// 获取当前包名
    /// </summary>
    public static string GetCurrentPackageName()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        if (Application.platform == RuntimePlatform.Android)
		{
			return activity.CallStatic<string>("GetCurrentPackageName");
		}
#endif
        return "";
    }

	public static void CallReStartGame ()
	{
#if UNITY_ANDROID && !UNITY_EDITOR
		if (Application.platform == RuntimePlatform.Android)
		{
			activity.CallStatic("CallRestartGame");
		}
#endif
    }

    public static void CallSdkPay(string serverId, string productId, string orderGo, string uid, string ext)
	{
#if UNITY_ANDROID && !UNITY_EDITOR
		if (Application.platform == RuntimePlatform.Android)
		{
            activity.CallStatic("SdkPay", serverId, productId, orderGo, uid, ext);
		}
#endif
    }

    public static void CallSdkPay(string uid, string goodsName, string price, string orderGo, string payNotifyURL, string productdataID)
    {
#if UNITY_ANDROID && !UNITY_EDITOR
		if (Application.platform == RuntimePlatform.Android)
		{
			activity.CallStatic("SdkPay", orderGo, payNotifyURL, price, goodsName, "-",productdataID);
		}
#endif
    }

	public static void CallSetEnteredGame()
	{
#if UNITY_ANDROID && !UNITY_EDITOR
		if (Application.platform == RuntimePlatform.Android)
		{
			activity.CallStatic("SetEnteredGame");
		}
#endif
    }

    public static void CallSubmitUserInfo (string userInfo)
	{
#if UNITY_ANDROID && !UNITY_EDITOR
		if (Application.platform == RuntimePlatform.Android)
		{
			activity.CallStatic("SdkSubmitUserInfo", userInfo);
		}
#endif
	}

    public static string CallGetChannel ()
	{
#if UNITY_ANDROID && !UNITY_EDITOR
		//if (Application.platform == RuntimePlatform.Android)
		//{
		//	return activity.CallStatic<string>("GetChannel");
		//}
#endif
        return "dev";
	}

	public static string CallGetAppVersion ()
	{
#if UNITY_ANDROID && !UNITY_EDITOR
		if (Application.platform == RuntimePlatform.Android)
		{
			return activity.CallStatic<string>("GetAppVersion");
		}
#endif
        return "0.0.0";
	}

    public static string GetAndroidID()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
	    if (Application.platform == RuntimePlatform.Android)
		{
			return activity.CallStatic<string>("GetAndroidID");
		}
#endif
        return "";
    }

    public static bool CallGetIsShowLogo()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
		if (Application.platform == RuntimePlatform.Android)
		{
			return activity.CallStatic<bool>("getIsShowLogo");
		}
#endif
        return true;
    }
    

	public static void CallCheckHasExitBox ()
	{
#if UNITY_ANDROID && !UNITY_EDITOR
		if (Application.platform == RuntimePlatform.Android)
		{
			activity.CallStatic("CheckHasExitBox");
		}
#endif
	}
    
	public static void CallHideSplashActivity()
	{
#if UNITY_ANDROID && !UNITY_EDITOR
		if (Application.platform == RuntimePlatform.Android)
        {
            activity.CallStatic("hideSplashActivity");
        }
#endif
	}

	public static void CallSetSoundSystemInited()
	{
#if UNITY_ANDROID && !UNITY_EDITOR
		if (Application.platform == RuntimePlatform.Android)
		{
			activity.CallStatic("SetSoundSystemInited");
		}
#endif
    }

    #endregion
}
