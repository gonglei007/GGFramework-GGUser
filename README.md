<p align="center">
  <h1 align="center">GGUser (内测版)</h1>
  <p align="center">
    <a href="https://github.com/gonglei007/GGFramework-GGUser/watchers" target="_blank"><img src="https://img.shields.io/github/watchers/gonglei007/GGFramework-GGUser.svg" style="display: inherit;"/></a>
    <a href="https://github.com/gonglei007/GGFramework-GGUser/stargazers" target="_blank"><img src="https://img.shields.io/github/stars/gonglei007/GGFramework-GGUser.svg" style="display: inherit;"/></a>
    <a href="https://github.com/gonglei007/GGFramework-GGUser/network/members" target="_blank"><img src="https://img.shields.io/github/forks/gonglei007/GGFramework-GGUser.svg" style="display: inherit;"/></a>
    <img src="https://img.shields.io/github/repo-size/gonglei007/GGFramework-GGUser.svg" style="display: inherit;"/>
  </p>
  <p align="center">一个游戏客户端的用户 / 登录 / 支付 SDK 管理框架</p>
</p>

## 简介

GGUser 是一个面向游戏客户端的**用户系统与 SDK 接入管理框架**，用于统一对接各个渠道的账号登录、支付、数据上报等能力。它通过抽象 SDK 接口 + 工厂 + 事件回调，将具体渠道实现与游戏业务解耦，支持方便地切换和接入多个渠道 SDK。

## 功能与特性

**用户与 SDK 管理**

- **用户系统**（`UserSystem`）：驱动 SDK 初始化、登录、支付全流程，以内置状态机管理各阶段（初始化 / 登录成功等）
- **业务门面**（`UserManager`）：在用户系统之上提供 `Init` / `Login` / `Pay` / `SubmitData` / `HasLogined` 等高层 API

**多渠道 SDK 接入（策略模式）**

- `ISDK` 抽象接口 + `SDKFactory` 工厂，按 `SDKTarget` 创建对应渠道实现
- 预置实现：**QuickSDK**、**智尚 SDK**、**NativeSDK**（默认）、**InternalSDK**
- 编辑器内通过 `SIMULATION_MODE_SDK` 宏启用模拟 SDK，无需真机能跑通登录 / 支付流程

**与原生平台桥接**

- `NativeLuaBridge`：Unity（含 Lua）与 Android / iOS 原生代码的双向桥接
  - Android：`AndroidJavaClass` 静态调用原生 SDK 方法
  - iOS：`[DllImport("__Internal")]` 声明原生 C 接口
- 平台接口：设备 ID / UID 获取封装

**可插拔解耦**

- `UIAdaptor`：通过委托注入弹窗 / 本地化 / 等待等 UI 能力，不绑定具体 UI 框架
- `LogAdaptor`：通过委托注入错误 / 网络异常上报
- 事件驱动：以 `Events` 枚举 + 回调对外广播登录、支付等生命周期事件

**数据模型**

- `UserData` / `Order` / `User` 等数据模型自带 JSON 序列化，便于打包上报与回调解析

## 工程内容

<table>
    <tr><th>目录</th><th>内容</th><th>说明</th></tr>
    <tr>
        <td>GGUser/Assets/Scripts/GGUser</td>
        <td>框架代码</td>
        <td>可以直接复制到目标工程中使用。</td>
    </tr>
    <tr>
        <td>GGUser/Assets/Demo</td>
        <td>演示工程</td>
        <td>演示最小初始化用法。</td>
    </tr>
</table>

## 核心模块

| 模块 | 说明 |
| ---- | ---- |
| `Systems/UserSystem` | 核心用户系统（单例）：SDK 初始化 / 登录 / 支付流程与事件广播 |
| `User/UserManager` | 高层业务门面（单例）：认证 / 登录 / 支付 / 数据上报 API |
| `User/ISDK`、`User/SDKFactory` | SDK 抽象接口与工厂（策略模式） |
| `User/QuickSDK`、`ZhiShangSDK`、`NativeSDK`、`InternalSDK` | 各渠道 SDK 实现 |
| `Platform/NativeLuaBridge`、`PlatformInterfaces`、`AndroidInterfaces` | 原生平台桥接 |

## 依赖

* **MonsterLove/StateMachine** — Unity 轻量状态机库（位于 `Assets/LocalPackages`）
* **SimpleJson** — 轻量 JSON 序列化库（位于 `Assets/LocalPackages`）

## 设计模式

* **策略模式** — `ISDK` 接口 + 多实现，按渠道切换
* **简单工厂** — `SDKFactory.CreateSDK(target)`
* **单例** — `UserManager` / `UserSystem` / `NativeLuaBridge`
* **状态机** — 内置 `StateMachine<States>` 驱动登录 / 支付流程
* **适配器 / 依赖倒置** — `UIAdaptor` / `LogAdaptor` 委托注入
* **桥接** — `NativeLuaBridge` 实现 Unity ↔ 原生双向通信

## 文档

* 使用文档(TODO)

## 进群沟通

| | | |
| --- | -------- | -------- |
| QQ群: | 242500383 | [![GLTOP游戏研发与技术1群](https://pub.idqqimg.com/wpa/images/group.png)](https://qm.qq.com/cgi-bin/qm/qr?k=fy4Z65nE-5Jd1ay8FkJpDc9iPJyW3d38&jump_from=webapi) |

## TODO-List

* 完善 Login 环节的「游戏服认证 / 登录」对接（当前为预留位，可与 GGNetwork 组合）
* 更完整的 Demo 演示 与 一键对接说明

## 更多资料

* [游戏开发图谱](https://github.com/gonglei007/GameDevMind)
