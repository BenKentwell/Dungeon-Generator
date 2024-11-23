using System.Collections;
using System.Collections.Generic;
using System.Numerics;
using UnityEngine;
using UnityEngine.Assertions;
using Quaternion = UnityEngine.Quaternion;
using Vector2 = UnityEngine.Vector2;
using Vector3 = UnityEngine.Vector3;

namespace DungeonGenerator
{
    [AddComponentMenu("DungeonGenerator/LevelGenerator")]
    public class LevelGenerator : MonoBehaviour
    {
        [Header("Dungeon Generation")]
        [SerializeField, Tooltip("The total amnount of rooms to spawn.")]
        public int AmountOfRooms;
        
        
        [SerializeField, Tooltip("An Array of prefab rooms.")]
        public RoomPropensities<Room, int>[] Rooms;

        [Header("Tile Presets")] [SerializeField, Tooltip("Default Wall object.")]
        private GameObject WallObject;

        //Allows room generation to be called in editor
        [EditorButton("Generate")] public bool StartGenerating;

        /// <summary>
        /// This function is the bread and butter of the alogorithm.
        /// Praise be the lords of platformers
        /// </summary>
        void Generate()
        {
            List<Room> roomsRequiredToSpawn = new List<Room>();

            for (int i = 0; i < Rooms.Length; i++)
            {
                for (int j = 0; j < Rooms[i]._AmountRequiredToSpawn; j++)
                {
                    roomsRequiredToSpawn.Add(Rooms[i]._Room);
                }
            }

            Assert.IsTrue(AmountOfRooms >= roomsRequiredToSpawn.Count, "The amount of rooms attempting to spawn is less that the required total of rooms. ");


            //If we have reached here, We can begin generating rooms and append them to a parent object
            GameObject NewLevel = GameObject.Instantiate(new GameObject(), this.transform);
            NewLevel.name = "GeneratedRoom" + System.DateTime.Now;

            List<Room> RoomsToSpawn = new List<Room>();
            for (int i = 0; i < roomsRequiredToSpawn.Count; i++)
            {
                RoomsToSpawn.Add(roomsRequiredToSpawn[i]);
            }

            for (int i = 0; i < AmountOfRooms - roomsRequiredToSpawn.Count -1; i++)
            {
                int rand = Random.Range(0, Rooms.Length - 1);
                RoomsToSpawn.Add(Rooms[rand]._Room);
            }

            for (int i = 0; i < RoomsToSpawn.Count; i++)
            {
                GameObject room = Instantiate(new GameObject(), NewLevel.transform);
                room.name = $"Room : {i}";

                Vector2 boundsSize = WallObject.GetComponent<SpriteRenderer>().bounds.size;

                //Top
                for (int x = 0; x < RoomsToSpawn[i].width; x++)
                {
                        GameObject tile = GameObject.Instantiate(WallObject, new Vector3(
                            (boundsSize.x * x) -
                            (boundsSize.x / 2), boundsSize.y /2, 0) , Quaternion.identity, room.transform);
                        tile.name = $" Room:{i} | Wall:{x},{0}";
                    
                }

                //Bottom
                 for (int x = 0; x < RoomsToSpawn[i].width; x++)
                 {
                      
                          GameObject tile = GameObject.Instantiate(WallObject, new Vector3(
                              (boundsSize.x * x) -
                              (boundsSize.x / 2), RoomsToSpawn[i].height * boundsSize.y - boundsSize.y / 2, 0) , Quaternion.identity, room.transform);
                          tile.name = $" Room:{i} | Wall:{x},{RoomsToSpawn[i].height*  boundsSize.y}";
                      
                 }

                //Right
                for (int y = 0; y < RoomsToSpawn[i].height; y++)
                {
                    GameObject tile = GameObject.Instantiate(WallObject, new Vector3(boundsSize.x * RoomsToSpawn[i].width - boundsSize.x / 2,
                        (boundsSize.y * y) - boundsSize.y / 2,0), Quaternion.identity, room.transform);

                    tile.name = $" Room:{i} | Wall:{boundsSize.x * RoomsToSpawn[i].height},{y}";

                }
                for (int y = 0; y < RoomsToSpawn[i].height; y++)
                {
                    GameObject tile = GameObject.Instantiate(WallObject, new Vector3(-boundsSize.x / 2,
                        (boundsSize.y * y) - boundsSize.y / 2,0), Quaternion.identity, room.transform);

                    tile.name = $" Room:{i} | Wall:{0},{y}";

                }

            }
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

