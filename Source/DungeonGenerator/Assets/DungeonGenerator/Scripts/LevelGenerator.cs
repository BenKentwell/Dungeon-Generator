using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace DungeonGenerator
{
    [AddComponentMenu("DungeonGenerator/LevelGenerator")]
    public class LevelGenerator : MonoBehaviour
    {
        [SerializeField, Tooltip("An Array of prefab rooms. ")]
        private Room[] Rooms;

        //Utility
        [EditorButton("Generate")] public bool generate;

      

        void Generate()
        {
            Debug.Log("Generating");
        }
    }
}

