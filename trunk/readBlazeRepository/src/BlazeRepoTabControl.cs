using ScintillaNet.Configuration;
using ScintillaNet.Design;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.Design;
using System.Text.RegularExpressions;
using System.Drawing;
using System.Drawing.Design;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Xml;

namespace jwh.blaze.application
{
    #region BlazeRepoTabControl Class

    /// <summary>
    /// BlazeRepoTabControl Class should (doesn't) hold a set of BlazeRepoTabs
    /// </summary>
    /// <remarks>
    /// TODO This isn't returning tabs of type Blaze Tab. Either fix
    /// return type, or if we have to check type (function, rule, etc) and
    /// cast anyway, then not needed, just use standard tab control
    /// </remarks>
    //[ToolboxBitmap(typeof(System.Windows.Forms.TabControl)),
    //Designer(typeof(Designers.TabControlExDesigner))]
    public class BlazeRepoTabControl : TabControl
    {
        public List<BlazeRepoTabPage> blazeTabs = new List<BlazeRepoTabPage>();
        public Dictionary<string, string> globalVarTypeDictionary;
        public List<string> globalVarList;
        public BlazeRepoTabControl()
        {
        }

        #region Properties

        [Editor(typeof(BlazeRepoTabPageCollectionEditor), typeof(UITypeEditor))]
        public new TabPageCollection TabPages
        {
            get
            {
                return base.TabPages;
            }
        }


        #endregion

        #region TabpageExCollectionEditor

        internal class BlazeRepoTabPageCollectionEditor : CollectionEditor
        {
            public BlazeRepoTabPageCollectionEditor(System.Type type)
                : base(type)
            {
            }

            protected override Type CreateCollectionItemType()
            {
                return typeof(BlazeRepoTabPage);
            }
        }

        #endregion

        private void InitializeComponent()
        {
            this.SuspendLayout();
            // 
            // BlazeRepoTabControl
            // 
            this.KeyDown += new System.Windows.Forms.KeyEventHandler(this.BlazeRepoTabControl_KeyDown);
            this.ResumeLayout(false);

        }

        private void BlazeRepoTabControl_KeyDown(object sender, KeyEventArgs e)
        {

        }
    }


    #endregion

    // TODO designer code isn't filled out enough to work
    // this was based on the TabControlEx form found online, which
    // has the rest of the needed code so these controls can
    // be properly shown in the visual editor
    /// <summary>
    /// BlazeRepoTab class is the base class used for specific tabs
    /// </summary>
    /// <remarks>
    /// Used as base class only, possibly not really needed since tabs
    /// no longer have much in common, and need to check type and cast
    /// in most situations anyway. Only real benefit at this point
    /// is keeping handle to Blaze file for saving
    /// </remarks>
    [Designer(typeof(System.Windows.Forms.Design.ScrollableControlDesigner))]
    public class BlazeRepoTabPage : TabPage
    {
        public BlazeFile tabBlazeFile;
        private bool _hasChanged;
        public string idename;
        protected BlazeRepoTabControl hostControl;


        /// <summary>
        /// Handles if text has changed and adds star to tab to indicate
        /// </summary>
        public bool hasChanged
        {
            get
            {
                return _hasChanged;
            }
            set
            {
                if (value == true)
                {
                    this.Text += "*";
                    _hasChanged = true;
                }
                else if (value == false)
                {
                    this.Text = idename;
                    _hasChanged = false;
                }
            }
        }

        public BlazeRepoTabPage()
        {
            _hasChanged = false;
        }

        /// <summary>
        /// get type method, since these sometimes
        /// don't get a "GetType" method
        /// </summary>
        /// <returns>BlazeRepoTabPage</returns>
        public Type getType()
        {
            return typeof(BlazeRepoTabPage);
        }

