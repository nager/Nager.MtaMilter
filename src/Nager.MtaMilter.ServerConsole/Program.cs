using Nager.MtaMilter;
using SuperSimpleTcp;

// Sources:
// - https://github.com/emersion/go-milter/blob/master/milter-protocol.txt
// - https://gitlab.com/noumenia/libmilterphp/-/blob/master/library/Milter.inc.php?ref_type=heads

class Program
{
    private static SimpleTcpServer? _tcpServer;
    private static object _lock = new object();

    static void Main(string[] args)
    {
        _tcpServer = new SimpleTcpServer("127.0.0.1", 20007);
        _tcpServer.Settings.StreamBufferSize = 10000;
        _tcpServer.Settings.NoDelay = false;
        _tcpServer.Events.ClientConnected += Events_ClientConnected;
        _tcpServer.Events.ClientDisconnected += Events_ClientDisconnected;
        _tcpServer.Events.DataReceived += Events_DataReceived;
        _tcpServer.Start();

        Console.WriteLine("Virtual Milter ready on 127.0.0.1:20007");
        Console.WriteLine("Wait for connections, press any key for quit");
        Console.ReadLine();

        _tcpServer.Events.ClientConnected -= Events_ClientConnected;
        _tcpServer.Events.ClientDisconnected -= Events_ClientDisconnected;
        _tcpServer.Events.DataReceived -= Events_DataReceived;
        _tcpServer.Stop();
        _tcpServer.Dispose();
    }

    private static void Events_ClientConnected(object? sender, ConnectionEventArgs e)
    {
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine($"ClientConnected - {e.IpPort}");
        Console.ResetColor();
    }

    private static void Events_ClientDisconnected(object? sender, ConnectionEventArgs e)
    {
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine($"ClientDisconnected - {e.IpPort}");
        Console.ResetColor();
    }

    private static void SendAnswer(string ipPort, byte[] response)
    {
        if (_tcpServer is null)
        {
            return;
        }

        var lengthData = BitConverter.GetBytes(response.Length);

        var package = new List<byte>();
        package.AddRange(lengthData.Reverse());
        package.AddRange(response);

        _tcpServer.Send(ipPort, package.ToArray());
    }

    private static void Events_DataReceived(object? sender, DataReceivedEventArgs e)
    {
        lock (_lock)
        {
            try
            {
                Console.WriteLine("--------------------------------------------------");

                if (_tcpServer is null)
                {
                    return;
                }

                var received = e.Data.AsSpan();

                var response = MilterProcessor.ProcessData(received);
                if (response != null)
                {
                    SendAnswer(e.IpPort, response);
                }
            }
            finally
            {
                Console.WriteLine("---------------------------------");
            }
        }
    }
}
