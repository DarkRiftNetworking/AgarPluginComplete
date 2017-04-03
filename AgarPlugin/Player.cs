using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AgarPlugin
{
    class Player : IEdible
    {
        public uint ID { get; set; }
        public float X { get; set; }
        public float Y { get; set; }
        public float Radius { get; set; }
        public byte ColorR { get; set; }
        public byte ColorG { get; set; }
        public byte ColorB { get; set; }
    
        public Player(uint ID, float x, float y, float radius, byte colorR, byte colorG, byte colorB)
        {
            this.ID = ID;
            this.X = x;
            this.Y = y;
            this.Radius = radius;
            this.ColorR = colorR;
            this.ColorG = colorG;
            this.ColorB = colorB;
        }
    }
}
