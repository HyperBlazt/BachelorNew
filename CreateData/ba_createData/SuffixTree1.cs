using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace ba_createData
{
    /// <summary>
    /// Taken from https://gist.github.com/axefrog/2373868
    /// C# Suffix tree implementation based on Ukkonen's algorithm. 
    /// Full explanation here: http://stackoverflow.com/questions/9452701/ukkonens-suffix-tree-algorithm-in-plain-english
    /// </summary>
    [Serializable]
    public class SuffixTree
    {
        public char? CanonizationChar { get; set; }
        public string Word { get; set; }
        private int CurrentSuffixStartIndex { get; set; }
        private int CurrentSuffixEndIndex { get; set; }
        private Node LastCreatedNodeInCurrentIteration { get; set; }
        private int UnresolvedSuffixes { get; set; }
        public Node RootNode { get; set; }
        public Node ActiveNode { get; set; }
        private Edge ActiveEdge { get; set; }
        private int DistanceIntoActiveEdge { get; set; }
        private char LastCharacterOfCurrentSuffix { get; set; }
        private int NextNodeNumber { get; set; }
        private int NextEdgeNumber { get; set; }


        //public SuffixTree(string word)
        //{
        //    Word = word;
        //    RootNode = new Node(this);
        //    ActiveNode = RootNode;
        //}

        /// <summary>
        /// The changed.
        /// </summary>
        public event Action<SuffixTree> Changed;

        /// <summary>
        /// The trigger changed.
        /// </summary>
        private void TriggerChanged()
        {
            var handler = this.Changed;
            if (handler != null)
            {
                handler(this);
            }
        }

        /// <summary>
        /// The message.
        /// </summary>
        public event Action<string, object[]> Message;

        /// <summary>
        /// The send message.
        /// </summary>
        /// <param name="format">
        /// The format.
        /// </param>
        /// <param name="args">
        /// The args.
        /// </param>
        private void SendMessage(string format, params object[] args)
        {
            var handler = this.Message;
            if (handler != null)
            {
                handler(format, args);
            }
        }

        /// <summary>
        /// The build.
        /// </summary>
        public void Build()
        {
            this.RootNode = new SuffixTree.Node(this);
            this.ActiveNode = this.RootNode;

            for (this.CurrentSuffixEndIndex = 0; this.CurrentSuffixEndIndex < this.Word.Length; this.CurrentSuffixEndIndex++)
            {
                this.SendMessage("=== ITERATION {0} ===", this.CurrentSuffixEndIndex);
                this.LastCreatedNodeInCurrentIteration = null;
                this.LastCharacterOfCurrentSuffix = this.Word[this.CurrentSuffixEndIndex];

                for (this.CurrentSuffixStartIndex = this.CurrentSuffixEndIndex - this.UnresolvedSuffixes; this.CurrentSuffixStartIndex <= this.CurrentSuffixEndIndex; this.CurrentSuffixStartIndex++)
                {
                    var wasImplicitlyAdded = !this.AddNextSuffix();
                    if (wasImplicitlyAdded)
                    {
                        this.UnresolvedSuffixes++;
                        break;
                    }
                    if (this.UnresolvedSuffixes > 0)
                    {
                        this.UnresolvedSuffixes--;
                    }
                }
            }
        }

        /// <summary>
        /// The add next suffix.
        /// </summary>
        /// <returns>
        /// The <see cref="bool"/>.
        /// </returns>
        /// <exception cref="Exception">
        /// </exception>
        private bool AddNextSuffix()
        {
            var suffixTree = new StringBuilder();
            var suffix = string.Concat(this.Word.Substring(this.CurrentSuffixStartIndex, this.CurrentSuffixEndIndex - this.CurrentSuffixStartIndex), "{", this.Word[this.CurrentSuffixEndIndex], "}");
            suffixTree.Append(String.Format("The next suffix of '{0}' to add is '{1}' at indices {2},{3}",
                this.Word,
                suffix,
                this.CurrentSuffixStartIndex,
                this.CurrentSuffixEndIndex));

            //this.SendMessage("The next suffix of '{0}' to add is '{1}' at indices {2},{3}", this.Word, suffix, this.CurrentSuffixStartIndex, this.CurrentSuffixEndIndex);
            //this.SendMessage(" => ActiveNode:             {0}", this.ActiveNode);
            //this.SendMessage(" => ActiveEdge:             {0}", this.ActiveEdge == null ? "none" : this.ActiveEdge.ToString());
            //this.SendMessage(" => DistanceIntoActiveEdge: {0}", this.DistanceIntoActiveEdge);
            //this.SendMessage(" => UnresolvedSuffixes:     {0}", this.UnresolvedSuffixes);
            if (this.ActiveEdge != null && this.DistanceIntoActiveEdge >= this.ActiveEdge.Length)
            {
                throw new Exception("BOUNDARY EXCEEDED");
            }

            if (this.ActiveEdge != null)
            {
                return this.AddCurrentSuffixToActiveEdge();
            }

            if (this.GetExistingEdgeAndSetAsActive())
            {
                return false;
            }

            this.ActiveNode.AddNewEdge();
            this.TriggerChanged();

            this.UpdateActivePointAfterAddingNewEdge();
            return true;
        }

        /// <summary>
        /// The get existing edge and set as active.
        /// </summary>
        /// <returns>
        /// The <see cref="bool"/>.
        /// </returns>
        private bool GetExistingEdgeAndSetAsActive()
        {
            Edge edge;
            if (this.ActiveNode.Edges.TryGetValue(this.LastCharacterOfCurrentSuffix, out edge))
            {
                this.SendMessage("Existing edge for {0} starting with '{1}' found. Values adjusted to:", this.ActiveNode, this.LastCharacterOfCurrentSuffix);
                this.ActiveEdge = edge;
                this.DistanceIntoActiveEdge = 1;
                this.TriggerChanged();

                this.NormalizeActivePointIfNowAtOrBeyondEdgeBoundary(this.ActiveEdge.StartIndex);
                this.SendMessage(" => ActiveEdge is now: {0}", this.ActiveEdge);
                this.SendMessage(" => DistanceIntoActiveEdge is now: {0}", this.DistanceIntoActiveEdge);
                this.SendMessage(" => UnresolvedSuffixes is now: {0}", this.UnresolvedSuffixes);

                return true;
            }

            this.SendMessage("Existing edge for {0} starting with '{1}' not found", this.ActiveNode, this.LastCharacterOfCurrentSuffix);
            return false;
        }

        /// <summary>
        /// The add current suffix to active edge.
        /// </summary>
        /// <returns>
        /// The <see cref="bool"/>.
        /// </returns>
        private bool AddCurrentSuffixToActiveEdge()
        {
            var nextCharacterOnEdge = this.Word[this.ActiveEdge.StartIndex + this.DistanceIntoActiveEdge];
            if (nextCharacterOnEdge == this.LastCharacterOfCurrentSuffix)
            {
                this.SendMessage("The next character on the current edge is '{0}' (suffix added implicitly)", this.LastCharacterOfCurrentSuffix);
                this.DistanceIntoActiveEdge++;
                this.TriggerChanged();

                this.SendMessage(" => DistanceIntoActiveEdge is now: {0}", this.DistanceIntoActiveEdge);
                this.NormalizeActivePointIfNowAtOrBeyondEdgeBoundary(this.ActiveEdge.StartIndex);

                return false;
            }

            this.SplitActiveEdge();
            this.ActiveEdge.Tail.AddNewEdge();
            this.TriggerChanged();

            this.UpdateActivePointAfterAddingNewEdge();

            return true;
        }

        /// <summary>
        /// The update active point after adding new edge.
        /// </summary>
        private void UpdateActivePointAfterAddingNewEdge()
        {
            if (ReferenceEquals(this.ActiveNode, this.RootNode))
            {
                if (this.DistanceIntoActiveEdge > 0)
                {
                    this.SendMessage(
                        "New edge has been added and the active node is root. The active edge will now be updated.");
                    this.DistanceIntoActiveEdge--;
                    this.SendMessage(" => DistanceIntoActiveEdge decremented to: {0}", this.DistanceIntoActiveEdge);
                    this.ActiveEdge = this.DistanceIntoActiveEdge == 0
                                          ? null
                                          : this.ActiveNode.Edges[this.Word[this.CurrentSuffixStartIndex + 1]];
                    this.SendMessage(" => ActiveEdge is now: {0}", this.ActiveEdge);
                    this.TriggerChanged();

                    this.NormalizeActivePointIfNowAtOrBeyondEdgeBoundary(this.CurrentSuffixStartIndex + 1);
                }
            }
            else
            {
                this.UpdateActivePointToLinkedNodeOrRoot();
            }
        }

        /// <summary>
        /// The normalize active point if now at or beyond edge boundary.
        /// </summary>
        /// <param name="firstIndexOfOriginalActiveEdge">
        /// The first index of original active edge.
        /// </param>
        private void NormalizeActivePointIfNowAtOrBeyondEdgeBoundary(int firstIndexOfOriginalActiveEdge)
        {
            var walkDistance = 0;
            while (this.ActiveEdge != null && this.DistanceIntoActiveEdge >= this.ActiveEdge.Length)
            {
                this.SendMessage("Active point is at or beyond edge boundary and will be moved until it falls inside an edge boundary");
                this.DistanceIntoActiveEdge -= this.ActiveEdge.Length;
                this.ActiveNode = this.ActiveEdge.Tail ?? this.RootNode;
                if (this.DistanceIntoActiveEdge == 0)
                {
                    this.ActiveEdge = null;
                }
                else
                {
                    walkDistance += this.ActiveEdge.Length;
                    var c = this.Word[firstIndexOfOriginalActiveEdge + walkDistance];
                    this.ActiveEdge = this.ActiveNode.Edges[c];
                }

                this.TriggerChanged();
            }
        }

        /// <summary>
        /// The split active edge.
        /// </summary>
        private void SplitActiveEdge()
        {
            this.ActiveEdge = this.ActiveEdge.SplitAtIndex(this.ActiveEdge.StartIndex + this.DistanceIntoActiveEdge);
            this.SendMessage(" => ActiveEdge is now: {0}", this.ActiveEdge);
            this.TriggerChanged();
            if (this.LastCreatedNodeInCurrentIteration != null)
            {
                this.LastCreatedNodeInCurrentIteration.LinkedNode = ActiveEdge.Tail;
                this.SendMessage(" => Connected {0} to {1}", this.LastCreatedNodeInCurrentIteration, this.ActiveEdge.Tail);
                this.TriggerChanged();
            }

            this.LastCreatedNodeInCurrentIteration = this.ActiveEdge.Tail;
        }

        /// <summary>
        /// The update active point to linked node or root.
        /// </summary>
        private void UpdateActivePointToLinkedNodeOrRoot()
        {
            this.SendMessage("The linked node for active node {0} is {1}", this.ActiveNode, this.ActiveNode.LinkedNode == null ? "[null]" : this.ActiveNode.LinkedNode.ToString());
            if (this.ActiveNode.LinkedNode != null)
            {
                this.ActiveNode = this.ActiveNode.LinkedNode;
                this.SendMessage(" => ActiveNode is now: {0}", this.ActiveNode);
            }
            else
            {
                this.ActiveNode = this.RootNode;
                this.SendMessage(" => ActiveNode is now ROOT", this.ActiveNode);
            }

            this.TriggerChanged();

            if (this.ActiveEdge != null)
            {
                var firstIndexOfOriginalActiveEdge = this.ActiveEdge.StartIndex;
                this.ActiveEdge = this.ActiveNode.Edges[this.Word[this.ActiveEdge.StartIndex]];
                this.TriggerChanged();
                this.NormalizeActivePointIfNowAtOrBeyondEdgeBoundary(firstIndexOfOriginalActiveEdge);
            }
        }

        /// <summary>
        /// The render tree.
        /// </summary>
        /// <returns>
        /// The <see cref="string"/>.
        /// </returns>
        public string RenderTree()
        {
            var writer = new StringWriter();
            this.RootNode.RenderTree(writer, string.Empty);
            return writer.ToString();
        }

        /// <summary>
        /// The write dot graph.
        /// </summary>
        /// <returns>
        /// The <see cref="string"/>.
        /// </returns>
        public string WriteDotGraph()
        {
            var sb = new StringBuilder();
            sb.AppendLine("digraph {");
            sb.AppendLine("rankdir = LR;");
            sb.AppendLine("edge [arrowsize=0.5,fontsize=11];");
            for (var i = 0; i < this.NextNodeNumber; i++)
            {
                sb.AppendFormat(
                    "node{0} [label=\"{0}\",style=filled,fillcolor={1},shape=circle,width=.1,height=.1,fontsize=11,margin=0.01];",
                    i,
                    this.ActiveNode.NodeNumber == i ? "cyan" : "lightgrey").AppendLine();
            }

            this.RootNode.WriteDotGraph(sb);
            sb.AppendLine("}");
            return sb.ToString();
        }

        /// <summary>
        /// The extract all substrings.
        /// </summary>
        /// <returns>
        /// The <see>
        ///         <cref>HashSet</cref>
        ///     </see>
        ///     .
        /// </returns>
        public HashSet<string> ExtractAllSubstrings()
        {
            var set = new HashSet<string>();
            this.ExtractAllSubstrings(string.Empty, set, this.RootNode);
            return set;
        }

        /// <summary>
        /// The extract all substrings.
        /// </summary>
        /// <param name="str">
        /// The str.
        /// </param>
        /// <param name="set">
        /// The set.
        /// </param>
        /// <param name="node">
        /// The node.
        /// </param>
        private void ExtractAllSubstrings(string str, HashSet<string> set, Node node)
        {
            foreach (var edge in node.Edges.Values)
            {
                var edgeStr = edge.StringWithoutCanonizationChar;
                var edgeLength = !edge.EndIndex.HasValue && this.CanonizationChar.HasValue ? edge.Length - 1 : edge.Length; // assume tailing canonization char
                for (var length = 1; length <= edgeLength; length++)
                {
                    set.Add(string.Concat(str, edgeStr.Substring(0, length)));
                }
                if (edge.Tail != null)
                {
                    this.ExtractAllSubstrings(string.Concat(str, edge.StringWithoutCanonizationChar), set, edge.Tail);
                }
            }
        }

        /// <summary>
        /// The extract substrings for indexing.
        /// </summary>
        /// <param name="maxLength">
        /// The max length.
        /// </param>
        /// <returns>
        /// The <see>
        ///         <cref>List</cref>
        ///     </see>
        ///     .
        /// </returns>
        public List<string> ExtractSubstringsForIndexing(int? maxLength = null)
        {
            var list = new List<string>();
            this.ExtractSubstringsForIndexing(string.Empty, list, maxLength ?? this.Word.Length, this.RootNode);
            return list;
        }

        /// <summary>
        /// The extract substrings for indexing.
        /// </summary>
        /// <param name="str">
        /// The str.
        /// </param>
        /// <param name="list">
        /// The list.
        /// </param>
        /// <param name="len">
        /// The len.
        /// </param>
        /// <param name="node">
        /// The node.
        /// </param>
        private void ExtractSubstringsForIndexing(string str, List<string> list, int len, Node node)
        {
            foreach (var edge in node.Edges.Values)
            {
                var newstr = string.Concat(str, this.Word.Substring(edge.StartIndex, Math.Min(len, edge.Length)));
                if (len > edge.Length && edge.Tail != null)
                {
                    this.ExtractSubstringsForIndexing(newstr, list, len - edge.Length, edge.Tail);
                }
                else
                {
                    list.Add(newstr);
                }
            }
        }

        /// <summary>
        /// The edge.
        /// </summary>
        [Serializable]
        public class Edge
        {
            /// <summary>
            /// The _tree.
            /// </summary>
            private readonly SuffixTree _tree;

            /// <summary>
            /// Initializes a new instance of the <see cref="Edge"/> class.
            /// </summary>
            /// <param name="tree">
            /// The tree.
            /// </param>
            /// <param name="head">
            /// The head.
            /// </param>
            public Edge(SuffixTree tree, Node head)
            {
                this._tree = tree;
                this.Head = head;
                this.StartIndex = tree.CurrentSuffixEndIndex;
                this.EdgeNumber = this._tree.NextEdgeNumber++;
            }

            /// <summary>
            /// Gets the head.
            /// </summary>
            public Node Head { get; private set; }

            /// <summary>
            /// Gets the tail.
            /// </summary>
            public Node Tail { get; private set; }

            /// <summary>
            /// Gets the start index.
            /// </summary>
            public int StartIndex { get; private set; }

            /// <summary>
            /// Gets or sets the end index.
            /// </summary>
            public int? EndIndex { get; set; }

            /// <summary>
            /// Gets the edge number.
            /// </summary>
            public int EdgeNumber { get; private set; }

            /// <summary>
            /// Gets the length.
            /// </summary>
            public int Length => (this.EndIndex ?? this._tree.Word.Length - 1) - this.StartIndex + 1;

            public Edge SplitAtIndex(int index)
            {
                _tree.SendMessage("Splitting edge {0} at index {1} ('{2}')", this, index, _tree.Word[index]);
                var newEdge = new Edge(_tree, Head);
                var newNode = new Node(_tree);
                newEdge.Tail = newNode;
                newEdge.StartIndex = StartIndex;
                newEdge.EndIndex = index - 1;
                Head = newNode;
                StartIndex = index;
                newNode.Edges.Add(_tree.Word[StartIndex], this);
                newEdge.Head.Edges[_tree.Word[newEdge.StartIndex]] = newEdge;
                _tree.SendMessage(" => Hierarchy is now: {0} --> {1} --> {2} --> {3}", newEdge.Head, newEdge, newNode, this);
                return newEdge;
            }

            public override string ToString()
            {
                return string.Concat(_tree.Word.Substring(StartIndex, (EndIndex ?? _tree.CurrentSuffixEndIndex) - StartIndex + 1), "(",
                    StartIndex, ",", EndIndex.HasValue ? EndIndex.ToString() : "#", ")");
            }

            public string StringWithoutCanonizationChar
            {
                get { return _tree.Word.Substring(StartIndex, (EndIndex ?? _tree.CurrentSuffixEndIndex - (_tree.CanonizationChar.HasValue ? 1 : 0)) - StartIndex + 1); }
            }

            public string String
            {
                get { return _tree.Word.Substring(StartIndex, (EndIndex ?? _tree.CurrentSuffixEndIndex) - StartIndex + 1); }
            }

            public void RenderTree(TextWriter writer, string prefix, int maxEdgeLength)
            {
                var strEdge = _tree.Word.Substring(StartIndex, (EndIndex ?? _tree.CurrentSuffixEndIndex) - StartIndex + 1);
                writer.Write(strEdge);
                if (Tail == null)
                    writer.WriteLine();
                else
                {
                    var line = new string(RenderChars.HorizontalLine, maxEdgeLength - strEdge.Length + 1);
                    writer.Write(line);
                    Tail.RenderTree(writer, string.Concat(prefix, new string(' ', strEdge.Length + line.Length)));
                }
            }

            public void WriteDotGraph(StringBuilder sb)
            {
                if (Tail == null)
                    sb.AppendFormat("leaf{0} [label=\"\",shape=point]", EdgeNumber).AppendLine();
                string label, weight, color;
                if (_tree.ActiveEdge != null && ReferenceEquals(this, _tree.ActiveEdge))
                {
                    if (_tree.ActiveEdge.Length == 0)
                        label = string.Empty;
                    else if (_tree.DistanceIntoActiveEdge > Length)
                        label = "<<FONT COLOR=\"red\" SIZE=\"11\"><B>" + String + "</B> (" + _tree.DistanceIntoActiveEdge + ")</FONT>>";
                    else if (_tree.DistanceIntoActiveEdge == Length)
                        label = "<<FONT COLOR=\"red\" SIZE=\"11\">" + String + "</FONT>>";
                    else if (_tree.DistanceIntoActiveEdge > 0)
                        label = "<<TABLE BORDER=\"0\" CELLPADDING=\"0\" CELLSPACING=\"0\"><TR><TD><FONT COLOR=\"blue\"><B>" + String.Substring(0, _tree.DistanceIntoActiveEdge) + "</B></FONT></TD><TD COLOR=\"black\">" + String.Substring(_tree.DistanceIntoActiveEdge) + "</TD></TR></TABLE>>";
                    else
                        label = "\"" + String + "\"";
                    color = "blue";
                    weight = "5";
                }
                else
                {
                    label = "\"" + String + "\"";
                    color = "black";
                    weight = "3";
                }
                var tail = Tail == null ? "leaf" + EdgeNumber : "node" + Tail.NodeNumber;
                sb.AppendFormat("node{0} -> {1} [label={2},weight={3},color={4},size=11]", Head.NodeNumber, tail, label, weight, color).AppendLine();
                if (Tail != null)
                    Tail.WriteDotGraph(sb);
            }
        }

        [Serializable]
        public class Node
        {
            private readonly SuffixTree _tree;

            public Node(SuffixTree tree)
            {
                _tree = tree;
                Edges = new Dictionary<char, Edge>();
                NodeNumber = _tree.NextNodeNumber++;
            }

            public Dictionary<char, Edge> Edges { get; private set; }
            public Node LinkedNode { get; set; }
            public int NodeNumber { get; private set; }

            public void AddNewEdge()
            {
                _tree.SendMessage("Adding new edge to {0}", this);
                var edge = new Edge(_tree, this);
                Edges.Add(_tree.Word[_tree.CurrentSuffixEndIndex], edge);
                _tree.SendMessage(" => {0} --> {1}", this, edge);
            }

            public void RenderTree(TextWriter writer, string prefix)
            {
                var strNode = string.Concat("(", NodeNumber.ToString(new string('0', _tree.NextNodeNumber.ToString().Length)), ")");
                writer.Write(strNode);
                var edges = Edges.Select(kvp => kvp.Value).OrderBy(e => _tree.Word[e.StartIndex]).ToArray();
                if (edges.Any())
                {
                    var prefixWithNodePadding = prefix + new string(' ', strNode.Length);
                    var maxEdgeLength = edges.Max(e => (e.EndIndex ?? _tree.CurrentSuffixEndIndex) - e.StartIndex + 1);
                    for (var i = 0; i < edges.Length; i++)
                    {
                        char connector, extender = ' ';
                        if (i == 0)
                        {
                            if (edges.Length > 1)
                            {
                                connector = RenderChars.TJunctionDown;
                                extender = RenderChars.VerticalLine;
                            }
                            else
                                connector = RenderChars.HorizontalLine;
                        }
                        else
                        {
                            writer.Write(prefixWithNodePadding);
                            if (i == edges.Length - 1)
                                connector = RenderChars.CornerRight;
                            else
                            {
                                connector = RenderChars.TJunctionRight;
                                extender = RenderChars.VerticalLine;
                            }
                        }
                        writer.Write(string.Concat(connector, RenderChars.HorizontalLine));
                        var newPrefix = string.Concat(prefixWithNodePadding, extender, ' ');
                        edges[i].RenderTree(writer, newPrefix, maxEdgeLength);
                    }
                }
            }

            public override string ToString()
            {
                return string.Concat("node #", NodeNumber);
            }

            public void WriteDotGraph(StringBuilder sb)
            {
                if (LinkedNode != null)
                    sb.AppendFormat("node{0} -> node{1} [label=\"\",weight=.01,style=dotted]", NodeNumber, LinkedNode.NodeNumber).AppendLine();
                foreach (var edge in Edges.Values)
                    edge.WriteDotGraph(sb);
            }
        }
        [Serializable]
        public static class RenderChars
        {
            public const char TJunctionDown = '┬';
            public const char HorizontalLine = '─';
            public const char VerticalLine = '│';
            public const char TJunctionRight = '├';
            public const char CornerRight = '└';
        }
    }
}
