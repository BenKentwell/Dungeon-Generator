using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace DungeonGenerator
{
    [AddComponentMenu("DungeonGenerator/Interior")]
    public class Interior : MonoBehaviour
    {
        [SerializeField] private IGeneratorGameplaySpawnable[] _spawnables;

        [EditorButton("FindAllSpawnables")] public bool bFindSpawnable;

        void FindAllSpawnables()
        {

            //no clear, lame
            _spawnables = new IGeneratorGameplaySpawnable[] { };

           _spawnables = GetComponentsInChildren<IGeneratorGameplaySpawnable>();
        }

        
    }
    

}