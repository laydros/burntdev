﻿//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:2.0.50727.3623
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace jwh.blaze.application.Properties {
    
    
    [global::System.Runtime.CompilerServices.CompilerGeneratedAttribute()]
    [global::System.CodeDom.Compiler.GeneratedCodeAttribute("Microsoft.VisualStudio.Editors.SettingsDesigner.SettingsSingleFileGenerator", "9.0.0.0")]
    internal sealed partial class Settings : global::System.Configuration.ApplicationSettingsBase {
        
        private static Settings defaultInstance = ((Settings)(global::System.Configuration.ApplicationSettingsBase.Synchronized(new Settings())));
        
        public static Settings Default {
            get {
                return defaultInstance;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("S:/benefitCal_WL/dbsystem/com/fidelity/definedbenefit/benefitcalculator/repositor" +
            "y/client")]
        public string repoPathString {
            get {
                return ((string)(this["repoPathString"]));
            }
            set {
                this["repoPathString"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("11")]
        public int editorFontSize {
            get {
                return ((int)(this["editorFontSize"]));
            }
            set {
                this["editorFontSize"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("S:\\BenefitCal_WL\\dbes\\Channel\\BenefitCal_WL\\scripts\\Blazec.bat")]
        public string compilerPath {
            get {
                return ((string)(this["compilerPath"]));
            }
            set {
                this["compilerPath"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("S:\\BenefitCal_WL\\dbes\\Channel\\BenefitCal_WL\\scripts\\RuleInit.cmd")]
        public string ruleInitPath {
            get {
                return ((string)(this["ruleInitPath"]));
            }
            set {
                this["ruleInitPath"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("S:\\BenefitCal_WL\\dbes\\Channel\\BenefitCal_WL\\scripts\\")]
        public string dbesChannelScriptDir {
            get {
                return ((string)(this["dbesChannelScriptDir"]));
            }
            set {
                this["dbesChannelScriptDir"] = value;
            }
        }
    }
}
