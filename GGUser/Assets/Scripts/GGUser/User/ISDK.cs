using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GGFramework.GGUser
{
    public interface ISDK
    {
        void Init();
        void Login();
        void Logout();
        void Quit();
        void Pay(Order order, UserSystem.UserData userData = null);
        void SubmitData(string jsonData);
    }
}
