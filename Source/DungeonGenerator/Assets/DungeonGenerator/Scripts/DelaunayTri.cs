using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using Unity.Mathematics;
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.UIElements;
using static DungeonGenerator.DelaunayTri;
using static UnityEngine.RuleTile.TilingRuleOutput;
using Edge = DungeonGenerator.DelaunayTri.Triangle.Edge;

namespace DungeonGenerator
{
    public class DelaunayTri
    {
        public List<Triangle> triangles = new();
        public List<Vector2> points = new();
       public List<Edge> edges = new();

       public MinimumSpanningTree MST = new MinimumSpanningTree();

        public int iter = 0;

        public Triangle SuperTriangle { get; private set; }

        public void AddPoint(Vector2 _point)
        {
            points.Add(_point);
        }


        public void TriangulateAll()
        {
            
                SuperTriangle = GetSuperTriangle(points);
                triangles.Add(SuperTriangle);
                foreach (Vector2 point in points)
                {
                    List<Edge> polygon = new List<Edge>();

                    foreach (Triangle t in triangles)
                    {
                        if (t.PointInCircumCircle(point))
                        {
                            t.isBad = true;
                            polygon.Add(new Edge(t.Point1, t.Point2));
                            polygon.Add(new Edge(t.Point2, t.Point3));
                            polygon.Add(new Edge(t.Point3, t.Point1));
                        }
                    }

                    triangles.RemoveAll((Triangle _t) => _t.isBad);

                    for (int i = 0; i < polygon.Count; i++)
                    {
                        for (int j = i+1; j < polygon.Count; j++)
                        {
                            if (polygon[i].EqualsEdge(polygon[j]))
                            {
                                polygon[i].isBad = true;
                                polygon[j].isBad = true;
                            }
                        }
                    }

                    polygon.RemoveAll(_e => _e.isBad);

                    foreach (var edge in polygon)
                    {
                        triangles.Add(new Triangle(edge.Point1, edge.Point2, point));
                    }
                }

                triangles.RemoveAll((Triangle _t) => _t.ContainsVertex(SuperTriangle.Point1) || _t.ContainsVertex(SuperTriangle.Point2) ||
                                                    _t.ContainsVertex(SuperTriangle.Point3));

                HashSet<Edge> edgeSet = new HashSet<Edge>();

                foreach (Triangle t in triangles)
                {
                    var ab = new Edge(t.Point1, t.Point2);
                    var bc = new Edge(t.Point2, t.Point3);
                    var ca = new Edge(t.Point3, t.Point1);

                    if (edgeSet.Add(ab))
                    {
                        edges.Add(ab);
                    }

                    if (edgeSet.Add(bc))
                    {
                        edges.Add(bc);
                    }

                    if (edgeSet.Add(ca))
                    {
                        edges.Add(ca);
                    }
                }
        }

        //Returns the triangle that encapsulates all points 
        
        private Triangle GetSuperTriangle(List<Vector2> _points)
        {
            float minX = points[0].x;
            float minY = points[0].y;
            float maxX = minX;
            float maxY = minY;

            foreach (Vector2 vertex in points)
            {
                if (vertex.x < minX) minX = vertex.x;
                if (vertex.x > maxX) maxX = vertex.x;
                if (vertex.y < minY) minY = vertex.y;
                if (vertex.y > maxY) maxY = vertex.y;
            }


            float dx = maxX - minX;
            float dy = maxY - minY;
            float deltaMax = Mathf.Max(dx, dy) * 2;

            Vector2 p1 = new Vector2(minX - 1, minY - 1);
            Vector2 p2 = new Vector2(minX - 1, maxY + deltaMax);
            Vector2 p3 = new Vector2(maxX + deltaMax, minY - 1);

            return new Triangle(p1, p2, p3);
        }

        //Returns true if both tris satisfy a delaunay structure. 
        private bool IsDelaunayTri(Triangle _triangle1, Triangle _triangle2)
        {
            Edge sharedEdge = GetSharedEdge(_triangle1, _triangle2);
            if (sharedEdge == null)
                return true;

            Vector2 oppositePoint = GetOppositePoint(sharedEdge,_triangle1);

            return _triangle1.PointInCircumCircle(oppositePoint);
        }

