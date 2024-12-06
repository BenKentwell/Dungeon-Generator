using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Numerics;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Assertions;
using Rect = DungeonGenerator.EditorCollision.Rect;
using Quaternion = UnityEngine.Quaternion;
using Random = UnityEngine.Random;
using Vector2 = UnityEngine.Vector2;
using Vector3 = UnityEngine.Vector3;

namespace DungeonGenerator
{
    [AddComponentMenu("DungeonGenerator/LevelGenerator")]
    public class LevelGenerator : MonoBehaviour
    {
        [Header("Dungeon Generation")] [SerializeField, Tooltip("The total amount of rooms to spawn.")]
        private int AmountOfRooms;

        [SerializeField
         , Tooltip("Maximum bounds of the level. All rooms will be spawned within this size. Size is in tiles. ")]
        private int[] MaximumLevelSize = new int[2];

        [SerializeField, Tooltip("An Array of prefab rooms.")]
        private RoomPropensities<Room, int>[] Rooms;

        private GameObject[,] tiles = null;


        [Header("Tile Presets")] [SerializeField, Tooltip("Default Wall object.")]
        private GameObject WallObject;

        private List<RoomCollider<Room, BoxCollider2D>> roomColliders = new
            List<RoomCollider<Room, BoxCollider2D>>();

        //Allows room generation to be called in editor
        [EditorButton("Generate", ButtonWidth = 200)]
        public bool StartGenerating;

        //Allows room generation to be called in editor
        [EditorButton("DeleteMesh", ButtonWidth = 200)]
        public bool DeleteConnections;

        private DelaunayTri delaunayMesh;

        /// <summary>
        /// This function is the bread and butter of the alogorithm.
        /// Praise be the lords of platformers
        /// </summary>
        void Generate()
        {
            
            delaunayMesh = new DelaunayTri();
            tiles = new GameObject[MaximumLevelSize[0], MaximumLevelSize[1]];

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
                room.name = "Room : " + roomID + " {" + RoomsToSpawn[i].name + "}";

                Vector2 boundsSize = WallObject.GetComponent<SpriteRenderer>().bounds.size;

                GameObject wallsGameObject = new GameObject("Room" + roomID + "Walls");
                wallsGameObject.transform.SetParent(room.transform, true);


                BoxCollider2D col = room.GetComponent<BoxCollider2D>();
                col.offset = new Vector2(((boundsSize.x * RoomsToSpawn[i].width) / 2) - (boundsSize.x / 2),
                    (boundsSize.y * RoomsToSpawn[i].height) / 2);
                col.size = new Vector2(boundsSize.x * RoomsToSpawn[i].width + boundsSize.x,
                    boundsSize.y * RoomsToSpawn[i].height + boundsSize.y);

                RoomCollider<Room, BoxCollider2D> roomColPair =
                    new RoomCollider<Room, BoxCollider2D>(RoomsToSpawn[i], col);

                RoomData roomData = new RoomData(RoomsToSpawn[i].width, RoomsToSpawn[i].height, boundsSize
                    , wallsGameObject.transform);
                //find a spot within bounds    
                for (int j = 0; j < roomColliders.Count; j++)
                {
                    if (roomColPair._Collider2D.gameObject == roomColliders[j]._Collider2D.gameObject)
                    {
                        continue;
                    }


                    int k = 0;
                    //while (roomColPair._Collider2D.IsTouching(roomColliders[j]._Collider2D))
                    while (EditorCollision.EditorIsTouching(roomColPair._Collider2D, roomColliders[j]._Collider2D))
                    {
                        MoveRoom(room, boundsSize, AverageLocationOfRooms);
                        k++;
                        if (k > 100)
                            break;
                    }
                }

                WallParent wallParent = wallsGameObject.AddComponent<WallParent>();
                wallParent.Generate(roomData, WallObject);

                Vector2 connectionPoint
                    = room.gameObject.transform.position + new Vector3(col.offset.x, col.offset.y, 0);

                delaunayMesh.InsertPoint(connectionPoint);

                roomColliders.Add(roomColPair);
            }
            //delaunayMesh.RemoveSuperTriangle();
        }

        private void DeleteMesh()
        {
            delaunayMesh = null;
        }


        private void MoveRoom(GameObject _room, Vector2 _tileSize, Vector2 _averageLocationOfRooms = default)
        {
            int xx, xy, yx, yy;
            xx = -MaximumLevelSize[0] / 2;
            xy = MaximumLevelSize[0] / 2;
            yx = -MaximumLevelSize[1] / 2;
            yy = MaximumLevelSize[1] / 2;

            _room.transform.position
                = GetTileLocation(new Vector3(Random.Range(xx, xy), Random.Range(yx, yy), 0), _tileSize);
        }


        public static void Shuffle<T>(List<T> list)
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
        private Vector2 GetTileLocation(Vector2 location, Vector2 tileSize, Vector2 tileOrigin = default)
        {
            Vector2 ratio = location / tileSize;
            int ratioX = (int)ratio.x;
            int ratioY = (int)ratio.y;

            ratio.x = ratioX * tileSize.x;
            ratio.y = ratioY * tileSize.y;
            return ratio;
        }

        void OnDrawGizmos()
        {
            if (delaunayMesh != null && delaunayMesh.triangles.Count > 3)
            {
                Gizmos.color = Color.green;
                foreach (DelaunayTri.Triangle triangle in delaunayMesh.triangles)
                {
                    Gizmos.DrawLine(triangle.Point1, triangle.Point2);
                    Gizmos.DrawLine(triangle.Point2, triangle.Point3);
                    Gizmos.DrawLine(triangle.Point3, triangle.Point1);
                }

                Gizmos.color = Color.red;
                foreach (Vector2 point in delaunayMesh.points)
                {
                    Gizmos.DrawCube(point, Vector3.one);
                }
                Gizmos.color = Color.yellow;
      }
        }
    }

    #region Utility

    public enum EWallDirection
    {
        Bottom = 0
        , Top = 1
        , Left = 2
        , Right = 3
    }


    //Utility
    [System.Serializable]
    public class RoomPropensities<TKey, TValue>
    {
        public RoomPropensities()
        {
        }

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
        public RoomCollider()
        {
        }

        public RoomCollider(Room _key, BoxCollider2D Collider)
        {
            _Room = _key;
            _Collider2D = Collider;
        }

        public Room _Room;
        public BoxCollider2D _Collider2D;
    }


    public struct RoomData
    {
        public int WallSizeX;
        public int WallSizeY;
        public Vector2 BoundSize;
        public Transform ParentTransform;
        public float TileOriginOnSprite;

        public RoomData(int _wallSizeX, int _wallSizeY, Vector2 _boundsSize
            , Transform _parentTransform, float _tileOriginOnSprite = 0.5f)
        {
            WallSizeX = _wallSizeX;
            WallSizeY = _wallSizeY;
            BoundSize = _boundsSize;
            ParentTransform = _parentTransform;
            TileOriginOnSprite = _tileOriginOnSprite;
        }
    }

    #endregion
}