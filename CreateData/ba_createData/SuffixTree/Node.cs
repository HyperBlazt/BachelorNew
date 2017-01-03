// https://github.com/atillabyte/SuffixTree/tree/master/src/SuffixTree
// d.15-11-2016

using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace ba_createData.suffixTree
{
    [Serializable]
    [DebuggerDisplay("Label={Label}, Edges.Count={Edges.Count}")]
    public class Node
    {
        internal uint Label { get; set; }
        internal Dictionary<char, Edge> Edges { get; private set; }

        internal Node SuffixPointer { get; set; }

        public Node(uint label)
        {
            this.Label = label;
            this.Edges = new Dictionary<char, Edge>();
            this.SuffixPointer = null;
        }

        /// <summary>
        /// finds next route starting from the current node
        /// </summary>
        /// <param name="start"></param>
        /// <param name="followSuffixNode"></param>
        /// <returns></returns>
        internal Tuple<Node, Edge> FindNextRoute(int start, bool followSuffixNode)
        {
            if (followSuffixNode && null != SuffixPointer)
            {
                return new Tuple<Node, Edge>(SuffixPointer, null);
            }

            var edge = FindEdgeByChar(start);
            if (null == edge)
            {
                return null;
            }

            // search terminated in a node
            return edge.Route.Length == 1 ? new Tuple<Node, Edge>(edge.EndNode, edge) : new Tuple<Node, Edge>(null, edge);

            //search did not terminate in a node
        }

        /// <summary>
        /// Adds a new node to the tree
        /// </summary>
        /// <param name="label">Node label</param>
        /// <param name="start">Start position in the text</param>
        /// <param name="end">End position in the text</param>
        internal void AddNode(uint label, int start, int end = -1)
        {
            var newNode = new Node(label);
            var newEdge = new Edge(newNode, start, end);
            Edges.Add(newEdge.Route[0], newEdge);
        }

        internal Edge FindEdgeByChar(int start)
        {
            //we have reached the end of the string
            return start >= SuffixTree.Text.Length ? null : FindEdgeByChar(SuffixTree.Text[start]);
        }

        internal Edge FindEdgeByChar(char c)
        {
            return !Edges.ContainsKey(c) ? null : Edges[c];
        }
    }
}