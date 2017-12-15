using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AgarPlugin
{
    static class Tags
    {
        public static readonly ushort SpawnPlayerTag = 0;
        public static readonly ushort MovePlayerTag = 1;
        public static readonly ushort DespawnPlayerTag = 2;

        public static readonly ushort SpawnFoodTag = 3;
        public static readonly ushort MoveFoodTag = 4;

        public static readonly ushort SetRadiusTag = 5;
    }
}
