using System.Net.Sockets;
using System.Text;

namespace Lesson43;

public class Client
{
    public string Id { get; } = Guid.NewGuid().ToString();
    public StreamReader Reader { get; }
    public StreamWriter Writer { get; }
    public string UserName { get; set; }

    private TcpClient _client;
    private Server _server;

    public Client(TcpClient client, Server server)
    {
        _client = client;
        _server = server;
        var stream = client.GetStream();
        Writer = new StreamWriter(stream, Encoding.Unicode);
        Reader = new StreamReader(stream, Encoding.Unicode);
        server.AddClient(this);
    }
    public void Close()
    {
        Writer.Close();
        Reader.Close();
        _client.Close();
    }
}