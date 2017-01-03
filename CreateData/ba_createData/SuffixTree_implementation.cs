using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace ba_createData
{
    [Serializable]
    public class SuffixTreeImplementation
    {
        public string CompleteString;


        [Serializable]
        public class Node
        {
            public int Index = -1;
            public Dictionary<char, Node> Children = new Dictionary<char, Node>();
        }

        public Node Root = new Node();
        public String Text;

        private void InsertSuffix(string s, int from)
        {
            var cur = Root;
            for (int i = from; i < s.Length; ++i)
            {
                var c = s[i];
                if (!cur.Children.ContainsKey(c))
                {
                    var n = new Node() { Index = from };
                    cur.Children.Add(c, n);

                    // Very slow assertion. 
                    Debug.Assert(Find(s.Substring(from)).Any());

                    return;
                }
                cur = cur.Children[c];
            }
            Debug.Assert(false, "It should never be possible to arrive at this case");
            throw new Exception("Suffix tree corruption");
        }

        private static IEnumerable<Node> VisitTree(Node n)
        {
            foreach (var n1 in n.Children.Values)
                foreach (var n2 in VisitTree(n1))
                    yield return n2;
            yield return n;
        }

        private IEnumerable<int> Find(string s)
        {
            var n = FindNode(s);
            if (n == null) yield break;
            foreach (var n2 in VisitTree(n))
                yield return n2.Index;
        }

        private Node FindNode(string s)
        {
            var cur = Root;
            for (int i = 0; i < s.Length; ++i)
            {
                var c = s[i];
                if (!cur.Children.ContainsKey(c))
                {
                    // We are at a leaf-node.
                    // What we do here is check to see if the rest of the string is at this location. 
                    for (var j = i; j < s.Length; ++j)
                        if (Text[cur.Index + j] != s[j])
                            return null;
                    return cur;
                }
                cur = cur.Children[c];
            }
            return cur;
        }

        public SuffixTreeImplementation(string s)
        {
            Text = s;
            for (var i = s.Length - 1; i >= 0; --i)
                InsertSuffix(s, i);
            Debug.Assert(VisitTree(Root).Count() - 1 == s.Length);
        }

    }
}