using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Numerics;
using System.Threading.Tasks;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.UIElements;
using Debug = UnityEngine.Debug;
using Rect = DungeonGenerator.EditorCollision.Rect;
using Quaternion = UnityEngine.Quaternion;
using Random = UnityEngine.Random;
using Vector2 = UnityEngine.Vector2;
using Vector3 = UnityEngine.Vector3;
using static DungeonGenerator.DelaunayTri;
using UnityEditor;
using Unity.VisualScripting.Dependencies.Sqlite;

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

        [SerializeField, Tooltip("Size (As tiles) of a hallway")]
        private int HallSize = 1;

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
                "The amount of rooms attempting to spawn is less that the required total of rooms.");

            // Create the new level GameObject
            GameObject NewLevel = new GameObject("GeneratedRoom" + System.DateTime.Now);
            NewLevel.transform.parent = transform;
            List<Room> RoomsToSpawn = new List<Room>();
            int roomsRequired = roomsRequiredToSpawn.Count;

            // Add required rooms to the list
            for (int i = 0; i < roomsRequired; i++)
            {
                RoomsToSpawn.Add(roomsRequiredToSpawn[i]);
            }

            roomsRequiredToSpawn.Clear();

            // Randomly select remaining rooms from the copy list
            for (int i = 0; i < AmountOfRooms - roomsRequired; i++)
            {
                int rand = Random.Range(0, RoomsCopy.Count);
                RoomsToSpawn.Add(RoomsCopy[rand]._Room);
            }

            Shuffle(RoomsToSpawn);

            roomColliders = new List<RoomCollider<Room, BoxCollider2D>>();

            Vector2 AverageLocationOfRooms = Vector2.zero;
            Vector2 endRoom = new Vector2();

            // Loop through all rooms to instantiate them and place them on the grid
            for (int i = 0; i < RoomsToSpawn.Count; i++)
            {
                string roomID = "" + (i + 1);

                GameObject room = Instantiate(RoomsToSpawn[i].GetInteriorForRoom().gameObject, NewLevel.transform);
                room.name = "Room : " + roomID + " {" + RoomsToSpawn[i].name + "}";

                Vector2 boundsSize = WallObject.GetComponent<SpriteRenderer>().bounds.size;

                room.AddComponent<Rigidbody2D>();

                GameObject wallsGameObject = new GameObject("Room" + roomID + "Walls");
                wallsGameObject.transform.SetParent(room.transform, true);

                BoxCollider2D col = room.GetComponent<BoxCollider2D>();
                col.offset = new Vector2(((boundsSize.x * RoomsToSpawn[i].width) / 2) - (boundsSize.x / 2),
                    (boundsSize.y * RoomsToSpawn[i].height) / 2);
                col.size = new Vector2(boundsSize.x * RoomsToSpawn[i].width + boundsSize.x,
                    boundsSize.y * RoomsToSpawn[i].height + boundsSize.y);

                RoomCollider<Room, BoxCollider2D> roomColPair =
                    new RoomCollider<Room, BoxCollider2D>(RoomsToSpawn[i], col);

                RoomData roomData = new RoomData(RoomsToSpawn[i].width, RoomsToSpawn[i].height, boundsSize,
                    room.transform);

                MoveRoom(room, boundsSize, AverageLocationOfRooms);
                // Ensure room is placed on the grid without colliding with others
                for (int j = 0; j < roomColliders.Count; j++)
                {
                    if (roomColPair._Collider2D.gameObject == roomColliders[j]._Collider2D.gameObject)
                    {
                        continue;
                    }

                    int k = 0;
                    while (EditorCollision.EditorIsTouching(roomColPair._Collider2D, roomColliders[j]._Collider2D))
                    {
                        MoveRoom(room, boundsSize, AverageLocationOfRooms);
                        k++;
                        if (k > 100)
                        {
                            Debug.Log("Overload in moving levels, Could not find suitable position for room");
                            break;
                        }
                    }
                }

                WallParent wallParent = wallsGameObject.AddComponent<WallParent>();
                wallParent.Generate(roomData, WallObject);

                Vector2 connectionPoint
                    = room.gameObject.transform.position + new Vector3(col.offset.x, col.offset.y, 0);

                delaunayMesh.AddPoint(connectionPoint);
                if (i == roomsRequiredToSpawn.Count - 1)
                    endRoom = connectionPoint;

                roomColliders.Add(roomColPair);
            }

            delaunayMesh.TriangulateAll();
            delaunayMesh.MST.GetTree(delaunayMesh.triangles, delaunayMesh.points[0]);

            GameObject halls = new GameObject();
            halls.transform.SetParent(NewLevel.transform);
            List<BoxCollider2D> cols = new List<BoxCollider2D>();
            foreach (RoomCollider<Room, BoxCollider2D> roomCollider in roomColliders)
            {
                cols.Add(roomCollider._Collider2D);
            }

            StartCoroutine(CreateHalls(delaunayMesh.MST.minSpanningTree, tileSize, halls, cols));
        }

        private void DeleteMesh()
        {
            delaunayMesh = null;
        }


        private void MoveRoom(GameObject _room, Vector2 _tileSize, Vector2 _averageLocationOfRooms = default)
        {

            float TilePos(float x, float _size) => x * _size;

            // Calculate the grid boundaries
            int xx = 0;
            int xy = MaximumLevelSize[0] ;
            int yx = 0;
            int yy = MaximumLevelSize[1];

            // Get a valid grid-aligned position for the room
            Vector3 gridPosition =new Vector3(TilePos(Random.Range(xx, xy), _tileSize.x), TilePos(Random.Range(yx, yy), _tileSize.y), 0);

            // Move the room to this grid position
            _room.transform.position = gridPosition;
        }

        private IEnumerator CreateHalls(List<Triangle.Edge> _mEdges, Vector2 _tileSize, GameObject _parent,
        List<BoxCollider2D> _rooms, Vector2 _origin = default)
        {
            Vector2 gridTile = WallObject.GetComponent<SpriteRenderer>().bounds.size;
            GameObject og = new GameObject();

            List<BoxCollider2D> emptyTile = new List<BoxCollider2D>();
            Vector2 halfBoundsSize = gridTile * 0.5f;

            float TilePos(float x, float _size) => x * _size;
            float TileMinusHalf(float _f, float _f1, float _size) => TilePos(_f, _size) - _f1;

            // Create the tile grid (this section remains similar to the original)
            for (int i = 0; i < MaximumLevelSize[0]; i++)
            {
                for (int j = 0; j < MaximumLevelSize[1]; j++)
                {
                   
                    Vector3 tilePosition = new Vector3(TilePos(i, gridTile.x), 
                        TilePos(j, gridTile.y), 0);

                    GameObject newtile = Instantiate(og, tilePosition, Quaternion.identity, _parent.transform);
                    

                    BoxCollider2D col = newtile.AddComponent<BoxCollider2D>();
                    col.size = gridTile;
                    emptyTile.Add(col);
                    EditorUtility.SetDirty(newtile);
                }
            }

            yield return new WaitForSecondsRealtime(0.1f);

            //Destroy walls that intercect with edges
            foreach (Triangle.Edge edge in _mEdges)
            {

                float angle = Vector2.SignedAngle(edge.Point1, edge.Point2);
                angle -= 90f;
                Vector2 newAng = new Vector2(Mathf.Sin(angle), Mathf.Cos(angle));

                Vector2 offset1 = edge.Point1 + newAng * HallSize;
                Vector2 offset2 = edge.Point2 + newAng * HallSize;
                Triangle.Edge dupEdge = new Triangle.Edge(offset1, offset2);

                Vector2 offset11 = edge.Point1 - newAng * HallSize;
                Vector2 offset22 = edge.Point2 - newAng * HallSize;
                Triangle.Edge dupEdge1 = new Triangle.Edge(offset11, offset22);

                
                LayerMask mask = LayerMask.GetMask("Wall");
                RaycastHit2D[] hits = Physics2D.LinecastAll(dupEdge1.Point1, dupEdge1.Point2, mask);
                foreach (RaycastHit2D hit in hits)
                {
                    GameObject.DestroyImmediate(hit.collider.gameObject);
                }

                hits = Physics2D.LinecastAll(dupEdge.Point1, dupEdge.Point2, mask);

                foreach (RaycastHit2D hit in hits)
                {
                    GameObject.DestroyImmediate(hit.collider.gameObject);
                }

                hits = Physics2D.LinecastAll(edge.Point1, edge.Point2, mask);

                foreach (RaycastHit2D hit in hits)
                {
                    GameObject.DestroyImmediate(hit.collider.gameObject);
                }
            }
            yield return new WaitForSecondsRealtime(0.1f);
            List<GameObject> Walls = new List<GameObject>();

            // Iterate over every grid tile
            for (int i = 0; i < MaximumLevelSize[0]; i++)
            {
                for (int j = 0; j < MaximumLevelSize[1]; j++)
                {
                    // Calculate the center position of the current tile
                    Vector2 tileCenter = new Vector2(TilePos(i, gridTile.x), TilePos(j, gridTile.y));



                    // Check if the tile intersects with any edge
                    foreach (Triangle.Edge edge in _mEdges)
                    {
                        //Get angle as Radians
                        //Add root 2
                        //convert to vec2 and add to point 1 and 2

                        float angle = Vector2.SignedAngle(edge.Point1, edge.Point2);
                        angle -= 90;
                        Vector2 newAng = new Vector2(Mathf.Sin(angle), Mathf.Cos(angle));

                        Vector2 offset1 = edge.Point1 + newAng * HallSize;
                        Vector2 offset2 = edge.Point2 + newAng * HallSize;
                        Triangle.Edge dupEdge = new Triangle.Edge(offset1, offset2);

                        Vector2 offset11 = edge.Point1 - newAng * HallSize;
                        Vector2 offset22 = edge.Point2 - newAng * HallSize;
                        Triangle.Edge dupEdge1 = new Triangle.Edge(offset11, offset22);

                        if (IsTileIntersectingEdge(tileCenter, dupEdge1, gridTile))
                        {
                            // Instantiate wall at the tile position
                            GameObject wall = Instantiate(WallObject, tileCenter, Quaternion.identity, _parent.transform);
                            wall.layer = LayerMask.NameToLayer("Hall");
                            Walls.Add(wall);
                            break; 
                        }
                       
                        if (IsTileIntersectingEdge(tileCenter, dupEdge, gridTile))
                        {
                            // Instantiate wall at the tile position
                            GameObject wall = Instantiate(WallObject, tileCenter, Quaternion.identity, _parent.transform);
                            wall.layer = LayerMask.NameToLayer("Hall");
                            Walls.Add(wall);
                            break;
                        }
                    }
                }
            }

            yield return new WaitForSecondsRealtime(0.1f);

            // Clean up walls that intersect with rooms
            foreach (BoxCollider2D room in _rooms)
            {
                for (int i = 0; i < Walls.Count; i++)
                {
                    LayerMask mask = LayerMask.GetMask("Hall");
                    Vector2 size = new Vector2(room.size.x - gridTile.x - Single.Epsilon, room.size.y -gridTile.y - Single.Epsilon);
                    RaycastHit2D[] hits = Physics2D.BoxCastAll(room.transform.position + room.bounds.extents, size , 0,Vector2.zero, 100,mask);
                    Debug.DrawLine(room.transform.position, room.transform.position + new Vector3(size.x, size.y, 0));
                    foreach (RaycastHit2D hit in hits)
                    {
                        if(hit.transform.gameObject != null)
                            DestroyImmediate(hit.transform.gameObject);
                    }
                    
                }
            }

            for (int i = emptyTile.Count-1; i > 0; i--)
            {
                if (emptyTile != null)
                {

                    DestroyImmediate(emptyTile[i].gameObject);
                }
                emptyTile.RemoveAt(i);
            }
        }

        private bool IsTileIntersectingEdge(Vector2 tileCenter, Triangle.Edge edge, Vector2 tileSize)
        {
            // Get the four corners of the tile based on the tile center and size
            Vector2 topLeft = tileCenter + new Vector2(-tileSize.x / 2, tileSize.y / 2);
            Vector2 topRight = tileCenter + new Vector2(tileSize.x / 2, tileSize.y / 2);
            Vector2 bottomLeft = tileCenter + new Vector2(-tileSize.x / 2, -tileSize.y / 2);
            Vector2 bottomRight = tileCenter + new Vector2(tileSize.x / 2, -tileSize.y / 2);

            // Create the four sides of the tile as edges
            Triangle.Edge topEdge = new Triangle.Edge(topLeft, topRight);
            Triangle.Edge rightEdge = new Triangle.Edge(topRight, bottomRight);
            Triangle.Edge bottomEdge = new Triangle.Edge(bottomRight, bottomLeft);
            Triangle.Edge leftEdge = new Triangle.Edge(bottomLeft, topLeft);

            // Check if any of the tile edges intersects with the input edge
            return IsEdgeIntersectingEdge(edge, topEdge) || IsEdgeIntersectingEdge(edge, rightEdge) ||
                   IsEdgeIntersectingEdge(edge, bottomEdge) || IsEdgeIntersectingEdge(edge, leftEdge);
        }

        // Helper function to check if two edges intersect
        private bool IsEdgeIntersectingEdge(Triangle.Edge edge1, Triangle.Edge edge2)
        {
            // Calculate the direction vectors for both edges
            Vector2 dir1 = edge1.Point2 - edge1.Point1;
            Vector2 dir2 = edge2.Point2 - edge2.Point1;

            // Calculate the determinant (cross product) of the directions
            float det = dir1.x * dir2.y - dir1.y * dir2.x;

            // If the determinant is close to zero, the lines are parallel and do not intersect
            if (Mathf.Abs(det) < Mathf.Epsilon)
                return false;

            // Calculate the intersection point using Cramer's rule
            float t1 = ((edge2.Point1.x - edge1.Point1.x) * dir2.y - (edge2.Point1.y - edge1.Point1.y) * dir2.x) / det;
            float t2 = ((edge2.Point1.x - edge1.Point1.x) * dir1.y - (edge2.Point1.y - edge1.Point1.y) * dir1.x) / det;

            // Check if the intersection point is within the bounds of both edges
            return t1 >= 0 && t1 <= 1 && t2 >= 0 && t2 <= 1;
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


        private GameObject GetTileAt(Vector2 _position)
        {
            return null;
        }

        void OnDrawGizmosSelected()
        {
            if (delaunayMesh != null && delaunayMesh.MST != null)
            {
                /*  Gizmos.color = Color.red;
                  foreach (DelaunayTri.Triangle.Edge edge in delaunayMesh.edges)
                  {
                      Gizmos.DrawLine(edge.Point1, edge.Point2);
                  }*/

                Gizmos.color = new Color(0, 1, 1, 1);
                foreach (DelaunayTri.Triangle.Edge edge in delaunayMesh.MST.minSpanningTree)
                {
                    Gizmos.DrawLine(edge.Point1, edge.Point2);
                }


                Gizmos.color = Color.yellow;
                Gizmos.DrawLine(delaunayMesh.SuperTriangle.Point1 * 1.1f, delaunayMesh.SuperTriangle.Point2 * 1.1f);
                Gizmos.DrawLine(delaunayMesh.SuperTriangle.Point2 * 1.1f, delaunayMesh.SuperTriangle.Point3 * 1.1f);
                Gizmos.DrawLine(delaunayMesh.SuperTriangle.Point3 * 1.1f, delaunayMesh.SuperTriangle.Point1 * 1.1f);

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
        Bottom = 0,
        Top = 1,
        Left = 2,
        Right = 3
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