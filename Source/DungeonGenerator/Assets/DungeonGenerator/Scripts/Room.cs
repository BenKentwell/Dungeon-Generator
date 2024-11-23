using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace DungeonGenerator
{


    [CreateAssetMenu(fileName = "Room", menuName = "DungeonGenerator/Room", order = 1)]
    public class Room : ScriptableObject
    {

        private GUID _guid;

        
        [SerializeField, Tooltip("Pixel size of each tile")] public int xTileBounds, yTileBounds;

        [SerializeField, Tooltip("Amount of tiles in room")] public int width, height;
        public Sprite WallSprite;

        public RoomInteriorPreset<Interior, float>[] Interior;

        [EditorButton("AssignBoundsFromSprite")] public bool assignSize;
        public void AssignBoundsFromSprite()
        {
            yTileBounds = (int)WallSprite.rect.height;
            xTileBounds = (int)WallSprite.rect.width;
        }

       public Interior GetInteriorForRoom()
        {
            float total = 0;
            for (int i = 0; i < Interior.Length; i++)
            {
                total += Interior[i]._weight;
            }

            float Target = Random.Range(0, total);

            float currentCount = 0;

            for (int i = 0; i < Interior.Length; i++)
            {
                currentCount += Interior[i]._weight;
                if (currentCount >= Target)
                {
                    return Interior[i]._interiorGameObject;
                }
            }

            return null;
        }
    }

    #region Utility


    //Utility
    [System.Serializable]
    public class RoomInteriorPreset<TKey, TValue>
    {

        public RoomInteriorPreset() { }

        public RoomInteriorPreset(Interior key, float value)
        {
            _interiorGameObject = key;
            _weight = value;

        }

        public Interior _interiorGameObject;

        [Tooltip("The likelyhood of this interior spawning.")]
        public float _weight;
    }



    #endregion


}





