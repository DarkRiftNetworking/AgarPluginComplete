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

        const byte SPAWN_TAG = 0;
        const byte MOVEMENT_TAG = 1;

        const ushort SPAWN_SUBJECT = 0;
        const ushort DESPAWN_SUBJECT = 1;

        const ushort MOVE_SUBJECT = 0;
        const ushort RADIUS_SUBJECT = 1;

        public override string Name => nameof(AgarPlayerManager);

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

            DarkRiftWriter newPlayerWriter = new DarkRiftWriter();
            newPlayerWriter.Write(newPlayer.ID);
            newPlayerWriter.Write(newPlayer.X);
            newPlayerWriter.Write(newPlayer.Y);
            newPlayerWriter.Write(newPlayer.Radius);
            newPlayerWriter.Write(newPlayer.ColorR);
            newPlayerWriter.Write(newPlayer.ColorG);
            newPlayerWriter.Write(newPlayer.ColorB);

            Message newPlayerMessage = new TagSubjectMessage(SPAWN_TAG, SPAWN_SUBJECT, newPlayerWriter);

            foreach (Client client in ClientManager.GetAllClients().Where(x => x != e.Client))
                client.SendMessage(newPlayerMessage, SendMode.Reliable);

            players.Add(e.Client, newPlayer);

            DarkRiftWriter playerWriter = new DarkRiftWriter();

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

            Message playerMessage = new TagSubjectMessage(SPAWN_TAG, SPAWN_SUBJECT, playerWriter);

            e.Client.SendMessage(playerMessage, SendMode.Reliable);         //TODO Might need to be fragmented? Good to introduce it here...

            e.Client.MessageReceived += MovementMessageReceived;
        }

        void MovementMessageReceived(object sender, MessageReceivedEventArgs e)
        {
            TagSubjectMessage message = e.Message as TagSubjectMessage;

            if (message != null && message.Tag == MOVEMENT_TAG)
            {
                DarkRiftReader reader = e.Message.GetReader();

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

                DarkRiftWriter writer = new DarkRiftWriter();
                writer.Write(player.ID);
                writer.Write(player.X);
                writer.Write(player.Y);
                e.Message.SetWriter(writer);

                e.DistributeTo.UnionWith(ClientManager.GetAllClients().Where(x => x != client));
            }
        }

        void ClientDisconnected(object sender, ClientDisconnectedEventArgs e)
        {
            players.Remove(e.Client);

            DarkRiftWriter writer = new DarkRiftWriter();

            writer.Write(e.Client.GlobalID);

            TagSubjectMessage message = new TagSubjectMessage(SPAWN_TAG, DESPAWN_SUBJECT, writer);

            foreach (Client client in ClientManager.GetAllClients())
                client.SendMessage(message, SendMode.Reliable);
        }

        void SendRadiusUpdate(Player player)
        {
            DarkRiftWriter writer = new DarkRiftWriter();
            writer.Write(player.ID);
            writer.Write(player.Radius);

            TagSubjectMessage message = new TagSubjectMessage(MOVEMENT_TAG, RADIUS_SUBJECT, writer);

            foreach (Client client in ClientManager.GetAllClients())
                client.SendMessage(message, SendMode.Unreliable);
        }

        void Kill(Player player)
        {
            //Basic kill function that assigns new XY and reset radius to 1
            Random r = new Random();
            player.X = (float)r.NextDouble() * MAP_WIDTH - MAP_WIDTH / 2;
            player.Y = (float)r.NextDouble() * MAP_WIDTH - MAP_WIDTH / 2;

            DarkRiftWriter writer = new DarkRiftWriter();
            writer.Write(player.ID);
            writer.Write(player.X);
            writer.Write(player.Y);

            TagSubjectMessage message = new TagSubjectMessage(MOVEMENT_TAG, MOVE_SUBJECT, writer);

            foreach (Client client in ClientManager.GetAllClients())
                client.SendMessage(message, SendMode.Unreliable);

            player.Radius = 1f;

            SendRadiusUpdate(player);
        }
    }
}
