using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

namespace DungeonGenerator
{
    public static class DelaunayTri
    {
        public static void BowyerWatson(List<Vector2> _points, Vector2 _superTriangleSize)
        {
           List<Triangle> triangles = new List<Triangle>();
           List<Triangle> badTriangles = new List<Triangle>();
           
        }

        public class Triangle
        {
            public Vector2 Point1;
            public Vector2 Point2;
            public Vector2 Point3;

            public Vector2 Circumcentre;
            public float radius;

            public Triangle ParentTri;
            public List<Triangle> ChildrenTri;
            public Triangle(Vector2 _point1, Vector2 _point2, Vector2 _point3)
            {
                Point1 = _point1;
                Point2 = _point2;
                Point3 = _point3;
                radius = GetRadius();
            }

            public bool PointInCircumCircle(Vector2 _point)
            {
                Vector2 pointToCheck = _point;
                Vector2 triPoint1 = Point1;
                Vector2 triPoint2 = Point2;
                Vector2 triPoint3 = Point3;

                Vector2 circumcentre;
                float distance = Vector2.Distance(pointToCheck, Circumcentre);
                return distance < radius;

            }

            public float GetRadius()
            {
                Vector2 point;
                float a, b, c;
                a = Vector2.Distance(Point1, Point2);
                b = Vector2.Distance(Point2, Point3);
                c = Vector2.Distance(Point3, Point1);

                float bottomEquation = (a+b+c) * (b +c-a) * (c+ a   -b) * (a + b - c);
                float rad = (a * b * c) / MathF.Sqrt(bottomEquation);
                return rad; 
            }
        }

        public struct Edge
        {
            public Vector2 Point1;
            public Vector2 Point2;
            public Edge(Vector2 _point1, Vector2 _point2)
            {
                Point1 = _point1;
                Point2 = _point2;
            }
        }
          

    }
}
