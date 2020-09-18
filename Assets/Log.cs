using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class Log : MonoBehaviour
{
    public static Log instance;

    public void Awake() {
        if (instance == null) instance = this;
    }

    //----

    public TextMeshProUGUI text;

    public static void Write(string entry) {
        instance.WriteToLog(entry);
    }

    public void WriteToLog(string entry) {
        text.text = text.text + "\n >" + entry;
    }
}
