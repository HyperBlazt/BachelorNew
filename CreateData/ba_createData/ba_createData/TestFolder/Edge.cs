/* Edge.cs
 * To Do: add comments
 * I need a better hashing system for large data sets. 
 * 
 * 
 * This is a suffix tree algorithm for .NET written in C#. Feel free to use it as you please!
 * This code was derived from Mark Nelson's article located here: http://marknelson.us/1996/08/01/suffix-trees/
 * Have Fun 
 * 
 * Zikomo A. Fields 2008
 * 

 * 
 */


using System;
using System.Collections.Generic;

namespace ba_createData.TestFolder
{
    [Serializable]
    public struct Edge
    {
        public int indexOfFirstCharacter;
        public int indexOfLastCharacter;
        public int startNode;
        public int endNode;        

        public const int HASH_TABLE_SIZE = 306785407;        
        
        public Edge(int startNode)
        {            
            this.startNode = -1;
            this.indexOfFirstCharacter = 0;
            this.indexOfLastCharacter = 0;
            this.endNode = 0;
        }

        public Edge(string theString, int indexOfFirstCharacter, int indexOfLastCharacter, int parentNode)
        {            
            this.indexOfFirstCharacter = indexOfFirstCharacter;
            this.indexOfLastCharacter = indexOfLastCharacter;
            this.startNode = parentNode;
            this.endNode = Node.Count++;
        }

        public Edge(Edge edge)
        {
            this.startNode = edge.startNode;
            this.endNode = edge.endNode;
            this.indexOfFirstCharacter = edge.indexOfFirstCharacter;
            this.indexOfLastCharacter = edge.indexOfLastCharacter;            
        }

        public void Copy(Edge edge)
        {
            this.startNode = edge.startNode;
            this.endNode = edge.endNode;
            this.indexOfFirstCharacter = edge.indexOfFirstCharacter;
            this.indexOfLastCharacter = edge.indexOfLastCharacter;            
        }

        static public void Insert(Edge edge)
        {
            int i = Hash(edge.startNode, SuffixTree.theString[edge.indexOfFirstCharacter]);
            if (!SuffixTree.Edges.ContainsKey(i))
            {
                SuffixTree.Edges.Add(i, new Edge(-1));
            }
            while (SuffixTree.Edges[i].startNode != -1)
            {
                i = ++i % HASH_TABLE_SIZE;
                if (!SuffixTree.Edges.ContainsKey(i))
                {
                    SuffixTree.Edges.Add(i, new Edge(-1));
                }

            }
            SuffixTree.Edges[i] = edge;
        }

        static public void Remove(Edge edge)
        {            
            int i = Hash(edge.startNode, SuffixTree.theString[edge.indexOfFirstCharacter]);
            while (SuffixTree.Edges[i].startNode != edge.startNode || SuffixTree.Edges[i].indexOfFirstCharacter != edge.indexOfFirstCharacter)
            {
                i = ++i % HASH_TABLE_SIZE;
            }
            for (; ; )
            {
                
                Edge tempEdge = SuffixTree.Edges[i];
                tempEdge.startNode = -1;
                SuffixTree.Edges[i] = tempEdge;
                int j = i;
                for (; ; )
                {
                    i = ++i % HASH_TABLE_SIZE;
                    if (!SuffixTree.Edges.ContainsKey(i))
                    {
                        SuffixTree.Edges.Add(i, new Edge(-1));
                    }
                    if (SuffixTree.Edges[i].startNode == -1)
                    {
                        return;
                    }

                    int r = Hash(SuffixTree.Edges[i].startNode, SuffixTree.theString[SuffixTree.Edges[i].indexOfFirstCharacter]);
                    if (i >= r && r > j)
                    {
                        continue;
                    }
                    if (r > j && j > i)
                    {
                        continue;
                    }
                    if (j > i && i >= r)
                    {
                        continue;
                    }
                    break;
                }
                SuffixTree.Edges[j].Copy(SuffixTree.Edges[i]);
            }
        }

        static public int SplitEdge(Suffix s, string theString, Dictionary<int, Edge> edges, Dictionary<int, Node> nodes,ref Edge edge)
        {
            Remove(edge);
            Edge newEdge = new Edge(theString, edge.indexOfFirstCharacter,
                edge.indexOfFirstCharacter + s.indexOfLastCharacter 
                - s.indexOfFirstCharacter, s.originNode);
            Edge.Insert(newEdge);
            if (nodes.ContainsKey(newEdge.endNode))
            {
                nodes[newEdge.endNode].SuffixNode = s.originNode;
            }
            else
            {
                Node newNode = new Node();
                newNode.SuffixNode = s.originNode;
                nodes.Add(newEdge.endNode, newNode);
            }

            edge.indexOfFirstCharacter += s.indexOfLastCharacter - s.indexOfFirstCharacter + 1;
            edge.startNode = newEdge.endNode;
            Edge.Insert(edge);            
            return newEdge.endNode;
           
        }

        static public Edge Find(string theString, Dictionary<int, Edge> edges, int node, int c)
        {
            int i = Hash(node, c);
            for (; ; )
            {
                if (!edges.ContainsKey(i))
                {
                    edges.Add(i,new Edge(-1));
                }
                if (edges[i].startNode == node)
                {
                    if (c == theString[edges[i].indexOfFirstCharacter])
                    {
                        return edges[i];
                    }                   
                }
                if (edges[i].startNode == -1)
                {
                    return edges[i];
                }
                i = ++i % HASH_TABLE_SIZE;
            }            
        }

        public static int Hash(int node, int c)
        {
            int rtnValue = ((node << 8) + c) % (int)HASH_TABLE_SIZE;
            return rtnValue;
        }
    }
}
