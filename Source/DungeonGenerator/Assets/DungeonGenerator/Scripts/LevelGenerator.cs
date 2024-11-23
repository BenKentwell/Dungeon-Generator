using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace DungeonGenerator
{
    [AddComponentMenu("DungeonGenerator/LevelGenerator")]
    public class LevelGenerator : MonoBehaviour
    {
        [Header("Dungeon Generation")]
        [SerializeField, Tooltip("The total amnount of rooms to spawn.")]
        public int amount;
        [SerializeField, Tooltip("An Array of prefab rooms.")]
        public RoomPropensities<Room, int>[] Rooms;


        //Utility
        [EditorButton("Generate")] public bool StartGenerating;

        void Generate()
        {
            Debug.Log("Generating");
        }
    }

    #region Utility

    //Utility
    [System.Serializable]
    public class RoomPropensities<TKey, TValue>
    {

        public RoomPropensities() { }

        public RoomPropensities(Room _key, int _value)
        {
            _Room = _key;
            _AmountRequiredToSpawn = _value;

        }

        public Room _Room;

        [Tooltip("The likelyhood of this interior spawning.")]
        public int _AmountRequiredToSpawn;
    }

    #endregion

}

