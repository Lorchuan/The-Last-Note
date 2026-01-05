using UnityEngine;
using System.IO.Ports;

public class SerialTest : MonoBehaviour
{
    // 在 Inspector 里填入 COM15
    public string portName = "COM15";
    SerialPort sp;

    void Start()
    {
        Debug.Log($"[单线程测试] 准备连接 {portName}...");
        try
        {
            // 使用最保守的设置
            sp = new SerialPort(portName, 115200, Parity.None, 8, StopBits.One);
            sp.ReadTimeout = 500;
            sp.WriteTimeout = 500;

            // 关键：不要设 Dtr/Rts，不要设 Handshake

            sp.Open(); // 在主线程打开

            Debug.Log("<color=green>[测试成功] 端口已打开！驱动没问题了。</color>");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[测试失败] 依然报错: {e.Message}");
        }
    }

    void OnDestroy()
    {
        if (sp != null && sp.IsOpen) sp.Close();
    }
}