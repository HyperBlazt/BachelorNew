// https://github.com/atillabyte/SuffixTree/tree/master/src/SuffixTree
// d.15-11-2016

using System.Xml.Serialization;

namespace ba_createData.suffixTree
{

    using System;
    using System.Collections.Generic;
    using System.Diagnostics;

    /// <summary>
    /// Visit the node
    /// </summary>
    /// <param name="sourceLabel">Node label</param>
    public delegate void VisitNode(int sourceLabel);

    /// <summary>
    /// Visit the edge
    /// </summary>
    /// <param name="source">Source node label</param>
    /// <param name="target">Target node label</param>
    /// <param name="start">Start of the suffix</param>
    /// <param name="end">End of the suffix</param>
    public delegate void VisitEdge(int source, int target, int start, int end);

    [Serializable]
    [DebuggerDisplay("_activeLength={_activeLength}, _minDistance={_minDistance}, _currentNodeNumber={_currentNodeNumber}")]
    public class SuffixTree
    {
        private Node _activeNode;

        // keep track of last branch nodes to add suffix pointers
        private Node _lastBranchNode;


        private int _activeLength;
        private int _lastBranchIndex;
        private uint _currentNodeNumber;
        private int _minDistance ;

        /// <summary>
        /// Root node for walking the tree
        /// </summary>
        internal Node RootNode { get; private set; }


        public string CompleteString => Text;

        /// <summary>
        /// Original text
        /// </summary>
        public static string Text { get; set; }

        /// <summary>
        /// Try finding the suffix in the tree
        /// </summary>
        /// <param name="suffix"></param>
        /// <param name="start"></param>
        /// <param name="end"></param>
        /// <returns></returns>
        public bool TryFind(string suffix, ref int start, ref int end)
        {
            if (string.IsNullOrWhiteSpace(suffix))
            {
                return false;
            }

            var current = this.RootNode;
            Edge edge = null;
            var setEnd = true;

            for (var i = 0; i < suffix.Length;)
            {
                edge = current.FindEdgeByChar(suffix[i]);

                if (edge == null)
                {
                    return false;
                }

                // we skip one character in the edge as well as the
                // suffix since it was already picked up by the call above
                var j = edge.Walk(suffix, i, 1);
                i += j;

                // if we have walked the edge...
                if (i >= suffix.Length)
                {
                    // ... and terminated before reaching a node
                    if (j < edge.Route.Length)
                    {
                        end = NormalizeEndValue(edge.End) - (edge.Route.Length - j);
                        setEnd = false;
                    }
                    break;
                }

                if (j < edge.Route.Length)
                {
                    return false;
                }

                current = edge.EndNode;
            }

            if (setEnd)
            {
                if (edge != null) end = NormalizeEndValue(edge.End);
            }

            start = end - suffix.Length + 1;
            return true;
        }

        /// <summary>
        /// Construct the tree for a given string of text
        /// </summary>
        public SuffixTree()
        {
        }

        /// <summary>
        /// Creates the actual suffix tree
        /// </summary>
        public void Create(string text)
        {
            Text = text;
            if (string.IsNullOrWhiteSpace(text))
            {
                throw new ArgumentNullException(text);
            }
            _activeNode = new Node(_currentNodeNumber);
            RootNode = _activeNode;
            // some of our loop iterations actually constitute one route
            // in that case we should not chose to accidentally slip and 
            // follow the suffix node
            var followSuffixNode = false;

            for (var i = 0; i < Text.Length;)
            {
                // make sure the lower bound remains within its boundaries
                ValidateAndUpdateMinDistance(i);

                var nodeEdge = _activeNode.FindNextRoute(i + _activeLength, followSuffixNode);

                //if we have terminated in a non-leaf node we are done
                if (i + _activeLength >= Text.Length && nodeEdge == null)
                {
                    break;
                }

                // we could not find anything, add to the tree
                if (nodeEdge == null)
                {
                    _activeNode.AddNode(++_currentNodeNumber, i + _activeLength);
                    _lastBranchIndex = i + _activeLength;
                    i++;
                    followSuffixNode = true;
                    continue;
                }

                var node = nodeEdge.Item1;
                var edge = nodeEdge.Item2;

                if (edge == null)
                {
                    //we found a suffix node
                    _activeNode = node;
                    _activeLength--;
                    followSuffixNode = false;
                    continue;
                }
                else if (node != null)
                {
                    //we found a new active
                    _activeNode = node;
                    _activeLength++;
                    followSuffixNode = false;
                    continue;
                }

                // now walk the chosen path and see where the current suffix diverges
                var edgePosTuple = edge.WalkTheEdge(i, ref _activeLength, ref _minDistance, ref _activeNode);

                edge = edgePosTuple.Item1;
                var j = edgePosTuple.Item2;

                if (j == edge.Route.Length)
                {
                    _activeNode = edge.EndNode;
                    _activeLength += edge.Route.Length;
                    followSuffixNode = false;
                    continue;
                }

                // we now need to insert a new branch node
                _minDistance = j;
                _lastBranchIndex = i + j + _activeLength;

                if (_lastBranchIndex >= Text.Length)
                {
                    i++;
                    followSuffixNode = true;
                    continue;
                }

                // we are inserting a new branch node
                var newBranchNode = edge.Split(edge.Start + j - 1, ++_currentNodeNumber);

                // if we have reached this branch node through a route of just
                // one character - the last branch node should be set as the 
                if (edge.Route.Length == 1)
                {
                    newBranchNode.SuffixPointer = _activeNode;
                }

                // the second check is because of the root-node suffix pointer special case
                // above
                if (null != _lastBranchNode && _lastBranchNode.SuffixPointer == null)
                {
                    _lastBranchNode.SuffixPointer = newBranchNode;
                }

                newBranchNode.AddNode(++_currentNodeNumber, _lastBranchIndex);
                _lastBranchNode = newBranchNode;
                i++;
                followSuffixNode = true;
            }
        }

        /// <summary>
        /// Breadth-first walk of the tree
        /// </summary>
        public void WalkTree(VisitNode visitNode, VisitEdge visitEdge)
        {
            Queue<Node> walkingQueue = new Queue<Node>();

            for (walkingQueue.Enqueue(this.RootNode); walkingQueue.Count > 0;)
            {
                var currentNode = walkingQueue.Dequeue();
                visitNode((int)currentNode.Label);
                foreach (var edge in currentNode.Edges)
                {
                    walkingQueue.Enqueue(edge.Value.EndNode);
                    visitEdge((int)currentNode.Label, (int)edge.Value.EndNode.Label, edge.Value.Start, NormalizeEndValue(edge.Value.End));
                }
            }
        }

        private static int NormalizeEndValue(int end)
        {
            if (end < 0)
            {
                return Text.Length - 1;
            }

            return end;
        }

        // makes sure that minDistance remains a lower bound
        // in the equation lastBranchIndex >= i + activeLength + minDistance
        private void ValidateAndUpdateMinDistance(int index)
        {
            if (_lastBranchIndex < _activeLength + _minDistance + index)
            {
                _minDistance = Math.Max(0, _lastBranchIndex - _activeLength - index);
            }
        }

    }
}