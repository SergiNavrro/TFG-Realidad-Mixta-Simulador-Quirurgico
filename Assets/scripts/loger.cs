using TMPro;
using UnityEngine;

public class loger : MonoBehaviour
{
    public TextMeshProUGUI textoLog;
    private string log = "";

    void OnEnable() { Application.logMessageReceived += HandleLog; }
    void OnDisable() { Application.logMessageReceived -= HandleLog; }

    void HandleLog(string mensaje, string stack, LogType tipo)
    {
        string color = tipo == LogType.Error || tipo == LogType.Exception ? "red" :
                       tipo == LogType.Warning ? "yellow" : "white";
        log = $"<color={color}>{mensaje}</color>\n" + log;
        if (log.Length > 2000) log = log.Substring(0, 2000);
        if (textoLog != null) textoLog.text = log;
    }
}