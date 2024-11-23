using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace DungeonGenerator
{

    [AddComponentMenu("DungeonGenerator/GameplaySpawnable/Collectable")]
    public class Collectable : MonoBehaviour, IGeneratorGameplaySpawnable
    {
        public int XTilePos { get; set; }
        public int YTilePos { get; set; }

        public void Spawn()
        {

        }
    }
}