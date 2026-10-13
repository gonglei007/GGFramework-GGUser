/// <summary>
/// 全局配置常量。供原生桥接等平台相关代码使用。
/// </summary>
public static class Global
{
    /// <summary>
    /// Android 原生桥接类的全限定名（需与 Android 工程中实际的接口类一致）。
    /// NativeLuaBridge.UnityCallNative 会通过 AndroidJavaClass 反射该类并调用其静态方法 "CallInterface"。
    /// 接入真机时请修改为你的原生接口类名。
    /// </summary>
    public static string AndroidInterfaceName = "com.yourcompany.sampleproject.UnityInterface";
}
