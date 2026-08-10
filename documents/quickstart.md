# GGUser 快速接入指南

> 目标：从把代码复制进工程开始，到完成「SDK 初始化 → 登录 → 支付 → 事件订阅」的最少可运行链路。
>
> 本文所有示例代码均来自框架真实源码 API，可在 **Unity 2021.3.4f1c1** 编辑器下直接运行（默认走模拟 SDK）。

---

## 1. 复制代码到工程

把仓库中的框架目录整个复制到你的 Unity 工程 `Assets/Scripts/` 下：

```
GGUser/Assets/Scripts/GGUser/   （框架代码，命名空间 GGFramework.GGUser）
```

复制后应包含以下核心子目录：

```
GGUser/Assets/Scripts/GGUser/
├── Systems/       # UserSystem.cs（核心用户系统）
├── User/          # UserManager.cs / ISDK.cs / SDKFactory.cs / User.cs / Order.cs /
│                  # 各渠道实现（QuickSDK / ZhiShangSDK / NativeSDK / InternalSDK）
├── Platform/      # NativeLuaBridge.cs / PlatformInterfaces.cs / AndroidInterfaces.cs
├── Adaptors/      # UIAdaptor.cs / LogAdaptor.cs
└── Dependency/    # Utils/Singleton.cs / CoroutineUtil.cs（泛型单例与协程工具）
```

> 如果你的工程还没有 `Assets/Scripts/Dependency/Utils/Singleton.cs` 和 `CoroutineUtil.cs`，
> 请一并复制（`Singleton<T>` 是所有单例的基类，`CoroutineUtil` 是状态机延时切状态所依赖的协程工具）。

## 2. 引入依赖库（必需）

框架的状态机和 JSON 序列化依赖以下两个库（位于本仓库 `Assets/LocalPackages`）：

| 依赖 | 用途 | 引用处 |
| ---- | ---- | ---- |
| **MonsterLove/StateMachine** | 轻量状态机（`using MonsterLove.StateMachine;`） | `UserSystem.cs` / `UserManager.cs` |
| **SimpleJson** | 轻量 JSON 序列化（`using SimpleJson;`） | `UserSystem.cs` / `Order.cs` / 各 SDK |

两个库缺一不可，否则框架代码无法编译。别忘了它们的 `.cs` 需要能进入编译（放在 `Assets` 下即可）。

## 3. 最小初始化

`UserManager` 是给业务层使用的高层门面，`UserSystem` 是底层用户系统（单例）。
**只需在游戏启动（如任意 `MonoBehaviour.Start`）里调用一次 `Init`，传入渠道名即可。**

创建脚本 `Bootstrap.cs`（示例与仓库中的 `Demo.cs` 一致）：

```csharp
using UnityEngine;
using GGFramework.GGUser;

public class Bootstrap : MonoBehaviour
{
    void Start()
    {
        // 初始化用户系统，channel 为当前渠道标识（如 "demo_channel"）
        UserManager.Instance.Init("demo_channel");
    }
}
```

> `UserManager.Instance.Init(string channel = null)` 内部会调用
> `UserSystem.Instance.Init(channel)`，并订阅 SDK 生命周期事件（登录、支付、登出等）。

把该脚本挂到场景中的任意 `GameObject` 上即可。

## 4. 登录

登录为**异步**流程，通过事件回调感知结果。调用 `UserManager.Login(serverID, callback)`：

```csharp
using UnityEngine;
using GGFramework.GGUser;
using SimpleJson;

public class LoginFlow : MonoBehaviour
{
    void Start()
    {
        // 第二个参数为进入游戏后的回调（返回 SimpleJson.JsonObject）
        UserManager.Instance.Login(1001, (JsonObject response) =>
        {
            Debug.Log("游戏登录成功，准备进入游戏。");
        });
    }
}
```

登录链路（由框架自动驱动）：

1. `UserManager.Login` 检查 `UserSystem.Instance.HasLogined()`；
2. 未登录则调用 `UserSystem.Instance.Login()`，由其状态机依次执行 `SDKIniting → SDKLogining → SDKLogined`；
3. 登录成功广播 `UserSystem.Events.SDKLoginSuccess`，`UserSystem.Instance.UserInfo` 会持有登录后的 `User`；
4. `UserManager` 收到后进入自身的 `Authing → Logining → Logined` 状态，最终触发第 2 个参数回调。

> **编辑器（默认 NativeSDK）说明**：`UserSystem.SDKLogin()` 与 `SDKInit()` 中带有
> `#if UNITY_EDITOR` 分支，会在编辑器里直接模拟「初始化成功 / 登录成功」，无需真机即可跑通流程。

## 5. 支付

支付前先构造 `Order`，再调用 `UserManager.Pay(order)`：

```csharp
using UnityEngine;
using GGFramework.GGUser;

public class PayFlow : MonoBehaviour
{
    public void BuyDiamond()
    {
        Order order = new Order
        {
            id        = "order_20240801_1001", // 游戏侧订单号
            productID = "com.demo.diamond",     // 商品 ID（与渠道后台配置一致）
            name      = "钻石礼包",              // 商品名称
            desc      = "1000 钻石",            // 商品描述
            price     = 6.0f,                   // 商品价格（元）
            amount    = 1,                      // 购买数量
            number    = 1000,                   // 对应资源（钻石）数量
            url       = "http://demo.com/pay/callback", // 支付回调接口
            extra     = "server=1001;role=007", // 扩展数据
        };

        // 发起支付
        UserManager.Instance.Pay(order);
    }
}
```

