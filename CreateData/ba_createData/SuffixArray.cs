/*
 Copyright (c) 2012 Eran Meir
 Permission is hereby granted, free of charge, to any person obtaining a copy
 of this software and associated documentation files (the "Software"), to deal
 in the Software without restriction, including without limitation the rights
 to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
 copies of the Software, and to permit persons to whom the Software is
 furnished to do so, subject to the following conditions:
 
 The above copyright notice and this permission notice shall be included in
 all copies or substantial portions of the Software.
 
 THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
 OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
 SOFTWARE.
*/

using System.Runtime.Serialization.Formatters.Binary;

namespace ba_createData
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Text;
    using System.Threading;

    using C5;

    using Newtonsoft.Json;

    /// <summary>
    /// The suffix array.
    /// </summary>
    [Serializable]
    public class SuffixArray
    {


        private string fileDirectory = Thread.GetDomain().BaseDirectory + "Builder\\";

        /// <summary>
        /// The eoc.
        /// </summary>
        private const int Eoc = int.MaxValue;

        /// <summary>
        /// The m_sa.
        /// </summary>
        private int[] mSa;

        /// <summary>
        /// The m_isa.
        /// </summary>
        private int[] mIsa;

        /// <summary>
        /// The m_lcp.
        /// </summary>
        private int[] mLcp;

        /// <summary>
        /// The m_chain heads dict.
        /// </summary>
        private readonly HashDictionary<char, int> mChainHeadsDict = new HashDictionary<char, int>(new CharComparer());

        /// <summary>
        /// The m_chain stack.
        /// </summary>
        private List<Chain> mChainStack ;

        /// <summary>
        /// The m_sub chains.
        /// </summary>
        private ArrayList<Chain> mSubChains;

        /// <summary>
        /// The m_next rank.
        /// </summary>
        private int mNextRank = 1;

        /// <summary>
        /// The m_str.
        /// </summary>
        private string mStr;

        /// <summary>
        /// Gets the length.
        /// </summary>
        private int Length => LoadTextFile("mStr.txt", this.fileDirectory).Length;

        public int this[int index] => JsonConvert.DeserializeObject<int[]>(File.ReadAllText(this.fileDirectory + "mSa.txt"))[index];

        public string StringRepresentation => GetString(JsonConvert.DeserializeObject<int[]>(File.ReadAllText(this.fileDirectory + "mSa.txt")));

        /// 
        /// <summary>
        /// Build a suffix array from string str
        /// </summary>
        /// <param name="str">A string for which to build a suffix array with LCP information</param>
        public SuffixArray(string str) : this(str, true) { }


        public void CleanSuffixArray()
        {
            var mSa_load = JsonConvert.DeserializeObject<int[]>(File.ReadAllText(this.fileDirectory + "mSa.txt"));
            var mStr_load = LoadTextFile("mStr.txt", this.fileDirectory);

            List<int> stringList = new List<int>();
            foreach (var index in mSa_load)
            {
                var substring = mStr_load.Substring(index, mStr_load.Length - index);
                if (substring.Length >= 33)
                {
                    if (substring[32].Equals('$'))
                    {
                        stringList.Add(index);
                    }
                }
            }
            File.WriteAllText(this.fileDirectory + "mSa.txt", String.Empty);
            File.WriteAllText(this.fileDirectory + "mSa.txt", JsonConvert.SerializeObject(stringList.ToArray()));
        }

        public string GetString(int[] bytes)
        {
            StringBuilder newString = new StringBuilder();
            foreach (var str in bytes)
            {
                newString.Append(str + "; ");
            }
            return newString.ToString();

        }



        public string LoadTextFile(string fileName, string path)
        {
            using (StreamReader file = new StreamReader(path + fileName, true))
            {
                return file.ReadToEnd();
            }
        }

        public void SaveTextFile(string data, string fileName, string path)
        {
            using (StreamWriter file = new StreamWriter(path + fileName, true))
            {
                file.Write(data);
            }
        }

        /// 
        /// <summary>
        /// Build a suffix array from string str
        /// </summary>
        /// <param name="str">A string for which to build a suffix array</param>
        /// <param name="buildLcps">Also calculate LCP information</param>
        public SuffixArray(string str, bool buildLcps)
        {
            var databaseDirectory = Thread.GetDomain().BaseDirectory + "Builder\\";
            if (Directory.Exists(databaseDirectory))
            {
                DirectoryInfo di = new DirectoryInfo(databaseDirectory);
                foreach (FileInfo file in di.GetFiles())
                {
                    file.Delete();
                }
            }
            else
            {
                Directory.CreateDirectory(databaseDirectory);
            }
            SaveTextFile(str, "mStr.txt", databaseDirectory);
            try
            {
                File.WriteAllText(this.fileDirectory + "mSa.txt", JsonConvert.SerializeObject(new int[str.Length]));
                File.WriteAllText(this.fileDirectory + "mIsa.txt", JsonConvert.SerializeObject(new int[str.Length]));
                var serializer = new BinaryFormatter();
                using (var stream = File.OpenWrite(this.fileDirectory + "mSubChains.dat"))
                {
                    serializer.Serialize(stream, new ArrayList<Chain>());
                }

                var mSubChainsSerializer = new BinaryFormatter();
                using (var stream = File.OpenWrite(this.fileDirectory + "mChainStack.dat"))
                {
                    mSubChainsSerializer.Serialize(stream, new List<Chain>());
                }

                this.FormInitialChains();
                this.BuildSuffixArray();
                if (buildLcps)
                    this.BuildLcpArray();

                // Addon - cleans suffix array
                this.CleanSuffixArray();

            }
            catch (Exception e)
            {
                var file = File.CreateText(@databaseDirectory + "Error.txt");
                file.Close();
                using (StreamWriter file1 = new StreamWriter(@databaseDirectory + "Error.txt", true))
                {
                    file1.WriteLine("Error: " + e.Message);
                }
            }
        }

        /// 
        /// <summary>Find the index of a substring </summary>
        /// <param name="substr">Substring to look for</param>
        /// <returns>First index in the original string. -1 if not found</returns>
        public int IndexOf(string substr)
        {
            var mStr_load = LoadTextFile("mStr.txt", this.fileDirectory);
            var mSa_load = JsonConvert.DeserializeObject<int[]>(File.ReadAllText(this.fileDirectory + "mSa.txt"));
            int l = 0;
            int r = mSa_load.Length;
            int m = -1;

            if ((substr == null) || (substr.Length == 0))
            {
                return -1;
            }

            // Binary search for substring
            while (r > l)
            {
                m = (l + r) / 2;
                if (mStr_load.Substring(mSa_load[m]).CompareTo(substr) < 0)
                {
                    l = m + 1;
                }
                else
                {
                    r = m;
                }
            }
            if ((l == r) && (l < mStr_load.Length) && (mStr_load.Substring(mSa_load[l]).StartsWith(substr)))
            {
                return mSa_load[l];
            }
            else
            {
                return -1;
            }
        }

        private void FormInitialChains()
        {
            // Link all suffixes that have the same first character
            this.FindInitialChains();
            this.SortAndPushSubchains();
        }

        private void FindInitialChains()
        {

            var mIsa_load = JsonConvert.DeserializeObject<int[]>(File.ReadAllText(this.fileDirectory + "mIsa.txt"));
            var mStr_load = LoadTextFile("mStr.txt", this.fileDirectory);
            var mSubChains_load = new ArrayList<Chain>();
            var serializer = new BinaryFormatter();
            using (var stream = File.OpenRead(this.fileDirectory + "mSubChains.dat"))
            {
                mSubChains_load = (ArrayList<Chain>)serializer.Deserialize(stream);
            }

            // Scan the string left to right, keeping rightmost occurences of characters as the chain heads
            for (int i = 0; i < mStr_load.Length; i++)
            {
                if (this.mChainHeadsDict.Contains(mStr_load[i]))
                {
                    mIsa_load[i] = this.mChainHeadsDict[mStr_load[i]];
                }
                else
                {
                    mIsa_load[i] = Eoc;
                }
                this.mChainHeadsDict[mStr_load[i]] = i;
            }

            // Prepare chains to be pushed to stack
            foreach (int headIndex in this.mChainHeadsDict.Values)
            {
                Chain newChain = new Chain(mStr_load);
                newChain.Head = headIndex;
                newChain.Length = 1;
                mSubChains_load.Add(newChain);
            }
            File.WriteAllText(this.fileDirectory + "mSubChains.dat", string.Empty);
            using (var stream = File.OpenWrite(this.fileDirectory + "mSubChains.dat"))
            {
                serializer.Serialize(stream, mSubChains_load);
            }
            File.WriteAllText(this.fileDirectory + "mIsa.txt", String.Empty);
            File.WriteAllText(this.fileDirectory + "mIsa.txt", JsonConvert.SerializeObject(mIsa_load));

            // Free variables and free memory
            GC.Collect();
        }

        private void SortAndPushSubchains()
        {
            var mSubChains_load = new ArrayList<Chain>();
            var serializer = new BinaryFormatter();
            using (var stream = File.OpenRead(this.fileDirectory + "mSubChains.dat"))
            {
                mSubChains_load = (ArrayList<Chain>)serializer.Deserialize(stream);
            }
            var mChainStack_load = new List<Chain>();
            var mChainStackSerializer = new BinaryFormatter();
            using (var stream = File.OpenRead(this.fileDirectory + "mChainStack.dat"))
            {
                mChainStack_load = (List<Chain>)mChainStackSerializer.Deserialize(stream);
            }
            mSubChains_load.Sort();
            for (int i = mSubChains_load.Count - 1; i >= 0; i--)
            {
                mChainStack_load.Add(mSubChains_load[i]);
            }

            File.WriteAllText(this.fileDirectory + "mChainStack.dat", string.Empty);
            using (var stream = File.OpenWrite(this.fileDirectory + "mChainStack.dat"))
            {
                mChainStackSerializer.Serialize(stream, mChainStack_load);
            }

            // Free variables and free memory
            GC.Collect();
        }

        private int GetStackLength()
        {
            var mChainStack_load = new List<Chain>();
            var mChainStackSerializer = new BinaryFormatter();
            using (var stream = File.OpenRead(this.fileDirectory + "mChainStack.dat"))
            {
                mChainStack_load = (List<Chain>)mChainStackSerializer.Deserialize(stream);
            }
            return mChainStack_load.Count;
        }

        private List<Chain> GetListOfStacks()
        {
            var mChainStack_load = new List<Chain>();
            var mChainStackSerializer = new BinaryFormatter();
            using (var stream = File.OpenRead(this.fileDirectory + "mChainStack.dat"))
            {
                mChainStack_load = (List<Chain>)mChainStackSerializer.Deserialize(stream);
            }
            return mChainStack_load;
        }

        private void SaveListOfStacks(List<Chain> mChainStack)
        {
            var mChainStackSerializer = new BinaryFormatter();
            File.WriteAllText(this.fileDirectory + "mChainStack.dat", string.Empty);
            using (var stream = File.OpenWrite(this.fileDirectory + "mChainStack.dat"))
            {
                mChainStackSerializer.Serialize(stream, mChainStack);
            }
        }

        private void BuildSuffixArray()
        {
            var mIsa_load = JsonConvert.DeserializeObject<int[]>(File.ReadAllText(this.fileDirectory + "mIsa.txt"));
            while (GetStackLength() > 0)
            {
                var mChainStack_load = GetListOfStacks();
                // Pop chain
                Chain chain = mChainStack_load[mChainStack_load.Count - 1];
                mChainStack_load.RemoveAt(mChainStack_load.Count - 1);
                SaveListOfStacks(mChainStack_load);

                if (mIsa_load[chain.Head] == Eoc)
                {
                    // Singleton (A chain that contain only 1 suffix)
                    this.RankSuffix(chain.Head);
                }
                else
                {
                    //RefineChains(chain);
                    this.RefineChainWithInductionSorting(chain);
                }
            }
        }

        private void ExtendChain(Chain chain)
        {
            var mSubChains_load = new ArrayList<Chain>();
            var serializer = new BinaryFormatter();
            using (var stream = File.OpenRead(fileDirectory + "mSubChains.dat"))
            {
                mSubChains_load = (ArrayList<Chain>)serializer.Deserialize(stream);
            }
            var mStr_load = LoadTextFile("mStr.txt", this.fileDirectory);
            var mIsa_load = JsonConvert.DeserializeObject<int[]>(File.ReadAllText(this.fileDirectory + "mIsa.txt"));
            char sym = mStr_load[chain.Head + chain.Length];
            if (this.mChainHeadsDict.Contains(sym))
            {
                // Continuation of an existing chain, this is the leftmost
                // occurence currently known (others may come up later)
                mIsa_load[this.mChainHeadsDict[sym]] = chain.Head;
                mIsa_load[chain.Head] = Eoc;
            }
            else
            {
                // This is the beginning of a new subchain
                mIsa_load[chain.Head] = Eoc;
                Chain newChain = new Chain(mStr_load);
                newChain.Head = chain.Head;
                newChain.Length = chain.Length + 1;
                mSubChains_load.Add(newChain);
            }
            // Save index in case we find a continuation of this chain

            File.WriteAllText(this.fileDirectory + "mSubChains.dat", string.Empty);
            using (var stream = File.OpenWrite(this.fileDirectory + "mSubChains.dat"))
            {
                serializer.Serialize(stream, mSubChains_load);
            }
            File.WriteAllText(this.fileDirectory + "mIsa.txt", String.Empty);
            File.WriteAllText(this.fileDirectory + "mIsa.txt", JsonConvert.SerializeObject(mIsa_load));
            this.mChainHeadsDict[sym] = chain.Head;

            // Free variables and free memory
            GC.Collect();
        }

        private void RefineChainWithInductionSorting(Chain chain)
        {
            var mStr_load = LoadTextFile("mStr.txt", this.fileDirectory);
            var mIsa_load = JsonConvert.DeserializeObject<int[]>(File.ReadAllText(this.fileDirectory + "mIsa.txt"));
            // TODO - refactor/beautify some
            ArrayList<SuffixRank> notedSuffixes = new ArrayList<SuffixRank>();
            this.mChainHeadsDict.Clear();

            var mSubChains_load = new ArrayList<Chain>();
            var serializer = new BinaryFormatter();
            using (var stream = File.OpenRead(fileDirectory + "mSubChains.dat"))
            {
                mSubChains_load = (ArrayList<Chain>)serializer.Deserialize(stream);
            }
            mSubChains_load.Clear();
            using (var stream = File.OpenWrite(this.fileDirectory + "mSubChains.dat"))
            {
                serializer.Serialize(stream, mSubChains_load);
            }

            while (chain.Head != Eoc)
            {
                int nextIndex = mIsa_load[chain.Head];
                if (chain.Head + chain.Length > mStr_load.Length - 1)
                {
                    // If this substring reaches end of string it cannot be extended.
                    // At this point it's the first in lexicographic order so it's safe
                    // to just go ahead and rank it.
                    this.RankSuffix(chain.Head);
                }
                else if (mIsa_load[chain.Head + chain.Length] < 0)
                {
                    SuffixRank sr = new SuffixRank();
                    sr.Head = chain.Head;
                    sr.Rank = -mIsa_load[chain.Head + chain.Length];
                    notedSuffixes.Add(sr);
                }
                else
                {
                    this.ExtendChain(chain);
                }
                chain.Head = nextIndex;
            }

            // Keep stack sorted
            this.SortAndPushSubchains();
            this.SortAndRankNotedSuffixes(notedSuffixes);

            // Free variables and free memory
            GC.Collect();
        }

        private void SortAndRankNotedSuffixes(ArrayList<SuffixRank> notedSuffixes)
        {
            notedSuffixes.Sort(new SuffixRankComparer());
            // Rank sorted noted suffixes 
            for (int i = 0; i < notedSuffixes.Count; ++i)
            {
                this.RankSuffix(notedSuffixes[i].Head);
            }
        }

        private void RankSuffix(int index)
        {
            var mIsa_load = JsonConvert.DeserializeObject<int[]>(File.ReadAllText(this.fileDirectory + "mIsa.txt"));
            var mSa_load = JsonConvert.DeserializeObject<int[]>(File.ReadAllText(this.fileDirectory + "mSa.txt"));
            // We use the ISA to hold both ranks and chain links, so we differentiate by setting
            // the sign.
            mIsa_load[index] = -this.mNextRank;
            mSa_load[this.mNextRank - 1] = index;
            this.mNextRank++;
            File.WriteAllText(this.fileDirectory + "mIsa.txt", String.Empty);
            File.WriteAllText(this.fileDirectory + "mSa.txt", String.Empty);
            File.WriteAllText(this.fileDirectory + "mIsa.txt", JsonConvert.SerializeObject(mIsa_load));
            File.WriteAllText(this.fileDirectory + "mSa.txt", JsonConvert.SerializeObject(mSa_load));
            // Free variables and free memory
            GC.Collect();
        }

        private void BuildLcpArray()
        {
            var mSa_load = JsonConvert.DeserializeObject<int[]>(File.ReadAllText(this.fileDirectory + "mSa.txt"));
            this.mLcp = new int[mSa_load.Length + 1];
            this.mLcp[0] = this.mLcp[mSa_load.Length] = 0;

            for (int i = 1; i < mSa_load.Length; i++)
            {
                this.mLcp[i] = this.CalcLcp(mSa_load[i - 1], mSa_load[i]);
            }

            // Free variables and free memory
            GC.Collect();
        }

        private int CalcLcp(int i, int j)
        {
            var mStr_load = LoadTextFile("mStr.txt", this.fileDirectory);
            int lcp;
            int maxIndex = this.mStr.Length - Math.Max(i, j);       // Out of bounds prevention
            for (lcp = 0; (lcp < maxIndex) && (mStr_load[i + lcp] == mStr_load[j + lcp]); lcp++)
            {
            }
            return lcp;
        }

    }

    #region HelperClasses
    [Serializable]
    internal class Chain : IComparable<Chain>
    {

        public int Head;
        public int Length;
        public string MStr;

        public Chain(string str)
        {
            this.MStr = str;
        }

        public int CompareTo(Chain other)
        {
            return this.MStr.Substring(this.Head, this.Length).CompareTo(this.MStr.Substring(other.Head, other.Length));
        }

        public override string ToString()
        {
            return this.MStr.Substring(this.Head, this.Length);
        }
    }

    [Serializable]
    internal class CharComparer : System.Collections.Generic.EqualityComparer<char>
    {
        public override bool Equals(char x, char y)
        {
            return x.Equals(y);
        }

        public override int GetHashCode(char obj)
        {
            return obj.GetHashCode();
        }
    }

    [Serializable]
    internal struct SuffixRank
    {
        public int Head;
        public int Rank;
    }

    [Serializable]
    internal class SuffixRankComparer : IComparer<SuffixRank>
    {
        public bool Equals(SuffixRank x, SuffixRank y)
        {
            return x.Rank.Equals(y.Rank);
        }

        public int Compare(SuffixRank x, SuffixRank y)
        {
            return x.Rank.CompareTo(y.Rank);
        }
    }
    #endregion
}