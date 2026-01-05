using UnityEngine;
using System.Net.Sockets;
using System.Text;
using System.Threading;

public class SocketClient : MonoBehaviour
{
    [Header("Socket设置")]
    public string serverIP = "127.0.0.1";
    public int serverPort = 5000;

    private TcpClient client;
    private NetworkStream stream;
    private bool isConnected = false;

    void Start()
    {
        // 启动时连接到服务器
        ConnectToServer();
    }

    void Update()
    {
        // 检测空格键按下
        if (Input.GetKeyDown(KeyCode.Space))
        {
            SendMessageToServer("Unity客户端: 空格键被按下了!");
        }

        // 检测R键重新连接
        if (Input.GetKeyDown(KeyCode.R))
        {
            if (!isConnected)
            {
                ConnectToServer();
            }
        }
    }

    void ConnectToServer()
    {
        try
        {
            // 创建TCP客户端并连接
            client = new TcpClient();
            client.Connect(serverIP, serverPort);
            stream = client.GetStream();
            isConnected = true;

            Debug.Log("成功连接到Python服务器: " + serverIP + ":" + serverPort);
        }
        catch (System.Exception e)
        {
            Debug.LogError("连接服务器失败: " + e.Message);
            isConnected = false;
        }
    }

    void SendMessageToServer(string message)
    {
        if (!isConnected || stream == null)
        {
            Debug.LogWarning("未连接到服务器，无法发送消息");
            return;
        }

        try
        {
            // 将消息转换为字节数组
            byte[] data = Encoding.UTF8.GetBytes(message);

            // 发送消息
            stream.Write(data, 0, data.Length);
            Debug.Log("发送消息: " + message);
        }
        catch (System.Exception e)
        {
            Debug.LogError("发送消息失败: " + e.Message);
            isConnected = false;
        }
    }

    void OnApplicationQuit()
    {
        // 应用退出时关闭连接
        CloseConnection();
    }

    void CloseConnection()
    {
        if (stream != null)
        {
            stream.Close();
            stream = null;
        }

        if (client != null)
        {
            client.Close();
            client = null;
        }

        isConnected = false;
        Debug.Log("已断开与服务器的连接");
    }
}
