using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using Unity.Mathematics;
using UnityEditor;
using UnityEngine;
using UnityEngine.Assertions;
using Edge = DungeonGenerator.DelaunayTri.Triangle.Edge;

namespace DungeonGenerator
{
    public class DelaunayTri
    {
        public List<Triangle> triangles = new();
        public List<Vector2> points = new();

        public int iter = 0;

        public Triangle superTriangle { get; private set; }

        public void InsertPoint(Vector2 _point)
        {
            points.Add(_point);


            if (points.Count < 3)
            {
                return;
            }

            if (triangles.Count == 0)
            {
                superTriangle = GetSuperTriangle(points);
                triangles.Add(superTriangle);
                // points.Add(superTriangle.Point1);
                // points.Add(superTriangle.Point2);
                // points.Add(superTriangle.Point3);
                foreach (Vector2 point in points)
                {
                    CreateNewTris(point);
                }

                return;
            }


            CreateNewTris(_point);
        }

        private void CreateNewTris(Vector2 _point)
        {
            Triangle newInsertion = null;
            iter++;
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
                return;
            }

            //Create 3 new tris from the parent tri, All connecting 2 points from the parent and one from the new point
            triangles.Remove(newInsertion);

            Vector2 highest = newInsertion.Point1;
            if(newInsertion.Point2.y > highest.y)
                highest = newInsertion.Point2;
            if (newInsertion.Point3.y > highest.y)
                highest = newInsertion.Point3;


            Vector2 leftMost = new(float.MaxValue, float.MaxValue);
            if (newInsertion.Point1 != highest)
                leftMost = newInsertion.Point1;
            if (newInsertion.Point2.x < leftMost.x && newInsertion.Point2 != highest)
                leftMost = newInsertion.Point2;
            if (newInsertion.Point3.x < leftMost.x && newInsertion.Point3 != highest)
                leftMost = newInsertion.Point3;

            Vector2 rightMost = new(float.MinValue, float.MinValue);
            if (newInsertion.Point1 != highest)
                rightMost = newInsertion.Point1;
            if (newInsertion.Point2.x > rightMost.x && newInsertion.Point2 != highest)
                rightMost = newInsertion.Point2;
            if (newInsertion.Point3.x > rightMost.x && newInsertion.Point3 != highest)
                rightMost = newInsertion.Point3;

            float dx = highest.x - _point.x;
            Triangle newTriangle1;
            //Anticlockwise, (point , hightest other)
            if (dx < 0)
            {
                newTriangle1 = new Triangle(highest, _point, leftMost);
            }
            else//highest, other, point
            {
                newTriangle1 = new Triangle(highest, rightMost, _point);
            }

             
            newTriangle1.index = iter;


            Triangle newTriangle2 = new Triangle(_point, newInsertion.Point3, newInsertion.Point2);
            newTriangle2.index = iter;
            Triangle newTriangle3 = new Triangle(newInsertion.Point1, newInsertion.Point3, _point);
            newTriangle3.index = iter;

            newTriangle1.ParentTri = newInsertion;
            newTriangle2.ParentTri = newInsertion;
            newTriangle3.ParentTri = newInsertion;

            newInsertion.ChildrenTri.Add(newTriangle1);
            newInsertion.ChildrenTri.Add(newTriangle2);
            newInsertion.ChildrenTri.Add(newTriangle3);

            triangles.Add(newTriangle1);
            triangles.Add(newTriangle2);
            triangles.Add(newTriangle3);


            Triangulate(newTriangle1);
            Triangulate(newTriangle2);
            Triangulate(newTriangle3);
        }

        /// <summary>
        /// Find all the triangles that need to be flipped. Called as a result of inserting a nw point
        /// </summary>
        /// <param name="_triangle"></param>
        private void Triangulate(Triangle _triangle)
        {
            List<Triangle> trisToFlip = new List<Triangle>();


            //For each edge, Find the triangle already in the list fo tris with a shared edge. 
            foreach (Edge edge in _triangle.Edges)
            {
                Triangle neighbour = _triangle.GetNeighbour(edge, triangles);

                if (neighbour != null && !IsDelaunayTri(_triangle, neighbour))
                {
                    trisToFlip.Add(neighbour);

                    //FlipEdge(_triangle, neighbour);
                }
            }

            foreach (Triangle flip in trisToFlip)
            {
                FlipEdge(_triangle, flip);
            }
        }