        /// <summary>
        /// Save tab
        /// </summary>
        /// <remarks>
        /// See, we still have to check the tab type anyway.
        /// Current design only saves body of rule or function.
        /// Adding or removing parameters or variables does not 
        /// yet exist, and will probably be handled at creation
        /// time, not when clicking save
        /// </remarks>
        public void saveTab()
        {
            if (this.GetType() == typeof(BlazeFunctionTabPage))
            {
                BlazeFunctionTabPage tab = (BlazeFunctionTabPage)this;
                tab.saveBody();
            }
            else if (this.GetType() == typeof(BlazeRuleTabPage))
            {
                BlazeRuleTabPage tab = (BlazeRuleTabPage)this;
                tab.saveBody();
            }
        }


    }

    public class BlazeFunctionTabPage : BlazeRepoTabPage
    {
        public ScintillaNet.Scintilla codeBox { get; set; }
        public SplitContainer codeSplitter { get; set; }
        public ListView parameterView { get; set; }
        public BlazeFunction tabFunctionFile;
        public string functionBody;

        public BlazeFunctionTabPage(BlazeFunction inBlazeFile, BlazeRepoTabControl parentControl)
        {
            base.tabBlazeFile = inBlazeFile;
            base.Text = inBlazeFile.idename;
            base.idename = inBlazeFile.idename;
            base.hostControl = parentControl;

            tabFunctionFile = (BlazeFunction)inBlazeFile;

            MenuItem[] paramMenuItems = createParamMenu();
            ContextMenu paramListMenu = new ContextMenu();

            paramListMenu.MenuItems.Add("Add Parameter");
            paramListMenu.MenuItems.Add("Remove Parameter");

            codeSplitter = new SplitContainer();
            codeSplitter.Orientation = Orientation.Horizontal;
            codeSplitter.Parent = this;
            codeSplitter.SplitterWidth = 5;
            codeSplitter.SplitterDistance = this.Height - 100;
            codeSplitter.Dock = DockStyle.Fill;

            codeBox = new ScintillaNet.Scintilla();
            codeBox.Margins[0].Width = 30;
            codeBox.Margins.Margin2.Width = 16;
            codeBox.MatchBraces = true;

            codeBox.Folding.MarkerScheme = ScintillaNet.FoldMarkerScheme.BoxPlusMinus;
            codeBox.Font = new System.Drawing.Font("DejaVu Sans Mono", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            codeBox.Styles.BraceBad.FontName = "DejaVu Sans Mono";
            codeBox.Styles.BraceLight.FontName = "DejaVu Sans Mono";
            codeBox.Styles.ControlChar.FontName = "DejaVu Sans Mono";
            codeBox.Styles.Default.FontName = "DejaVu Sans Mono";
            codeBox.Styles.IndentGuide.FontName = "DejaVu Sans Mono";
            codeBox.Styles.LastPredefined.FontName = "DejaVu Sans Mono";
            codeBox.Styles.LineNumber.FontName = "DejaVu Sans Mono";
            codeBox.Styles.Max.FontName = "DejaVu Sans Mono";
            codeBox.Folding.IsEnabled = true;
            codeBox.ConfigurationManager.CustomLocation = "ScintillaNET.xml";
            codeBox.ConfigurationManager.Language = "cs";
            codeBox.AutoComplete.List.AddRange(hostControl.globalVarList);
            foreach (string var in hostControl.globalVarList)
            {
                codeBox.Lexing.Keywords[1] += " " + var;
            }

            parameterView = new ListView();
            parameterView.View = View.Details;
            parameterView.FullRowSelect = true;
            parameterView.ContextMenu = paramListMenu;
            parameterView.Columns.Add("Name", 150);
            parameterView.Columns.Add("Type", 130);
            foreach (BlazeVariable param in inBlazeFile.functionParameters)
            {
                ListViewItem paramItem = new ListViewItem(param.name);
                paramItem.SubItems.Add(param.type);
                parameterView.Items.Add(paramItem);
                codeBox.AutoComplete.List.Add(param.name);
                codeBox.Lexing.Keywords[1] += " " + param.name;
            }
            codeBox.AutoComplete.List.Sort();

            codeSplitter.Panel1.Controls.Add(parameterView);
            codeSplitter.Panel2.Controls.Add(codeBox);
            parameterView.Dock = DockStyle.Fill;
            codeBox.Dock = DockStyle.Fill;
            codeBox.Text = inBlazeFile.functionBody;

            // Event handlers
            codeBox.CharAdded += new EventHandler<ScintillaNet.CharAddedEventArgs>(codeBox_CharAdded);
            codeBox.TextChanged += new EventHandler<EventArgs>(codeBox_TextChanged);
        }

        void codeBox_TextChanged(object sender, EventArgs e)
        {
            if (base.hasChanged == false)
            {
                base.hasChanged = true;
            }
        }

        void codeBox_CharAdded(object sender, ScintillaNet.CharAddedEventArgs e)
        {
            // UNDONE this will cause autocomplete to show up when pressing "."

            if (e.Ch == '.')
            {
                string varBeforePeriod = this.codeBox.GetWordFromPosition(this.codeBox.CurrentPos - 1);
                if (varBeforePeriod != null && varBeforePeriod != "")
                {
                    if (hostControl.globalVarTypeDictionary.ContainsKey(varBeforePeriod))
                    {
                        codeBox.CallTip.Show("var is of type: " + hostControl.globalVarTypeDictionary[varBeforePeriod]);
                    }
                    // list<string> s = new list<string>();
                }
            }
        }



        public void saveBody()
        {
            System.Diagnostics.Debug.WriteLine("BlazeRepoTabControl: saving function " + tabFunctionFile.blazeFileInfo.FullName);
            tabFunctionFile.functionBody = codeBox.Text;
            tabFunctionFile.saveBody();
            this.hasChanged = false;
        }

        public void addParameter()
        {

        }

        public void removeParameter()
        {

        }

        public MenuItem[] createParamMenu()
        {
            MenuItem[] theList = new MenuItem[10];
            theList[0] = new MenuItem("Add Parameter");
            theList[1] = new MenuItem("Remove Parameter");

            return theList;
        }

        public void setCodeBoxFont()
        {

        }
    }

