using System;

namespace AgarPlugin
{
    public class FoodItem : IEdible
    {
        public ushort ID { get; set; }
        public float Radius => 0.1f;
        public float X { get; set; }
        public float Y { get; set; }

        public byte ColorR { get; set; }
        public byte ColorG { get; set; }
        public byte ColorB { get; set; }

        public FoodItem(ushort ID, float x, float y, byte colorR, byte colorG, byte colorB)
        {
            this.ID = ID;
            this.X = x;
            this.Y = y;
            this.ColorR = colorR;
            this.ColorG = colorG;
            this.ColorB = colorB;
        }
    }
}