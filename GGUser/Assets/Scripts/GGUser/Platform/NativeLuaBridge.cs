using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
//using LuaInterface;
using SimpleJson;
using System.Runtime.InteropServices;

/// <summary>
/// 桥接原生代码和Lua代码的接口。
/// </summary>
public class NativeLuaBridge : Singleton<NativeLuaBridge>
{
	//public static LuaFunction onNativeCallUnityX = null;
	public static Action<string, string> onNativeCallUnity = null;

#if UNITY_IOS && !UNITY_EDITOR
	[DllImport("__Internal")]
	private static extern void QKInit();
	[DllImport("__Internal")]
	private static extern void QKLogin();
	[DllImport("__Internal")]
	private static extern void QKLogout();
	[DllImport("__Internal")]
	private static extern void QKCenter();
	[DllImport("__Internal")]
	private static extern void QKUserID();
	[DllImport("__Internal")]
	private static extern void QKUserToken();
	[DllImport("__Internal")]
	private static extern void QKUserName();
	[DllImport("__Internal")]
	private static extern void QKCongzi(string productId,string productName,float amount,string orderNo,string callBackUrl,string extrasParams);
	[DllImport("__Internal")]
	private static extern void QKUpdateRole(string roleId, string role_name, string sv_name, string role_level, string vipLevel);
#endif

	void Awake()
	{
        DontDestroyOnLoad(gameObject);
		Debug.Log("=========== NativeLuaBridge is working! ===========");
		UnityCallNative("Test", "Hello-->");

	}

	public void Init()
    {
    }

    /// <summary>
    /// Unity(和Lua)代码调用原生代码的接口。
    /// </summary>
    /// <param name="methodName"></param>
    /// <param name="jsonString"></param>
    public string UnityCallNative(string methodName, string jsonString)
    {
        jsonString = jsonString == null ? "" : jsonString;

        Debug.LogFormat("UnityCallNative => method:{0}  param:{1}", methodName, jsonString);
#if UNITY_ANDROID && !UNITY_EDITOR
		if (Application.platform == RuntimePlatform.Android)
		{
	        AndroidJavaClass androidInterface = new AndroidJavaClass(Global.AndroidInterfaceName);
		    if (androidInterface == null) {
			    throw new Exception("没有找到AndroidJavaClass！-" + Global.AndroidInterfaceName);
            }
            return androidInterface.CallStatic<string>("CallInterface", methodName, jsonString);
		}
#endif
#if UNITY_IOS && !UNITY_EDITOR
		if (Application.platform == RuntimePlatform.IPhonePlayer)
		{
			if (string.IsNullOrEmpty(jsonString))
			{
				CallInterface(methodName);
			}
			else
			{
				CallInterface(methodName, jsonString);
			}
			return "";
		}
#endif
		return "";
	}

#if UNITY_IOS && !UNITY_EDITOR
	private void CallInterface(string method)
	{
		switch (method)
		{
			case "Init":
				QKInit();
				break;
			case "Login":
				QKLogin();
				break;
			case "Logout":
				QKLogout();
				break;
			case "QKCenter":
				QKCenter();
				break;
			case "QKUserID":
				QKUserID();
				break;
			case "QKUserToken":
				QKUserToken();
				break;
			case "QKUserName":
				QKUserName();
				break;
		}
	}

	private void CallInterface(string method, string jsonString)
	{
		switch (method)
		{
			case "Pay":
				{
					UserSystem.Order order = JsonUtility.FromJson<UserSystem.Order>(jsonString);
					string productId = order.productID;
					string productName = order.name;
					int amount = order.amount;
					string orderNo = order.id;
					string callBackUrl = order.url;
					string extra = order.extra;
					QKCongzi(productId, productName, amount, orderNo, callBackUrl, extra);
				}
				break;
			case "QKUpdateRole":
				break;
		}
	}
#endif


	/// <summary>
	/// 原生代码调用Unity代码的接口。
	/// </summary>
	/// <param name="jsonString"></param>
	public void NativeCallUnity(string jsonString)
	{
		Debug.Log("NativeCallUnity => json string:" + jsonString);
		//UISystem.Instance.PopDialog("1. NativeCallUnity", jsonString);
		try
		{
			JsonObject jsonObject = SimpleJson.SimpleJson.DeserializeObject<JsonObject>(jsonString);
		    if (jsonObject != null && jsonObject.ContainsKey("method") && jsonObject.ContainsKey("param")) {
				string methodName = jsonObject["method"].ToString();
				string methodParam = jsonObject["param"].ToString();
				//UISystem.Instance.PopDialog("2. NativeCallUnity", jsonString);
				if (onNativeCallUnity != null)
				{
					onNativeCallUnity(methodName, methodParam);
				}
				//if (onNativeCallUnityX != null)
				//{
				//	onNativeCallUnityX.Call(methodName, methodParam);
				//}
		    }
		}
		catch (Exception e)
		{
			Debug.LogError(e);
			throw e;
		}
	}
}
