using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using GGFramework.GGUser;

/// <summary>
/// GGPlatform 聚合平台演示：运行时以代码构建 uGUI 面板，串起
/// 「初始化 → 登录换平台 token → 支付（路由/校验/发货）→ 启动补单 → 恢复购买」全链路。
/// 默认使用 SimulationChannel + 模拟网关（注入 onPost），编辑器内可直接点击跑通。
/// 场景只需挂载本脚本，UI 与 EventSystem 会在运行时自动创建。
/// </summary>
public class Demo : MonoBehaviour
{
    private Text logText;
    private readonly List<string> logLines = new List<string>();
    private const int MaxLogLines = 40;

    private void Start()
    {
        BuildUI();
        InitPlatform();
    }

    // ---------------- 平台流程 ----------------

    private void InitPlatform()
    {
        GGPlatform gg = GGPlatform.Instance;
        // 使用默认配置（仅 simulation 渠道）。真实项目可改用 PlatformConfig.FromJson(远程配置)。
        gg.Init(PlatformConfig.Default());

        // 注入网关 HTTP 传输（框架不绑定网络库）。此处用模拟实现：直接判定校验通过。
        gg.Gateway.onPost = (url, body, onResponse) =>
        {
            Log("<color=#8888ff>[网关]</color> POST " + url);
            Log("<color=#8888ff>[网关]</color> body=" + body);
            if (url.Contains("verify"))
            {
                onResponse("{\"delivered\":true,\"orderId\":\"srv_" + DateTime.Now.Ticks + "\",\"message\":\"模拟发货成功\"}");
            }
            else
            {
                onResponse("{\"platformUid\":\"platform_uid\",\"platformToken\":\"platform_token\"}");
            }
        };

        Log("<color=#40c040>平台已初始化</color>（simulation 渠道 + 模拟网关）");
    }

    private void DoLogin()
    {
        Log("→ 登录（渠道登录 → 网关换平台 token）...");
        GGPlatform.Instance.Login(result =>
        {
            if (result == null) { Log("<color=#ff4040>登录无返回</color>"); return; }
            if (result.success)
                Log("<color=#40c040>登录成功</color> 渠道=" + result.channel
                    + " 渠道UID=" + result.channelUid
                    + " 平台UID=" + result.platformUid
                    + " 平台Token=" + result.platformToken);
            else
                Log("<color=#ff4040>登录失败：</color>" + result.message);
        });
    }

    private void DoPay()
    {
        GGPlatform gg = GGPlatform.Instance;
        PayRequest req = new PayRequest
        {
            unifiedProductId = "gem_60",
            developerOrderId = "order_" + DateTime.Now.Ticks,
            userId = "u_demo",
        };
        PayContext ctx = gg.CreatePayContext("gem_60", PayProductType.Consumable);

        Log("→ 支付 gem_60（订单 " + req.developerOrderId + "）：路由 → 购买 → 网关校验 → 发货后 finish");
        gg.Pay(req, ctx, outcome =>
        {
            if (outcome == null) { Log("<color=#ff4040>支付无返回</color>"); return; }
            if (outcome.success)
            {
                Log("<color=#40c040>支付并发货成功</color> 渠道=" + outcome.purchase.channel
                    + " 事务=" + outcome.purchase.storeTransactionId
                    + " 网关订单=" + (outcome.verify != null ? outcome.verify.orderId : "-"));
            }
            else
            {
                Log("<color=#ffaa00>支付未完成：</color>" + outcome.message
                    + (outcome.purchase != null ? "（事务 " + outcome.purchase.storeTransactionId + " 已保留，等待补单）" : ""));
            }
        });
    }

    private void DoProcessUnfinished()
    {
        Log("→ 启动补单（拉取未完成事务 → 校验 → 发货 → finish）...");
        GGPlatform.Instance.ProcessUnfinished("simulation", outcome =>
        {
            if (outcome != null && outcome.success)
                Log("<color=#40c040>补单成功</color> 事务=" + outcome.purchase.storeTransactionId);
        });
        Log("补单检查完成（simulation 渠道无挂起事务时为空）。");
    }

    private void DoRestore()
    {
        Log("→ 恢复购买...");
        GGPlatform.Instance.Restore("simulation", list =>
        {
            int count = list != null ? list.Count : 0;
            Log("恢复购买返回 " + count + " 条记录。");
        });
    }

    // ---------------- 日志 ----------------

    private void Log(string line)
    {
        Debug.Log("[Demo] " + line);
        logLines.Add(line);
        if (logLines.Count > MaxLogLines) logLines.RemoveAt(0);
        if (logText != null) logText.text = string.Join("\n", logLines.ToArray());
    }

    // ---------------- 运行时构建 uGUI ----------------

    private Font uiFont;

    private void BuildUI()
    {
        uiFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (uiFont == null) uiFont = Resources.GetBuiltinResource<Font>("Arial.ttf");

        EnsureEventSystem();

        Canvas canvas = CreateCanvas();
        CreateTitle(canvas.transform);
        CreateButtons(canvas.transform);
        CreateLogArea(canvas.transform);
    }

