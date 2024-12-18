using System.Collections;
using System.Collections.Generic;
using DungeonGenerator;
using UnityEngine;

namespace DungeonGenerator
{

    public class Player : MonoBehaviour, IGeneratorGameplaySpawnable
    {
        public int XTilePos { get; set; }
        public int YTilePos { get; set; }

        [SerializeField] private GameObject playerGameObject;

        public void Spawn()
        {
            Instantiate(playerGameObject, transform.position, Quaternion.identity, transform);
        }
    }

}