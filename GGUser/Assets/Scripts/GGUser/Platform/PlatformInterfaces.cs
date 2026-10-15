using UnityEngine;

/// <summary>
/// 平台相关的通用信息接口。
/// 说明：本类保持全局命名空间以兼容既有引用；如需归入 GGFramework.GGUser 命名空间，
/// 需同步替换所有调用点并处理 .meta GUID 变更。
/// </summary>
public class PlatformInterfaces
{
	private static string deviceUID = null;
	private static string deviceID = null;

	/// <summary>
	/// 获取设备唯一标识（Android ID / iOS IDFV / 编辑器 SystemInfo）。
	/// 结果会缓存，首次调用后不再走原生反射。
	/// </summary>
	public static string GetDeviceID()
	{
		if (!string.IsNullOrEmpty(deviceID)) return deviceID;
#if UNITY_EDITOR
		deviceID = SystemInfo.deviceUniqueIdentifier;
#elif UNITY_ANDROID
		deviceID = AndroidInterfaces.GetAndroidID();
		if (string.IsNullOrEmpty(deviceID)) deviceID = SystemInfo.deviceUniqueIdentifier;
#elif UNITY_IOS
		// iOS 上如需 IDFA 请自行接入 AdSupport 框架；此处默认使用 IDFV（deviceUniqueIdentifier）。
		deviceID = SystemInfo.deviceUniqueIdentifier;
#else
		deviceID = SystemInfo.deviceUniqueIdentifier;
#endif
		return deviceID;
	}

	/// <summary>
	/// 获取设备唯一标识。iOS 上原设计取 IDFA，注意送审时的勾选。
	/// 当前实现回退到 SystemInfo.deviceUniqueIdentifier（IDFV），避免额外的原生桥接依赖。
	/// </summary>
	public static string GetDeviceUID()
	{
		if (deviceUID == null)
		{
			if (Application.platform == RuntimePlatform.IPhonePlayer)
			{
				//TODO: 如需 IDFA，请接入 AdSupport 后调用 iOSInterfaces.CallGetDeviceUID()。
				deviceUID = SystemInfo.deviceUniqueIdentifier;
			}
			else
			{
				deviceUID = SystemInfo.deviceUniqueIdentifier;
			}
		}
		return deviceUID;
	}
}
