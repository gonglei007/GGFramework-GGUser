using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using SimpleJson;

namespace GGFramework.GGUser
{
    public class Order
    {
        public Order()
        {
#if UNITY_EDITOR
            isDev = true;
#endif
        }

        public string id = null;        // 订单ID
        public string productID = null; // 商品ID
        public string name = "商品x";      // 商品名称
        public string desc = "";      // 商品描述
        public float price = 1.0f;      // 商品价格
        public int amount = 1;          // 购买商品数量
        public int number = 1;          // 此商品的资源（元宝）数量
        public string url = "";       // 回调接口
        public string extra = "";     // 扩展数据

        public bool isDev = false;      // 是否为开发版

        public string GetJsonString()
        {
            JsonObject jsonObject = new JsonObject();
            jsonObject["id"] = this.id;
            jsonObject["productID"] = this.productID;
            jsonObject["name"] = this.name;
            jsonObject["desc"] = this.desc;
            jsonObject["price"] = this.price;
            jsonObject["amount"] = this.amount;
            jsonObject["number"] = this.number;
            jsonObject["url"] = this.url;
            jsonObject["extra"] = this.extra;
            return jsonObject.ToString();
        }
    };
}
