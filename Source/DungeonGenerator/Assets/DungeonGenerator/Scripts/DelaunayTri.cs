using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Assertions;

namespace DungeonGenerator
{
    public class DelaunayTri
    {
        private List<Triangle> triangles = new();
        private List<Vector2> points = new();

        public void InsertPoint(Vector2 _point)
        {
            points.Add(_point);

            Assert.IsTrue(points.Count >= 3
                , "Cannot Generate Triangulation, Provided points are less than required to generate a triangle");
            if (points.Count < 3)
                return;

            if (triangles.Count == 0)
                triangles.Add(GetSuperTriangle(points));

            Triangle newInsertion = null;

            //Find a triangle that bounds this point
            foreach (Triangle triangle in triangles)
            {
                if (triangle.PointInCircumCircle(_point))
                {
                    newInsertion = triangle;
                    break;
                }
            }

            //Validation. This shouldt get hit
            if (newInsertion == null)
            {
                Assert.IsNotNull(newInsertion, "Unable to find triange to split for insertion");
                return;
            }

            //Create 3 new tris from the parent tri, All connecting 2 points from the parent and one from the new point
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

        /// <summary>
        /// Find all the triangles that need to be flipped. Called as a result of inserting a nw point
        /// </summary>
        /// <param name="_triangle"></param>
        private void Triangulate(Triangle _triangle)
        {
            List<Triangle> trisToFlip = new List<Triangle>();
            List<Edge> edges = new List<Edge>();
            edges.Add(new Edge(_triangle.Point1, _triangle.Point2));
            edges.Add(new Edge(_triangle.Point2, _triangle.Point3));
            edges.Add(new Edge(_triangle.Point3, _triangle.Point1));

            //For each edge, Find the triangle already in the list fo tris with a shared edge. 
            foreach (Edge edge in edges)
            {
                Triangle neighbour = _triangle.GetNeighbour(edge);
                if (neighbour != null && !IsDelaunayTri(_triangle, neighbour))
                {
                    trisToFlip.Add(neighbour);
                }
            }

            foreach (Triangle flip in trisToFlip)
            {
                FlipEdge(_triangle, flip);
            }
        }

        //Returns the triangle that encapsulates all points 
        private static Triangle GetSuperTriangle(List<Vector2> _points)
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

        //Returns true if both tris satisfy a delaunay structure. 
        private bool IsDelaunayTri(Triangle _triangle1, Triangle _triangle2)
        {
            Edge sharedEdge = GetSharedEdge(_triangle1, _triangle2);
            if (sharedEdge == null)
                return true;

            Vector2 oppositePoint = GetOppositePoint(sharedEdge.Point1, sharedEdge.Point2, _triangle1, _triangle2);

            return _triangle1.PointInCircumCircle(oppositePoint);
        }

        private void FlipEdge(Triangle _triangle1, Triangle _triangle2)
        {
            Edge sharedEdge = GetSharedEdge(_triangle1, _triangle2);
            if (sharedEdge == null)
                return;

            Vector2 oppositePointTriangle1
                = GetOppositePoint(sharedEdge.Point1, sharedEdge.Point2, _triangle1, _triangle2);

            Vector2 oppositePointTriangle2
                = GetOppositePoint(sharedEdge.Point1, sharedEdge.Point2, _triangle2, _triangle1);

            Triangle newTri1 = new Triangle(sharedEdge.Point1, oppositePointTriangle1, oppositePointTriangle2);
            Triangle newTri2 = new Triangle(sharedEdge.Point2, oppositePointTriangle2, oppositePointTriangle1);

            //Remove both triangles from their parents children and add the new tris.
            _triangle1.ParentTri.ChildrenTri.Remove(_triangle1);
            _triangle2.ParentTri.ChildrenTri.Remove(_triangle2);
            _triangle1.ParentTri.ChildrenTri.Add(newTri1);
            _triangle2.ParentTri.ChildrenTri.Add(newTri2);

            triangles.Remove(_triangle1);
            triangles.Remove(_triangle2);

            triangles.Add(newTri1);
            triangles.Add(newTri2);
        }


        private Edge GetSharedEdge(Triangle _triangle1, Triangle _triangle2)
        {
            foreach (Edge t1Edge in _triangle1.Edges)
            {
                foreach (Edge t2Edge in _triangle2.Edges)
                {
                    if (t1Edge.Equals(t2Edge))
                        return t1Edge;
                }
            }

            return null;
        }

        private Vector2 GetOppositePoint(Vector2 _point1, Vector2 _point2, Triangle _triangle1, Triangle _triangle2)
        {
            if (!_triangle1.Point1.Equals(_point1) && !_triangle1.Point1.Equals(_point2))
                return _triangle1.Point1;

            if (!_triangle1.Point2.Equals(_point1) && !_triangle1.Point2.Equals(_point2))
                return _triangle1.Point2;

            if (!_triangle1.Point3.Equals(_point1) && !_triangle1.Point3.Equals(_point2))
                return _triangle1.Point3;

            if (!_triangle2.Point1.Equals(_point1) && !_triangle2.Point1.Equals(_point2))
                return _triangle2.Point1;

            if (!_triangle2.Point2.Equals(_point1) && !_triangle2.Point2.Equals(_point2))
                return _triangle2.Point2;

            return _triangle2.Point3;
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

            public List<Edge> Edges;

            public Triangle(Vector2 _point1, Vector2 _point2, Vector2 _point3)
            {
                Point1 = _point1;
                Point2 = _point2;
                Point3 = _point3;
                radius = GetRadius();

                Edges.Add(new Edge(Point1, Point2));
                Edges.Add(new Edge(Point2, Point3));
                Edges.Add(new Edge(Point3, Point1));
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

                float bottomEquation = (a + b + c) * (b + c - a) * (c + a - b) * (a + b - c);
                float rad = (a * b * c) / MathF.Sqrt(bottomEquation);
                return rad;
            }

            //Returns the neighbouring triangle of this edge.
            public Triangle GetNeighbour(Edge _edge)
            {
                //Cache data

                Vector2 point1 = _edge.Point1;
                Vector2 point2 = _edge.Point2;

                if (point1.Equals(Point1) && point2.Equals(Point2) ||
                    point1.Equals(Point2) && point2.Equals(Point1))
                    return ChildrenTri.FirstOrDefault(t =>
                        t.Point1.Equals(Point3) || t.Point2.Equals(Point3) || t.Point3.Equals(Point3));

                if (point1.Equals(Point2) && point2.Equals(Point3) || point1.Equals(Point3) && point2.Equals(Point2))
                    return ChildrenTri.FirstOrDefault(t =>
                        t.Point1.Equals(Point1) || t.Point2.Equals(Point1) || t.Point3.Equals(Point1));


                if (point1.Equals(Point3) && point2.Equals(Point1) || point1.Equals(Point1) && point2.Equals(Point3))
                    return ChildrenTri.FirstOrDefault(t =>
                        t.Point1.Equals(Point2) || t.Point1.Equals(Point2) || t.Point1.Equals(Point2));

                return null;
            }
        }

        public class Edge
        {
            public Vector2 Point1;
            public Vector2 Point2;


            public Edge(Vector2 _point1, Vector2 _point2)
            {
                Point1 = _point1;
                Point2 = _point2;
            }

            public override bool Equals(object obj)
            {
                Edge other = (Edge)obj;


                if (other.Point1 == Point1 && other.Point2 == Point2 ||
                    other.Point1 == Point2 && other.Point2 == Point1)
                    return true;


                return false;
            }
        }
    }
}