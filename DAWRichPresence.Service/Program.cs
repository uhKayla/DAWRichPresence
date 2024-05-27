using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipes;
using System.Threading.Tasks;
using DiscordRPC;
using DiscordRPC.Logging;

namespace DAWRichPresence.Service
{
    class DiscordService
    {
        private static DiscordRpcClient client;
        private static readonly Dictionary<string, string> clientIds = new Dictionary<string, string>
        {
            { "Ableton Live", "client_id_for_ableton" },
            { "FL Studio", "client_id_for_fl_studio" },
            { "Bitwig Studio", "1244793162887594121" },
            { "Cubase", "client_id_for_cubase" },
            // Add more mappings as needed
        };

        static void Main(string[] args)
        {
            if (args.Length == 0)
            {
                Console.WriteLine("DAW name not provided.");
                return;
            }

            string dawName = args[0];
            if (clientIds.TryGetValue(dawName, out string clientId))
            {
                InitializeDiscordClient(clientId);
                Task.Run(() => ListenForUpdates());

                Console.WriteLine("Discord Rich Presence Service is running...");
                Console.ReadLine(); // Keep the service running
            }
            else
            {
                Console.WriteLine($"No client ID configured for DAW: {dawName}");
                Console.ReadLine();
            }
        }

        private static void InitializeDiscordClient(string clientId)
        {
            try
            {
                client = new DiscordRpcClient(clientId);
                client.Logger = new ConsoleLogger() { Level = LogLevel.Warning };

                client.OnReady += (sender, e) =>
                {
                    Console.WriteLine($"Received Ready from user {e.User.Username}");
                };

                client.OnError += (sender, e) =>
                {
                    Console.WriteLine($"Error: {e.Message}");
                };

                client.OnPresenceUpdate += (sender, e) =>
                {
                    Console.WriteLine($"Presence updated: {e.Presence}");
                };

                client.Initialize();

                Console.WriteLine("Discord client initialized.");
            }
            catch (Exception ex)
            {
                Console.WriteLine("Exception in InitializeDiscordClient: " + ex.Message);
                Console.WriteLine(ex.StackTrace);
            }
        }

        private static void ListenForUpdates()
        {
            try
            {
                while (true)
                {
                    using (var server = new NamedPipeServerStream("DiscordPipe", PipeDirection.In))
                    {
                        Console.WriteLine("Waiting for connection...");
                        server.WaitForConnection();
                        Console.WriteLine("Connected to plugin.");

                        using (var reader = new StreamReader(server))
                        {
                            while (true)
                            {
                                var line = reader.ReadLine();
                                if (line == null) break;
                                Console.WriteLine($"Received update: {line}");
                                UpdatePresence(line);
                            }
                        }
                        Console.WriteLine("Disconnected from plugin.");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Exception in ListenForUpdates: " + ex.Message);
                Console.WriteLine(ex.StackTrace);
            }
        }

        private static void UpdatePresence(string message)
        {
            try
            {
                var parts = message.Split(';');
                if (parts.Length >= 2)
                {
                    client.SetPresence(new RichPresence()
                    {
                        Details = parts[0],
                        State = parts[1],
                        Timestamps = Timestamps.Now,
                    });
                    Console.WriteLine($"Updated presence: Details = {parts[0]}, State = {parts[1]}");
                }
                else
                {
                    Console.WriteLine("Invalid message format received.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Exception in UpdatePresence: " + ex.Message);
                Console.WriteLine(ex.StackTrace);
            }
        }
    }
}
