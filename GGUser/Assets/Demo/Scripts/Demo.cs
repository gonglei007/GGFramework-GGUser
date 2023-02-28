using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using GGFramework.GGUser;

public class Demo : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        UserManager.Instance.Init("demo_channel");
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    // return list result.
    void Test()
    {
        // 
    }
}
