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
using Edge = Unity.VisualScripting.Edge;
using Vector2 = UnityEngine.Vector2;

namespace DungeonGenerator
{
    public class MinimumSpanningTree
    {
        private List<Triangle.Edge> queue;
        HashSet<Node> closed;
        public List<Triangle.Edge> minSpanningTree;
        private List<Triangle.Edge> visitedEdges;
        private List<Vector2> visitedNodes;

        public Queue<Triangle.Edge> queueEdge;

        public void GetTree(List<Triangle> _tree, Vector2 _startNode)
        {
            // Initialize the necessary collections
            queueEdge = new Queue<Triangle.Edge>();
            visitedEdges = new List<Triangle.Edge>();
            visitedNodes = new List<Vector2>();
            minSpanningTree = new List<Triangle.Edge>();

            // Variable to hold the current edge and node
            Triangle.Edge CurrentEdge = null;
            Vector2 CurrentNode = _startNode;

            // Create a list of all edges from the triangles
            List<Triangle.Edge> edges = new List<Triangle.Edge>();
            foreach (Triangle tri in _tree)
            {
                foreach (Triangle.Edge edge in tri.Edges)
                {
                    if (!edges.Contains(edge))
                    {
                        edges.Add(edge);

                        // Initialize CurrentEdge if it connects to the start node
                        if (edge.HasPoint(_startNode) && CurrentEdge == null)
                        {
                            CurrentEdge = edge;
                        }
                    }
                }
            }

            // If no edge was found that connects to the start node, return
            if (CurrentEdge == null)
            {
                return;
            }

            // Enqueue the first edge to start the MST process
            queueEdge.Enqueue(CurrentEdge);

            // Process the edges until we build the MST
            while (queueEdge.Count > 0)
            {
                // Dequeue the edge and mark it as visited
                CurrentEdge = queueEdge.Dequeue();
                visitedEdges.Add(CurrentEdge);

                

                // Find the point this edge connects to
                Vector2 nextNode = GetOpppositeNode(CurrentEdge, CurrentNode);

                // Add the current node to visited nodes
                visitedNodes.Add(CurrentNode);

                // List to hold edges connected to the next node
                List<Triangle.Edge> connections = new List<Triangle.Edge>();

                // Find all edges that are connected to the next node and haven't been visited yet
                foreach (Triangle.Edge e in edges)
                {
                    // If edge contains the next node, and we haven't visited either of the edge's points
                    if (e.HasPoint(nextNode) && !visitedEdges.Contains(e) && !visitedNodes.Contains(e.Point1) && !visitedNodes.Contains(e.Point2))
                    {
                        connections.Add(e);

                        // Enqueue the edge to the queue if not already there
                        if (!queueEdge.Contains(e))
                        {
                            
                            queueEdge.Enqueue(e);
                        }
                    }
                }
                
                if (connections.Count > 0)
                {
                    // Add the current edge to the minimum spanning tree
                    minSpanningTree.Add(CurrentEdge);
                    // Select the minimum weight edge from the connections
                    Triangle.Edge lowestEdge = connections[0];
                    foreach (Triangle.Edge connection in connections)
                    {
                        if (connection.Weight < lowestEdge.Weight)
                        {
                            lowestEdge = connection;
                        }
                    }

                    // Update current node and current edge
                    CurrentNode = nextNode;
                    CurrentEdge = lowestEdge;
                }
                queueEdge = new Queue<Triangle.Edge>(queueEdge.OrderBy(x => x.Weight));
            }
        }

        public static Vector2 GetOpppositeNode(Triangle.Edge _edge, Vector2 _currentNode)
        {
            Vector2 opp;
            opp = _edge.Point1 == _currentNode ? _edge.Point2 : _edge.Point1;
            return opp;
        }
    }
}