    public class BlazeRulesetTabPage : BlazeRepoTabPage
    {
        public BlazeRuleset tabRulesetFile;
        public SplitContainer codeSplitter { get; set; }
        public ListView parameterView { get; set; }
        public ListView ruleListView { get; set; }

        public BlazeRulesetTabPage(BlazeFile inBlazeFile)
        {
            base.Text = inBlazeFile.idename;

            tabRulesetFile = (BlazeRuleset)inBlazeFile;

            codeSplitter = new SplitContainer();
            codeSplitter.Orientation = Orientation.Horizontal;
            codeSplitter.Parent = this;
            codeSplitter.SplitterWidth = 5;
            codeSplitter.SplitterDistance = this.Height - 100;
            codeSplitter.Dock = DockStyle.Fill;

            ruleListView = new ListView();
            ruleListView.View = View.Details;
            ruleListView.FullRowSelect = true;
            ruleListView.Columns.Add("Rule", 300);
            // TODO show comments
            foreach (BlazeRule rule in tabRulesetFile.ruleList)
            {
                ListViewItem ruleItem = new ListViewItem(rule.idename);
                ruleListView.Items.Add(ruleItem);
            }

            parameterView = new ListView();
            parameterView.View = View.Details;
            parameterView.FullRowSelect = true;
            parameterView.Columns.Add("Name", 150);
            parameterView.Columns.Add("Type", 130);
            foreach (BlazeVariable param in tabRulesetFile.rulesetParameters)
            {
                ListViewItem paramItem = new ListViewItem(param.name);
                paramItem.SubItems.Add(param.type);
                parameterView.Items.Add(paramItem);
            }

            codeSplitter.Panel1.Controls.Add(parameterView);
            codeSplitter.Panel2.Controls.Add(ruleListView);
            parameterView.Dock = DockStyle.Fill;
            ruleListView.Dock = DockStyle.Fill;
        }
    }