    private static void EnsureEventSystem()
    {
        if (UnityEngine.EventSystems.EventSystem.current == null
            && FindObjectOfType<UnityEngine.EventSystems.EventSystem>() == null)
        {
            GameObject es = new GameObject("EventSystem");
            es.AddComponent<UnityEngine.EventSystems.EventSystem>();
            es.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
        }
    }

    private static Canvas CreateCanvas()
    {
        GameObject go = new GameObject("DemoCanvas");
        Canvas canvas = go.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        CanvasScaler scaler = go.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1280, 720);
        scaler.matchWidthOrHeight = 0.5f;
        go.AddComponent<GraphicRaycaster>();
        return canvas;
    }

    private void CreateTitle(Transform parent)
    {
        GameObject go = new GameObject("Title");
        go.transform.SetParent(parent, false);
        Text text = go.AddComponent<Text>();
        text.font = uiFont;
        text.text = "GGPlatform 聚合平台演示";
        text.fontSize = 30;
        text.alignment = TextAnchor.MiddleLeft;
        text.color = Color.white;
        RectTransform rt = text.rectTransform;
        rt.anchorMin = new Vector2(0, 1);
        rt.anchorMax = new Vector2(1, 1);
        rt.pivot = new Vector2(0.5f, 1);
        rt.anchoredPosition = new Vector2(0, -10);
        rt.sizeDelta = new Vector2(0, 44);
    }

    private void CreateButtons(Transform parent)
    {
        string[] labels = { "初始化平台", "登录", "支付 gem_60", "启动补单", "恢复购买", "清空日志" };
        Action[] actions = { InitPlatform, DoLogin, DoPay, DoProcessUnfinished, DoRestore, ClearLog };

        for (int i = 0; i < labels.Length; i++)
        {
            Action captured = actions[i];
            CreateButton(parent, labels[i], i, () => captured());
        }
    }

    private void ClearLog()
    {
        logLines.Clear();
        if (logText != null) logText.text = "";
    }

    private void CreateButton(Transform parent, string label, int index, Action onClick)
    {
        GameObject go = new GameObject("Btn_" + label);
        go.transform.SetParent(parent, false);

        Image img = go.AddComponent<Image>();
        img.color = new Color(0.22f, 0.42f, 0.72f, 1f);
        Button button = go.AddComponent<Button>();
        ColorBlock colors = button.colors;
        colors.highlightedColor = new Color(0.30f, 0.52f, 0.85f, 1f);
        colors.pressedColor = new Color(0.16f, 0.32f, 0.58f, 1f);
        button.colors = colors;
        button.onClick.AddListener(() => onClick());

        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0, 1);
        rt.anchorMax = new Vector2(0, 1);
        rt.pivot = new Vector2(0, 1);
        rt.sizeDelta = new Vector2(220, 46);
        rt.anchoredPosition = new Vector2(20, -70 - index * 56);

        GameObject txtGo = new GameObject("Text");
        txtGo.transform.SetParent(go.transform, false);
        Text text = txtGo.AddComponent<Text>();
        text.font = uiFont;
        text.text = label;
        text.fontSize = 22;
        text.alignment = TextAnchor.MiddleCenter;
        text.color = Color.white;
        RectTransform trt = text.rectTransform;
        trt.anchorMin = Vector2.zero;
        trt.anchorMax = Vector2.one;
        trt.offsetMin = Vector2.zero;
        trt.offsetMax = Vector2.zero;
    }

    private void CreateLogArea(Transform parent)
    {
        GameObject bg = new GameObject("LogArea");
        bg.transform.SetParent(parent, false);
        Image bgImg = bg.AddComponent<Image>();
        bgImg.color = new Color(0f, 0f, 0f, 0.55f);
        RectTransform bgRt = bg.GetComponent<RectTransform>();
        bgRt.anchorMin = new Vector2(0, 0);
        bgRt.anchorMax = new Vector2(1, 1);
        bgRt.offsetMin = new Vector2(260, 20);
        bgRt.offsetMax = new Vector2(-20, -60);

        GameObject txtGo = new GameObject("LogText");
        txtGo.transform.SetParent(bg.transform, false);
        logText = txtGo.AddComponent<Text>();
        logText.font = uiFont;
        logText.fontSize = 20;
        logText.alignment = TextAnchor.UpperLeft;
        logText.color = new Color(0.9f, 0.9f, 0.9f, 1f);
        logText.horizontalOverflow = HorizontalWrapMode.Wrap;
        logText.verticalOverflow = VerticalWrapMode.Truncate;
        logText.supportRichText = true;
        RectTransform trt = logText.rectTransform;
        trt.anchorMin = Vector2.zero;
        trt.anchorMax = Vector2.one;
        trt.offsetMin = new Vector2(12, 12);
        trt.offsetMax = new Vector2(-12, -12);
    }
}
