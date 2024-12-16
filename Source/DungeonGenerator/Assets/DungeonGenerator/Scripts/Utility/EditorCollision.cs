using System;
using System.Collections;
using System.Collections.Generic;
using System.Numerics;
using System.Runtime.InteropServices.WindowsRuntime;
using TMPro;
using Unity.Burst.CompilerServices;
using UnityEngine;
using UnityEngine.UIElements;
using static DungeonGenerator.EditorCollision;
using Vector2 = UnityEngine.Vector2;
using Vector3 = UnityEngine.Vector3;

namespace DungeonGenerator
{
    public static class EditorCollision
    {
        //Simple overlap algo
        public static bool EditorIsTouching(BoxCollider2D _lhsBoxCollider2D, BoxCollider2D _rhsBoxCollider2D)
        {
            // Get the world position of the left-hand side (LHS) box
            Vector2 lhsPosition = _lhsBoxCollider2D.bounds.center + _lhsBoxCollider2D.gameObject.transform.position ;
            // Get the world position of the right-hand side (RHS) box
            Vector2 rhsPosition = _rhsBoxCollider2D.bounds.center + _rhsBoxCollider2D.gameObject.transform.position ;

            // Convert the collider sizes to world space
            Vector2 lhsSize = _lhsBoxCollider2D.size * _lhsBoxCollider2D.gameObject.transform.lossyScale;
            Vector2 rhsSize = _rhsBoxCollider2D.size * _rhsBoxCollider2D.gameObject.transform.lossyScale;

            // Create Rects for both colliders in world space
            Rect lhsRect = new Rect(lhsPosition.x, lhsPosition.y, lhsSize.x, lhsSize.y);
            Rect rhsRect = new Rect(rhsPosition.x, rhsPosition.y, rhsSize.x, rhsSize.y);
            
            // Check if the two rectangles are overlapping
            return IsRectOverlap(lhsRect, rhsRect);
        }

        

        // Helper function to check if two Rects overlap
        private static bool IsRectOverlap(Rect lhs, Rect rhs)
        {
            // Check if lhs and rhs overlap on the x-axis and y-axis
            return lhs.x <= rhs.x + rhs.width &&
                   lhs.x + lhs.width > rhs.x &&
                   lhs.y <= rhs.y + rhs.height &&
                   lhs.y + lhs.height > rhs.y;
        }
        public struct Rect
        {
            public float x, y, width, height;

            public Rect(float _x, float _y, float _w, float _h)
            {
                x = _x;
                y = _y;
                width = _w;
                height = _h;
            }
        }
    }
}