    public class BlazeRuleTabPage : BlazeRepoTabPage
    {
        public ScintillaNet.Scintilla codeBox { get; set; }
        public SplitContainer codeSplitter { get; set; }
        public ListView parameterView { get; set; }
        public BlazeRule tabRuleFile;
        public BlazeRuleset parentRuleset;
        public string ruleBody;

        public BlazeRuleTabPage(BlazeRule inBlazeFile, BlazeRepoTabControl parentControl)
        {
            base.Text = inBlazeFile.idename;
            base.idename = inBlazeFile.idename;
            base.tabBlazeFile = inBlazeFile.parentRuleset;
            base.hostControl = parentControl;
            parentRuleset = inBlazeFile.parentRuleset;

            tabRuleFile = inBlazeFile;

            codeSplitter = new SplitContainer();
            codeSplitter.Orientation = Orientation.Horizontal;
            codeSplitter.Parent = this;
            codeSplitter.SplitterWidth = 5;
            codeSplitter.SplitterDistance = this.Height - 100;
            codeSplitter.Dock = DockStyle.Fill;

            codeBox = new ScintillaNet.Scintilla();
            codeBox.Margins[0].Width = 30;
            codeBox.Margins.Margin2.Width = 16;
            codeBox.MatchBraces = true;
            codeBox.ConfigurationManager.CustomLocation = "ScintillaNET.xml";
            codeBox.ConfigurationManager.Language = "cs";
            codeBox.CharAdded += new EventHandler<ScintillaNet.CharAddedEventArgs>(codeBox_CharAdded);
            codeBox.TextChanged += new EventHandler<EventArgs>(codeBox_TextChanged);
            codeBox.AutoComplete.List.AddRange(hostControl.globalVarList);
            foreach (string var in hostControl.globalVarList)
            {
                codeBox.Lexing.Keywords[1] += " " + var;
            }

            parameterView = new ListView();
            parameterView.View = View.Details;
            parameterView.FullRowSelect = true;
            parameterView.Columns.Add("Name", 150);
            parameterView.Columns.Add("Type", 130);
            foreach (BlazeVariable param in inBlazeFile.ruleParameters)
            {
                ListViewItem paramItem = new ListViewItem(param.name);
                paramItem.SubItems.Add(param.type);
                parameterView.Items.Add(paramItem);
                codeBox.Lexing.Keywords[1] += " " + param.name;
            }

            codeSplitter.Panel1.Controls.Add(parameterView);
            codeSplitter.Panel2.Controls.Add(codeBox);
            parameterView.Dock = DockStyle.Fill;
            codeBox.Dock = DockStyle.Fill;
            codeBox.Text = inBlazeFile.ruleBody;
        }

        void codeBox_TextChanged(object sender, EventArgs e)
        {
            if (base.hasChanged == false)
            {
                this.hasChanged = true;
            }
            // throw new NotImplementedException();
        }

        void codeBox_CharAdded(object sender, ScintillaNet.CharAddedEventArgs e)
        {
            // UNDONE this will cause autocomplete to show up when pressing "."

            if (e.Ch == '.')
            {
                string varBeforePeriod = this.codeBox.GetWordFromPosition(this.codeBox.CurrentPos - 1);
                if (varBeforePeriod != null && varBeforePeriod != "")
                {
                    if (hostControl.globalVarTypeDictionary.ContainsKey(varBeforePeriod))
                    {
                        codeBox.CallTip.Show("var is of type: " + hostControl.globalVarTypeDictionary[varBeforePeriod]);
                    }
                    // list<string> s = new list<string>();
                }
            }
        }

        public void saveBody()
        {
            System.Diagnostics.Debug.WriteLine("BlazeRepoTabControl: Saving rule body " + tabRuleFile.idename);
            tabRuleFile.ruleBody = codeBox.Text;
            parentRuleset.saveRuleBody(tabRuleFile);
            base.hasChanged = false;
        }
    }

    public class BlazeProjectListTabPage : BlazeRepoTabPage
    {
        public BlazeProjectListTabPage(BlazeFile inBlazeFile)
        {

        }
    }
}
