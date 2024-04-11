using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;

namespace Application
{
    public class Message
    {
        public string? Data { get; set; }
        public string? Sender { get; set; }
        public DateTime? Time { get; set; }
    }

    public class Program
    {
        public static async Task Main(string[] args) {
            /*#####################################
             * Creates a client socket and connects it to an IPEndPoint host
             * Returns the client socket as Task<Socket>
             ######################################*/
            static async Task<Socket> CreateSocket(string host, int port)
            {
                IPHostEntry ipHostInfo = await Dns.GetHostEntryAsync(host);
                IPAddress ipAddress = ipHostInfo.AddressList[0];
                IPEndPoint ipEndPoint = new(ipAddress, port);

                Socket client = new(
                    ipEndPoint.AddressFamily,
                    SocketType.Stream,
                    ProtocolType.Tcp);

                await client.ConnectAsync(ipEndPoint);

                return client;
            }

            /*####################################
             * Sends a UTF8 byte encoded string through socket
             * Expects <|ACK|> as response
             #####################################*/
            static async Task SendMessageString(Socket socket, string message)
            {
                while (true)
                {
                    //send a message
                    byte[] messageBytes = Encoding.UTF8.GetBytes($"{message}<|EOM|>");
                    await socket.SendAsync(messageBytes, SocketFlags.None);
                    Console.WriteLine($"Socket client sent message \"{message}\"");

                    //receive ACK / acknowledgment
                    var rcvBuff = new byte[1024];
                    var rcv = await socket.ReceiveAsync(rcvBuff, SocketFlags.None);
                    var rcvResp = Encoding.UTF8.GetString(rcvBuff, 0, rcv);
                    if (rcvResp == "<|ACK|>")
                    {
                        Console.WriteLine($"Socket client received acknowledgment \"{rcvResp}\"");
                        break;
                    }
                }
            }

            static async Task SendMessageMessage(Socket socket, Message message)
            {
                Console.WriteLine(JsonSerializer.Serialize(message));
                while (true)
                {
                    // Sends message json as bytes
                    byte[] messageBytes = JsonSerializer.SerializeToUtf8Bytes(message);
                    //byte[] messageBytes = Encoding.UTF8.GetBytes($"{message}<|EOM|>");
                    await socket.SendAsync(messageBytes, SocketFlags.None);
                    await socket.SendAsync(Encoding.UTF8.GetBytes("<|EOM|>"), SocketFlags.None);
                    Console.WriteLine($"Socket client sent message \"{Encoding.UTF8.GetString(messageBytes)}\"");

                    //receive ACK / acknowledgment
                    var rcvBuff = new byte[1024];
                    var rcv = await socket.ReceiveAsync(rcvBuff, SocketFlags.None);
                    var rcvResp = Encoding.UTF8.GetString(rcvBuff, 0, rcv);
                    if (rcvResp == "<|ACK|>")
                    {
                        Console.WriteLine($"Socket client received acknowledgment \"{rcvResp}\"");
                        break;
                    }
                }
            }

            /*##################################
             * Safely close and dispose of the socket
             ###################################*/
            static void CloseSocket(Socket socket)
            {
                socket.Shutdown(SocketShutdown.Both);
                socket.Close();
            }

            /*##################################
             * Gets user input from the console, line by line
             * Returns the input as string
             ###################################*/
            static string GetUserInput()
            {
                string? message = Console.ReadLine();
                if (message == null)
                {
                    Console.WriteLine("Please just write something...\n");
                    return GetUserInput();
                }
                return message;
            }

            Socket client = await CreateSocket(Dns.GetHostName(), 8080);
            var message = new Message
            {
                Data = "This is a json serialized message",
                Sender = "Mathias S Augdal",
                Time = DateTime.Now,
            };

            await SendMessageMessage(client, message);

            Console.WriteLine("\nPress enter to continue...\n");
                while (Console.ReadKey().Key != ConsoleKey.Enter) { }
                CloseSocket(client);
        }
    }
}