using UnityEngine;
using Unity.Networking.Transport;
using Unity.Collections;
using System.Text;
using TMPro;

public class Client : MonoBehaviour
{
    [SerializeField] private TMP_InputField _messageInput;
    [SerializeField] private TextMeshProUGUI _messageDisplayer;

    private NetworkDriver _driver;
    private NetworkConnection _connection;

    /// <summary>
    /// Joins local server.
    /// </summary>
    public void Join()
    {
        // creates driver and init connection
        _driver = NetworkDriver.Create();
        _connection = default;

        // gets endPoint and connect to driver from it
        NetworkEndpoint endpoint = NetworkEndpoint.Parse("127.0.0.1", 7777);
        _connection = _driver.Connect(endpoint);

        Debug.Log("Connection to server...");
    }

    void Update()
    {
        if (!_driver.IsCreated) return;

        // signals we are ready to process any updates
        _driver.ScheduleUpdate().Complete();

        if (!_connection.IsCreated) return;

        DataStreamReader stream;
        NetworkEvent.Type cmd;

        // while there are an event to process...
        while ((cmd = _connection.PopEvent(_driver, out stream)) != NetworkEvent.Type.Empty)
        {
            // if event is a connection
            if (cmd == NetworkEvent.Type.Connect)
            {
                Debug.Log("Connected to server");
            }
            // if event is data
            else if (cmd == NetworkEvent.Type.Data)
            {
                // TO DO : PIMP MY CHAT
                byte[] data = new byte[stream.Length];
                stream.ReadBytes(data);
                string message = Encoding.UTF8.GetString(data);
                Debug.Log("Chat: " + message);
                _messageDisplayer.text += message + "\n";
            }
            // if event is a disconnect
            else if (cmd == NetworkEvent.Type.Disconnect)
            {
                Debug.Log("Disconnected");
                _connection = default;
            }
        }
    }

    /// <summary>
    /// Sends message from input field to server.
    /// </summary>
    public void SendMessageToServer()
    {
        if (!_connection.IsCreated) return;

        // gets message from input field and encode it in bytes
        string message = _messageInput.text;
        byte[] data = Encoding.UTF8.GetBytes(message);

        // gets writer, writes in it the message in bytes and schedules it for sending
        _driver.BeginSend(_connection, out DataStreamWriter writer);
        writer.WriteBytes(data);
        _driver.EndSend(writer);
    }

    void OnDestroy()
    {
        if (_driver.IsCreated) _driver.Dispose();
    }
}
