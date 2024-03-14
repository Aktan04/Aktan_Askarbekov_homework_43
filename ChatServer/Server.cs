using System.Net;
using System.Net.Sockets;

namespace Lesson43;

public class Server
{
    private TcpListener _listener = new TcpListener(IPAddress.Any, 11000);
    public Dictionary<string, Client> _clients = new Dictionary<string, Client>();

    public void AddClient(Client client)
    {
        _clients.Add(client.Id, client);
    }
    
    public async Task JoinMessage(string message, string id)
    {
        foreach (var (_, client) in _clients)
        {
            if (id != client.Id)
            {
                await client.Writer.WriteLineAsync(message);
                await client.Writer.FlushAsync();
            }
        }
    }
    
    public async Task BroadcastMessage(string message, string senderId)
    {
        var parts = message.Split(':');
        if (parts.Length != 2)
        {
            Console.WriteLine("Invalid message format");
            return;
        }
        var recipients = parts[0].Split(',');
        var actualMessage = parts[1].Trim(); 
        foreach (var recipientName in recipients)
        {
            var recipient = _clients.Values.FirstOrDefault(client => client.UserName == recipientName.Trim());
            if (recipient != null)
            {
                var formattedMessage = $"{_clients[senderId].UserName}({DateTime.Now:HH:mm}): {actualMessage}"; 
                await recipient.Writer.WriteLineAsync(formattedMessage);
                await recipient.Writer.FlushAsync();
            }
            else
            {
                Console.WriteLine($"User '{recipientName.Trim()}' is not logged in.");
            }
        }
    }
    
    public async Task ProcessAsync()
    {
        try
        {
            _listener.Start();
            Console.WriteLine("Server is running, waiting for connections");
            while (true)
            {
                TcpClient tcpClient = await _listener.AcceptTcpClientAsync();
                Client client = new Client(tcpClient, this);
                Task.Run(async () =>
                {
                    try
                    {
                        string? userName;
                        bool loggedIn = false;
                        do
                        {
                            userName = await client.Reader.ReadLineAsync();
                            if (IsUserLoggedIn(userName))
                            {
                                await client.Writer.WriteLineAsync("User already logged in. Please choose a different name.");
                                await client.Writer.FlushAsync();
                            }
                            else
                            {
                                loggedIn = true;
                            }
                        } while (!loggedIn);
                        client.UserName = userName;
                        await SendLoggedInUsersList(client);
                        string joinMessage = $"{userName} join the chat";
                        Console.WriteLine(joinMessage);
                        await JoinMessage(joinMessage, client.Id);
                        while (true)
                        {
                            string? message = await client.Reader.ReadLineAsync();
                            if (string.IsNullOrEmpty(message))
                            {
                                continue;
                            }
                            Console.WriteLine($"{client.UserName}({DateTime.Now:HH:mm}) enter - {message}"); 
                            await BroadcastMessage(message, client.Id);
                        }
                    }
                    catch (Exception e)
                    { 
                        Console.WriteLine(e.Message);
                    }
                });
            }
        }
        catch (Exception e)
        {
         DisconnectAll();
        }
    }
    
    private bool IsUserLoggedIn(string userName)
    {
        return _clients.Any(client => client.Value.UserName == userName);
    }

    private async Task SendLoggedInUsersList(Client client)
    {
        var loggedInUsers = _clients.Values.Select(c => c.UserName);
        var message = "Logged in users: " + string.Join(", ", loggedInUsers);
        await client.Writer.WriteLineAsync(message);
        await client.Writer.FlushAsync();
    }
    
    private void DisconnectAll()
    {
        foreach (var (_, client) in _clients)
        {
            client.Close();
        }
        _listener.Stop();
    }
}