using System;
using System.ComponentModel;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Text;
using System.Windows.Forms;
using System.Xml;

namespace jwh.blaze.application
{
	
    public class BlazeRepository
    {
        private string m_repoPath;
        public List<BlazeClient> clients = new List<BlazeClient>();

        public BlazeRepository(string path, bool loadAll)
        {
            string substringDirectory;
            string[] directoryArray = Directory.GetDirectories(path);
            this.m_repoPath = path;

            try
            {
                if (directoryArray.Length != 0)
                {
                    foreach (string directory in directoryArray)
                    {
                        DirectoryInfo clientDir = new DirectoryInfo(directory);

                        substringDirectory = directory.Substring(
                        directory.LastIndexOf('\\') + 1,
                        directory.Length - directory.LastIndexOf('\\') - 1);

                        if (substringDirectory != ".svn")
                        {
                            BlazeClient currentClient = new BlazeClient(substringDirectory, clientDir);
                            clients.Add(currentClient);
                            if (loadAll == true)
                            {
                                currentClient.populateClient(null);
                            }
                        }
                    }
                }
            }
            catch (UnauthorizedAccessException)
            {
                //parentNode.Nodes.Add("Access denied");
            } // end catch
        }

        public string getRepoPath()
        {
            return m_repoPath;
        }

        public BlazeClient getClient(string clientName)
        {
            BlazeClient toReturn = null;
            foreach (BlazeClient c in clients)
            {
                if (c.getClientName() == clientName)
                {
                    toReturn = c;
                }
            }

            return toReturn;
        }
    }

    public class BlazeClient
    {
        private string m_clientName;
        private DirectoryInfo m_clientDir;
        public DirectoryInfo Service;
        public DirectoryInfo Techlib;
        public DirectoryInfo Testing;
        public Dictionary<string, string> globalVarDictionary= new Dictionary<string, string>();
        public List<BlazeRulebase> rulebases = new List<BlazeRulebase>();
        public List<BlazeVariable> globalVarList = new List<BlazeVariable>();
        public List<String> globalVarStrings = new List<string>();

        public BlazeClient(string clientName, DirectoryInfo clientDir)
        {
            this.m_clientName = clientName;
            this.m_clientDir = clientDir;
            loadTopDirs();
        }

        private void loadTopDirs()
        {
            DirectoryInfo[] topDirArray = this.m_clientDir.GetDirectories();
            string dirName;
            foreach (DirectoryInfo topDir in topDirArray)
            {
                dirName = topDir.Name.ToLower();
                switch (dirName)
                {
                    case "techlib":
                        Techlib = topDir;
                        break;
                    case "service":
                        Service = topDir;
                        break;
                    case "testing":
                        Testing = topDir;
                        break;
                }
            }
        }

        public void populateClient(ToolStripProgressBar progressBar)
        {
            string substringDirectory;
            string dirName;
            DirectoryInfo[] techLibArray = Techlib.GetDirectories();
            // if this has been created and is being refreshed, we need to clear everything out
            rulebases.Clear();
            globalVarStrings.Clear();
            globalVarList.Clear();
            globalVarDictionary.Clear();

            try
            {
                if (techLibArray.Length != 0)
                {
                    foreach (DirectoryInfo rbDir in techLibArray)
                    {
                        if (progressBar != null)
                        {
                            progressBar.Increment(5);
                        }
                        Console.WriteLine("Looking at directory: " + rbDir);
                        dirName = rbDir.Name;
                        substringDirectory = dirName.Substring(
                        dirName.LastIndexOf('\\') + 1,
                        dirName.Length - dirName.LastIndexOf('\\') - 1);

                        if (substringDirectory != ".svn")
                        {
                            BlazeRulebase rb = new BlazeRulebase(rbDir, this);
                            rulebases.Add(rb);
                            rb.populateRulebase();
                            globalVarList.AddRange(rb.rbVariableList);
                        }
                    }
                    // we now have all rbs populated, and all vars in the global var list. make dictionary
                    foreach (BlazeVariable v in globalVarList)
                    {
                        if (v.type != null)
                        {
                            globalVarDictionary.Add(v.name, v.type);
                            globalVarStrings = new List<string>(globalVarDictionary.Keys);
                            globalVarStrings.Sort();
                        }

                    }
                }
            }
            catch (UnauthorizedAccessException)
            {
                //parentNode.Nodes.Add("Access denied");
            } // end catch

        }

