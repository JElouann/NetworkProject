using System.Text;
using TMPro;
using Unity.Networking.Transport;
using UnityEngine;
using UnityEngine.UI;

public class ClientGameBehaviour : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI _textDisplay;
    [SerializeField] private TMP_InputField _message;
    [SerializeField] private Button _sendButton;

    NetworkDriver m_Driver;
    NetworkConnection m_Connection;

    public void Join()
    {
        //_sendButton.onClick.AddListener(SendMessage);

        m_Driver = NetworkDriver.Create();

        var endpoint = NetworkEndpoint.Parse("127.0.0.1", 7777);
        m_Connection = m_Driver.Connect(endpoint);
    }

    void OnDestroy()
    {
        m_Driver.Dispose();
    }

    void Update()
    {
        if (!m_Driver.IsCreated) return;

        m_Driver.ScheduleUpdate().Complete();

        if (!m_Connection.IsCreated)
        {
            return;
        }

        Unity.Collections.DataStreamReader stream;
        NetworkEvent.Type cmd;
        while ((cmd = m_Connection.PopEvent(m_Driver, out stream)) != NetworkEvent.Type.Empty)
        {
            if (cmd == NetworkEvent.Type.Connect)
            {
                Debug.Log("We are now connected to the server.");
            }

            else if (cmd == NetworkEvent.Type.Data)
            {
                int length = stream.ReadInt();
                byte[] buffer = new byte[length];
                stream.ReadBytes(buffer);

                string message = Encoding.UTF8.GetString(buffer);

                Debug.Log($" Received the message : {buffer}");

            }

            else if (cmd == NetworkEvent.Type.Disconnect)
            {
                Debug.Log("Client got disconnected from server.");
                m_Connection = default;
            }
        }
    }

    public void SendMessage()
    {
        byte[] bytes = Encoding.UTF8.GetBytes(_message.text);

        m_Driver.BeginSend(m_Connection, out var writer);
        writer.WriteInt(bytes.Length);
        writer.WriteBytes(bytes);
        m_Driver.EndSend(writer);
    }
}
