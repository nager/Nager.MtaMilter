using Microsoft.Extensions.Logging;
using Nager.MtaMilter;
using Nager.MtaMilter.ServerConsole;

class Program
{
    static void Main(string[] args)
    {
        using var loggerFactory = LoggerFactory.Create(builder =>
        {
            builder.SetMinimumLevel(LogLevel.Debug);
            builder.AddConsole();
        });

        var logger = loggerFactory.CreateLogger<MilterProcessor>();
        var milterProcessor = new MilterProcessor(logger);

        var milterServerPort = 11332;
        using var milterServer = new MilterServer(milterProcessor, milterServerPort);

        milterServer.Start();
        Console.WriteLine($"Virtual Milter ready on 127.0.0.1:{milterServerPort}");
        Console.WriteLine("Wait for connections, press any key for quit");
        Console.ReadLine();
        milterServer.Stop();
    }
}