        public string getClientName()
        {
            return m_clientName;
        }

        public DirectoryInfo getClientDirectory()
        {
            return m_clientDir;
        }
    }

    public class BlazeRulebase
    {
        public const string _FUNCTION_FAMILY_STR = "Function Template";
        public const string _RULESET_FAMILY_STR = "Ruleset Template";
        public const string _PROJECT_ITEMS_FAMILY_STR = "Project Items Template";
        public string templatens = "http://www.blazesoft.com/template";
        public List<String> globalStringList = new List<string>();
        private string m_rulebaseName;
        private DirectoryInfo m_rulebaseDirectory;
        private XmlDocument xmlBlaze = new XmlDocument();
        public BlazeProjectItems rbProjectItems;
        public BlazeClient parentClient;
        public List<BlazeFunction> rbFunctionList = new List<BlazeFunction>();
        public List<BlazeFile> rbFileList = new List<BlazeFile>();
        public List<BlazeRuleset> rbRulesetList = new List<BlazeRuleset>();
        public List<BlazeVariable> rbVariableList = new List<BlazeVariable>();
        public List<string> rbVariableStringList = new List<string>();

        public BlazeRulebase(DirectoryInfo rbDir, BlazeClient inClient)
        {
            m_rulebaseDirectory = rbDir;
            m_rulebaseName = rbDir.Name;
            parentClient = inClient;
        }

        public string getRBName()
        {
            return m_rulebaseName;
        }

        public void populateRulebase()
        {
            FileInfo[] fileList = m_rulebaseDirectory.GetFiles();
            foreach (FileInfo file in fileList)
            {
                // avoid innovator_attbs, copyarea.db, and .bak files
                if (!file.Name.Contains(".innovator_attbs") && !file.Name.Contains(".db") && !file.Name.Contains(".bak"))
                {
                    try
                    {
                        XmlDocument bfile = new XmlDocument();
                        bfile.Load( file.FullName );
                        switch (getBlazeFamily(bfile))
                        {
                            case _FUNCTION_FAMILY_STR:
                                BlazeFunction bfunc = new BlazeFunction(file);
                                bfunc.blazeFamily = _FUNCTION_FAMILY_STR;
                                rbFunctionList.Add(bfunc);
                                rbFileList.Add(bfunc);
                                break;
                            case _RULESET_FAMILY_STR:
                                BlazeRuleset bruleset = new BlazeRuleset(file);
                                bruleset.blazeFamily = _RULESET_FAMILY_STR;
                                rbRulesetList.Add(bruleset);
                                rbFileList.Add(bruleset);
                                break;
                            case _PROJECT_ITEMS_FAMILY_STR:
                                BlazeProjectItems bproj = new BlazeProjectItems(file, this);
                                bproj.blazeFamily = _PROJECT_ITEMS_FAMILY_STR;
                                rbProjectItems = bproj;
                                rbFileList.Add(bproj);
                                rbVariableList = bproj.variableList;
                                // rbVariableStringList.AddRange(bproj.varStringList);
                                break;
                            default:
                                // TODO throw some sort of error here
                                // in theory this will grab anything newly implemented that can be added later
                                BlazeFile badFile = new BlazeFile(file);
                                badFile.blazeFileInfo = file;
                                rbFileList.Add(badFile);
                                break;
                        }
                    }
                    catch (XmlException e)
                    {
                        MessageBox.Show("Error loading XML data from " + file.FullName + " Exception text: " + e.ToString());
                    }
                    catch (Exception e)
                    {
                        MessageBox.Show("Unknown exception occured loading " + file.FullName + " Exception text: " + e.ToString());
                    }
                }
            }
        }

        public string getBlazeFamily(XmlDocument bfile)
        {
            string familyName = "";

            XmlNodeList family_name_node = bfile.GetElementsByTagName( "family", templatens );
            familyName = family_name_node[0].InnerText;

            return familyName;
        }
    }

    public class BlazeVariable
    {
        public string comment = "";
        public string name = "";
        public string type = "";
        public string initializer = "";

