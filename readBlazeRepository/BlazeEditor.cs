using jwh.utilities.blaze;
using ScintillaNet.Configuration;
using ScintillaNet.Design;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Windows.Forms;
using System.Xml;
using System.Xml.XPath;

namespace readBlazeRepository
{
    /// <summary>
    /// Primary IDE Window Class
    /// </summary>
    /// <remarks>
    /// TODO Seperate out TreeView into own file/class, possibly
    /// use a background worker or thread when loading the blaze
    /// repo and tree view so UI stays responsive. Improve progress
    /// feedback during these tasks
    /// </remarks>
    public partial class BlazeEditor : Form
    {
        // TODO Bring in variables for use in keywords and autocomplete
        // perhaps by pulling lists into this form class when filling
        // the treeview, and then passing them to the tab controler
        // when creating tabs
        // TODO clean up what is private, public, etc. Almost 
        // everything is public right now
        public string repoPath = Properties.Settings.Default.repoPathString;
        public BlazeRepository repo;
        public string substringDirectory;
        public Dictionary<string, IEnumerable<string>> typeListDictionary;
        public List<String> globalVarStringList;
        public Dictionary<string, string> globalVarDictionary;
        public int periodLoc;

        /// <summary>
        /// BlazeEditor class constructor
        /// </summary>
        /// <remarks>
        /// Use initialization to clear the tree view (maybe don't need to)
        /// Select repo tab... or we could switch tab order
        /// </remarks>
        public BlazeEditor()
        {
            InitializeComponent();
            // List<string> typeFuncAndMethodList = new List<string>();

        }

        /// <summary>
        /// When the window loads fill client list and
        /// select repo display tab
        /// </summary>
        /// <remarks>
        /// TODO change name to BlazeEditor instead of Form1 
        /// (needs to be done in both designer code and here)
        /// </remarks>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Form1_Load(object sender, EventArgs e)
        {
            refreshClientListView();
            repoProjectTabControl.SelectedTab = repoDisplayTab;

        }

        #region load_tree_view

        public void populateTreeView(BlazeRepository repo, TreeNode parentNode)
        {
            try
            {
                if (repo.clients.Count != 0)
                {
                    foreach (BlazeClient client in repo.clients)
                    {
                        dirSelectProgressBar.Increment(10);
                        TreeNode clientNode = new TreeNode(client.getClientName());
                        parentNode.Nodes.Add(clientNode);
                        clientNode.Tag = client;
                        populateClientList(client, clientNode);
                    }
                }
            }
            catch (UnauthorizedAccessException)
            {
                parentNode.Nodes.Add("Access denied");
            } // end catch
        }

        public void populateRepositoryListView()
        {
            try
            {
                if (repo.clients.Count != 0)
                {
                    foreach (BlazeClient client in repo.clients)
                    {
                        dirSelectProgressBar.Increment(10);
                        repositoryListBox.Items.Add(client.getClientName());
                    }
                }
            }
            catch (UnauthorizedAccessException)
            {
                System.Diagnostics.Debug.WriteLine("Problem creating entry for client");
            
            } // end catch
        }

        public void populateClientList(BlazeClient client, TreeNode tn)
        {
            foreach (BlazeRulebase rb in client.rulebases)
            {
                dirSelectProgressBar.Increment(2);
                TreeNode rbNode = new TreeNode(rb.getRBName());
                tn.Nodes.Add(rbNode);
                rbNode.Tag = rb;
                populateRBList(rb, rbNode);
            }
        }

        public void populateRBList(BlazeRulebase rb, TreeNode tn)
        {
            foreach (BlazeFile file in rb.rbFileList)
            {
                TreeNode fileNode = new TreeNode(file.blazeFileInfo.Name);
                tn.Nodes.Add(fileNode);
                fileNode.Tag = file;

                if(file.GetType() == typeof(BlazeRuleset))
                {
                    populateRulesetNode((BlazeRuleset)file, fileNode);
                }
            }
        }

        public void populateRulesetNode(BlazeRuleset rs, TreeNode tn)
        {
            foreach(BlazeRule rule in rs.ruleList)
            {
                TreeNode ruleNode = new TreeNode(rule.idename);
                tn.Nodes.Add(ruleNode);
                ruleNode.Tag = rule;
            }
        }

        #endregion

