using System;
using System.Collections;
using System.Collections.Generic;
using System.Numerics;
using TMPro;
using Unity.Burst.CompilerServices;
using UnityEngine;
using Vector2 = System.Numerics.Vector2;
using Vector3 = UnityEngine.Vector3;

namespace DungeonGenerator
{
    public static class EditorCollision
    {
        //Simple overlap algo
        //Required as unity checks for overlap events at runtime, not editor. This is nicer than tying to UnityEngine.Physics
        public static bool EditorIsTouching(BoxCollider2D _lhsBoxCollider2D, BoxCollider2D _rhsBoxCollider2D)
        {

            //Rect rect1 = {_pos1.x - (_wh1.x / 2), _pos1.y - (_wh1.y / 2), _wh1.x, _wh1.y};
            // 	Rect rect2 = { _pos2.x - (_wh2.x / 2), _pos2.y - (_wh2.y / 2), _wh2.x , _wh2.y };
            // 
            // 	if (rect1.x < rect2.x + rect2.w && rect1.x + rect1.w > rect2.x && rect1.y < rect2.y + rect2.h && rect1.y + rect1.h > rect2.y)
            // 	{
            // 		return true;
            // 	}
            // 
            // 	return false;
            // }

            float lhsX, lhsY, rhsX, rhsY;
            lhsX = (_lhsBoxCollider2D.gameObject.transform.position.x + _lhsBoxCollider2D.offset.x);
            lhsY = (_lhsBoxCollider2D.gameObject.transform.position.y + _lhsBoxCollider2D.offset.y);
            Rect rect1 = new Rect(lhsX , lhsY, _lhsBoxCollider2D.size.x, _lhsBoxCollider2D.size.y);

            rhsX = (_rhsBoxCollider2D.gameObject.transform.position.x + _rhsBoxCollider2D.offset.x);
            rhsY = (_rhsBoxCollider2D.gameObject.transform.position.y+ _rhsBoxCollider2D.offset.y);

            Rect rect2 = new Rect(rhsX , rhsY, _rhsBoxCollider2D.size.x, _rhsBoxCollider2D.size.y);

      
            if (rect1.x < rect2.x + rect2.w && rect1.x + rect1.w > rect2.x && rect1.y < rect2.y + rect2.h && rect1.y + rect1.h > rect2.y)
            {
                	return true;
            }

            return false;

        }

       public struct Rect
        {
            public float x, y, w, h;

            public Rect(float _x, float _y, float _w, float _h)
            {
                x = _x; y = _y; w = _w; h = _h;
            }
        }



    }

}
