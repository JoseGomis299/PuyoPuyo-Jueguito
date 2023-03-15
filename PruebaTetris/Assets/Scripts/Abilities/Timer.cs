using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Timer : MonoBehaviour
{
    
    public static Timer Instance { get; private set; }

    private void Awake()
    {
        Instance = this;
    }

    public void WaitForAction(Action action, float time)
    {
        StartCoroutine(PerformAction(action, time));
    }

    private IEnumerator PerformAction(Action action, float time)
    {
        var timeToStay = new WaitForSeconds(time);
        yield return timeToStay;

        action();
    }
    
}