支付结果通过事件回调感知（见下节「事件订阅」）。

> 编辑器（NativeSDK + `SIMULATION_MODE_SDK`）下，`UserSystem.SDKPay` 会直接模拟 `PaySuccess`。

## 6. 事件订阅

所有生命周期结果（初始化 / 登录 / 登出 / 支付）都通过 `UserSystem.Instance.onEvent` 广播。
事件委托签名固定为：

```csharp
public Action<States, Events, object> onEvent;
// 参数1：当前状态 参数2：事件 参数3：附加数据（如支付的订单号 / 失败信息）
```

示例——订阅并处理支付结果、登录失败等：

```csharp
using UnityEngine;
using GGFramework.GGUser;

public class EventListener : MonoBehaviour
{
    void Start()
    {
        UserSystem.Instance.onEvent += OnUserEvent;
    }

    void OnDestroy()
    {
        // 建议在销毁时退订，避免重复回调
        UserSystem.Instance.onEvent -= OnUserEvent;
    }

    void OnUserEvent(UserSystem.States state, UserSystem.Events e, object param)
    {
        switch (e)
        {
            case UserSystem.Events.SDKInitSuccess:
                Debug.Log("SDK 初始化成功");
                break;
            case UserSystem.Events.SDKInitFailed:
                Debug.LogWarning("SDK 初始化失败: " + param);
                break;
            case UserSystem.Events.SDKLoginSuccess:
                Debug.Log("SDK 登录成功, uid=" + param);
                break;
            case UserSystem.Events.SDKLoginFailed:
                Debug.LogWarning("SDK 登录失败: " + param);
                break;
            case UserSystem.Events.SDKLogoutSuccess:
                Debug.Log("SDK 登出成功");
                break;
            case UserSystem.Events.SDKPaySuccess:
                Debug.Log("支付成功: " + param);
                break;
            case UserSystem.Events.SDKPayFailed:
                Debug.LogWarning("支付失败: " + param);
                break;
            case UserSystem.Events.SDKPayCancel:
                Debug.Log("支付已取消: " + param);
                break;
        }
    }
}
```

## 7. 完整可运行示例

把下面这段代码挂到场景的 `GameObject` 上（或用它替换 `Demo.cs`），即可在编辑器里跑通完整链路：

```csharp
using UnityEngine;
using GGFramework.GGUser;

public class QuickStartDemo : MonoBehaviour
{
    void Start()
    {
        // 1. 事件订阅（先订阅再初始化，避免漏事件）
        UserSystem.Instance.onEvent += OnUserEvent;

        // 2. 初始化
        UserManager.Instance.Init("demo_channel");

        // 3. 登录（编辑器下会模拟成功）
        UserManager.Instance.Login(1001, (SimpleJson.JsonObject response) =>
        {
            Debug.Log("游戏服阶段完成，可进入游戏。UID=" + UserSystem.Instance.UserInfo.uid);
        });
    }

    void Update()
    {
        // 演示：按 P 键发起一笔支付
        if (Input.GetKeyDown(KeyCode.P))
        {
            PayDemo();
        }
    }

    void PayDemo()
    {
        Order order = new Order
        {
            id        = "order_demo_001",
            productID = "com.demo.60diamond",
            name      = "60 钻石",
            price     = 6.0f,
            amount    = 1,
            number    = 60,
            extra     = "channel=demo",
        };
        UserManager.Instance.Pay(order);
    }

    void OnUserEvent(UserSystem.States state, UserSystem.Events e, object param)
    {
        Debug.LogFormat("[GGUser] state={0} event={1} param={2}", state, e, param);
    }
}
```

运行预期（编辑器，NativeSDK / 模拟模式）：

1. `SDKInitSuccess` 事件回调；
2. `SDKLoginSuccess` 事件回调，并触发 `Login` 的第 2 个参数回调；
3. 按 `P` 触发 `SDKPaySuccess` 事件回调。

## 8. 常见问题

| 现象 | 原因 / 解决 |
| ---- | ---- |
| 编译报 `MonsterLove` / `SimpleJson` 找不到 | 未引入依赖库，见上文第 2 节 |
| `SDKFactory.CreateSDK` 返回空导致流程不走 | 当前 `CreateSDK` 的渠道实现为注释占位，默认走 `UserSystem.Awake` 里创建的 `NativeSDK`（编辑器下可用）。真机渠道需按 `manual.md`「SDK 渠道接入」一节实现 |
| 真机不弹渠道登录框 | 编辑器模拟宏 `SIMULATION_MODE_SDK` 在真机包默认未开启，`NativeSDK` 会走 `NativeLuaBridge.UnityCallNative("Login", ...)` 调到原生，需先对接原生回调 |
| 想强制走模拟登录 | 在 `InternalSDK.SubmitData` / `NativeSDK` 等文件顶部打开 `SIMULATION_MODE_SDK`（见 `manual.md`） |

## 下一步

功能细节、完整 API 参考、多渠道接入方式与编辑器模拟模式说明，请阅读 **[manual.md](./manual.md)（使用手册）**。