        public BlazeVariable()
        {

        }

    }

    #region BlazeFileTypes

    public class BlazeFile
    {
        public string templatens = "http://www.blazesoft.com/template";
        public string srlns = "http://www.blazesoft.com/srl";
        public XmlDocument xmlBlaze = new XmlDocument();
        public FileInfo blazeFileInfo;
        public string idename;
        public string blazeFamily;
        public string comment;

        public BlazeFile()
        {
        }

        public BlazeFile(FileInfo fi)
        {
            blazeFileInfo = fi;
        }

        public void loadMembers()
        {
            xmlBlaze.PreserveWhitespace = true;
            xmlBlaze.Load(blazeFileInfo.FullName);
            loadIdename();
        }

        private void loadIdename()
        {
            XmlNodeList ide_name_list = xmlBlaze.GetElementsByTagName("ide-name", templatens);
            idename = ide_name_list[0].InnerText;
        }

        public void saveOut()
        {
            System.Diagnostics.Debug.WriteLine("Attempting to save " + blazeFileInfo.FullName);
            try
            {
                XmlTextWriter tw = new XmlTextWriter(blazeFileInfo.FullName, null);

                tw.QuoteChar = '\'';
                xmlBlaze.Save(tw);
                tw.Close();
            }
            catch(Exception e)
            {
                MessageBox.Show("The following exception occured while saving " + blazeFileInfo.Name + "\n" + e.ToString());
            }
        }
    }

    public class BlazeFunction : BlazeFile
    {
        public List<BlazeVariable> functionParameters = new List<BlazeVariable>();
        private XmlDocument xdoc = new XmlDocument();
        public string functionBody;

        public BlazeFunction(FileInfo fi)
        {
            base.blazeFileInfo = fi;
            
            base.loadMembers();
            this.loadBody();
            this.loadParameters();
        }

        private void loadParameters()
        {
            XmlNodeList parameter_list = xmlBlaze.GetElementsByTagName("parameter", srlns);

            foreach (XmlElement parameter in parameter_list)
            {
                BlazeVariable v = new BlazeVariable();
                try {
                v.name = parameter["name", srlns].InnerText;
                if (parameter["type", srlns] != null)
                {
                    v.type = parameter["type", srlns].InnerText;
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("Error loading type for parameter " + v.name);                	
                }
                if (parameter["comment", srlns] != null)
                {
                    v.comment = parameter["comment", srlns].InnerText;
                }
                else
                {
                	System.Diagnostics.Debug.WriteLine("Error loading comment for parameter " + v.name);
                }
                this.functionParameters.Add(v);
                }
                catch (Exception e)
                {
                    MessageBox.Show("Exception while loading parameter " + parameter.Name + " in " + this.idename + "\n Error: " + e.ToString());
                }
            }
        }

        private void loadBody()
        {
            XmlNodeList funcBody = xmlBlaze.GetElementsByTagName("body", srlns);
            functionBody = funcBody[0].InnerText;
        }

        public void saveBody()
        {
            XmlNodeList funcBody = xmlBlaze.GetElementsByTagName("body", srlns);
            XmlNode bodyElement = funcBody[0];
            XmlCDataSection bodyCData = xmlBlaze.CreateCDataSection(functionBody);
            bodyElement.InnerText = "";
            bodyElement.AppendChild(bodyCData);
            
            // write out
            this.saveOut();
        }
    }

    public class BlazeRuleset : BlazeFile
    {
        public List<BlazeRule> ruleList = new List<BlazeRule>();
        public List<BlazeVariable> rulesetParameters = new List<BlazeVariable>();

        public BlazeRuleset(FileInfo fi)
        {
            base.blazeFileInfo = fi;
            base.loadMembers();
            this.loadParameters();
            this.loadRules();
        }

