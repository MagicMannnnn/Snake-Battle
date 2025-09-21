using System;
using System.Threading.Tasks;
using System.Text.Json;

class Snake
{
    private static WebSocketServer server;
    private static Test test;
    private static volatile bool showQValues = false;

    private static AI ai;
    private static string name = "network/networks/main"; //v8 best (54)
    private static string path = name + ".txt";

    static async Task Main(string[] args)
    {
        InitializeServer();
        StartTest();
        RegisterServerEvents();
        StartConsoleListener();
        await server.StartAsync();
    }

    private static void InitializeServer()
    {
        server = new WebSocketServer("http://localhost:5000/ws/");
    }

    private static void RegisterServerEvents()
    {
        server.OnMessageReceived += async (ws, msg) =>
        {
            try
            {

                msg = msg.Trim().ToLower();

                // JS requests game data
                if (msg == "getdata")
                {
                    test.Update();
                    var data = test.getState();

                    string json = JsonSerializer.Serialize(data);
                    if (showQValues)
                    {
                        Console.WriteLine(json);
                    }
                    await server.SendToClient(ws, json);

                }
                else if (msg == "restart")
                {
                    test.Reset();
                }

                if (showQValues)
                {
                    var data = test.getState();
                    Console.WriteLine($"Snake: {string.Join(", ", data.snake)}");
                    Console.WriteLine($"Apple: {data.apple}");
                    showQValues = false;
                }
            }
            catch 
            {
                await server.SendToClient(ws, "error");
            }
        };

        server.OnClientConnected += (ws) =>
        {
            Console.WriteLine("Client connected!");
            //_ = server.SendToClient(ws, "HELLO");
        };
    }

    private static void StartTest()
    {
        test = new Test(path);
    }

    private static void StartConsoleListener()
    {
        _ = Task.Run(() =>
        {
            while (true)
            {
                string line = Console.ReadLine(); // just waits for Enter
                showQValues = true;
            }
        });
    }
}
