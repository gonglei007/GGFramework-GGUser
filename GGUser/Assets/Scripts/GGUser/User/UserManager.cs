using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MonsterLove.StateMachine;
using SimpleJson;

namespace GGFramework.GGUser
{
    public class UserManager : Singleton<UserManager>
    {
        public enum States
        {
            None,
            Init,       // 初始化
            Authing,    // 账户验证中
            Logining,   // 账户登录中
            Logined,    // 账户已登录
        }

        private const bool AutoEnterGame = false;

        private int serverID = 0;
        private Action<JsonObject> loginCallback = null;
        private JsonObject loginResponse = null;
        private UserSystem.UserData userData = new UserSystem.UserData();
        private StateMachine<States> fsm;

        public UserSystem UserSystem
        {
            get
            {
                return UserSystem.Instance;
            }
        }

        private void Awake()
        {
            fsm = StateMachine<States>.Initialize(this, States.None);
        }
        public bool HasLogined()
        {
            return fsm.State == States.Logined;
        }

        public UserSystem.UserData GetUserData()
        {
            //Player.UpdateUserData(this.userData);
            //TODO: 
            return this.userData;
        }

        public void Init(string channel = null)
        {
            UserSystem.Instance.Init(channel);
            UserSystem.Instance.onEvent += (UserSystem.States state, UserSystem.Events @event, object param) =>
            {
                //StatisticSystem.Instance.SendEvent(@event.ToString());
                switch (@event)
                {
                    case UserSystem.Events.SDKLoginSuccess:
                        Debug.LogWarning("是否已经登录:" + UserSystem.Instance.HasLogined().ToString());
                        Debug.LogWarning("===>准备验证:" + param.ToString());// + "-状态:" + fsm.current);
                        if (AutoEnterGame)
                        {
                            fsm.ChangeState(States.Init);
                        }
                        break;
                    case UserSystem.Events.SDKInitFailed:
                    case UserSystem.Events.SDKLoginFailed:
                        UserSystem.Instance.UIAdaptor.ShowDialog("login_error", "login__error_description", true, (ret)=> {
                        });
                        break;
                    case UserSystem.Events.SDKPaySuccess:
                        break;
                    case UserSystem.Events.SDKPayFailed:
                    case UserSystem.Events.SDKPayCancel:
                        //GONetworkManager.Instance.PayCancel(param.ToString(), (SimpleJson.JsonObject ret) => { }); 
                        break;
                    case UserSystem.Events.SDKLogoutSuccess:
                        //if (GOGameManager.Instance != null && GOGameManager.Instance.GetCurrentState() == GOGameManager.State.InGame) {
                        //    UnityEngine.Application.Quit();
                        //}
                        break;
                }
            };
        }
        public void Login(int serverID, Action<JsonObject> callback)
        {
            this.serverID = serverID;
            loginCallback = callback;
            // 如果还没有登录SDK就先登录SDK
            if (!UserSystem.Instance.HasLogined())
            {
                UserSystem.Instance.Login();
            }
            else
            {
                // 如果已经登录了SDK就开始走游戏登录流程。
                fsm.ChangeState(States.Init);
            }
        }

        public void Pay(Order order)
        {
            //Player.UpdateUserData(userData);
            UserSystem.Instance.SDKPay(order, userData);
        }

        public void SubmitData(UserSystem.UserData.Type type)
        {
            if (userData != null)
            {
                userData.type = type;
                //Player.UpdateUserData(userData);
                Debug.Log("提交用户数据[" + type.ToString() + "]-" + userData.GetJsonString());
                UserSystem.Instance.SDK.SubmitData(userData.GetJsonString());
            }
        }

        private void Init_Enter()
        {
            Debug.LogWarning("===>UserManager.Init_Enter");
            // 先检查sdk是否已经成功登录
            fsm.ChangeState(States.Authing);
        }
        private void Authing_Enter()
        {
            //TODO: 让游戏服务器去跟第三方后台作用户验证。
            //GONetworkManager.Instance.GameAuth(UserSystem.Instance.UserInfo.uid, UserSystem.Instance.UserInfo.token, (JsonObject response) => {
            //    // 检查reponse结果。
            //    // 如果验证成功，则登录。
            //    if (response == null || !response.ContainsKey("code"))
            //    {
            //        OnException("认证失败", "服务器参数异常!");
            //    }
            //    else {
            //        int code = Convert.ToInt32(response["code"]);
            //        if (code == BaseNetworkManager.CODE_OK)
            //        {
            //            fsm.ChangeState(States.Logining);
            //        }
            //        else {
            //            if (UserSystem.Instance.onDialog != null)
            //            {
            //                UserSystem.Instance.onDialog("认证失败", "认证错误!-"+response["msg"], true, (bool ret) =>
            //                {
            //                    fsm.ChangeState(States.Init);
            //                });
            //            }
            //            else
            //            {
            //                fsm.ChangeState(States.None);
            //            }
            //        }
            //    }
            //});
        }
        private void Logining_Enter()
        {
            //TODO: 游戏服作登录操作。
            //GONetworkManager.Instance.GameLogin(UserSystem.Instance.UserInfo.uid, this.serverID, (JsonObject response) => {
            //    loginResponse = response;
            //    // 如果登录成功，则进入大厅。
            //    fsm.ChangeState(States.Logined);
            //});
        }
        private void Logined_Enter()
        {
            Debug.Log("游戏登录成功！马上准备进入游戏...");
            if (loginCallback != null)
            {
                loginCallback(loginResponse);
            }
            //StatisticSystem.Instance.Login();
            //CrashReport.Instance.Login();
        }

        private void OnException(string title, string message)
        {
            UserSystem.Instance.UIAdaptor.ShowDialog(title, message, true, (ret) => {
                fsm.ChangeState(States.Init);
            });
            //if (UserSystem.Instance.onDialog != null)
            //{
            //    UserSystem.Instance.onDialog(title, message, true, (bool ret) =>
            //    {
            //        fsm.ChangeState(States.Init);
            //    });
            //}
            //else
            //{
            //    fsm.ChangeState(States.None);
            //}
        }
    }
}
