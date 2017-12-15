using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using DarkRift.Server;
using DarkRift;

namespace AgarPlugin
{
    public class AgarPlayerManager : Plugin
    {
        const float MAP_WIDTH = 20;
        
        public override bool ThreadSafe => false;

        public override Version Version => new Version(1, 0, 0);

        Dictionary<Client, Player> players = new Dictionary<Client, Player>();

        public AgarPlayerManager(PluginLoadData pluginLoadData) : base(pluginLoadData)
        {
            ClientManager.ClientConnected += ClientConnected;
            ClientManager.ClientDisconnected += ClientDisconnected;
        }

        void ClientConnected(object sender, ClientConnectedEventArgs e)
        {
            Random r = new Random();
            Player newPlayer = new Player(
                e.Client.GlobalID,
                (float)r.NextDouble() * MAP_WIDTH - MAP_WIDTH / 2,
                (float)r.NextDouble() * MAP_WIDTH - MAP_WIDTH / 2,
                1f,
                (byte)r.Next(0, 200),
                (byte)r.Next(0, 200),
                (byte)r.Next(0, 200)
            );

            using (DarkRiftWriter newPlayerWriter = DarkRiftWriter.Create())
            {
                newPlayerWriter.Write(newPlayer.ID);
                newPlayerWriter.Write(newPlayer.X);
                newPlayerWriter.Write(newPlayer.Y);
                newPlayerWriter.Write(newPlayer.Radius);
                newPlayerWriter.Write(newPlayer.ColorR);
                newPlayerWriter.Write(newPlayer.ColorG);
                newPlayerWriter.Write(newPlayer.ColorB);

                using (Message newPlayerMessage = Message.Create(Tags.SpawnPlayerTag, newPlayerWriter))
                {
                    foreach (Client client in ClientManager.GetAllClients().Where(x => x != e.Client))
                        client.SendMessage(newPlayerMessage, SendMode.Reliable);
                }
            }

            players.Add(e.Client, newPlayer);

            using (DarkRiftWriter playerWriter = DarkRiftWriter.Create())
            {
                foreach (Player player in players.Values)
                {
                    playerWriter.Write(player.ID);
                    playerWriter.Write(player.X);
                    playerWriter.Write(player.Y);
                    playerWriter.Write(player.Radius);
                    playerWriter.Write(player.ColorR);
                    playerWriter.Write(player.ColorG);
                    playerWriter.Write(player.ColorB);
                }

                using (Message playerMessage = Message.Create(Tags.SpawnPlayerTag, playerWriter))
                    e.Client.SendMessage(playerMessage, SendMode.Reliable);
            }

            e.Client.MessageReceived += MovementMessageReceived;
        }

        void MovementMessageReceived(object sender, MessageReceivedEventArgs e)
        {
            using (Message message = e.GetMessage() as Message)
            {
                if (message.Tag == Tags.MovePlayerTag)
                {
                    using (DarkRiftReader reader = message.GetReader())
                    {
                        float newX = reader.ReadSingle();
                        float newY = reader.ReadSingle();

                        Client client = (Client)sender;

                        Player player = players[client];

                        player.X = newX;
                        player.Y = newY;

                        AgarFoodManager foodManager = PluginManager.GetPluginByType<AgarFoodManager>();

                        foreach (FoodItem food in foodManager.Food)
                        {
                            if (Math.Pow(player.X - food.X, 2) + Math.Pow(player.Y - food.Y, 2) < Math.Pow(player.Radius, 2))
                            {
                                player.Radius += food.Radius;
                                SendRadiusUpdate(player);
                                foodManager.Eat(food);
                            }
                        }

                        foreach (Player p in players.Values)
                        {
                            if (p != player && Math.Pow(player.X - p.X, 2) + Math.Pow(player.Y - p.Y, 2) < Math.Pow(player.Radius, 2))
                            {
                                player.Radius += p.Radius;
                                SendRadiusUpdate(player);
                                Kill(p);
                            }
                        }

                        using (DarkRiftWriter writer = DarkRiftWriter.Create())
                        {
                            writer.Write(player.ID);
                            writer.Write(player.X);
                            writer.Write(player.Y);
                            message.SetWriter(writer);
                        }

                        foreach (Client c in ClientManager.GetAllClients().Where(x => x != client))
                            c.SendMessage(message, e.SendMode);
                    }
                }
            }
        }

        void ClientDisconnected(object sender, ClientDisconnectedEventArgs e)
        {
            players.Remove(e.Client);

            using (DarkRiftWriter writer = DarkRiftWriter.Create())
            {
                writer.Write(e.Client.GlobalID);

                using (Message message = Message.Create(Tags.DespawnPlayerTag, writer))
                {
                    foreach (Client client in ClientManager.GetAllClients())
                        client.SendMessage(message, SendMode.Reliable);
                }
            }
        }

        void SendRadiusUpdate(Player player)
        {
            using (DarkRiftWriter writer = DarkRiftWriter.Create())
            {
                writer.Write(player.ID);
                writer.Write(player.Radius);

                using (Message message = Message.Create(Tags.SetRadiusTag, writer))
                {
                    foreach (Client client in ClientManager.GetAllClients())
                        client.SendMessage(message, SendMode.Unreliable);
                }
            }
        }

        void Kill(Player player)
        {
            //Basic kill function that assigns new XY and reset radius to 1
            Random r = new Random();
            player.X = (float)r.NextDouble() * MAP_WIDTH - MAP_WIDTH / 2;
            player.Y = (float)r.NextDouble() * MAP_WIDTH - MAP_WIDTH / 2;

            using (DarkRiftWriter writer = DarkRiftWriter.Create())
            {
                writer.Write(player.ID);
                writer.Write(player.X);
                writer.Write(player.Y);

                using (Message message = Message.Create(Tags.MovePlayerTag, writer))
                {
                    foreach (Client client in ClientManager.GetAllClients())
                        client.SendMessage(message, SendMode.Unreliable);
                }
            }

            player.Radius = 1f;

            SendRadiusUpdate(player);
        }
    }
}
