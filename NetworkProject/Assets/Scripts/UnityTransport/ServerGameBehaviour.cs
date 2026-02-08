using System.Text;
using TMPro;
using Unity.Collections;
using Unity.Networking.Transport;
using UnityEngine;
using UnityEngine.UI;

public class ServerGameBehaviour : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI _textDisplay;
    [SerializeField] private TMP_InputField _message;
    [SerializeField] private Button _sendButton;


    NetworkDriver m_Driver;
    NativeList<NetworkConnection> m_Connections;

    private int _number;

    public void Host()
    {
        //_sendButton.onClick.AddListener(BroadcastMessage);

        m_Driver = NetworkDriver.Create();
        m_Connections = new NativeList<NetworkConnection>(16, Allocator.Persistent);

        var endpoint = NetworkEndpoint.AnyIpv4.WithPort(7777);
        if (m_Driver.Bind(endpoint) != 0)
        {
            Debug.LogError("Failed to bind to port 7777.");
            return;
        }

        m_Driver.Listen();
    }

    void OnDestroy()
    {
        if (m_Driver.IsCreated)
        {
            m_Driver.Dispose();
            m_Connections.Dispose();
        }
    }

    void Update()
    {
        if (!m_Driver.IsCreated) return;

        m_Driver.ScheduleUpdate().Complete();

        for (int i = 0; i < m_Connections.Length; i++)
        {
            if (!m_Connections[i].IsCreated)
            {
                m_Connections.RemoveAtSwapBack(i);
                i--;
            }
        }

        // Accept new connections.
        NetworkConnection c;
        while ((c = m_Driver.Accept()) != default)
        {
            m_Connections.Add(c);
            Debug.Log("Accepted a connection.");
        }

        for (int i = 0; i < m_Connections.Length; i++)
        {
            DataStreamReader stream;
            NetworkEvent.Type cmd;
            while ((cmd = m_Driver.PopEventForConnection(m_Connections[i], out stream)) != NetworkEvent.Type.Empty)
            {
                if (cmd == NetworkEvent.Type.Data)
                {
                    int length = stream.ReadInt();
                    byte[] buffer = new byte[length];
                    stream.ReadBytes(buffer);

                    string message = Encoding.UTF8.GetString(buffer);

                    Debug.Log($" Received the message : {buffer}");
                }

                else if (cmd == NetworkEvent.Type.Disconnect)
                {
                    Debug.Log("Client disconnected from the server.");
                    m_Connections[i] = default;
                    break;
                }
            }
        }

    }

    public void SendMessage(string message, int i)
    {
        byte[] bytes = Encoding.UTF8.GetBytes(message);

        m_Driver.BeginSend(NetworkPipeline.Null, m_Connections[i], out var writer);
        writer.WriteInt(bytes.Length);
        writer.WriteBytes(bytes);
        m_Driver.EndSend(writer);
    }

    public void BroadcastMessage()
    {
        string message = _message.text;
        byte[] bytes = Encoding.UTF8.GetBytes(message);

        foreach (var conn in m_Connections)
        {
            if (!conn.IsCreated) continue;

            m_Driver.BeginSend(conn, out DataStreamWriter writer);
            writer.WriteInt(bytes.Length);
            writer.WriteBytes(bytes);
            m_Driver.EndSend(writer);
        }
    }
}
