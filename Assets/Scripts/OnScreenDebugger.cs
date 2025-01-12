using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.VFX;

public class OnScreenDebugger : MonoBehaviour
{
    [SerializeField] private bool enableDebugger = false;
    [SerializeField] private TMP_Text debugger;

    private int count = 0;

    private void Awake()
    {
        debugger.gameObject.SetActive(enableDebugger);
    }


    public void UpdateDebugger(string text)
    {
        if (enableDebugger)
        {
            debugger.text = count + " | " + text + "\n" + debugger.text ;
            count++;
        }
    }

}
