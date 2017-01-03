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

using ba_createData.TestFolder;

namespace ba_createData
{
    using System.Collections.Generic;
    using StringSearch.Collections;
    using System.Diagnostics;
    using System.Threading.Tasks;
    using System;
    using System.ComponentModel;
    using System.IO;
    using System.Text;
    using System.Threading;
    using System.Windows.Forms;

    /// <summary>
    /// The form 1.
    /// </summary>
    public partial class Form1 : Form
    {

        /// <summary>
        /// The set text callback.
        /// </summary>
        /// <param name="text">
        /// The text.
        /// </param>
        public delegate void SetTextCallback(string text);

        /// <summary>
        /// The set text.
        /// </summary>
        /// <param name="text">
        /// The text.
        /// </param>
        public void SetText(string text)
        {
            // InvokeRequired required compares the thread ID of the
            // calling thread to the thread ID of the creating thread.
            // If these threads are different, it returns true.
            if (textBox1.InvokeRequired)
            {
                var d = new SetTextCallback(SetText);
                Invoke(d, text);
            }
            else
            {
                textBox1.Text = text;
            }
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="text"></param>
        public void SetArrayTime(string text)
        {
            // InvokeRequired required compares the thread ID of the
            // calling thread to the thread ID of the creating thread.
            // If these threads are different, it returns true.
            if (SuffixArrayTime.InvokeRequired)
            {
                SetTextCallback d = SetArrayTime;
                Invoke(d, text);
            }
            else
            {
                SuffixArrayTime.Text = text;
            }
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="text"></param>
        public void SetTreeTime(string text)
        {
            // InvokeRequired required compares the thread ID of the
            // calling thread to the thread ID of the creating thread.
            // If these threads are different, it returns true.
            if (SuffixTreeTime.InvokeRequired)
            {
                SetTextCallback d = SetTreeTime;
                Invoke(d, text);
            }
            else
            {
                SuffixTreeTime.Text = text;
            }
        }

        /// <summary>
        /// The _reset event.
        /// </summary>
        private readonly AutoResetEvent _resetEvent = new AutoResetEvent(false);

        /// <summary>
        /// The build suffix array.
        /// </summary>
        private readonly BackgroundWorker _buildSuffixArray = new BackgroundWorker();

        /// <summary>
        /// The build suffix array.
        /// </summary>
        private readonly BackgroundWorker _buildSuffixTree = new BackgroundWorker();

        /// <summary>
        /// The object used for cancelling tasks.
        /// </summary>
        private CancellationTokenSource _mCancelTokenSource;

        /// <summary>
        /// Initializes a new instance of the <see cref="Form1"/> class. 
        /// The form 1.
        /// </summary>
        public Form1()
        {
            InitializeComponent();
            InitializeWorkerProcesses();

            //var suffixTree= ReadFromBinaryFile<SuffixTree>(Thread.GetDomain().BaseDirectory + "suffixTrees\\000.data");
            //var test = suffixTree.Find("000605d7cbb3928b07f8e7473c820f4c$");
            //var someResult = 0;

            //var filePacementDirectoryTask = Thread.GetDomain().BaseDirectory + "suffixTrees\\";
            //var objectSuffixTree = ReadFromBinaryFile<suffixTree.SuffixTree>(filePacementDirectoryTask + "000.data");

            //var test1 = 0;
            //var test2 = 0;
            //var tryFind = objectSuffixTree.TryFind("000605d7cbb3928b07f8e7473c820f4c$", ref test1, ref test2);
            //SetText("UUhhh:" + tryFind);
        }

        /// <summary>
        /// The initialize worker processes.
        /// </summary>
        private void InitializeWorkerProcesses()
        {
            //this.createDataFile.WorkerReportsProgress = true;
            //this.createDataFile.WorkerSupportsCancellation = true;
            //this.createDataFile.DoWork += CreateDatafileDoWork;

            this._buildSuffixArray.WorkerReportsProgress = true;
            this._buildSuffixArray.WorkerSupportsCancellation = true;
            this._buildSuffixArray.DoWork += CreateSuffixArrayDoWork;

            this._buildSuffixTree.WorkerReportsProgress = true;
            this._buildSuffixTree.WorkerSupportsCancellation = true;
            this._buildSuffixTree.DoWork += CreateSuffixTreeDoWork;
        }

        /// <summary>
        /// The cancel.
        /// </summary>
        public void Cancel()
        {
            createDataFile.CancelAsync();
            _resetEvent.WaitOne();
        }

        /// <summary>
        /// The create datafile_ do work.
        /// </summary>
        /// <param name="sender">
        /// The sender.
        /// </param>
        /// <param name="e">
        /// The e.
        /// </param>
        private void CreateDatafileDoWork(object sender, DoWorkEventArgs e)
        {
            SetText("Updating database....");
            while (!e.Cancel)
            {
                var buildingData = new UpdateData();
                buildingData.UpdateDataFile();
                Cancel();
            }

            _resetEvent.WaitOne();

        }

        private void CreateSuffixArrayDoWork(object sender, DoWorkEventArgs e)
        {
            var path = Thread.GetDomain().BaseDirectory + "Data\\";
            var files = Directory.GetFiles(path);

            _mCancelTokenSource = new CancellationTokenSource();

            // Get a reference to the cancellation token.
            var readFileCancelToken = _mCancelTokenSource.Token;

            // I am iterating through the list of files to use one task per file to do them in parallel.
            Stopwatch stopWatch = new Stopwatch();
            stopWatch.Start();
            foreach (var file in files)
            {
                var buildSuffixArrayTask = Task.Factory.StartNew(() =>
                {
                    // If cancel has been chosen, throw an exception now before doing anything.
                    readFileCancelToken.ThrowIfCancellationRequested();
                    try
                    {
                        LongProcess(file);
                    }

                    catch (Exception)
                    {
                        //
                    }
                    finally
                    {
                        GC.Collect();
                    }

                }, readFileCancelToken, TaskCreationOptions.LongRunning, TaskScheduler.Default);

                Task.WaitAll(buildSuffixArrayTask);
            }

            stopWatch.Stop();
            SetArrayTime(stopWatch.Elapsed.ToString());
        }


        public void LongProcess(string file)
        {

            var filePacementDirectoryTask = Thread.GetDomain().BaseDirectory + "SuffixArrays\\";
            var pathTask = Thread.GetDomain().BaseDirectory + "Data\\";
            var fileName = Path.GetFileName(file);
            var completeStringTask = DataBaseToText(pathTask + fileName);
            var SA = new MemoryEfficientByteAlignedBigULongArray(completeStringTask.Length);
            var suffixArray = SAIS.Sufsort(completeStringTask, SA, completeStringTask.Length);
            ExportData(fileName, filePacementDirectoryTask, suffixArray, completeStringTask);
        }


        /// <summary>
        /// Export the suffix array to file for later process
        /// </summary>
        /// <param name="fileName"></param>
        /// <param name="directory"></param>
        /// <param name="suffixArray"></param>
        /// <param name="completeText"></param>
        private void ExportData(string fileName, string directory, HashSet<long> suffixArray, string completeText)
        {
            if (!File.Exists(@directory + "string_" + fileName))
            {
                var stringText = File.CreateText(@directory + "string_" + fileName);
                stringText.Close();
            }
            using (var file = new StreamWriter(@directory + "string_" + fileName, true))
            {
                file.WriteLine(completeText);
            }

            var result = string.Join(";", suffixArray);
            File.WriteAllText(directory + fileName, result);
        }

        /// <summary>
        /// The create suffix array do work.
        /// </summary>
        /// <param name="sender">
        /// The sender.
        /// </param>
        /// <param name="e">
        /// The e.
        /// </param>
        private void CreateSuffixTreeDoWork(object sender, DoWorkEventArgs e)
        {

            var path = Thread.GetDomain().BaseDirectory + "Data\\";
            var files = Directory.GetFiles(path);

            _mCancelTokenSource = new CancellationTokenSource();

            // Get a reference to the cancellation token.
            var readFileCancelToken = _mCancelTokenSource.Token;

            // I am iterating through the list of files to use one task per file to do them in parallel.
            Stopwatch stopWatch = new Stopwatch();
            stopWatch.Start();
            foreach (var file in files)
            {
                var buildSuffixTreeTask = Task.Factory.StartNew(() =>
                {
                    // If cancel has been chosen, throw an exception now before doing anything.
                    readFileCancelToken.ThrowIfCancellationRequested();
                    try
                    {
                        var completeStringTask = DataBaseToText(file);
                        BuildSuffixTree(completeStringTask, file);
                    }

                    catch (Exception ex)
                    {
                        //
                    }
                    finally
                    {
                        GC.Collect();
                    }

                }, readFileCancelToken, TaskCreationOptions.LongRunning, TaskScheduler.Default);

                Task.WaitAll(buildSuffixTreeTask);
            }

            stopWatch.Stop();
            SetTreeTime(stopWatch.Elapsed.ToString());
        }


        /// <summary>
        /// 
        /// </summary>
        private void BuildSuffixTree(string text, string path)
        {
            var suffixTree = new SuffixTree(text,path);
            suffixTree.BuildTree();
        }

        /// <summary>
        /// Writes the given object instance to a binary file.
        /// <para>Object type (and all child types) must be decorated with the [Serializable] attribute.</para>
        /// <para>To prevent a variable from being serialized, decorate it with the [NonSerialized] attribute; cannot be applied to properties.</para>
        /// </summary>
        /// <typeparam name="T">The type of object being written to the XML file.</typeparam>
        /// <param name="filePath">The file path to write the object instance to.</param>
        /// <param name="objectToWrite">The object instance to write to the XML file.</param>
        /// <param name="append">If false the file will be overwritten if it already exists. If true the contents will be appended to the file.</param>
        public static void WriteToBinaryFile<T>(string filePath, T objectToWrite, SuffixTree tree, bool append = false)
        {
            using (Stream stream = File.Open(filePath, append ? FileMode.Append : FileMode.Create))
            {
                using (BinaryWriter writer = new BinaryWriter(stream))
                {
                    tree.Save(writer, tree);
                }
            }
        }

        /// <summary>
        /// Reads an object instance from a binary file.
        /// </summary>
        /// <typeparam name="T">The type of object to read from the XML.</typeparam>
        /// <param name="filePath">The file path to read the object instance from.</param>
        /// <returns>Returns a new instance of the object read from the binary file.</returns>
        public static T ReadFromBinaryFile<T>(string filePath)
        {
            using (Stream stream = File.Open(filePath, FileMode.Open))
            {
                var binaryFormatter = new System.Runtime.Serialization.Formatters.Binary.BinaryFormatter();
                return (T)binaryFormatter.Deserialize(stream);
            }
        }

        /// <summary>
        /// The load.
        /// </summary>
        /// <param name="filePath">
        /// </param>
        /// <returns>
        /// The <see cref="string"/>.
        /// </returns>
        public static string DataBaseToText(string filePath)
        {
            var stringHolder = string.Empty;

            using (Stream f = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                var offset = 0;
                var len = f.Length;
                var buffer = new byte[len];

                var readLen = 10000000;


                while (offset != len)
                {
                    if (offset + readLen > len)
                    {
                        readLen = (int)len - offset;
                    }

                    offset += f.Read(buffer, offset, readLen);
                }

                var incomingOffset = 0;
                var outboundBuffer = new byte[10000000];

                while (incomingOffset < buffer.Length)
                {
                    var length = Math.Min(outboundBuffer.Length, buffer.Length - incomingOffset);

                    Buffer.BlockCopy(buffer, incomingOffset, outboundBuffer, 0, readLen);

                    incomingOffset += length;
                    var currentString = Encoding.UTF8.GetString(outboundBuffer);
                    stringHolder += RemoveSpecialCharacters(currentString).Replace("!", string.Empty).Trim();
                }

                f.Close();
                return stringHolder;
            }
        }

        /// <summary>
        /// The remove special characters.
        /// </summary>
        /// <param name="str">
        /// The str.
        /// </param>
        /// <returns>
        /// The <see cref="string"/>.
        /// </returns>
        public static string RemoveSpecialCharacters(string str)
        {
            var sb = new StringBuilder();
            foreach (var c in str)
            {
                if ((c >= '0' && c <= '9') || (c >= 'A' && c <= 'Z') || (c >= 'a' && c <= 'z') || c == '$')
                {
                    sb.Append(c);
                }
            }
            return sb.ToString();
        }

        private void textBox3_TextChanged(object sender, EventArgs e)
        {

        }

        private void button1_Click(object sender, EventArgs e)
        {
            var database = new MalwareDatabase();
            database.CreateDatabase();
        }

        private void button2_Click(object sender, EventArgs e)
        {
            _buildSuffixArray.RunWorkerAsync();
        }

        private void button3_Click(object sender, EventArgs e)
        {
            _buildSuffixTree.RunWorkerAsync();
        }
    }
}