        private bool FlipEdge(Triangle _triangle1, Triangle _triangle2)
        {
            Edge sharedEdge = GetSharedEdge(_triangle1, _triangle2);
            if (sharedEdge == null)
                return false;
            
            Vector2 oppositePointTriangle1 =
                GetOppositePoint(sharedEdge, _triangle2);
            Vector2 oppositePointTriangle2 =
                GetOppositePoint(sharedEdge, _triangle1);

            Triangle newTri1 = new Triangle(sharedEdge.Point1, oppositePointTriangle1, oppositePointTriangle2);
            Triangle newTri2 = new Triangle(sharedEdge.Point2, oppositePointTriangle2, oppositePointTriangle1);

            triangles.Remove(_triangle1);
            triangles.Remove(_triangle2);

            triangles.Add(newTri1);
            triangles.Add(newTri2);

            newTri1.ParentTri = _triangle1.ParentTri;
            newTri2.ParentTri = _triangle2.ParentTri;

            // Update parent-child relationships
            if (_triangle1.ParentTri != null)
            {
                _triangle1.ParentTri.ChildrenTri.Remove(_triangle1);
                newTri1.ParentTri.ChildrenTri.Add(newTri1);
            }

            if (_triangle2.ParentTri != null)
            {
                _triangle2.ParentTri.ChildrenTri.Remove(_triangle2);
                newTri2.ParentTri.ChildrenTri.Add(newTri2);
            }

            return true;
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

        private Vector2 GetOppositePoint(Edge _commonEdge, Triangle _triangle)
        {
            // Ensure that we check both directions of the shared edge
            if (!Vector2.Equals(_triangle.Point1, _commonEdge.Point1) && !Vector2.Equals(_triangle.Point1, _commonEdge.Point2))
            {
                return _triangle.Point1; // This is the opposite point to the shared edge
            }
            else if (!Vector2.Equals(_triangle.Point2, _commonEdge.Point1) && !Vector2.Equals(_triangle.Point2, _commonEdge.Point2))
            {
                return _triangle.Point2; // This is the opposite point to the shared edge
            }
            else if (!Vector2.Equals(_triangle.Point3, _commonEdge.Point1) && !Vector2.Equals(_triangle.Point3, _commonEdge.Point2))
            {
                return _triangle.Point3; // This is the opposite point to the shared edge
            }

            // In case no opposite point was found (should not happen)
            return Vector2.zero;
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

            public bool isBad;

            public List<Edge> Edges;

            public Triangle(Vector2 _point1, Vector2 _point2, Vector2 _point3)
            {

                isBad = false;
                ChildrenTri = new List<Triangle>();
                Edges = new List<Edge>();
                if (!IsCounterClockwise(_point1, _point2, _point3))
                {
                    Point1 = _point1;
                    Point2 = _point3;
                    Point3 = _point2;
                }
                else
                {
                    Point1 = _point1;
                    Point2 = _point2;
                    Point3 = _point3;
                }

                radius = GetRadius();

                Circumcentre = GetCircumcentre();
                Edges.Add(new Edge(Point1, Point2));
                Edges.Add(new Edge(Point2, Point3));
                Edges.Add(new Edge(Point3, Point1));

               
            }

            private bool IsCounterClockwise(Vector2 point1, Vector2 point2, Vector2 point3)
            {
                var result = (point2.x- point1.x) * (point3.y - point1.y) -
                             (point3.x- point1.x) * (point2.y - point1.y);
                return result > 0;
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

            public bool PointInCircumCircle(Vector3 _point)
            {
                Vector3 a = Point1;
                Vector3 b = Point2;
                Vector3 c = Point3;

                float ab = a.sqrMagnitude;
                float cd = b.sqrMagnitude;
                float ef = c.sqrMagnitude;

                float circumX = (ab * (c.y - b.y) + cd * (a.y - c.y) + ef * (b.y - a.y)) / (a.x * (c.y - b.y) + b.x * (a.y - c.y) + c.x * (b.y - a.y));
                float circumY = (ab * (c.x - b.x) + cd * (a.x - c.x) + ef * (b.x - a.x)) / (a.y * (c.x - b.x) + b.y * (a.x - c.x) + c.y * (b.x - a.x));

                Vector3 circum = new Vector3(circumX / 2, circumY / 2);
                float circumRadius = Vector3.SqrMagnitude(a - circum);
                float dist = Vector3.SqrMagnitude(_point - circum);
                return dist <= circumRadius;
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

            public bool ContainsVertex(Vector2 v)
            {
                return Vector2.Distance(v, Point1) < 0.01f
                       || Vector2.Distance(v, Point2) < 0.01f
                       || Vector2.Distance(v, Point3) < 0.01f;
            }

            public class Edge
            {
                public Vector2 Point1;
                public Vector2 Point2;
                public bool isBad;

                public float Weight { get; private set; }
                public Edge(Vector2 _point1, Vector2 _point2)
                {
                    isBad = false;
                    Point1 = _point1;
                    Point2 = _point2;
                    Weight = Vector2.Distance(Point1, Point2);
                }

                public bool HasPoint(Vector2 _point)
                {
                    bool has = false;
                    has = (_point == Point1 || _point == Point2) ? true : false;
                    return has;
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
                    float epsilon = 0.001f;
                    float distance1 = Vector2.Distance(other.Point1, Point1);
                    float distance2 = Vector2.Distance(other.Point2, Point2);
                    float distance3 = Vector2.Distance(other.Point2, Point1);
                    float distance4 = Vector2.Distance(other.Point1, Point2);

                    if((distance1 < epsilon && distance2 < epsilon) || (distance3 < epsilon && distance4 < epsilon))
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