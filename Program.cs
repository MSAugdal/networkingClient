namespace Networking
{
    using System.Net;
    using System.Net.Sockets;
    using System.Text;

    internal enum MessageState
    {
        EXIT,
        WRITE,
        SEND,
    }

    public class Program
    {
        public static async void Main(string[] args)
        {
            /*#####################################
             * Creates a client socket and connects it to an IPEndPoint host
             * Returns the client socket as Task<Socket>
             ######################################*/
            async Task<Socket> CreateSocket(string host, int port)
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
            async Task SendMessageString(Socket socket, string message)
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

            static void UserInputHandler(string userInput)
            {
                Console.WriteLine("handled");
            }


            Socket client = await CreateSocket(Dns.GetHostName(), 8080);
            string message = GetUserInput();

            await SendMessageString(client, $"{message}<|EOM|>");

            Console.WriteLine("\nPress enter to continue...\n");
            while (Console.ReadKey().Key != ConsoleKey.Enter) { }
            CloseSocket(client);
        }
    }
}