        private void refreshClientListView()
        {
            Cursor = Cursors.WaitCursor;
            ruleBaseTreeView.Nodes.Clear();
            dirSelectProgressBar.Value = 5;
            repo = new BlazeRepository(repoPath, false);
            dirSelectProgressBar.Value = 25;
            populateRepositoryListView();
            dirSelectProgressBar.Value = 0;
            Cursor = Cursors.Default;
        }

        private void refreshProjectTreeView(string inClientName)
        {
            Cursor = Cursors.WaitCursor;
            ruleBaseTreeView.Nodes.Clear();
            dirSelectProgressBar.Value = 5;
            BlazeClient clientToLoad = repo.getClient(inClientName);

            ruleBaseTreeView.Nodes.Add(inClientName);
            clientToLoad.populateClient(dirSelectProgressBar);
            // client is populated, grab global vars
            globalVarStringList = new List<string>(clientToLoad.globalVarStrings);
            globalVarDictionary = new Dictionary<string, string>(clientToLoad.globalVarDictionary);
            codeDisplayTabs.globalVarList = globalVarStringList;
            codeDisplayTabs.globalVarTypeDictionary = globalVarDictionary;

            int i = 0;
            foreach (KeyValuePair<string, string> s in globalVarDictionary)
            {
                System.Diagnostics.Debug.WriteLine(s.Key + " with value " + s.Value);
                i++;
                if (i > 15)
                    break;
            }

            dirSelectProgressBar.Value = 45;
            populateClientList(clientToLoad, ruleBaseTreeView.Nodes[0]);
            //populateTreeView(repo, ruleBaseTreeView.Nodes[0]);
            ruleBaseTreeView.Nodes[0].Expand();
            repoProjectTabControl.SelectedTab = projectTreeTab;
            dirSelectProgressBar.Value = 0;
            Cursor = Cursors.Default;
            System.Diagnostics.Debug.WriteLine("ruleBaseTreeView nodes: " + ruleBaseTreeView.Nodes.Count);
            System.Diagnostics.Debug.WriteLine("repo clients nodes: " + repo.clients.Count);
        }

        private void ruleBaseTreeView_NodeMouseDoubleClick(object sender, TreeNodeMouseClickEventArgs e)
        {
            openBlazeFileFromTree(e.Node);           
        }

        public BlazeRepoTabPage openBlazeFile(object inBlazeFile)
        {
            dirSelectProgressBar.Increment(15);
            BlazeRepoTabPage newTabPage = new BlazeRepoTabPage();

            if (inBlazeFile.GetType() == typeof(BlazeRule))
            {
                newTabPage = new BlazeRuleTabPage((BlazeRule)inBlazeFile, codeDisplayTabs);
            }
            else
            {
                // this is getting ugly must be a way TODO this better
                BlazeFile blazeFileToOpen = (BlazeFile)inBlazeFile;
                string templateType = blazeFileToOpen.blazeFamily;

                dirSelectProgressBar.Increment(5);

                switch (templateType)
                {
                    case "Function Template":
                        newTabPage = new BlazeFunctionTabPage((BlazeFunction)blazeFileToOpen, codeDisplayTabs);
                        break;
                    case "Project Items Template":
                        newTabPage = new BlazeProjectListTabPage((BlazeProjectItems)blazeFileToOpen);
                        break;
                    case "Ruleset Template":
                        newTabPage = new BlazeRulesetTabPage((BlazeRuleset)blazeFileToOpen);
                        break;
                    default:
                        MessageBox.Show("Problem creating tab, file type " + templateType + " not supported");
                        break;
                }
            }

            return newTabPage;
        }

        #region button_and_menu_clicks

        private void refreshDir_Click(object sender, EventArgs e)
        {

            // Cursor = Cursors.WaitCursor;
            // ruleBaseTreeView.Nodes.Clear();
            //dirSelectProgressBar.Value = 5;
            //ruleBaseTreeView.Nodes.Add("Clients");
            //repo = new BlazeRepository(repoPath, true);
            //dirSelectProgressBar.Value = 25;
            //populateTreeView(repo, ruleBaseTreeView.Nodes[0]);
            //ruleBaseTreeView.Nodes[0].Expand();
            //dirSelectProgressBar.Value = 0;
            //Cursor = Cursors.Default;
        }

