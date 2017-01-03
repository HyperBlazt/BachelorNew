/* SuffixTree.cs
 * To Do: add comments
 * 
 * 
 * This is a suffix tree algorithm for .NET written in C#. Feel free to use it as you please!
 * This code was derived from Mark Nelson's article located here: http://marknelson.us/1996/08/01/suffix-trees/
 * Have Fun 
 * 
 * Zikomo A. Fields 2008
 *  
 *  d. 14-12-16 kl. 12:08
 *  https://code.google.com/archive/p/csharsuffixtree/source/default/source
 */

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Threading;

namespace ba_createData.TestFolder
{
    public class SuffixTree
    {        
        public static string theString;       
        public static Dictionary<int, Edge> Edges = null;
        public static Dictionary<int, Node> Nodes = null;

        private string _path;
        public SuffixTree(string theString, string path)
        {
            _path = path;
            SuffixTree.theString = theString;
            Nodes = new Dictionary<int, Node>();
            Edges = new Dictionary<int, Edge>();            
        }

        public void BuildTree()
        {
            Suffix active = new Suffix(SuffixTree.theString, Edges, 0, 0, -1);
            for (int i = 0; i <= theString.Length - 1; i++)
            {
                AddPrefix(active, i);
            }

            var filePacementDirectoryTask = Thread.GetDomain().BaseDirectory + "suffixTrees\\";
            var fileName = Path.GetFileName(_path);
            using (Stream stream = File.Open(filePacementDirectoryTask + fileName, FileMode.Create))
            {
                using (BinaryWriter writer = new BinaryWriter(stream))
                {
                    Save(writer, this);
                }
            }
        }
        public void Save(BinaryWriter writer, SuffixTree tree)
        {
            writer.Write(SuffixTree.Edges.Count);
            writer.Write(SuffixTree.theString.Length);
            writer.Write(SuffixTree.theString);
            foreach (KeyValuePair<int, Edge> edgePair in SuffixTree.Edges)
            {
                writer.Write(edgePair.Key);
                writer.Write(edgePair.Value.endNode);
                writer.Write(edgePair.Value.startNode);
                writer.Write(edgePair.Value.indexOfFirstCharacter);
                writer.Write(edgePair.Value.indexOfLastCharacter);
            }
        }


        public void Save(Stream stream, SuffixTree tree)
        {
            using (BinaryWriter writer = new BinaryWriter(stream))
            {
                Save(writer, tree);
            }
        }

        public static SuffixTree LoadFromFile(BinaryReader reader)
        {
            SuffixTree tree;
            int count = reader.ReadInt32();
            int theStringLength = reader.ReadInt32();
            string theString = reader.ReadString();
            tree = new SuffixTree(theString, "");
            for (int i = 0; i < count; i++)
            {
                int key = reader.ReadInt32();
                Edge readEdge = new Edge(-1);
                readEdge.endNode = reader.ReadInt32();
                readEdge.startNode = reader.ReadInt32();
                readEdge.indexOfFirstCharacter = reader.ReadInt32();
                readEdge.indexOfLastCharacter = reader.ReadInt32();
                SuffixTree.Edges.Add(key, readEdge);
            }
            return tree;
        }

        public SuffixTree LoadFromFile(Stream stream)
        {
            SuffixTree tree;
            using (BinaryReader reader = new BinaryReader(stream))
            {
                tree = LoadFromFile(reader);
            }
            return tree;
        }

        public bool Search(string search)
        {
            return Search(search, false);
        }

        public bool Search(string search, bool caseSensitive)
        {
            if (!caseSensitive) search = search.ToLower(CultureInfo.CurrentCulture);

            if (search.Length == 0)
            {
                return false;
            }
            int index = 0;
            Edge edge;
            if (!SuffixTree.Edges.TryGetValue((int)Edge.Hash(0, search[0]), out edge))
            {
                return false;
            }                

            if (edge.startNode == -1)
            {
                return false;
            }
            else
            {
                for (; ; )
                {
                    for (int j = edge.indexOfFirstCharacter; j <= edge.indexOfLastCharacter; j++)
                    {
                        if (index >= search.Length)
                        {
                            return true;
                        }
                        char test = theString[j];
                        if (SuffixTree.theString[j] != search[index++])
                        {
                            return false;
                        }
                    }
                    if (index < search.Length)
                    {
                        Edge value;
                        if (SuffixTree.Edges.TryGetValue(Edge.Hash(edge.endNode, search[index]), out value))
                        {
                            edge = value;
                        }
                        else
                        {
                            return false;
                        }
                    }
                    else
                    {
                        return true;
                    }
                }
            }
        }

        private void AddPrefix(Suffix active, int indexOfLastCharacter)
        {
            int parentNode;
            int lastParentNode = -1;

            for (; ; )
            {
                Edge edge = new Edge(-1);
                parentNode = active.originNode;

                if (active.IsExplicit)
                {
                    edge = Edge.Find(SuffixTree.theString, SuffixTree.Edges, active.originNode, theString[indexOfLastCharacter]);
                    if (edge.startNode != -1)
                    {
                        break;
                    }
                }
                else
                {
                    edge = Edge.Find(SuffixTree.theString, SuffixTree.Edges, active.originNode, theString[active.indexOfFirstCharacter]);
                    int span = active.indexOfLastCharacter - active.indexOfFirstCharacter;
                    if (theString[edge.indexOfFirstCharacter + span + 1] == theString[indexOfLastCharacter])
                    {
                        break;
                    }
                    parentNode = Edge.SplitEdge(active, theString, Edges, Nodes,ref edge);
                }

                Edge newEdge = new Edge(SuffixTree.theString, indexOfLastCharacter, SuffixTree.theString.Length - 1, parentNode);                
                Edge.Insert(newEdge);
                if (lastParentNode > 0)
                {
                    Nodes[lastParentNode].SuffixNode = parentNode;                   
                }
                lastParentNode = parentNode;

                if (active.originNode == 0)
                {
                    active.indexOfFirstCharacter++;
                }
                else
                {
                    active.originNode = Nodes[active.originNode].SuffixNode;
                }                
                active.Canonize();
            }
            if (lastParentNode > 0)
            {
                Nodes[lastParentNode].SuffixNode = parentNode;
            }
            active.indexOfLastCharacter++;
            active.Canonize();
        }
    }
}
