using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace DungeonGenerator
{
    public class WallParent : MonoBehaviour
    {
        private GameObject WallObject;

        // Generate walls for a room, ensuring correct positioning on the grid
        public void Generate(RoomData _roomData, GameObject _wallGameObject)
        {
            WallObject = _wallGameObject;
            transform.SetParent(_roomData.ParentTransform, false);
            GenerateWall(_roomData.WallSizeX, _roomData.WallSizeY, _roomData.BoundSize, _roomData.ParentTransform, _roomData.TileOriginOnSprite);
        }

        /// <summary>
        /// Generates a series of wall objects and assigns their locations in the level grid
        /// </summary>
        /// <param name="_wallSizeX"> Amount of tiles in the X direction to spawn</param>
        /// <param name="_wallSizeY"> Amount of tiles in the Y direction to spawn</param>
        /// <param name="_boundsSize">Dimensions of the wall sprite (height or width depending on which wall is generated)</param>
        /// <param name="_tileOriginOnSprite">Ratio of the offset the origin of the sprite is. Defaults to 0.5f so origin is the middle of the sprite</param>
        private void GenerateWall(int _wallSizeX, int _wallSizeY, Vector2 _boundsSize, Transform _parentTransform, float _tileOriginOnSprite = 0.5f)
        {
            // Cache Half the bound size to calculate proper offsets for the grid
            Vector2 halfBoundsSize = _boundsSize * _tileOriginOnSprite;

            // Function to calculate position for each tile in grid
            float TilePos(float x, float _size) => x * _size;
            string TileName(int _i, int _j) => "Wall:" + _i + "," + _j;

            // Top side walls
            for (int x = 0; x < _wallSizeX; x++)
            {
                Vector3 tilePosition = new Vector3(TilePos(x, _boundsSize.x),
                    TilePos(_wallSizeY, _boundsSize.y), 0);
                GameObject tile = GameObject.Instantiate(WallObject, transform, true);
                tile.transform.SetLocalPositionAndRotation(tilePosition,
                    Quaternion.identity);

                tile.name = TileName(x, _wallSizeY);
            }

            // Bottom side walls
            for (int x = 0; x < _wallSizeX; x++)
            {
                GameObject tile = GameObject.Instantiate(WallObject, transform, true);
                tile.transform.SetLocalPositionAndRotation(new Vector3(
                        TilePos(x, _boundsSize.x),
                        0, 0),
                    Quaternion.identity);
                tile.name = TileName(x, 0);
            }

            // Right side walls
            for (int y = 0; y < _wallSizeY + 1; y++)
            {
                GameObject tile = GameObject.Instantiate(WallObject, transform, true);
                tile.transform.SetLocalPositionAndRotation(new Vector3(
                        TilePos(_wallSizeX, _boundsSize.x),
                        TilePos(y , _boundsSize.y), 0),
                    Quaternion.identity);
                tile.name = TileName(_wallSizeX, y);
            }

            // Left side walls
            for (int y = 1; y < _wallSizeY; y++)
            {
                GameObject tile = GameObject.Instantiate(WallObject, transform, true);
                tile.transform.SetLocalPositionAndRotation(new Vector3(
                        0,
                        TilePos(y, _boundsSize.y) , 0),
                    Quaternion.identity);
                tile.name = TileName(0, y);
            }
        }
    }
}