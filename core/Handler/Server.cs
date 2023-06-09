﻿using Sisma.Database;
using Sisma.Models;
using WatsonWebsocket;

namespace Sisma.Handler;

public class Server
{
    public readonly (string address, int port) Host;
    public readonly List<Room> Rooms;
    public readonly List<Match> Matches;
    public readonly List<Client> Clients;
    public readonly List<Cluster> Clusters;
    public readonly WatsonWsServer Socket;

    public Server((string address, int port) host)
    {
        Host = host;
        Rooms = SismaDatabase.LoadRoom() ?? new List<Room>();
        Matches = new List<Match>();
        Clients = new List<Client>();
        Clusters = SismaDatabase.LoadCluster() ?? new List<Cluster>();
        Socket = new WatsonWsServer(host.address, host.port, false);

        Socket.ClientConnected += (_, connection) =>
        {
            Client.Auth(connection, this);
        };

        Socket.MessageReceived += (_, data) =>
        {
            foreach (var client in Clients)
            {
                if (client.Connection.Client.Guid.ToString() == data.Client.Guid.ToString())
                {
                    client.OnMessage(data.Data.ToArray(), data.MessageType);
                    return;
                }
            }
        };

        Socket.ClientDisconnected += (_, connection) =>
        {
            foreach (var client in Clients)
            {
                if (client.Connection.Client.Guid.ToString() == connection.Client.Guid.ToString())
                {
                    client.OnDisconnect();
                    return;
                }
            }
        };

        Socket.ServerStopped += (_, o) =>
        {
            Clients.Clear();
            Console.WriteLine("[WEBSOCKET SERVER CLOSED]");
        };

        Socket.Start();

        // Start matchmaking system
        Matchmaking.Init(this);

        Console.WriteLine($"[WEBSOCKET SERVER STARTED] {host}");
    }
}
