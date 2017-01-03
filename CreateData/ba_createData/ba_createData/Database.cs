/*
 Copyright (c) 2016 Mark Roland, University of Copenhagen, Department of Computer Science
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

namespace ba_createData
{
    using System;
    using System.IO;
    using System.Text.RegularExpressions;
    using System.Threading;

    /// <summary>
    /// The malware database.
    /// </summary>
    [Serializable]
    public class MalwareDatabase
    {

        /// <summary>
        /// The create database, such that each row in the .csv files are concat with the termination character '$'
        /// </summary>
        public void CreateDatabase()
        {
            var databaseDirectory = Thread.GetDomain().BaseDirectory;
            string path = Thread.GetDomain().BaseDirectory + "Hashes//";
            var files = Directory.GetFiles(path);
            if (files.Length != 0)
            {
                foreach (var file in files)
                {
                    string pattern = "^[0-9a-fA-F]{32}$";
                    using (StreamReader sr = new StreamReader(file))
                    {
                        // currentLine will be null when the StreamReader reaches the end of file
                        string currentLine;
                        while ((currentLine = sr.ReadLine()) != null)
                        {
                            // Checking current line to insure that it is a MD5 hash, with appropriate length
                            if (Regex.Match(currentLine, pattern, RegexOptions.None).Success)
                            {
                                var filePath = databaseDirectory + "\\Data\\" + "complete" + ".data";
                                // var filePath = databaseDirectory + "\\Data\\" +  "complete.data";
                                using (
                                    var fileStream = new FileStream(
                                        filePath,
                                        FileMode.Append,
                                        FileAccess.Write,
                                        FileShare.Write))
                                using (var bw = new BinaryWriter(fileStream))
                                {
                                    bw.Write(currentLine + "$");
                                }
                            }
                        }

                        sr.Close();
                        sr.Dispose();
                    }
                }
            }
        }
    }
    /// <summary>
    /// Taken from https://gist.github.com/axefrog/2373868
    /// C# Suffix tree implementation based on Ukkonen's algorithm. 
    /// Full explanation here: http://stackoverflow.com/questions/9452701/ukkonens-suffix-tree-algorithm-in-plain-english
    /// </summary>
}
