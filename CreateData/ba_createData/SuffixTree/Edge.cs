// https://github.com/atillabyte/SuffixTree/tree/master/src/SuffixTree
// d.15-11-2016

using System;
using System.Diagnostics;

namespace ba_createData.suffixTree
{
    [Serializable]
    [DebuggerDisplay("Start={Start}, End={End}, Route={Route}")]
    public class Edge
    {
        internal Node EndNode { get; private set; }
        internal int Start { get; private set; }
        internal int End { get; private set; }

        internal string Route => GetSubstring();

        /// <summary>
        /// constructor that takes relative text position
        /// </summary>
        /// <param name="node"></param>
        /// <param name="start"></param>
        /// <param name="end"></param>
        internal Edge(Node node, int start, int end = -1)
        {
            if (node == null)
            {
                throw new ArgumentNullException(nameof(node));
            }

            if (start < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(start), @"start cannot be negative");
            }

            // pretend that "end" can be infinite, and then compare with start
            if (start > (uint)end)
            {
                throw new ArgumentOutOfRangeException(nameof(start), @"cannot start the string after its end");
            }

            // infinity is just -1
            if (end < 0)
            {
                end = -1;
            }

            this.Start = start;
            this.End = end;
            this.EndNode = node;
        }

        private int GetLength()
        {
            return this.End < 0 ? SuffixTree.Text.Length - this.Start : this.End - this.Start + 1;
        }

        private string GetSubstring()
        {
            return SuffixTree.Text.Substring(this.Start, GetLength());
        }

        /// <summary>
        /// Splits the edge into two new edges.
        /// </summary>
        /// <param name="end">Index of the end of the old edge</param>
        /// <param name="currentNodeNumber"></param>
        /// <returns></returns>
        internal Node Split(int end, uint currentNodeNumber)
        {
            var nextStart = end + 1;
            var oldNode = EndNode;

            var newEdge = new Edge(oldNode, nextStart, this.End);
            var newNode = new Node(currentNodeNumber);

            End = end;
            EndNode = newNode;
            newNode.Edges.Add(newEdge.Route[0], newEdge);
            return newNode;
        }

        /// <summary>
        /// Keep comparing original text from position i
        /// with what is in the edge
        /// </summary>
        /// <param name="i">Index of comparison start in the original text</param>
        /// <param name="activeLength"></param>
        /// <param name="minDistance"></param>
        /// <param name="activeNode"></param>
        /// <returns>(edge, index) - the edje the character in it where the walk ended</returns>
        internal Tuple<Edge, int> WalkTheEdge(int i, ref int activeLength, ref int minDistance, ref Node activeNode)
        {
            var text = SuffixTree.Text;
            var skipCharacters = minDistance;
            var index = i + activeLength;

            // we know we do not need any comparisons on this edge
            if (skipCharacters >= Route.Length)
            {
                var edge = EndNode.FindEdgeByChar(i + Route.Length);
                activeLength += Route.Length;
                minDistance -= Route.Length;

                activeNode = EndNode;
                return edge.WalkTheEdge(i, ref activeLength, ref minDistance, ref activeNode);
            }

            var j = Walk(text, index, skipCharacters);
            return new Tuple<Edge, int>(this, j);
        }

        /// <summary>
        /// Walk this single edge to see whether it matches the substring
        /// </summary>
        /// <param name="suffix">Search string</param>
        /// <param name="i">Starting index</param>
        /// <param name="skip"></param>
        /// <returns></returns>
        internal int Walk(string suffix, int i, int skip = 0)
        {
            int j;
            for (j = skip, i += j; j < Route.Length && i < suffix.Length; j++, i++)
            {
                if (Route[j] != suffix[i])
                {
                    break;
                }
            }

            return j;
        }
    }
}