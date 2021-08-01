using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class RunOnce : MonoBehaviour
{

    public static RunOnce Instance;

    private static bool hasProcessedQuit = false;

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; } // stops dups running
        DontDestroyOnLoad(gameObject); // keep me forever
        Instance = this; // set the reference to it

        //... code to run only once...
    }

    bool WantsToQuit()
    {
        if (hasProcessedQuit) return true;

        StartCoroutine(GameStateManager.DeleteRoom(() =>
        {
            hasProcessedQuit = true;
            Application.Quit();
        }));

        return false;
    }

    [RuntimeInitializeOnLoadMethod]
    void RunOnStart()
    {
        Application.wantsToQuit += WantsToQuit;
    }

}
