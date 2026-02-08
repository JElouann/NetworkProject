using System.Text;
using TMPro;
using Unity.Collections;
using Unity.Networking.Transport;
using UnityEngine;

public class ChatServer : MonoBehaviour
{
    private NetworkDriver driver;
    private NativeList<NetworkConnection> connections;
    [SerializeField] private TMP_InputField _message;


    public void Host()
    {
        driver = NetworkDriver.Create();
        connections = new NativeList<NetworkConnection>(16, Allocator.Persistent);

        var endpoint = NetworkEndpoint.AnyIpv4;
        endpoint.Port = 7777;

        if (driver.Bind(endpoint) != 0)
            Debug.LogError("Impossible de bind le port");
        else
            driver.Listen();
    }

    void OnDestroy()
    {
        driver.Dispose();
        connections.Dispose();
    }

    void Update()
    {
        if (!driver.IsCreated) return;

        driver.ScheduleUpdate().Complete();

        // Nettoyage connexions mortes
        for (int i = 0; i < connections.Length; i++)
        {
            if (!connections[i].IsCreated)
            {
                connections.RemoveAtSwapBack(i);
                i--;
            }
        }

        // Accept nouveaux clients
        NetworkConnection c;
        while ((c = driver.Accept()) != default)
        {
            connections.Add(c);
            Debug.Log("Client connecté");
        }

        // Lecture messages
        for (int i = 0; i < connections.Length; i++)
        {
            DataStreamReader stream;
            NetworkEvent.Type cmd;

            while ((cmd = driver.PopEventForConnection(connections[i], out stream)) != NetworkEvent.Type.Empty)
            {
                if (cmd == NetworkEvent.Type.Data)
                {
                    int length = stream.ReadInt();
                    byte[] buffer = new byte[length];
                    stream.ReadBytes(buffer);

                    string message = Encoding.UTF8.GetString(buffer);
                    Debug.Log("Reçu : " + message);

                    BroadcastMessage(message);
                }
                else if (cmd == NetworkEvent.Type.Disconnect)
                {
                    connections[i] = default;
                }
            }
        }
    }

    public void BroadcastMessage()
    {
        byte[] bytes = Encoding.UTF8.GetBytes(_message.text);

        foreach (var conn in connections)
        {
            if (!conn.IsCreated) continue;

            driver.BeginSend(conn, out DataStreamWriter writer);
            writer.WriteInt(bytes.Length);
            writer.WriteBytes(bytes);
            driver.EndSend(writer);
        }
    }
}
