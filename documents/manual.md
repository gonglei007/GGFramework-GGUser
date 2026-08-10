# GGUser 使用手册

> 一个游戏客户端的**用户 / 登录 / 支付 SDK 管理框架**。命名空间 `GGFramework.GGUser`，Unity 版本 **2021.3.4f1c1**。
>
> 本手册依据仓库**真实源码**编写，所有类名 / 方法名 / 字段名均与源码一致。

---

## 目录

- [1. 模块总览](#1-模块总览)
- [2. 核心数据模型](#2-核心数据模型)
- [3. UserSystem：核心用户系统](#3-usersystem核心用户系统)
- [4. UserManager：高层业务门面](#4-usermanager高层业务门面)
- [5. ISDK / SDKFactory：SDK 抽象与工厂](#5-isdk--sdkfactorysdk-抽象与工厂)
- [6. 各 SDK 渠道实现](#6-各-sdk-渠道实现)
- [7. 登录流程](#7-登录流程)
- [8. 支付流程](#8-支付流程)
- [9. 事件订阅与事件枚举](#9-事件订阅与事件枚举)
- [10. 平台桥接 NativeLuaBridge](#10-平台桥接-nativelualridge)
- [11. 适配器 UIAdaptor / LogAdaptor](#11-适配器-uiadaptor--logadaptor)
- [12. 编辑器模拟模式 SIMULATION_MODE_SDK](#12-编辑器模拟模式-simulation_mode_sdk)
- [13. 平台宏](#13-平台宏)
- [14. SDK 渠道接入方法](#14-sdk-渠道接入方法)
- [15. 第三方依赖](#15-第三方依赖)

---

## 1. 模块总览

| 模块 | 类型 | 职责 |
| ---- | ---- | ---- |
| `Systems/UserSystem` | 单例（MonoBehaviour） | 核心用户系统：驱动 SDK 初始化 / 登录 / 支付全流程，内置状态机 + 事件广播 |
| `User/UserManager` | 单例（MonoBehaviour） | 高层业务门面：对外提供 `Init / Login / Pay / SubmitData / HasLogined` |
| `User/ISDK` | 抽象接口 | 所有 SDK 渠道的统一行为契约 |
| `User/SDKFactory` | 静态工厂 | 按 `SDKTarget` 创建对应渠道实现（策略模式） |
| `User/QuickSDK`（`QuickSDKImpl`） | ISDK 实现 | QuickSDK 渠道对接 |
| `User/ZhiShangSDK` | ISDK 实现 | 智尚 SDK 渠道对接 |
| `User/NativeSDK` | ISDK 实现 | 原生（通用）SDK，**默认 / 编辑器模拟**实现 |
| `User/InternalSDK` | ISDK 实现 | 内部账号系统实现 |
| `User/User` / `User/Order` | 数据模型 | 用户信息 / 订单信息，自带 JSON 序列化 |
| `Platform/NativeLuaBridge` | 单例（MonoBehaviour） | Unity ↔ 原生双向桥接 |
| `Platform/PlatformInterfaces` / `AndroidInterfaces` | 静态工具 | 设备号 / 平台接口封装 |
| `Adaptors/UIAdaptor` / `LogAdaptor` | 适配器 | 通过委托注入 UI 弹窗 / 本地化 / 日志上报等能力 |
| `Dependency/Utils/Singleton` / `CoroutineUtil` | 基础工具 | 泛型单例基类 / 协程工具 |

---

## 2. 核心数据模型

### 2.1 `User`（用户信息）

| 成员 | 类型 | 默认 | 说明 |
| ---- | ---- | ---- | ---- |
| `uid` | `string` | `null` | 用户唯一标识 |
| `token` | `string` | `null` | 登录凭证 token |
| `account` | `string` | `null` | 账号 |
| `channel` | `string` | `null` | 渠道标识 |
| `isDev` | `bool` | `false` | 是否为开发版（编辑器下自动置 `true`） |

构造 / 方法：

| 签名 | 说明 |
| ---- | ---- |
| `User()` | 空构造，编辑器下 `isDev=true` |
| `User(string uid, string token, string account, string channel)` | 带参构造 |
| `void SetUser(string uid, string token, string account, string channel)` | 一次性设置全部用户字段 |

### 2.2 `Order`（订单信息）

| 成员 | 类型 | 默认 | 说明 |
| ---- | ---- | ---- | ---- |
| `id` | `string` | `null` | **订单 ID（游戏侧订单号）** |
| `productID` | `string` | `null` | 商品 ID（与渠道后台配置一致） |
| `name` | `string` | `"商品x"` | 商品名称 |
| `desc` | `string` | `""` | 商品描述 |
| `price` | `float` | `1.0f` | 商品价格（元） |
| `amount` | `int` | `1` | 购买商品数量 |
| `number` | `int` | `1` | 此商品对应的资源（如元宝 / 钻石）数量 |
| `url` | `string` | `""` | 支付回调接口 |
| `extra` | `string` | `""` | 扩展数据 |
| `isDev` | `bool` | `false` | 是否为开发版（编辑器下自动置 `true`） |

方法：

| 签名 | 说明 |
| ---- | ---- |
| `string GetJsonString()` | 把订单序列化为 JSON 字符串（含 `id/productID/name/desc/price/amount/number/url/extra`） |

### 2.3 `UserSystem.UserData`（游戏角色 / 用户数据，嵌套在 UserSystem 中）

类型枚举：

```csharp
public enum Type
{
    TNone       = -1,
    TCreateRole,   // 创建角色
    TEnterGame,    // 进入游戏
    TLevelUp,      // 角色升级
}
```

字段、方法：

| 成员 | 类型 | 默认 | 说明 |
| ---- | ---- | ---- | ---- |
| `type` | `Type` | `TNone` | 数据类型 |
| `roleID` | `string` | `""` | 角色 ID |
| `roleName` | `string` | `""` | 角色名 |
| `roleLevel` | `string` | `"1"` | 角色等级 |
| `roleCreateTime` | `long` | `0` | 角色创建时间戳 |
| `vipLevel` | `int` | `0` | VIP 等级 |
| `groupName` | `string` | `""` | 帮派 / 阵营名 |
| `serverID` | `int` | `0` | 服务器 ID |
| `serverName` | `string` | `""` | 服务器名 |
| `UserData()` | 构造 | — | 空构造 |
| `UserData(Type type)` | 构造 | — | 指定类型构造 |
| `string GetJsonString()` | 方法 | — | 序列化为 JSON 字符串 |

---

## 3. UserSystem：核心用户系统

单例：`UserSystem.Instance`。继承 `Singleton<UserSystem>`（MonoBehaviour）。

### 3.1 状态机 `States`

```csharp
public enum States
{
    None,          // 初始
    SDKIniting,    // SDK 初始化中
    SDKInited,     // SDK 初始化完成
    SDKLogining,   // SDK 账户登录中
    SDKLogined,    // SDK 账户已登录
}
```

状态流转由 `StateMachine<States>` 驱动：

```
None → SDKIniting → SDKLogining → SDKLogined
        （初始化失败/登录失败会回退到 SDKIniting / SDKLogining 重试）
```

### 3.2 常用属性

| 成员 | 说明 |
| ---- | ---- |
| `string Channel` | 当前渠道标识（`Init` 时设置） |
| `ISDK SDK` | 当前使用的 SDK 实现（`Awake` 中由 `SDKFactory.CreateSDK(SDKTarget.NativeSDK)` 创建） |
| `User UserInfo` | 登录成功后的用户信息（等价于字段 `user`） |
| `UIAdaptor UIAdaptor` | UI 适配器实例 |
| `LogAdaptor LogAdaptor` | 日志适配器实例 |
| `Action<States, Events, object> onEvent` | **事件广播委托**，登录 / 支付 / 初始化等生命周期事件都从这里发出 |

### 3.3 公开方法（API 参考）

| 方法 | 参数 | 返回 | 说明 |
| ---- | ---- | ---- | ---- |
| `Init` | `string channel` | `void` | 设置渠道号并复位 `readyLogin`，为登录做准备 |
| `Login` | — | `void` | 发起 SDK 登录；按当前状态自动流转（None→初始化，Inited/Logining→登录） |
| `HasLogined` | — | `bool` | 是否已处于 `SDKLogined` 状态 |
| `SDKInit` | — | `void` | 调用 `sdk.Init()`；编辑器下直接模拟初始化成功 |
| `SDKLogin` | — | `void` | 调用 `sdk.Login()`；编辑器下直接模拟登录成功 |
| `SDKLogout` | — | `void` | 调用 `sdk.Logout()` 登出 |
| `SDKQuit` | — | `void` | 调用 `sdk.Quit()` 退出 SDK |
| `SDKPay` | `Order order, UserSystem.UserData userData = null` | `void` | 调用 `sdk.Pay(order, userData)` 发起支付；编辑器下模拟支付成功 |
| `ChangeState` | `States state, float delay = 0.0f` | `void` | 切换状态机状态（可延时） |
| `SetEvent` | `States state, Event e` | `void` | 记录某个状态对应的待广播事件 |
| `TriggerEvent` | `States state, Events e, object param` | `void` | 触发 `onEvent` 广播 |

### 3.4 SDK 回调入口（一般由渠道实现调用）

| 方法 | 参数 | 说明 |
| ---- | ---- | ---- |
| `CallSDKInitSuccess` | `string ret` | 初始化成功回调；若处于待登录则切入 `SDKLogining` |
| `CallSDKInitFailed` | `string ret` | 初始化失败回调，广播 `SDKInitFailed` |
| `CallSDKLoginSuccess` | `User ret` | 登录成功回调，保存 `UserInfo`，切至 `SDKLogined` |
| `CallSDKLoginFailed` | `string ret` | 登录失败回调，广播 `SDKLoginFailed` |
| `CallSDKLogoutSuccess` | `string ret` | 登出成功回调 |
| `CallSDKPaySuccess` | `string ret` | 支付成功，广播 `SDKPaySuccess` |
| `CallSDKPayFailed` | `string ret` | 支付失败，广播 `SDKPayFailed` |
| `CallSDKPayCancel` | `string ret` | 支付取消，广播 `SDKPayCancel` |

### 3.5 嵌套类型

- `UserSystem.States` — 状态枚举
- `UserSystem.Events` — 事件枚举
- `UserSystem.Event` — `{ Events e; object v; }`，封装「某状态下要广播的事件及参数」
- `UserSystem.UserData` — 游戏角色数据（见 2.3）
- `UserSystem.User` — 即外部 `User` 类的别名引用（渠道实现中常见 `new UserSystem.User(...)`）

---

## 4. UserManager：高层业务门面

单例：`UserManager.Instance`。业务层**首选**入口，封装了认证 / 登录 / 游戏服登录的状态机。

### 4.1 状态机 `States`

```csharp
public enum States
{
    None,      // 初始
    Init,      // 初始化
    Authing,   // 账户验证中（预留：对接游戏服认证）
    Logining,  // 账户登录中（预留：对接游戏服登录）
    Logined,   // 账户已登录
}
```

### 4.2 属性

| 成员 | 说明 |
| ---- | ---- |
| `UserSystem UserSystem` | 便捷获取 `UserSystem.Instance` |
| `bool HasLogined()` | 是否处于 `Logined`（游戏服登录完成）状态 |

### 4.3 公开方法（API 参考）

| 方法 | 参数 | 返回 | 说明 |
| ---- | ---- | ---- | ---- |
| `Init` | `string channel = null` | `void` | **入口**：调用 `UserSystem.Instance.Init(channel)` 并订阅 `onEvent` 处理登录 / 支付 / 登出等事件 |
| `Login` | `int serverID, Action<JsonObject> callback` | `void` | 发起登录；未登录 SDK 时先登录 SDK，成功后走游戏服阶段，最终回调 `callback(loginResponse)` |
| `Pay` | `Order order` | `void` | 调用 `UserSystem.Instance.SDKPay(order, userData)` 发起支付 |
| `SubmitData` | `UserSystem.UserData.Type type` | `void` | 设置角色数据类型并调用 `SDK.SubmitData(json)` 上报角色数据 |
| `GetUserData` | — | `UserSystem.UserData` | 返回内部维护的 UserData 实例 |

> **注意** `Login` 中游戏服「认证 / 登录」环节（`Authing` / `Logining`）在源码中为**预留注释位**（TODO，建议配合 GGNetwork 实现自己的游戏服验证）。当前 demo 链路通过 `UserSystem` 事件把登录成功结果转发给回调。

### 4.4 Init 内置事件处理

`UserManager.Init` 订阅 `UserSystem.onEvent` 后，内置处理：

| 事件 | 行为（源码） |
| ---- | ---- |
| `SDKLoginSuccess` | 打日志检查 `HasLogined()`；若 `AutoEnterGame` 则 `fsm.ChangeState(States.Init)` |
| `SDKInitFailed` / `SDKLoginFailed` | 通过 `UIAdaptor.ShowDialog("login_error", "login__error_description", ...)` 弹错误框 |
| `SDKPaySuccess` / `SDKPayFailed` / `SDKPayCancel` | 预留位（可在此做游戏服发货确认 / 取消处理） |
| `SDKLogoutSuccess` | 预留位（可在此处理退出游戏 / 回登录界面） |

---

## 5. ISDK / SDKFactory：SDK 抽象与工厂

### 5.1 `ISDK` 接口

```csharp
public interface ISDK
{
    void Init();                                        // SDK 初始化
    void Login();                                       // 账户登录
    void Logout();                                      // 登出
    void Quit();                                        // 退出
    void Pay(Order order, UserSystem.UserData userData = null); // 支付
    void SubmitData(string jsonData);                   // 上报角色 / 用户数据
}
```

### 5.2 `SDKFactory`

```csharp
public enum SDKTarget
{
    QuickSDK,
    ZhiShangSDK,
    NativeSDK,
}
```

| 方法 | 参数 | 返回 | 说明 |
| ---- | ---- | ---- | ---- |
| `Init()` | — | `void` | 预留工厂初始化（源码中为空实现） |
| `CreateSDK` | `SDKTarget target` | `ISDK` | 按目标创建 SDK 实例（当前各 case 为注释占位，见 §6 备注） |

> 源码现状：`UserSystem.Awake` 里固定用 `SDKFactory.CreateSDK(SDKTarget.NativeSDK)` 创建默认 SDK。
> 工厂的 QuickSDK / ZhiShang 分支实现被注释，接入真实渠道时在对应 case 中 `sdk = new QuickSDKImpl();` 等即可（见 §14）。

---

## 6. 各 SDK 渠道实现

所有实现都遵循 `ISDK`，并通过 `NativeLuaBridge.onNativeCallUnity` 或原生桥接接收回调。

| 实现 | 类 | 说明 / 当前状态 |
| ---- | ---- | ---- |
| **NativeSDK** | `GGFramework.GGUser.NativeSDK` | **默认 / 推荐**。构造函数向 `NativeLuaBridge.onNativeCallUnity` 注册 `HandleSDKCallback`。编辑器下自动启用 `SIMULATION_MODE_SDK` 模拟全流程；真机走 `NativeLuaBridge.UnityCallNative` |
| **InternalSDK** | `GGFramework.GGUser.InternalSDK` | 内部账号系统。所有方法走 `NativeLuaBridge.UnityCallNative("Init/Login/...", json)`；`SubmitData` 受 `SIMULATION_MODE_SDK` 控制 |
| **ZhiShangSDK** | 全局（无命名空间）`ZhiShangSDK` | 智尚渠道。构造函数注册回调，`HandleSDKCallback` 解析 `InitSuccess/LoginSuccess/PaySuccess...` |
| **QuickSDKImpl** | 全局（无命名空间）`QuickSDKImpl` | QuickSDK 渠道。需定义 `QUICK_SDK` 宏并引入 `quicksdk` 命名空间；配套 `QuickSDKEventHandle`（继承 `QuickSDKListener`）把渠道回调转接到 `UserSystem.CallSDK*` |

各实现把原生回调统一转为对 `UserSystem.CallSDK*` 系列的调用，从而复用状态机与事件广播，例如：

```csharp
// NativeSDK.HandleSDKCallback 中的示例逻辑
case "InitSuccess":   UserSystem.Instance.CallSDKInitSuccess("");    break;
case "LoginSuccess":  /* 解析 jsonString 中 accountid/token */ 
                      UserSystem.Instance.CallSDKLoginSuccess(user); break;
case "PaySuccess":    UserSystem.Instance.CallSDKPaySuccess("");     break;
case "PayFailed":     UserSystem.Instance.CallSDKPayFailed(json["customorgerid"].ToString()); break;
case "PayCancel":     UserSystem.Instance.CallSDKPayCancel(json["customorgerid"].ToString()); break;
case "LogoutSuccess": Application.Quit(); break;
```

---

## 7. 登录流程

### 7.1 完整流程

```
① UserManager.Login(serverID, callback)
   │  （若 SDK 未登录）→ UserSystem.Login()
   ▼
② UserSystem 状态机：None → SDKIniting
   │  SDKIniting_Enter() → SDKInit() → sdk.Init()
   │  编辑器：直接 CallSDKInitSuccess
   ▼
③ 初始化成功 → CallSDKInitSuccess → SDKLogining
   │  SDKLogining_Enter() → SDKLogin() → sdk.Login()
   │  编辑器：直接 CallSDKLoginSuccess
   ▼
④ 登录成功 → CallSDKLoginSuccess(User) → 保存 UserInfo → SDKLogined
   │  广播 Events.SDKLoginSuccess（param=uid）
   ▼
⑤ UserManager 收到 SDKLoginSuccess
   │  →（游戏服认证/登录 Authing→Logining，预留位）→ Logined
   ▼
⑥ 触发 Login 的第 2 个参数 callback(loginResponse)
```

### 7.2 推荐接入代码

```csharp
using UnityEngine;
using GGFramework.GGUser;

// 1) 初始化
UserManager.Instance.Init("my_channel");

// 2) 登录（结果通过 onEvent 与回调两路获得）
UserManager.Instance.Login(1234, (SimpleJson.JsonObject response) =>
{
    Debug.Log("进入游戏，uid=" + UserSystem.Instance.UserInfo.uid);
});
```

> 登录为异步。建议通过 `UserSystem.Instance.onEvent` 订阅 `SDKLoginSuccess` / `SDKLoginFailed` 做 UI 反馈（转圈、错误提示）。

---

## 8. 支付流程

### 8.1 完整流程

```
① 业务构造 Order（id / productID / name / price / ...）
   ▼
② UserManager.Pay(order)
   ▼
③ UserSystem.SDKPay(order, userData) → sdk.Pay(order, userData)
   │  编辑器（SIMULATION_MODE_SDK）：直接 CallSDKPaySuccess
   ▼
④ 原生 / 渠道支付回调 → CallSDKPaySuccess / CallSDKPayFailed / CallSDKPayCancel
   ▼
⑤ 通过 onEvent 广播：
   Events.SDKPaySuccess(param=订单号) / SDKPayFailed / SDKPayCancel
```

### 8.2 推荐接入代码

```csharp
using UnityEngine;
using GGFramework.GGUser;

Order order = new Order
{
    id        = "order_20260810_123";  // 游戏侧唯一订单号
    productID = "com.game.gold_648";   // 与渠道后台一致
    name      = "648 元宝",
    desc      = "6480 元宝",
    price     = 648f,
    amount    = 1,
    number    = 6480,
    url       = "http://mygame.com/pay/notify",
    extra     = "server=3;role=10086",
};
UserManager.Instance.Pay(order);
```

支付结果在 `onEvent` 中订阅（见 §9）。

---

## 9. 事件订阅与事件枚举

### 9.1 事件枚举 `UserSystem.Events`

| 枚举值 | 含义 | 触发入口 | param 典型内容 |
| ---- | ---- | ---- | ---- |
| `SDKInitSuccess` | SDK 初始化成功 | `CallSDKInitSuccess` | 成功信息字符串 |
| `SDKInitFailed` | SDK 初始化失败 | `CallSDKInitFailed` | 失败信息字符串 |
| `SDKLoginSuccess` | SDK 账户登录成功 | `CallSDKLoginSuccess` | 登录用户的 `uid` |
| `SDKLoginFailed` | SDK 账户登录失败 | `CallSDKLoginFailed` | 失败信息字符串 |
| `SDKLogoutSuccess` | SDK 登出成功 | `CallSDKLogoutSuccess` | 信息字符串 |
| `SDKPaySuccess` | 支付成功 | `CallSDKPaySuccess` | 订单号等信息 |
| `SDKPayFailed` | 支付失败 | `CallSDKPayFailed` | 订单号等信息 |
| `SDKPayCancel` | 支付取消 | `CallSDKPayCancel` | 订单号等信息 |

### 9.2 订阅方式

委托签名（源码）：

```csharp
public Action<States, Events, object> onEvent;
```

```csharp
UserSystem.Instance.onEvent += (UserSystem.States state, UserSystem.Events e, object param) =>
{
    Debug.LogFormat("state={0} event={1} param={2}", state, e, param);
};
```

> 订阅时机建议在 `UserManager.Init` **之前**，避免漏掉 `Init` 之后立即触发的事件。

---

## 10. 平台桥接 NativeLuaBridge

单例：`NativeLuaBridge.Instance`。负责 Unity（含 Lua）与 Android / iOS 原生代码的双向通信。

### 10.1 成员

| 成员 | 类型 | 说明 |
| ---- | ---- | ---- |
| `onNativeCallUnity` | `static Action<string, string>` | 原生回调进入 Unity 的统一入口，分发 `method + jsonString` |
| `Init()` | `void` | 预留空实现 |
| `UnityCallNative(string methodName, string jsonString)` | `string` | Unity 调用原生；Android 走 `AndroidJavaClass.CallStatic("CallInterface", ...)`，iOS 走 `[DllImport("__Internal")]` |
| `NativeCallUnity(string jsonString)` | `void` | 原生调用 Unity；解析 `{method, param}` 后触发 `onNativeCallUnity` |

### 10.2 平台差异

- **Android**：`UnityCallNative` 使用 `AndroidJavaClass`（类名来自 `Global.AndroidInterfaceName`）静态调用 `"CallInterface"` 方法。
- **iOS**：通过 `[DllImport("__Internal")]` 声明原生 C 函数（`QKInit / QKLogin / QKLogout / QKCongzi / QKUpdateRole` 等），`UnityCallNative` 根据 method 分发：
  - 无参：`Init / Login / Logout / QKCenter / QKUserID / QKUserToken / QKUserName`
  - 有参：`Pay`（解析 `Order` JSON → `QKCongzi(productId, productName, amount, orderNo, callBackUrl, extra)`）、`QKUpdateRole`

`Awake` 中执行 `DontDestroyOnLoad(gameObject)`，保证全局常驻；并调用 `UnityCallNative("Test", ...)` 做连通性自检。

---

## 11. 适配器 UIAdaptor / LogAdaptor

框架通过**委托注入**把 UI 与日志上报能力解耦，不依赖具体 UI / 上报框架，可插拔替换。

### 11.1 `UIAdaptor`

| 成员 | 类型 | 说明 |
| ---- | ---- | ---- |
| `onDialog` | `Action<string, string, bool, Action<bool>>` | 弹窗实现：标题 / 内容 / 是否需要确认 / 回调 `bool` |
| `onGetText` | `Func<string, string>` | 本地化文本查找：传入 key 返回文本 |
| `onWaiting` | `Action<bool>` | 全局等待遮罩开关 |

| 方法 | 参数 | 返回 | 说明 |
| ---- | ---- | ---- | ---- |
| `GetText(string text)` | 文本 / key | `string` | 若设置了 `onGetText` 则本地化，否则原样返回 |
| `ShowDialog(string title, string message, bool confirm, Action<bool> callback)` | — | `void` | 弹窗；未设置 `onDialog` 时直接 `callback(true)` |
| `ShowWaiting(bool waiting)` | 是否显示等待 | `void` | 开关等待遮罩 |

示例注入：

```csharp
UserSystem.Instance.UIAdaptor.onDialog = (title, msg, confirm, cb) =>
{
    // 对接你自己的弹窗系统（示例输出到控制台）
    Debug.Log($"[Dialog] {title}: {msg}");
    cb?.Invoke(true);
};
UserSystem.Instance.UIAdaptor.onWaiting = (show) =>
{
    Debug.Log("等待遮罩: " + show);
};
```

### 11.2 `LogAdaptor`

| 成员 | 类型 | 说明 |
| ---- | ---- | ---- |
| `onPostError` | `Action<string, string>` | 错误上报：`(id, info)` |
| `onPostNetworkError` | `Action<string, JsonObject>` | 网络错误上报：`(host, param)` |

| 方法 | 参数 | 返回 | 说明 |
| ---- | ---- | ---- | ---- |
| `PostError(string id, string info)` | — | `void` | 上报错误（若设置了 `onPostError`） |
| `PostError(string host, JsonObject param)` | — | `void` | 上报网络错误（若设置了 `onPostNetworkError`） |

示例注入：

```csharp
UserSystem.Instance.LogAdaptor.onPostError = (id, info) =>
{
    Debug.LogError($"上报错误 {id}: {info}");
};
```

---

## 12. 编辑器模拟模式 SIMULATION_MODE_SDK

**核心便利特性**：在编辑器里无需真机 / 渠道 SDK，即可模拟跑通「初始化 → 登录 → 支付」全流程。

- `NativeSDK.cs` 顶部源码：

  ```csharp
  #if UNITY_EDITOR
  #define SIMULATION_MODE_SDK
  #endif
  ```

- 在该宏下，`NativeSDK` 的行为：

| 方法 | SIMULATION_MODE_SDK 开启 | 关闭（真机） |
| ---- | ---- | ---- |
| `Init()` | 直接 `HandleSDKCallback("InitSuccess")` | `UnityCallNative("Init", null)` |
| `Login()` | 直接 `HandleSDKCallback("LoginSuccess")` | `UnityCallNative("Login", null)` |
| `Logout()` / `Quit()` | 空处理 | `UnityCallNative("Logout"/"SdkQuit", null)` |
| `Pay(order, userData)` | 直接 `HandleSDKCallback("PaySuccess")` | `UnityCallNative("Pay", json)` |
| `SubmitData(json)` | 空处理 | `UnityCallNative("SubmitData", json)` |

- `UserSystem.SDKInit / SDKLogin / SDKPay` 也带 `#if UNITY_EDITOR` 分支直接模拟成功，双重保障。

> **手动强制模拟**：可在任意 SDK 实现中打开该宏（如 `InternalSDK.SubmitData` 内已引用 `SIMULATION_MODE_SDK`），使非编辑器平台也走纯模拟逻辑，方便联调 / 演示。

---

## 13. 平台宏

框架大量使用 Unity 平台条件编译宏来区分编辑器 / 真机 / 平台。

| 宏 | 含义 | 使用场景（源码示例） |
| ---- | ---- | ---- |
| `UNITY_EDITOR` | 仅在编辑器运行 | `UserSystem.SDKInit/Login/Pay` 中模拟成功；`User` / `Order` 的 `isDev=true`；`NativeSDK.cs` 定义 `SIMULATION_MODE_SDK` |
| `UNITY_ANDROID` | Android 平台 | `NativeLuaBridge.UnityCallNative` 走 `AndroidJavaClass`；`AndroidInterfaces` 中所有 `Call*` 方法 |
| `UNITY_IOS` | iOS 平台 | `NativeLuaBridge` 中 `[DllImport("__Internal")]` 声明原生函数；`UnityCallNative` 走 `CallInterface` |
| `!UNITY_EDITOR` | 「非编辑器」（真机） | `NativeLuaBridge` 中 Android / iOS 调用才真正执行；`AndroidInterfaces` 同理 |
| `QUICK_SDK` | 自定义：启用 QuickSDK | `QuickSDKImpl` / `QuickSDKEventHandle` 顶部 `#define QUICK_SDK`（默认注释关闭） |
| `LOGIN_FAILED_DEBUG` | 自定义：模拟登录失败 | `UserSystem.cs` 顶部（默认注释关闭），用于调试登录失败分支 |
| `SIMULATION_MODE_SDK` | 自定义：模拟 SDK 模式 | `NativeSDK` 等实现逻辑开关（见 §12） |

典型组合示例：

```csharp
#if UNITY_EDITOR
    // 仅编辑器执行的模拟逻辑
#elif UNITY_ANDROID
    // Android 真机逻辑
#elif UNITY_IOS
    // iOS 真机逻辑
#endif
```

> 参考 `PlatformInterfaces.GetDeviceUID()` / `AndroidInterfaces`：真机平台代码都被 `UNITY_ANDROID && !UNITY_EDITOR` 这样的组合宏包裹，保证编辑器下安全编译。

---

## 14. SDK 渠道接入方法

接入一个新渠道的完整步骤：

### 14.1 实现 `ISDK`

```csharp
using GGFramework.GGUser;

public class MySDK : ISDK
{
    public MySDK()
    {
        // 注册原生/渠道回调到统一入口
        NativeLuaBridge.onNativeCallUnity += HandleSDKCallback;
    }

    public void HandleSDKCallback(string methodName, string jsonString)
    {
        switch (methodName)
        {
            case "InitSuccess":   UserSystem.Instance.CallSDKInitSuccess(""); break;
            case "LoginSuccess":
                // 从 jsonString 解析 accountid / token 等
                User user = new User();
                user.uid = "uid_from_sdk";
                user.token = "...";
                user.channel = UserSystem.Instance.Channel;
                UserSystem.Instance.CallSDKLoginSuccess(user);
                break;
            case "PaySuccess":   UserSystem.Instance.CallSDKPaySuccess(""); break;
            case "PayFailed":    UserSystem.Instance.CallSDKPayFailed("");  break;
            case "PayCancel":    UserSystem.Instance.CallSDKPayCancel("");  break;
        }
    }

    public void Init()  { /* 渠道初始化；编辑器可走 NativeLuaBridge 或直接模拟 */ }
    public void Login() { /* 渠道登录 */ }
    public void Logout(){ /* 渠道登出 */ }
    public void Quit()  { /* 渠道退出 */ }
    public void Pay(Order order, UserSystem.UserData userData = null)
    {
        // 用 order 的各字段拼装渠道支付参数
    }
    public void SubmitData(string jsonData) { /* 上报角色数据 */ }
}
```

### 14.2 注册到工厂

在 `SDKFactory.CreateSDK` 对应 `case` 里启用实现：

```csharp
public static ISDK CreateSDK(SDKTarget target)
{
    ISDK sdk = null;
    switch (target)
    {
        case SDKTarget.QuickSDK:
            sdk = new QuickSDKImpl();   // 取消注释 / 启用
            break;
        case SDKTarget.ZhiShangSDK:
            sdk = new ZhiShangSDK();    // 取消注释 / 启用
            break;
        case SDKTarget.NativeSDK:
            sdk = new NativeSDK();
            break;
    }
    return sdk;
}
```

### 14.3 切换目标渠道

`UserSystem.Awake` 决定默认使用的 SDK：

```csharp
private void Awake()
{
    // 按当前渠道 / 宏选择目标
    sdk = SDKFactory.CreateSDK(SDKFactory.SDKTarget.NativeSDK);
}
```

将 `SDKTarget.NativeSDK` 改为你要接的渠道即可切换全局实现（或按平台宏 `#if UNITY_ANDROID ... #elif UNITY_IOS ...` 区分）。

> 接入要点：
> - 渠道**回调最终都必须映射到 `UserSystem.CallSDK*` 系列**，框架才能统一驱动状态机与事件。
> - 支付回调里解析到的订单号建议与 `Order.id` 对应，便于对账。
> - 编辑器调试优先用 `SIMULATION_MODE_SDK` 模拟，避免依赖原生环境。

---

## 15. 第三方依赖

| 依赖 | 位置 | 用途 |
| ---- | ---- | ---- |
| **MonsterLove/StateMachine** | `Assets/LocalPackages` | 状态机（`using MonsterLove.StateMachine;`） |
| **SimpleJson** | `Assets/LocalPackages` | JSON 序列化（`using SimpleJson;`，`JsonObject`） |

> 二者为编译硬依赖。接入时把对应 `.cs` 一并放入工程。
