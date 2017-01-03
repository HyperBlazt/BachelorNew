/* Node.cs
 * To Do: add comments
 * 
 * 
 * This is a suffix tree algorithm for .NET written in C#. Feel free to use it as you please!
 * This code was derived from Mark Nelson's article located here: http://marknelson.us/1996/08/01/suffix-trees/
 * Have Fun 
 * 
 * Zikomo A. Fields 2008
 *  
 */

using System;

namespace ba_createData.TestFolder
{
    [Serializable]
    public class Node
    {
        public int SuffixNode;

        public Node()
        {
            SuffixNode = -1;
        }
        public Node(Node node)
        {
            this.SuffixNode = node.SuffixNode;
        }
        public static int Count = 1; 
    }
}
