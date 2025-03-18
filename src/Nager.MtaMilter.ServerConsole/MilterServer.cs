using SuperSimpleTcp;

namespace Nager.MtaMilter.ServerConsole
{
    public class MilterServer : IDisposable
    {
        private readonly SimpleTcpServer _tcpServer;
        private readonly MilterProcessor _milterProcessor;
        private readonly object _lock = new object();

        public MilterServer(MilterProcessor milterProcessor)
        {
            this._milterProcessor = milterProcessor;

            this._tcpServer = new SimpleTcpServer("127.0.0.1", 11332);
            this._tcpServer.Settings.StreamBufferSize = 1000;
            this._tcpServer.Settings.NoDelay = false;
            this._tcpServer.Events.ClientConnected += this.Events_ClientConnected;
            this._tcpServer.Events.ClientDisconnected += this.Events_ClientDisconnected;
            this._tcpServer.Events.DataReceived += this.Events_DataReceived;
        }

        public void Dispose()
        {
            this._tcpServer.Events.ClientConnected -= this.Events_ClientConnected;
            this._tcpServer.Events.ClientDisconnected -= this.Events_ClientDisconnected;
            this._tcpServer.Events.DataReceived -= this.Events_DataReceived;

            this._tcpServer.Dispose();
        }

        public void Start()
        {
            this._tcpServer.Start();
        }

        public void Stop()
        {
            this._tcpServer.Stop();
        }

        private void Events_ClientConnected(object? sender, ConnectionEventArgs e)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($"ClientConnected - {e.IpPort}");
            Console.ResetColor();
        }

        private void Events_ClientDisconnected(object? sender, ConnectionEventArgs e)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($"ClientDisconnected - {e.IpPort}");
            Console.ResetColor();
        }

        private void SendAnswer(string ipPort, byte[] response)
        {
            if (this._tcpServer is null)
            {
                return;
            }

            var lengthData = BitConverter.GetBytes(response.Length);

            var package = new List<byte>();
            package.AddRange(lengthData.Reverse());
            package.AddRange(response);

            this._tcpServer.Send(ipPort, package.ToArray());
        }

        private void Events_DataReceived(object? sender, DataReceivedEventArgs e)
        {
            lock (this._lock)
            {
                try
                {
                    if (this._tcpServer is null)
                    {
                        return;
                    }

                    var received = e.Data.AsSpan();

                    var response = this._milterProcessor.ProcessData(received);
                    if (response != null)
                    {
                        this.SendAnswer(e.IpPort, response);
                    }
                }
                catch (Exception exception)
                {
                    Console.WriteLine($"{exception}");
                }
            }
        }
    }
}
