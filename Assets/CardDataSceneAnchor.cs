using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CardDataSceneAnchor : MonoBehaviour
{
    void Start()
    {
        DontDestroyOnLoad(this.gameObject);
        CardDataManager.Init();
        //CardDataManager.DeleteAllDownloadedData();
    }

    void Update()
    {
        CardDataManager.Update();
    }
}
