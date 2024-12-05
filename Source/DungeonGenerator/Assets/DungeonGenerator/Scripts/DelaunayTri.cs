using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Assertions;

namespace DungeonGenerator
{
    public  class DelaunayTri
    {
        private List<Triangle> triangles = new();
        private List<Vector2> points = new();

        public void InsertPoint(Vector2 _point)
        {
            points.Add(_point);

            Assert.IsTrue(points.Count >= 3, "Cannot Generate Triangulation, Provided points are less than required to generate a triangle");
            if (points.Count < 3)
                return;

            if (triangles.Count == 0)
                triangles.Add(GetSuperTriangle(points));

            Triangle newInsertion = null;


            foreach (Triangle triangle in triangles)
            {
                if (triangle.PointInCircumCircle(_point))
                {
                    newInsertion = triangle;
                    break;
                }
            }

            if (newInsertion == null)
            {
                Assert.IsNotNull(newInsertion, "Unable to find triange to split for insertion");
                return;
            }

            triangles.Remove(newInsertion);
            Triangle newTriangle1 = new Triangle(newInsertion.Point1, newInsertion.Point2, _point);
            Triangle newTriangle2 = new Triangle(newInsertion.Point2, newInsertion.Point3, _point);
            Triangle newTriangle3 = new Triangle(newInsertion.Point3, newInsertion.Point1, _point);

            newTriangle1.ParentTri = newInsertion;
            newTriangle2.ParentTri = newInsertion;
            newTriangle3.ParentTri = newInsertion;
            newInsertion.ChildrenTri.Add(newTriangle1);
            newInsertion.ChildrenTri.Add(newTriangle2);
            newInsertion.ChildrenTri.Add(newTriangle3);
            triangles.Add(newTriangle1);
            triangles.Add(newTriangle2);
            triangles.Add(newTriangle3);



        }

        public static Triangle GetSuperTriangle(List<Vector2> _points)
        {
            float minimumXPosition = _points[0].x;
            float minimumYPosition = _points[0].y;
            float maximumXPosition = _points[0].x;
            float maximumYPosition = _points[0].y;
            foreach (Vector2 point in _points)
            {
                minimumXPosition = point.x < minimumXPosition ? point.x : minimumXPosition;
                minimumYPosition = point.y < minimumYPosition ? point.y : minimumYPosition;

                maximumXPosition = point.x > maximumXPosition ? point.x : maximumXPosition;
                maximumYPosition = point.y > maximumYPosition ? point.y : maximumYPosition;
            }

            float dx = maximumXPosition - minimumXPosition;
            float dY = maximumYPosition - minimumYPosition;
            Vector2 A = new Vector2(minimumXPosition - dx, minimumYPosition - dY);
            Vector2 B = new Vector2(maximumXPosition + dx, maximumYPosition - dY);
            Vector2 C = new Vector2(minimumXPosition + dx, maximumYPosition + dY);
            
            Triangle superTriangle = new Triangle(A, B, C);
            return superTriangle;

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
