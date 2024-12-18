using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace DungeonGenerator
{
    public interface IGeneratorGameplaySpawnable
    {
        public int XTilePos { get; set; }
        public int YTilePos { get; set; }


        public void Spawn();
    }

}