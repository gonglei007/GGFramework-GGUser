//#define QUICK_SDK
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if QUICK_SDK
using quicksdk;
#endif
using GGFramework.GGUser;

public class QuickSDKImpl : ISDK
{
#if QUICK_SDK
    private QuickSDKEventHandle sdkEventHandle = null;
#endif

    public QuickSDKImpl() {
#if QUICK_SDK
        GameObject eventHandleInstance = new GameObject("QuickSDKEventHandle");
        eventHandleInstance.transform.parent = UserSystem.Instance.gameObject.transform;
        sdkEventHandle = eventHandleInstance.AddComponent<QuickSDKEventHandle>();
        eventHandleInstance.SetActive(true);
#endif
    }
    public void Init()
    {
#if QUICK_SDK
        Debug.Log("Start Call QuickSDK.init");
        QuickSDK.getInstance().init();
#endif
    }

    public void Login()
    {
#if QUICK_SDK
        QuickSDK.getInstance().login();
#endif
    }

    public void Logout()
    {
#if QUICK_SDK
        QuickSDK.getInstance().logout();
#endif
    }

    public void Quit()
    {
#if QUICK_SDK
        
#endif
    }

    public void Pay(Order order, UserSystem.UserData userData = null)
    {
#if QUICK_SDK
        OrderInfo orderInfo = new OrderInfo();
        GameRoleInfo gameRoleInfo = new GameRoleInfo();

        orderInfo.goodsID = order.productID;
        orderInfo.goodsName = order.name;
        orderInfo.goodsDesc = order.desc;
        orderInfo.extrasParams = order.extra;
        orderInfo.count = order.number;
        orderInfo.amount = order.price;
        orderInfo.callbackUrl = order.url;
        orderInfo.cpOrderID = order.id;
        // 以下的参数已经废弃。
        orderInfo.price = order.price;
        orderInfo.quantifier = "个";

        //TODO: GL - 下一版再做实现。
        gameRoleInfo.gameRoleBalance = "0";
        gameRoleInfo.gameRoleID = "000001";
        gameRoleInfo.gameRoleLevel = "1";
        gameRoleInfo.gameRoleName = "QuickSDK玩家";
        gameRoleInfo.partyName = "GO";
        gameRoleInfo.serverID = "1";
        gameRoleInfo.serverName = "游戏服务器";
        gameRoleInfo.vipLevel = "1";
        gameRoleInfo.roleCreateTime = "roleCreateTime";
        QuickSDK.getInstance().pay(orderInfo, gameRoleInfo);
#endif
    }
    public void SubmitData(string jsonData)
    {
#if QUICK_SDK
        //TODO: GL - QuickSDK.getInstance().submitData();
#endif
    }
}
