using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace jwh.blaze.application
{
    public partial class BlazeEditorSettingsWindow : Form
    {
        // UNDONE Not yet implemented.

        /// <summary>
        /// Window to choose settings for editor
        /// </summary>
        /// <remarks>
        /// On open should pull current settings from settings xml
        /// Either set as modal and change on close, or give "ok"
        /// & "apply" buttons to apply changes. Or maybe just say
        /// take effect on reset
        /// </remarks>
        public BlazeEditorSettingsWindow()
        {
            InitializeComponent();
        }

        private void BlazeEditorSettingsWindow_Load(object sender, EventArgs e)
        {

        }

        // if using "ok" & "apply" buttons this event will say "are you sure you want to close
        // without changes
        private void BlazeEditorSettingsWindow_FormClosing(object sender, FormClosingEventArgs e)
        {
            
        }

        /// <summary>
        /// Settings window closed
        /// </summary>
        /// <remarks>
        /// If using modal settings with save changes on close, save here.
        /// </remarks>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BlazeEditorSettingsWindow_FormClosed(object sender, FormClosedEventArgs e)
        {

        }
    }
}
