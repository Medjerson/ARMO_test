using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading;
using System.Diagnostics;

namespace ARMO_test
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }
        
        private Thread ytr;
        private Stopwatch stw;
        private Regex reg;
        private bool isBoth;
        private int filesCount = 0;

        internal event EventHandler<ProgressEventArgs> RefreshStat;
        internal event EventHandler<NodeEventArgs> RefreshNodes;

        private void Form1_Load(object sender, EventArgs e)
        {
            comboBox1.Items.Add("AND");
            comboBox1.Items.Add("OR");
            comboBox1.SelectedIndex = 0;
            RefreshStat += new EventHandler<ProgressEventArgs>(Form1_RefreshStat);
            RefreshNodes += new EventHandler<NodeEventArgs>(Form1_RefreshNodes);
            try
            {
                StreamReader sr = new StreamReader(Application.StartupPath + "//INFO.txt");
                textBox1.Text = sr.ReadLine();
                textBox2.Text = sr.ReadLine();
                textBox3.Text = sr.ReadLine();
                sr.Close();
            }
            catch (FileNotFoundException)
            {
                MessageBox.Show("Settings reading error", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
             
        }

        private void button1_Click(object sender, EventArgs e)
        {

            FolderBrowserDialog fdb = new FolderBrowserDialog();
            if (fdb.ShowDialog() == DialogResult.OK)
            {
                textBox1.Text = fdb.SelectedPath;
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            if ((textBox2.Text != string.Empty) & (textBox3.Text != string.Empty))
            {
                if (Directory.Exists(textBox1.Text))
                {
                    try
                    {
                        reg = new Regex(textBox2.Text);
                        if (comboBox1.SelectedIndex == 0)
                            isBoth = true;
                        else
                            isBoth = false;
                        stw = new Stopwatch();
                        treeView1.Nodes.Clear();
                        filesCount = 0;
                        stw.Start();
                        ytr = new Thread(newSearch);
                        ytr.Start();
                    }
                    catch (ArgumentException)
                    {
                        MessageBox.Show("Invalid name template. Pls try again.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
                else
                    MessageBox.Show("Invalid directory name.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            else
                MessageBox.Show("Empty criteria", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        private void Form1_FormClosed(object sender, FormClosedEventArgs e)
        {
            try
            {
                StreamWriter sw = new StreamWriter(Application.StartupPath + "//INFO.txt", false);

                sw.WriteLine(textBox1.Text);
                sw.WriteLine(textBox2.Text);
                sw.WriteLine(textBox3.Text);

                sw.Close();
                RefreshStat -= Form1_RefreshStat;
                RefreshNodes -= Form1_RefreshNodes;
            }
            catch (IOException)
            {
                MessageBox.Show("Settings writing error", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void Form1_RefreshStat(object sender, ProgressEventArgs e)
        {
            if (label9.InvokeRequired)
            {
                EventHandler<ProgressEventArgs> ep = new EventHandler<ProgressEventArgs>(Form1_RefreshStat);
                this.Invoke(ep, sender, e);
            }
            else
            {
                label5.Text = filesCount.ToString();
                if (stw != null)
                   label9.Text = ((int)(stw.Elapsed.TotalSeconds)).ToString() + " sec";
               label7.Text = e.getName;
            }
        }

        private void Form1_RefreshNodes(object sender, NodeEventArgs e)
        {
            if (treeView1.InvokeRequired)
            {
                EventHandler<NodeEventArgs> ep = new EventHandler<NodeEventArgs>(Form1_RefreshNodes);
                this.Invoke(ep, sender, e);
            }

            else
            {
                if (e.IsRemove)
                    e.GetNodes[e.GetIndex].Remove();
                else
                    e.GetNodes.Add(e.GetName);
            }

        }

        private void button3_Click(object sender, EventArgs e)
        {
            if ((ytr != null) && (ytr.ThreadState == System.Threading.ThreadState.Running))
                ytr.Abort();
            if (stw != null)
                stw.Stop();


        }

        #region helpers
        private void fileSearch(DirectoryInfo dinfo, TreeNodeCollection currentCollection)
        {
            Form1_RefreshNodes(null, new NodeEventArgs(currentCollection, dinfo.Name));
            int currentCollectionIndex = currentCollection.Count - 1;
            TreeNodeCollection childrenCollection = currentCollection[currentCollectionIndex].Nodes;
            FileInfo[] currentFiles = dinfo.GetFiles();
            for (int ii = 0; ii < currentFiles.Length; ++ii)
            {
                if (isBoth)
                {
                    if (reg.IsMatch(currentFiles[ii].Name) & strExists(currentFiles[ii]))
                    {
                        Form1_RefreshNodes(null, new NodeEventArgs(currentCollection, currentFiles[ii].Name));
                    }
                }
                else
                {
                    if (reg.IsMatch(currentFiles[ii].Name) ^ strExists(currentFiles[ii]))
                    {
                        Form1_RefreshNodes(null, new NodeEventArgs(childrenCollection, currentFiles[ii].Name));
                    }
                }
                ++filesCount;
                RefreshStat(null, new ProgressEventArgs(currentFiles[ii].Name));
                Thread.Sleep(10);
            }
            
            DirectoryInfo[] dlist = dinfo.GetDirectories();
            foreach (DirectoryInfo dir in dlist)
                fileSearch(dir, childrenCollection);
            if (childrenCollection.Count == 0)
                RefreshNodes(null, new NodeEventArgs(currentCollection, currentCollectionIndex));
            
        }
        private bool strExists(FileInfo inf)
        {
            StreamReader sr = new StreamReader(inf.OpenRead());
            while (!sr.EndOfStream)
                if (sr.ReadLine().Contains(textBox3.Text))
                    return true;
            return false;
        }

        private void newSearch()
        {
            fileSearch(new DirectoryInfo(Path.GetFullPath(textBox1.Text)), treeView1.Nodes);
        }


        internal class ProgressEventArgs : EventArgs
        {
            string currentFile;
            public ProgressEventArgs(string file)
            {
                currentFile = file;
            }
            public string getName
            {
                get { return currentFile; }
            }
        }

        internal class NodeEventArgs : EventArgs
        {
            string currentName;
            TreeNodeCollection currentCollection;
            bool isRemove;
            int removingIndex;
            public NodeEventArgs(TreeNodeCollection coll, string name)
            {
                currentName = name;
                currentCollection = coll;
                isRemove = false;
            }

            public NodeEventArgs(TreeNodeCollection coll, int index)
            {
                currentCollection = coll;
                removingIndex = index;
                isRemove = true;
            }

            public string GetName
            {
                get { return currentName; }
            }

            public TreeNodeCollection GetNodes
            {
                get { return currentCollection; }
            }

            public bool IsRemove
            {
                get { return isRemove; }
            }

            public int GetIndex
            {
                get { return removingIndex; }
            }

        }

        #endregion



    }
}