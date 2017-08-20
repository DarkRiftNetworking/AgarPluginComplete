using DarkRift;
using DarkRift.Server;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AgarPlugin
{
    public class AgarFoodManager : Plugin
    {
        const float MAP_WIDTH = 20;

        const byte FOOD_TAG = 2;
        const ushort SPAWN_SUBJECT = 0;
        const ushort MOVE_SUBJECT = 1;
        
        public override bool ThreadSafe => false;

        public override Version Version => new Version(1, 0, 0);

        public IEnumerable<FoodItem> Food => food;

        HashSet<FoodItem> food = new HashSet<FoodItem>();

        public AgarFoodManager(PluginLoadData pluginLoadData) : base(pluginLoadData)
        {
            Random r = new Random();

            for (uint i = 0; i < 20; i++)
            {
                FoodItem foodItem = new FoodItem(
                    i,
                    (float)r.NextDouble() * MAP_WIDTH - MAP_WIDTH / 2,
                    (float)r.NextDouble() * MAP_WIDTH - MAP_WIDTH / 2,
                    (byte)r.Next(0, 200),
                    (byte)r.Next(0, 200),
                    (byte)r.Next(0, 200)
                );

                food.Add(foodItem);
            }

            ClientManager.ClientConnected += ClientConnected;
        }

        void ClientConnected(object sender, ClientConnectedEventArgs e)
        {
            DarkRiftWriter foodWriter = new DarkRiftWriter();

            foreach (FoodItem foodItem in food)
            {
                foodWriter.Write(foodItem.ID);
                foodWriter.Write(foodItem.X);
                foodWriter.Write(foodItem.Y);
                foodWriter.Write(foodItem.ColorR);
                foodWriter.Write(foodItem.ColorG);
                foodWriter.Write(foodItem.ColorB);
            }

            Message playerMessage = new TagSubjectMessage(FOOD_TAG, SPAWN_SUBJECT, foodWriter);

            e.Client.SendMessage(playerMessage, SendMode.Reliable);
        }

        public void Eat(FoodItem foodItem)
        {
            Random r = new Random();

            foodItem.X = (float)r.NextDouble() * MAP_WIDTH - MAP_WIDTH / 2;
            foodItem.Y = (float)r.NextDouble() * MAP_WIDTH - MAP_WIDTH / 2;

            DarkRiftWriter foodWriter = new DarkRiftWriter();
            
            foodWriter.Write(foodItem.ID);
            foodWriter.Write(foodItem.X);
            foodWriter.Write(foodItem.Y);

            Message playerMessage = new TagSubjectMessage(FOOD_TAG, MOVE_SUBJECT, foodWriter);

            foreach (Client client in ClientManager.GetAllClients())
                client.SendMessage(playerMessage, SendMode.Reliable);
        }
    }
}
