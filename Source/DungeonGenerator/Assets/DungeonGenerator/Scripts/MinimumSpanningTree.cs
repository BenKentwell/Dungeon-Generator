using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Unity.VisualScripting;
using UnityEditor.Experimental.GraphView;
using UnityEditor.MemoryProfiler;
using UnityEngine;
using UnityEngine.Assertions;
using static DungeonGenerator.DelaunayTri;
using static UnityEngine.RuleTile.TilingRuleOutput;
using Edge = DungeonGenerator.DelaunayTri.Triangle.Edge;
using Vector2 = UnityEngine.Vector2;

namespace DungeonGenerator
{

    public class MinimumSpanningTree
    {
        private HashSet<Triangle.Edge> visitedEdges;
        private HashSet<Vector2> visitedNodes;
        public List<Triangle.Edge> minSpanningTree;
        private SortedList<float, Triangle.Edge> queueEdge;

        public void GetTree(List<Triangle> _tree, Vector2 _startNode)
        {
            visitedEdges = new HashSet<Triangle.Edge>();
            visitedNodes = new HashSet<Vector2>();
            minSpanningTree = new List<Triangle.Edge>();
            queueEdge = new SortedList<float, Triangle.Edge>();

            // Find the starting edge
            Edge currentEdge = FindStartingEdge(_tree, _startNode);
            if (currentEdge == null) return;

            Vector2 currentNode = _startNode;

            AddEdgesToQueue(currentNode, _tree);

            // While there are edges in the queue
            while (queueEdge.Count > 0)
            {
                // Get the lowest weighted edge
                var edgePair = queueEdge.First();
                currentEdge = edgePair.Value;
                queueEdge.RemoveAt(0); 

                Vector2 nextNode = GetOppositeNode(currentEdge, currentNode);

                if (visitedNodes.Contains(nextNode))
                {
                    continue;
                }

                visitedEdges.Add(currentEdge);
                visitedNodes.Add(currentNode);

                minSpanningTree.Add(currentEdge);
                AddEdgesToQueue(nextNode, _tree);
               
                currentNode = nextNode;
            }
        }

        private void AddEdgesToQueue(Vector2 node, List<Triangle> _tree)
        {
            // Enqueue all unvisited edges connected to the given node
            List<Edge> connections = GetUnvisitedConnections(node, _tree);
            foreach (var edge in connections)
            {
                if (!visitedEdges.Contains(edge) && !queueEdge.ContainsValue(edge))
                {
                    queueEdge.TryAdd(edge.Weight, edge);
                }
            }
        }

        private Edge FindStartingEdge(List<Triangle> _tree, Vector2 _startNode)
        {
            foreach (Triangle tri in _tree)
            {
                foreach (Triangle.Edge edge in tri.Edges)
                {
                    if ( edge.HasPoint(_startNode))
                    {
                        return edge;
                    }
                }
            }
            return null;
        }

        private List<Edge> GetUnvisitedConnections(Vector2 _node, List<Triangle> _tree)
        {
            List<Edge> connections = new List<Edge>();

            foreach (Triangle tri in _tree)
            {
                foreach (Edge edge in tri.Edges)
                {
                    if (edge.HasPoint(_node) && !visitedEdges.Contains(edge))
                    {
                        connections.Add(edge);
                    }
                }
            }

            return connections;
        }

        public static Vector2 GetOppositeNode(Edge _edge, Vector2 _currentNode)
        {
            return _edge.Point1 == _currentNode ? _edge.Point2 : _edge.Point1;
        }
    }
}
