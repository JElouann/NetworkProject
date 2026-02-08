using System.Text;
using TMPro;
using Unity.Collections;
using Unity.Networking.Transport;
using UnityEngine;

public class ChatClient : MonoBehaviour
{
    private NetworkDriver driver;
    private NetworkConnection connection;
    [SerializeField] private TMP_InputField _message;


    public void Join()
    {
        driver = NetworkDriver.Create();
        var endpoint = NetworkEndpoint.Parse("127.0.0.1", 7777);
        connection = driver.Connect(endpoint);
    }

    void OnDestroy()
    {
        driver.Dispose();
    }

    void Update()
    {
        if (!driver.IsCreated) return;

        driver.ScheduleUpdate().Complete();

        if (!connection.IsCreated)
            return;

        DataStreamReader stream;
        NetworkEvent.Type cmd;

        while ((cmd = connection.PopEvent(driver, out stream)) != NetworkEvent.Type.Empty)
        {
            if (cmd == NetworkEvent.Type.Data)
            {
                int length = stream.ReadInt();
                byte[] buffer = new byte[length];
                stream.ReadBytes(buffer);

                string message = Encoding.UTF8.GetString(buffer);
                Debug.Log("CHAT: " + message);

                // Update UI ici
            }
        }
    }

    public void SendMessage()
    {
        byte[] bytes = Encoding.UTF8.GetBytes(_message.text);

        driver.BeginSend(connection, out DataStreamWriter writer);
        writer.WriteInt(bytes.Length);
        writer.WriteBytes(bytes);
        driver.EndSend(writer);
    }
}
