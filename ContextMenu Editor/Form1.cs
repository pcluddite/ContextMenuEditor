using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Diagnostics;
using Microsoft.Win32;
using System.Security;
using System.IO;

namespace CEdit {
    public partial class Form1 : Form {

        private Dictionary<string, WinContextMenu> _dict;

        public Form1() {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e) {
            init();
        }

        private void button3_Click(object sender, EventArgs e) {
            refresh();
        }

        private void refresh() {
            foreach (var menu in _dict) {
                menu.Value.Dispose();
            }
            _dict.Clear();
            listView1.Items.Clear();
            init();
        }

        private void init() {
            _dict = new Dictionary<string, WinContextMenu>() {
                { "*", new WinContextMenu("*") },
                { "File", new WinContextMenu("File") },
                { "Folder", new WinContextMenu("Folder") },
                { "Directory", new WinContextMenu("Directory") }
            };

            foreach (var menu in _dict) {
                foreach (var entry in menu.Value.Entries.Values) {
                    listView1.Items.Add(entry.ToListViewItem(
                        menu.Value.FullPath + "\\" + entry.RelativePath
                        ));
                }
            }
        }

        private void deleteButton_Click(object sender, EventArgs e) {
            if (listView1.SelectedItems.Count != 1) {
                return;
            }

            if (MessageBox.Show(this, 
                "This action cannot be undone unless a backup has been made.\nAre you sure you want to delete this key?",
                Text, MessageBoxButtons.YesNo, MessageBoxIcon.Warning,
                MessageBoxDefaultButton.Button2) == DialogResult.No) {
                return;
            }
            if (Reg(true, "DELETE", listView1.SelectedItems[0].SubItems[2].Text, "/f") == 0) {
                refresh();
                MessageBox.Show(this, "Key was successfully deleted", Text, MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else {
                MessageBox.Show(this, "Could not delete registry key", Text, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private int Reg(params string[] args) {
            return Reg(false, args);
        }

        private int Reg(bool admin, params string[] args) {
            StringBuilder sb = new StringBuilder();
            foreach (string arg in args) {
                if (arg.Contains(' ')) {
                    sb.AppendFormat("\"{0}\"", arg);
                }
                else {
                    sb.Append(arg);
                }
                sb.Append(' ');
            }
            int exit;
            try {
                using (Process reg = new Process()) {
                    reg.StartInfo.FileName = "REG.EXE";
                    reg.StartInfo.Arguments = sb.ToString().Trim();
                    if (admin) {
                        reg.StartInfo.Verb = "runas";
                    }
                    reg.Start();
                    reg.WaitForExit();
                    exit = reg.ExitCode;
                }
                return exit;
            }
            catch (InvalidOperationException) {
                MessageBox.Show(this, "Could not run REG.EXE", Text, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            catch (IOException) {
                MessageBox.Show(this, "Could not save to the specified location", Text, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            return -1;
        }

        private void button1_Click(object sender, EventArgs e) {
            if (listView1.SelectedItems.Count != 1) {
                return;
            }
            if (exportDialog.ShowDialog() == DialogResult.OK) {

                int exit = Reg("EXPORT",
                           listView1.SelectedItems[0].SubItems[2].Text,
                           exportDialog.FileName, "/y");
                if (exit == 0) {
                    MessageBox.Show(this, "Registry key exported successfully", Text, MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                else {
                    MessageBox.Show(this, "Could not export registry key.\n(REG.EXE ended with exit code " + exit + ")", Text, MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }
    }
}