        private void loadParameters()
        {
            XmlNodeList parameter_list = xmlBlaze.GetElementsByTagName("parameter", srlns);

            foreach (XmlElement parameter in parameter_list)
            {
                BlazeVariable v = new BlazeVariable();
                try {
                v.name = parameter["name", srlns].InnerText;
                if (parameter["type", srlns] != null)
                {
                    v.type = parameter["type", srlns].InnerText;
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("Error loading type for parameter " + v.name);                	
                }
                if (parameter["comment", srlns] != null)
                {
                    v.comment = parameter["comment", srlns].InnerText;
                }
                else
                {
                	System.Diagnostics.Debug.WriteLine("Error loading comment for parameter " + v.name);
                }

                this.rulesetParameters.Add(v);
                }
                catch (Exception e)
                {
                    MessageBox.Show("Exception while loading parameter " + parameter.Name + " in " + this.idename + "\n Error: " + e.ToString());
                }
            }
        }

        private void loadRules()
        {
            XmlNodeList rules = xmlBlaze.GetElementsByTagName("rule", srlns);
            foreach (XmlElement rule in rules)
            {
                try
                {
                    string ruleName, ruleBody, ruleComment;
                    if (rule["name", srlns] != null)
                    {
                        ruleName = rule["name", srlns].InnerText;
                        if (rule["comment", srlns] != null)
                        {
                            ruleComment = rule["comment", srlns].InnerText;
                        }
                        else
                        {
                            System.Diagnostics.Debug.WriteLine("Error proccessing rule comment for " + ruleName);
                        }
                        if (rule["body", srlns] != null)
                        {
                            // we have a name and body, which is good enough to create the rule and add it
                            ruleBody = rule["body", srlns].InnerText;
                            BlazeRule newRule = new BlazeRule(ruleName, ruleBody, rulesetParameters, rule, this);
                            ruleList.Add(newRule);
                        }
                        else
                        {
                            System.Diagnostics.Debug.WriteLine("Error proccessing rule body for " + ruleName);
                        }

                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine("Error proccessing rule name in " + this.idename);
                    }
                }
                catch (NullReferenceException e)
                {
                    System.Diagnostics.Debug.WriteLine("Null Reference occured while processing rules for " + this.idename +
                        "\n\nException text: " + e.ToString());
                }
                catch (Exception e)
                {
                    System.Diagnostics.Debug.WriteLine("Unknown exception occured while proccessing rules for " + this.idename +
                        "\n\nException text: " + e.ToString());
                }
             }
        }

        public void saveRuleBody(BlazeRule ruleToSave)
        {
            XmlElement ruleBody = ruleToSave.ruleXml;
            ruleBody["body", srlns].InnerXml = "";
            XmlCDataSection bodyCData = xmlBlaze.CreateCDataSection(ruleToSave.ruleBody);
            ruleBody["body", srlns].AppendChild(bodyCData);

            this.saveOut();
        }
    }

    public class BlazeRule
    {
        public string ruleBody;
        public string idename;
        public XmlElement ruleXml;
        public BlazeRuleset parentRuleset;
        public List<BlazeVariable> ruleParameters = new List<BlazeVariable>();

        public BlazeRule(string inName, string inBody, List<BlazeVariable> inParameterList, XmlElement inElement, BlazeRuleset inParentRuleset)
        {
            idename = inName;
            ruleBody = inBody;
            ruleParameters = inParameterList;
            ruleXml = inElement;
            parentRuleset = inParentRuleset;
        }
    }

    public class BlazeProjectItems : BlazeFile
    {
        public List<BlazeVariable> variableList = new List<BlazeVariable>();
        public List<string> varStringList = new List<string>();

        public BlazeProjectItems(FileInfo fi, BlazeRulebase inParent)
        {
            base.blazeFileInfo = fi;
            base.loadMembers();
            this.loadVariableList();
        }

        private void loadVariableList()
        {
            foreach (XmlElement v in xmlBlaze.GetElementsByTagName("variable", srlns))
            {
                BlazeVariable newVar = new BlazeVariable();
                if (v["type", srlns] != null)
                {
                    newVar.type = v["type", srlns].InnerText;
                }
                else if (v["array", srlns] != null)
                {
                    XmlElement temp = v["array", srlns];
                    newVar.type = temp["type", srlns].InnerText;
                }
                if (v["name", srlns] != null)
                {
                    newVar.name = v["name", srlns].InnerText;
                }
                if (v["initializer", srlns] != null)
                {
                    newVar.initializer = v["initializer", srlns].InnerText;
                }
                variableList.Add(newVar);
                varStringList.Add(newVar.name);
            }
        }
    }

    #endregion
}
