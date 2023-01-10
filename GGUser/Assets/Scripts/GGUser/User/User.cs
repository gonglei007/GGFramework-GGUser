using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GGFramework.GGUser
{
    public class User
    {

        public User()
        {
#if UNITY_EDITOR
            isDev = true;
#endif
        }

        public User(string uid, string token, string account, string channel)
        {
            SetUser(uid, token, account, channel);
#if UNITY_EDITOR
            isDev = true;
#endif
        }

        public void SetUser(string uid, string token, string account, string channel)
        {
            this.uid = uid;
            this.token = token;
            this.account = account;
            this.channel = channel;
        }

        public string uid = null;
        public string token = null;
        public string account = null;
        public string channel = null;
        public bool isDev = false;
    };
}
