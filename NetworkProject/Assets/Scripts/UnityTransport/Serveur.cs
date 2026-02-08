using System.Text;
using TMPro;
using Unity.Collections;
using Unity.Networking.Transport;
using UnityEngine;

public class Server : MonoBehaviour
{
    [SerializeField] private TMP_InputField _messageInput;
    [SerializeField] private TextMeshProUGUI _messageDisplayer;

    private NetworkDriver driver;
    private NativeList<NetworkConnection> connections;

    public void StartHost()
    {
        driver = NetworkDriver.Create();
        connections = new NativeList<NetworkConnection>(16, Allocator.Persistent);

        NetworkEndpoint endpoint = NetworkEndpoint.AnyIpv4;
        endpoint.Port = 7777;

        if (driver.Bind(endpoint) != 0)
            Debug.Log("Impossible to bind the port");
        else
            driver.Listen();

        Debug.Log("Server launched on port 7777");
    }

    void Update()
    {
        if (!driver.IsCreated) return;

        driver.ScheduleUpdate().Complete();

        // Cleans expired connections
        for (int i = 0; i < connections.Length; i++)
        {
            if (!connections[i].IsCreated)
            {
                connections.RemoveAtSwapBack(i);
                i--;
            }
        }

        // Accepts in coming (waiting) connections
        NetworkConnection c;
        while ((c = driver.Accept()) != default)
        {
            connections.Add(c);
            Debug.Log("Client connected");
        }

        // For each connections, print message
        for (int i = 0; i < connections.Length; i++)
        {
            DataStreamReader stream;
            NetworkEvent.Type cmd;

            while ((cmd = driver.PopEventForConnection(connections[i], out stream)) != NetworkEvent.Type.Empty)
            {
                if (cmd == NetworkEvent.Type.Data)
                {
                    // Converts message to bytes array before printing it
                    byte[] data = new byte[stream.Length];
                    stream.ReadBytes(data);
                    string message = Encoding.UTF8.GetString(data);

                    Debug.Log("Client: " + message);
                    _messageDisplayer.text += message + "\n";
                }
                // Disconnects client
                else if (cmd == NetworkEvent.Type.Disconnect)
                {
                    Debug.Log("Client disconnected");
                    connections[i] = default;
                }
            }
        }
    }

    /// <summary>
    /// Sends the message from input field to every connected client.
    /// </summary>
    public void Broadcast()
    {
        // encodes message from input field to bytes array
        string message = _messageInput.text;
        byte[] data = Encoding.UTF8.GetBytes(message);

        print("Sending :" + message);

        // iterates over every connections
        foreach (var conn in connections)
        {
            if (!conn.IsCreated) continue;

            // gets writer, writes in it the message in bytes and schedules it for sending
            driver.BeginSend(conn, out DataStreamWriter writer);
            writer.WriteBytes(data);
            driver.EndSend(writer);
        }
    }

    void OnDestroy()
    {
        // Disposes driver and connections on destroy
        driver.Dispose();
        connections.Dispose();
    }
}