        private void saveToolStripMenuItem_Click(object sender, EventArgs e)
        {
            BlazeRepoTabPage selectedTab;

            if (codeDisplayTabs.TabPages.Count > 0)
            {
                if (codeDisplayTabs.SelectedTab.GetType() == typeof(BlazeFunctionTabPage) ||
                    codeDisplayTabs.SelectedTab.GetType() == typeof(BlazeRuleTabPage))
                {
                    selectedTab = (BlazeRepoTabPage)codeDisplayTabs.SelectedTab;
                    selectedTab.saveTab();
                }
            }
        }

        private void tabRemoveButton_Click(object sender, EventArgs e)
        {
            if (codeDisplayTabs.TabPages.Count > 0)
            {
                BlazeRepoTabPage tabPage = (BlazeRepoTabPage)codeDisplayTabs.SelectedTab;
                if (tabPage.hasChanged == true)
                {
                    DialogResult toClose = MessageBox.Show("Tab has unsaved changes, clicking OK will close without saving changes",
                                                            "Are you sure you want to close?", MessageBoxButtons.OKCancel);
                    if (toClose == DialogResult.OK)
                    {
                        codeDisplayTabs.TabPages.Remove(codeDisplayTabs.SelectedTab);
                    }
                }
                else
                {
                    codeDisplayTabs.TabPages.Remove(codeDisplayTabs.SelectedTab);
                }
            }
        }

        private void closeTabToolStripMenuItem_Click(object sender, EventArgs e)
        {
            tabRemoveButton.PerformClick();
        }
        #endregion

        private void baseSplitContainer_SplitterMoved(object sender, SplitterEventArgs e)
        {

        }

        private void repositoryListBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            // UNDONE Display some info about the client
        }

        private void repositoryListBox_DoubleClick(object sender, EventArgs e)
        {
            string selectedClient = repositoryListBox.SelectedItem.ToString();
            refreshProjectTreeView(selectedClient);
        }

        #region build_and_deploy

        private void runCommandAndCaptureOutput(string command, string path)
        {
            Process p = new Process();
            //StreamReader outputReader = null;
            //StreamReader errorReader = null;
            try
            {
                p.StartInfo.UseShellExecute = false;
                
                // p.StartInfo.ErrorDialog = false;
                // p.StartInfo.RedirectStandardError = true;
                // p.StartInfo.RedirectStandardOutput = true;
                // p.StartInfo.RedirectStandardInput = true;
                // p.StartInfo.CreateNoWindow = true;
                p.StartInfo.WorkingDirectory = Properties.Settings.Default.dbesChannelScriptDir;
                p.StartInfo.FileName = command;
                bool proccessStarted = p.Start();
                if (proccessStarted)
                {
                    // get the output stream
                    //outputReader = p.StandardOutput;
                    //errorReader = p.StandardError;
                    p.WaitForExit();

                    // display result
                    //string s = "Output" + Environment.NewLine + "===================" +
                    //    Environment.NewLine;
                    //s += outputReader.ReadToEnd();
                    //s += Environment.NewLine + "Error" + Environment.NewLine + "===================" +
                    //    Environment.NewLine;
                    //s += errorReader.ReadToEnd();
                    // displayTabControl.SelectedTab = outputDisplayTab;
                    // outputDisplayTextBox.AppendText(s);
                }
            }
            catch (Exception e)
            {
                MessageBox.Show("Exception encountered while running external command " + command + "\nException text: " + e.ToString());
            }

        }
        private void buildToolStripMenuItem_Click(object sender, EventArgs e)
        {
            buildToolStripMenuItem.Enabled = false;
            hotDeployToolStripMenuItem.Enabled = false;
            buildAndDeployBackgroundWorker.RunWorkerAsync(Properties.Settings.Default.compilerPath);
            outputDisplayTextBox.AppendText("Beginning build..." + Environment.NewLine);
        }

        private void hotDeployToolStripMenuItem_Click(object sender, EventArgs e)
        {
            buildToolStripMenuItem.Enabled = false;
            hotDeployToolStripMenuItem.Enabled = false;
            buildAndDeployBackgroundWorker.RunWorkerAsync(Properties.Settings.Default.ruleInitPath);
            outputDisplayTextBox.AppendText("Beginning hot deploy..." + Environment.NewLine);
        }

        private void backgroundWorker1_DoWork(object sender, DoWorkEventArgs e)
        {
            BackgroundWorker worker = sender as BackgroundWorker;
            runCommandAndCaptureOutput((string)e.Argument, Properties.Settings.Default.dbesChannelScriptDir);
        }

        private void backgroundWorker1_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {

        }

