using System;
using System.Collections;
using System.Collections.Generic;
using System.Numerics;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Assertions;
using Rect =  DungeonGenerator.EditorCollision.Rect ;
using Quaternion = UnityEngine.Quaternion;
using Random = UnityEngine.Random;
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

        [SerializeField, Tooltip("Maximum bounds of the level. All rooms will be spawned within this size. Size is in tiles. ")]
        private int[] MaximumLevelSize = new int[2];
        
        [SerializeField, Tooltip("An Array of prefab rooms.")]
        public RoomPropensities<Room, int>[] Rooms;

        private GameObject[,] tiles = null;
        

        [Header("Tile Presets")] [SerializeField, Tooltip("Default Wall object.")]
        private GameObject WallObject;

        private List<RoomCollider<Room, BoxCollider2D>> roomColliders = new
            List<RoomCollider<Room, BoxCollider2D>>();

        //Allows room generation to be called in editor
        [EditorButton("Generate", ButtonWidth = 200)] public bool StartGenerating;

      
        
        /// <summary>
        /// This function is the bread and butter of the alogorithm.
        /// Praise be the lords of platformers
        /// </summary>
        void Generate()
        {
            tiles = new GameObject[MaximumLevelSize[0],MaximumLevelSize[1]];

            //Get location of tile 0,0, then send postions through get location function. 

            Vector2 tilePos00 = transform.position;
            Vector2 tileSize = WallObject.GetComponent<SpriteRenderer>().size;


            List<RoomPropensities<Room, int>> RoomsCopy = new List<RoomPropensities<Room, int>>();
        

            List<Room> roomsRequiredToSpawn = new List<Room>();

            for (int i = 0; i < Rooms.Length; i++)
            {
                for (int j = 0; j < Rooms[i]._AmountRequiredToSpawn; j++)
                {
                    roomsRequiredToSpawn.Add(Rooms[i]._Room);
                    
                }

                if (Rooms[i]._AmountRequiredToSpawn <= 0)
                {
                    RoomsCopy.Add(Rooms[i]);
                }
            }

            Assert.IsTrue(AmountOfRooms >= roomsRequiredToSpawn.Count,
                "The amount of rooms attempting to spawn is less that the required total of rooms. ");


            //If we have reached here, We can begin generating rooms and append them to a parent object
            // GameObject NewLevel = GameObject.Instantiate( emp,new Vector3(0,0,0),Quaternion.identity, this.transform);

            GameObject NewLevel = new GameObject("GeneratedRoom" + System.DateTime.Now);
            NewLevel.transform.parent = transform;
            List<Room> RoomsToSpawn = new List<Room>();
            int roomsrequired = roomsRequiredToSpawn.Count;
            for (int i = 0; i < roomsrequired; i++)
            {
                RoomsToSpawn.Add(roomsRequiredToSpawn[i]);
            }

            roomsRequiredToSpawn.Clear();
            for (int i = 0; i < AmountOfRooms - roomsrequired; i++)
            {
                int rand = Random.Range(0, RoomsCopy.Count);
                RoomsToSpawn.Add(RoomsCopy[rand]._Room);
            }

            Shuffle(RoomsToSpawn);
            //Create rooms, interiors and allocate colliders to each rooms bounds;

            roomColliders = new
                List<RoomCollider<Room, BoxCollider2D>>();
            //This will track location of rooms and influence rooms to move away from this. 
            //This will increase performance
            Vector2 AverageLocationOfRooms = Vector2.zero;


            for (int i = 0; i < RoomsToSpawn.Count; i++)
            {
                string roomID = "" + (i + 1);

                GameObject room = Instantiate(RoomsToSpawn[i].GetInteriorForRoom().gameObject, NewLevel.transform);
                room.name = "Room : " + roomID +  " {" +  RoomsToSpawn[i].name + "}";
                
                Vector2 boundsSize = WallObject.GetComponent<SpriteRenderer>().bounds.size;

                GameObject wallsGameObject = new GameObject("Room" + roomID + "Walls");
                wallsGameObject.transform.parent = room.transform;

                //I probably need to do this after i find a position in the grid. 
                //If it stays here i will need to assign all the tiles to new spots on the grid
                //and that sucks

                //TODO Move the wall generation to after finding a room location that does not conflict with other rooms. 
                //Top
                for (int x = 0; x < RoomsToSpawn[i].width; x++)
                {
                    GameObject tile = GameObject.Instantiate(WallObject, new Vector3(
                            (boundsSize.x * x) - (boundsSize.x / 2),
                            0,
                            0)
                        , Quaternion.identity, wallsGameObject.transform);
                    tile.name = $" Room:{roomID} | Wall:{x},{0}";

                }

                //Bottom
                for (int x = 0; x < RoomsToSpawn[i].width; x++)
                {

                    GameObject tile = GameObject.Instantiate(WallObject, new Vector3(
                            (boundsSize.x * x) -
                            (boundsSize.x / 2),
                            (RoomsToSpawn[i].height * boundsSize.y), 0),
                        Quaternion.identity, wallsGameObject.transform);
                    tile.name = $" Room:{roomID} | Wall:{x},{RoomsToSpawn[i].height}";

                }

                //Right
                for (int y = 0; y < RoomsToSpawn[i].height; y++)
                {
                    GameObject tile = GameObject.Instantiate(WallObject, new Vector3(
                            boundsSize.x * RoomsToSpawn[i].width - (boundsSize.x / 2),
                            (boundsSize.y * y),
                            0),
                        Quaternion.identity, wallsGameObject.transform);

                    tile.name = $" Room:{roomID} | Wall:{RoomsToSpawn[i].width},{y}";

                }

                //left
                for (int y = 0; y < RoomsToSpawn[i].height; y++)
                {
                    GameObject tile = GameObject.Instantiate(WallObject, new Vector3(
                        -boundsSize.x / 2,
                        (boundsSize.y * y),
                        0), Quaternion.identity, wallsGameObject.transform);

                    tile.name = $" Room:{roomID} | Wall:{0},{y}";

                }



                //TODO Move this above wall generation and assign the root to a tile. 
                //Setting a root tile will allow the generator to assign walls into the grid. 

                //Create bounds for each room. 
                BoxCollider2D col = room.AddComponent<BoxCollider2D>();
                col.offset = new Vector2(((boundsSize.x * RoomsToSpawn[i].width) / 2) - (boundsSize.x / 2),
                    (boundsSize.y * RoomsToSpawn[i].height) / 2);
                col.size = new Vector2(boundsSize.x * RoomsToSpawn[i].width + boundsSize.x,
                    boundsSize.y * RoomsToSpawn[i].height + boundsSize.y);
                RoomCollider<Room, BoxCollider2D> roomColPair =
                    new RoomCollider<Room, BoxCollider2D>(RoomsToSpawn[i], col);
               

                //find a spot within bounds    
                for (int j = 0; j < roomColliders.Count; j++)
                {
                    if (roomColPair._Collider2D.gameObject == roomColliders[j]._Collider2D.gameObject)
                    {
                        continue;
                    }

                    //while (roomColPair._Collider2D.IsTouching(roomColliders[j]._Collider2D))
                   while (EditorCollision.EditorIsTouching(roomColPair._Collider2D, roomColliders[j]._Collider2D))
                    {
                        MoveRoom(roomColPair._Collider2D.gameObject, AverageLocationOfRooms);
                    }
                }



                roomColliders.Add(roomColPair);
            }
        }

     

        private void MoveRoom(GameObject _room, Vector2 _averageLocationOfRooms = new Vector2())
        {
            int xx, xy,yx,yy;
            xx = -MaximumLevelSize[0] / 2;
            xy = MaximumLevelSize[0] / 2;
            yx = -MaximumLevelSize[1] / 2;
            yy = MaximumLevelSize[1] / 2;
            _room.transform.position = new Vector3(Random.Range(xx, xy),Random.Range(yx,yy), 0);
        }


        public static void Shuffle<T>( List<T> list)
        {
            int n = list.Count;
            while (n > 1)
            {
                n--;
                int k = Random.Range(0, n + 1);
                T value = list[k];
                list[k] = list[n];
                list[n] = value;
            }
        }

        /// <summary>
        /// Finds the location of a tiles placment in the grid. 
        /// tileOriginShould alwats be 0,0. But pass it it to be save
        /// </summary>
        private Vector2 GetTileLocation(int _xTile, int yTile, Vector2 tileSize, Vector2 tileOrigin = new Vector2() )
        {
            return Vector2.zero;
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

    public class RoomCollider<TKey, TValue>
    {
        public RoomCollider(){ }

        public RoomCollider(Room _key, BoxCollider2D Collider)
        {
            _Room = _key;
            _Collider2D = Collider;
        }

        public Room _Room;
        public BoxCollider2D _Collider2D;
    }



    #endregion

}

