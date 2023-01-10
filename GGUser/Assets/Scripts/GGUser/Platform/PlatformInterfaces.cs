using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlatformInterfaces
{
	private static string deviceUID = null;

	public static string GetDeviceID() {
		string deviceID = "";
#if UNITY_EDITOR
#elif UNITY_ANDROID
#elif UNITY_IOS
#endif
		return deviceID;
	}

	/// <summary>	/// 获取设备唯一标识。iOS的是取IDFA，注意送审时的勾选。	/// </summary>	/// <returns>The device user interface.</returns>    public static string GetDeviceUID()
	{		if (deviceUID == null)
		{			if (Application.platform == RuntimePlatform.IPhonePlayer)			{				//TODO: GL - deviceUID = iOSInterfaces.CallGetDeviceUID();			}			else			{				deviceUID = SystemInfo.deviceUniqueIdentifier;			}		}		return deviceUID;	}
}