        private void backgroundWorker1_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            buildToolStripMenuItem.Enabled = true;
            hotDeployToolStripMenuItem.Enabled = true;
            outputDisplayTextBox.AppendText("Complete, see console window for any errors" + Environment.NewLine);
        }

        #endregion

        private void ruleBaseTreeView_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == 13)
            {

                openBlazeFileFromTree(ruleBaseTreeView.SelectedNode);
            }
        }
        
        /// <summary>
        /// Opens selected file from mouse doubleclick or enter key press
        /// </summary>
        /// <remarks>
        /// Handles feedback to user and checks to see if tab already exists.
        /// If tab exists switch to the selected tab.
        /// If tab does not exist ask the tab control to create the tab.
        /// </remarks>
        /// <param name="tn"></param>
        private void openBlazeFileFromTree(TreeNode tn)
        {
            Cursor = Cursors.WaitCursor;
            dirSelectProgressBar.Value = 1;
            bool tabExists = false;
            string fileToOpen = tn.Text;

            if (tn.Nodes.Count == 0 || tn.Tag.GetType() == typeof(BlazeRuleset))
            {
                if (codeDisplayTabs.TabPages.Count > 0)
                {
                    foreach (TabPage openTab in codeDisplayTabs.TabPages)
                    {
                        dirSelectProgressBar.Increment(2);
                        if (openTab.GetType() == typeof(BlazeRepoTabPage))
                        {
                            BlazeRepoTabPage tempTab = (BlazeRepoTabPage)openTab;
                            if (tempTab.idename == fileToOpen)
                            {
                                tabExists = true;
                                codeDisplayTabs.SelectedTab = openTab;
                                break;
                            }
                        }
                    }
                }
                if (tabExists == false)
                {
                    dirSelectProgressBar.Increment(10);
                    object blazeFileToOpen = tn.Tag;
                    BlazeRepoTabPage newBlazeTab = openBlazeFile(blazeFileToOpen);
                    codeDisplayTabs.TabPages.Add(newBlazeTab);
                    codeDisplayTabs.SelectedTab = newBlazeTab;
                }

            }
            dirSelectProgressBar.Value = 0;
            Cursor = Cursors.Default;
        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void undoToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (codeDisplayTabs.TabCount > 0)
            {
                if (codeDisplayTabs.SelectedTab.GetType() == typeof(BlazeFunctionTabPage))
                {
                    BlazeFunctionTabPage t = (BlazeFunctionTabPage)codeDisplayTabs.SelectedTab;
                    t.codeBox.UndoRedo.Undo();
                }
                else if (codeDisplayTabs.SelectedTab.GetType() == typeof(BlazeRuleTabPage))
                {
                    BlazeRuleTabPage rt = (BlazeRuleTabPage)codeDisplayTabs.SelectedTab;
                    rt.codeBox.UndoRedo.Undo();
                }
                else
                {
                    // Nothing else TODO currently, may be used to undo adding or removing functions,
                    // rules/rulebases, variables, paramaters, etc. later
                }
            }
        }

        private void redoToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (codeDisplayTabs.TabCount > 0)
            {
                if (codeDisplayTabs.SelectedTab.GetType() == typeof(BlazeFunctionTabPage))
                {
                    BlazeFunctionTabPage t = (BlazeFunctionTabPage)codeDisplayTabs.SelectedTab;
                    t.codeBox.UndoRedo.Redo();
                }
                else if (codeDisplayTabs.SelectedTab.GetType() == typeof(BlazeRuleTabPage))
                {
                    BlazeRuleTabPage rt = (BlazeRuleTabPage)codeDisplayTabs.SelectedTab;
                    rt.codeBox.UndoRedo.Redo();
                }
                else
                {
                    // Nothing else TODO currently, may be used to redo adding or removing functions,
                    // rules/rulebases, variables, paramaters, etc. later
                }
            }
        }

        private void cutToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (codeDisplayTabs.TabCount > 0)
            {
                if (codeDisplayTabs.SelectedTab.GetType() == typeof(BlazeFunctionTabPage))
                {
                    BlazeFunctionTabPage ft = (BlazeFunctionTabPage)codeDisplayTabs.SelectedTab;
                    ft.codeBox.NativeInterface.Cut();
                }
                else if (codeDisplayTabs.SelectedTab.GetType() == typeof(BlazeRuleTabPage))
                {
                    BlazeRuleTabPage rt = (BlazeRuleTabPage)codeDisplayTabs.SelectedTab;
                    rt.codeBox.NativeInterface.Cut();
                }
                else
                {
                    // Nothing else TODO currently, may be used to redo adding or removing functions,
                    // rules/rulebases, variables, paramaters, etc. later
                }
            }
        }

        private void copyToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (codeDisplayTabs.TabCount > 0)
            {
                if (codeDisplayTabs.SelectedTab.GetType() == typeof(BlazeFunctionTabPage))
                {
                    BlazeFunctionTabPage ft = (BlazeFunctionTabPage)codeDisplayTabs.SelectedTab;
                    ft.codeBox.NativeInterface.Copy();
                }
                else if (codeDisplayTabs.SelectedTab.GetType() == typeof(BlazeRuleTabPage))
                {
                    BlazeRuleTabPage rt = (BlazeRuleTabPage)codeDisplayTabs.SelectedTab;
                    rt.codeBox.NativeInterface.Copy();
                }
                else
                {
                    // Nothing else TODO currently, may be used to redo adding or removing functions,
                    // rules/rulebases, variables, paramaters, etc. later
                }
            }
        }

        private void pasteToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (codeDisplayTabs.TabCount > 0)
            {
                if (codeDisplayTabs.SelectedTab.GetType() == typeof(BlazeFunctionTabPage))
                {
                    BlazeFunctionTabPage ft = (BlazeFunctionTabPage)codeDisplayTabs.SelectedTab;
                    ft.codeBox.NativeInterface.Paste();
                }
                else if (codeDisplayTabs.SelectedTab.GetType() == typeof(BlazeRuleTabPage))
                {
                    BlazeRuleTabPage rt = (BlazeRuleTabPage)codeDisplayTabs.SelectedTab;
                    rt.codeBox.NativeInterface.Paste();
                }
                else
                {
                    // Nothing else TODO currently, may be used to redo adding or removing functions,
                    // rules/rulebases, variables, paramaters, etc. later
                }
            }
        }

        private void findToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (codeDisplayTabs.TabCount > 0)
            {
                if (codeDisplayTabs.SelectedTab.GetType() == typeof(BlazeFunctionTabPage))
                {
                    BlazeFunctionTabPage ft = (BlazeFunctionTabPage)codeDisplayTabs.SelectedTab;
                    ft.codeBox.FindReplace.Window.Show();
                }
                else if (codeDisplayTabs.SelectedTab.GetType() == typeof(BlazeRuleTabPage))
                {
                    BlazeRuleTabPage rt = (BlazeRuleTabPage)codeDisplayTabs.SelectedTab;
                    rt.codeBox.FindReplace.Window.Show();
                }
                else
                {
                    // Nothing else TODO currently, may be used to redo adding or removing functions,
                    // rules/rulebases, variables, paramaters, etc. later
                }
            }
        }

        private void aboutToolStripMenuItem_Click(object sender, EventArgs e)
        {

        }

        private void toolStripSaveButton_Click(object sender, EventArgs e)
        {
            saveToolStripMenuItem.PerformClick();
        }

        private void toolStripCutButton_Click(object sender, EventArgs e)
        {
            cutToolStripMenuItem.PerformClick();
        }

        private void toolStripCopyButton_Click(object sender, EventArgs e)
        {
            copyToolStripMenuItem.PerformClick();
        }

        private void toolStripPasteButton_Click(object sender, EventArgs e)
        {
            pasteToolStripMenuItem.PerformClick();
        }

        private void toolStripUndoButton_Click(object sender, EventArgs e)
        {
            undoToolStripMenuItem.PerformClick();
        }

        private void toolStripRedoButton_Click(object sender, EventArgs e)
        {
            redoToolStripMenuItem.PerformClick();
        }

        private void toolStripFindButton_Click(object sender, EventArgs e)
        {
            findToolStripMenuItem.PerformClick();
        }

        private void toolStripFindInFilesButton_Click(object sender, EventArgs e)
        {
            if (this.findInFilesToolStripMenuItem.Enabled)
            {
                findInFilesToolStripMenuItem.PerformClick();
            }
        }

        private void toolStripCompileButton_Click(object sender, EventArgs e)
        {
            buildToolStripMenuItem.PerformClick();
        }

        private void toolStripHotDeployButton_Click(object sender, EventArgs e)
        {
            hotDeployToolStripMenuItem.PerformClick();
        }
    }
}