        public void RemoveSuperTriangle()
        {
            List<Triangle> badTris = new List<Triangle>();

            points.Remove(superTriangle.Point1);
            points.Remove(superTriangle.Point2);
            points.Remove(superTriangle.Point3);

            triangles.Remove(superTriangle);
            foreach (Triangle triangle in triangles)
            {
                /*Edge edge = GetSharedEdge(superTriangle, triangle);

                if (edge != null)
               {
                    badTris.Add(triangle);
                    continue;
               }*/


                if (DoesTrisSharePoint(triangle, superTriangle))
                    badTris.Add(triangle);
            }

            foreach (Triangle badTri in badTris)
            {
                triangles.Remove(badTri);
            }
        }

        //Returns the triangle that encapsulates all points 
        private Triangle GetSuperTriangle(List<Vector2> _points)
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
            Vector2 B = new Vector2(maximumXPosition + dx, minimumYPosition - dY);
            Vector2 C = new Vector2(maximumXPosition - dx, maximumYPosition + dY * 2);

            Triangle superTriangle = new Triangle(A, C, B);
            return superTriangle;
        }

        //Returns true if both tris satisfy a delaunay structure. 
        private bool IsDelaunayTri(Triangle _triangle1, Triangle _triangle2)
        {
            Triangle.Edge sharedEdge = GetSharedEdge(_triangle1, _triangle2);
            if (sharedEdge == null)
                return true;

            Vector2 oppositePoint = GetOppositePoint(sharedEdge, _triangle2);

            return _triangle1.PointInCircumCircle(oppositePoint);
        }

        private void FlipEdge(Triangle _triangle1, Triangle _triangle2)
        {
            Edge sharedEdge = GetSharedEdge(_triangle1, _triangle2);
            if (sharedEdge == null)
                return;

            Vector2 oppositePointTriangle1
                = GetOppositePoint(sharedEdge, _triangle2);

            Vector2 oppositePointTriangle2
                = GetOppositePoint(sharedEdge, _triangle1);

            Triangle newTri1 = new Triangle(sharedEdge.Point1, oppositePointTriangle1, oppositePointTriangle2);
            Triangle newTri2 = new Triangle(oppositePointTriangle2, oppositePointTriangle1, sharedEdge.Point2);

            //Remove both triangles from their parents children and add the new tris.
            if (_triangle1.ParentTri != null)
            {
                _triangle1.ParentTri.ChildrenTri.Remove(_triangle1);
                _triangle1.ParentTri.ChildrenTri.Add(newTri1);
            }

            if (_triangle1.ParentTri == null)
            {
                Debug.Log("");
            }

            if (_triangle2.ParentTri != null)
            {
                _triangle2.ParentTri.ChildrenTri.Remove(_triangle2);
                _triangle2.ParentTri.ChildrenTri.Add(newTri2);
            }

            if (_triangle2.ParentTri == null)
            {
                Debug.Log("");
            }

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
                    if (t1Edge.EqualsEdge(t2Edge))
                        return t2Edge;
                }
            }

            return null;
        }

        private bool DoesTrisSharePoint(Triangle _triangle1, Triangle _triangle2)
        {
            List<Vector2> triPoints1 = new() { _triangle1.Point1, _triangle1.Point2, _triangle1.Point3 };

            List<Vector2> triPoints2 = new() { _triangle2.Point1, _triangle2.Point2, _triangle2.Point3 };

            foreach (Vector2 t1p in triPoints1)
            {
                foreach (Vector2 t2p in triPoints2)
                {
                    if (t1p == t2p)
                        return true;
                }
            }

            return false;
        }

        private Vector2 GetOppositePoint(Edge _edge, Triangle _triangle1)
        {
            //
            // if p1 and p2 are shared
            //
            Vector2 opposite = new Vector2();

            if (!_triangle1.Point1.Equals(_edge.Point1) && !_triangle1.Point1.Equals(_edge.Point2))
            {
                opposite = _triangle1.Point1;
                return opposite;
            }

            if (!_triangle1.Point2.Equals(_edge.Point1) && !_triangle1.Point2.Equals(_edge.Point2))
            {
                opposite = _triangle1.Point2;
                return opposite;
            }


            if (!_triangle1.Point3.Equals(_edge.Point1) && !_triangle1.Point3.Equals(_edge.Point2))
            {
                opposite = _triangle1.Point3;
                return opposite;
            }


            return opposite;
            /*if (!_triangle2.Point1.Equals(_point1) && !_triangle2.Point1.Equals(_point2))
                return _triangle2.Point1;

            if (!_triangle2.Point2.Equals(_point1) && !_triangle2.Point2.Equals(_point2))
                return _triangle2.Point2;

            return _triangle2.Point3;*/
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
            public int index = 0;

            public List<Edge> Edges;

            public Triangle(Vector2 _point1, Vector2 _point2, Vector2 _point3)
            {
                ChildrenTri = new List<Triangle>();
                Edges = new List<Edge>();
                Point1 = _point1;
                Point2 = _point2;
                Point3 = _point3;
                radius = GetRadius();

                Circumcentre = GetCircumcentre();
                Edges.Add(new Edge(Point1, Point2));
                Edges.Add(new Edge(Point2, Point3));
                Edges.Add(new Edge(Point3, Point1));
            }

            private Vector2 GetCircumcentre()
            {
                //Cache points
                float x1 = Point1.x, y1 = Point1.y;
                float x2 = Point2.x, y2 = Point2.y;
                float x3 = Point3.x, y3 = Point3.y;

                // Calculate the denominator (the determinant in the formulas)
                float d = 2 * (x1 * (y2 - y3) + x2 * (y3 - y1) + x3 * (y1 - y2));

                // Calculate the circumcenter coordinates (x, y)
                float x = ((x1 * x1 + y1 * y1) * (y2 - y3) +
                           (x2 * x2 + y2 * y2) * (y3 - y1) +
                           (x3 * x3 + y3 * y3) * (y1 - y2)) / d;

                float y = ((x1 * x1 + y1 * y1) * (x3 - x2) +
                           (x2 * x2 + y2 * y2) * (x1 - x3) +
                           (x3 * x3 + y3 * y3) * (x2 - x1)) / d;

                return new Vector2(x, y);
            }

            public bool PointInCircumCircle(Vector2 _point)
            {
                //Cache Data
                float ax = Point1.x - _point.x;
                float ay = Point1.y - _point.y;
                float bx = Point2.x - _point.x;
                float by = Point2.y - _point.y;
                float cx = Point2.x - _point.x;
                float cy = Point2.x - _point.y;

                float d = ((ax * ax + ay * ay) * (bx * cx - cx * by) -
                           (bx * bx + by * by) * (ax * cy - cx * ay) +
                           (cx * cx + cy * cy) * (ax * by - bx * ay)
                    );

                // float distance = Vector2.Distance(pointToCheck, Circumcentre);
                return d >= 0;
            }

            public float GetRadius()
            {
                float a, b, c;
                a = Vector2.Distance(Point1, Point2);
                b = Vector2.Distance(Point2, Point3);
                c = Vector2.Distance(Point3, Point1);

                float bottomEquation = (a + b + c) * (b + c - a) * (c + a - b) * (a + b - c);
                float rad = (a * b * c) / (MathF.Sqrt(bottomEquation));
                return rad;
            }

            //Returns the neighbouring triangle of this edge.
            public Triangle GetNeighbour(Edge _edge, List<Triangle> _tris)
            {
                //This is so wrong, what was i thinking ! 
                /*Vector2 point1 = _edge.Point1;
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
                        t.Point1.Equals(Point2) || t.Point2.Equals(Point2) || t.Point3.Equals(Point2));

                return null;
                */


                foreach (Triangle tri in ParentTri.ChildrenTri)
                {
                    if (tri == this)
                        continue;

                    foreach (Edge edge in tri.Edges)
                    {
                        if (edge.EqualsEdge(_edge))
                        {
                            return tri;
                        }
                    }
                }

                return null;
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

                /*public override bool Equals(object obj)
                {
                    Edge other = (Edge)obj;


                    if (other.Point1 == Point1 && other.Point2 == Point2 ||
                        other.Point1 == Point2 && other.Point2 == Point1)
                        return true;


                    return false;
                }*/

                public bool EqualsEdge(Edge other)
                {
                    if (Point1.Equals(other.Point1) && Point2.Equals(other.Point2) ||
                        Point2.Equals(other.Point1) && Point1.Equals(other.Point2))
                        return true;

                    return false;
                }

                public override int GetHashCode()
                {
                    return HashCode.Combine(Point1, Point2);
                }
            }
        }
    }
}