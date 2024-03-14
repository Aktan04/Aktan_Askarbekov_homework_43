using System.Net.Sockets;
using System.Text;

namespace ChatClient;

public class Client
{
    public async Task RunAsync()
    {
        string host;
        int port;
        bool isConnected = false;

        do
        {
            if (!InputServerDetails(out host, out port))
            {
                return; 
            }
            using TcpClient client = new TcpClient();
            try
            {
                client.Connect(host, port);
                var networkStream = client.GetStream();
                var reader = new StreamReader(networkStream, Encoding.Unicode);
                var writer = new StreamWriter(networkStream, Encoding.Unicode);
                Console.WriteLine("Enter your name:");
                string name = Console.ReadLine();
                Console.WriteLine($"{name} welcome to chat server");
                var receiveTask = ReceiveMessageAsync(reader);
                var sendTask = SendMessageAsync(writer, name);
                await Task.WhenAny(receiveTask, sendTask);
                isConnected = true; 
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                Console.WriteLine("Failed to connect to the server. Do you want to try again? (Y/N)");
                string response = Console.ReadLine().ToLower();
                if (response != "y")
                {
                    return;
                }
            }
            finally
            {
                client?.Close(); 
            }
        } while (!isConnected);

    }
      
    public bool InputServerDetails(out string host, out int port)
    {
        Console.WriteLine("Enter the IP address of the server:");
        host = Console.ReadLine();
        if (string.IsNullOrEmpty(host))
        {
            Console.WriteLine("Invalid IP address.");
            port = 0;
            return false;
        }

        Console.WriteLine("Enter the port number of the server:");
        if (!int.TryParse(Console.ReadLine(), out port))
        {
            Console.WriteLine("Invalid port number.");
            return false;
        }

        return true;
    }

    private async Task SendMessageAsync(StreamWriter writer, string userName)
    {
        await writer.WriteLineAsync(userName);
        await writer.FlushAsync();

        Console.WriteLine("Enter your message (format: <recipient1,recipient2,...>:<message>):");
        while (true)
        {
            string? input = Console.ReadLine();
            if (string.IsNullOrEmpty(input))
            {
                continue;
            }

            await writer.WriteLineAsync(input);
            await writer.FlushAsync();
        }
    }
    
    private async Task ReceiveMessageAsync(StreamReader reader)
    {
        while (true)
        {
            try
            {
                string? message = await reader.ReadLineAsync();
                if (string.IsNullOrEmpty(message))
                {
                    continue;       
                }

                Console.WriteLine(message);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
        }
    }  
}