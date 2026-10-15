//#define QUICK_SDK
using UnityEngine;
using GGFramework.GGUser;
#if QUICK_SDK
using quicksdk;

/// <summary>
/// QuickSDK 事件回调处理器。仅在定义 QUICK_SDK 宏时参与编译。
/// 已清理历史遗留的 200+ 行注释死代码，保留 QuickSDKListener 需要 override 的接口实现。
/// </summary>
public class QuickSDKEventHandle : QuickSDKListener
{
    void Start()
    {
        QuickSDK.getInstance().setListener(this);
    }

    // ---------------- QuickSDKListener 回调实现 ----------------

    public override void onInitSuccess()
    {
        UserSystem.Instance.CallSDKInitSuccess("");
    }

    public override void onInitFailed(ErrorMsg errMsg)
    {
        UserSystem.Instance.CallSDKInitFailed(errMsg.errMsg);
    }

    public override void onLoginSuccess(UserInfo userInfo)
    {
        UserSystem.Instance.CallSDKLoginSuccess(
            new User(userInfo.uid, userInfo.token, "", QuickSDK.getInstance().getConfigValue("ChannelID")));
    }

    public override void onSwitchAccountSuccess(UserInfo userInfo)
    {
        // 切换账号成功：业务侧应清除旧角色信息并以新的 userInfo 重进游戏，无需再次登录。
        Debug.Log("[QuickSDK] onSwitchAccountSuccess: " + userInfo.uid);
    }

    public override void onLoginFailed(ErrorMsg errMsg)
    {
        UserSystem.Instance.CallSDKLoginFailed(errMsg.errMsg);
    }

    public override void onLogoutSuccess()
    {
        UserSystem.Instance.CallSDKLogoutSuccess("");
        UserSystem.Instance.ChangeState(UserSystem.States.SDKLogining);
    }

    public override void onPaySuccess(PayResult payResult)
    {
        UserSystem.Instance.CallSDKPaySuccess(payResult.ToString());
    }

    public override void onPayCancel(PayResult payResult)
    {
        UserSystem.Instance.CallSDKPayCancel(payResult.ToString());
    }

    public override void onPayFailed(PayResult payResult)
    {
        UserSystem.Instance.CallSDKPayFailed(payResult.ToString());
    }

    public override void onExitSuccess()
    {
        // 退出成功回调。为避免与渠道冲突，请使用 QuickSDK.getInstance().exitGame() 而非 Application.Quit()。
    }

    public override void onSucceed(string infos)
    {
        Debug.Log("[QuickSDK] onSucceed: " + infos);
    }

    public override void onFailed(string message)
    {
        Debug.LogWarning("[QuickSDK] onFailed: " + message);
    }
}

#endif //QUICK_SDK
