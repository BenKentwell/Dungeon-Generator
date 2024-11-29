using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace DungeonGenerator
{
    public class WallParent : MonoBehaviour
    {
        private GameObject WallObject;


        public void Generate(RoomData _roomData, GameObject _wallGameObject)
        {
            WallObject = _wallGameObject;
            transform.SetParent(_roomData.ParentTransform, false);
            GenerateWall(_roomData.WallSizeX, _roomData.WallSizeY, _roomData.BoundSize, _roomData.ParentTransform
                , _roomData.TileOriginOnSprite);
        }


        /// <summary>
        /// Generates a series of wall objects and assigns their locations in the level and the tileset
        /// </summary>
        /// <param name="_wallSizeX"> Amount of tile to spawn</param>
        /// <param name="_wallSizeY"></param>
        /// <param name="_boundsSize">Dimension of the wall sprite.Height or Width depending on which wall is generated </param>
        /// <param name="_tileOriginOnSprite">Ratio of the offset the origin of the sprite is. Defaults to 0.5f so origin is the middle of the sprite</param>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        private void GenerateWall(int _wallSizeX, int _wallSizeY, Vector2 _boundsSize
            , Transform _parentTransform
            , float _tileOriginOnSprite = 0.5f)
        {
            //Cache Half the bound size
            Vector2 halfBoundsSize = _boundsSize * _tileOriginOnSprite;

            float TilePos(float x) => x * _boundsSize.x;
            string TileName(int _i, int _j) => "Wall:" + _i + "," + _j;
            float TileMinusHalf(float _f, float _f1) => TilePos(_f) - _f1;

            //top
            for (int x = 0; x < _wallSizeX; x++)
            {
                GameObject tile = GameObject.Instantiate(WallObject, transform, true);

                tile.transform.SetLocalPositionAndRotation(new Vector3(
                        TileMinusHalf(x, halfBoundsSize.x),
                        TilePos(_wallSizeY), 0),
                    Quaternion.identity);
                tile.name = TileName(x, _wallSizeY);
            }

            //bottom
            for (int x = 0; x < _wallSizeX; x++)
            {
                GameObject tile = GameObject.Instantiate(WallObject, transform, true);
                tile.transform.SetLocalPositionAndRotation(new Vector3(TileMinusHalf(x, halfBoundsSize.x), 0, 0)
                    , Quaternion.identity);
                tile.name = TileName(x, 0);
            }

            //Right
            for (int y = 0; y < _wallSizeY + 1; y++)
            {
                GameObject tile = GameObject.Instantiate(WallObject, transform, true);

                tile.transform.SetLocalPositionAndRotation(new Vector3(
                        TileMinusHalf(_wallSizeX, halfBoundsSize.x),
                        TilePos(y), 0),
                    Quaternion.identity);
                tile.name = TileName(_wallSizeX, y);
            }

            //left
            for (int y = 1; y < _wallSizeY; y++)
            {
                GameObject tile = GameObject.Instantiate(WallObject, transform, true);
                tile.transform.SetLocalPositionAndRotation(new Vector3(
                    -halfBoundsSize.y,
                    TilePos(y),
                    0), Quaternion.identity);

                tile.name = TileName(0, y);
            }
        }
    }
}