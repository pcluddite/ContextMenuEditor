using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Win32;
using System.Diagnostics;
using System.IO;

namespace CEdit {
    public interface IContextLocation : IDisposable{
        IContextLocation Background { get; }
        RegistryKey ParentKey { get; }
        RegistryKey Shell { get; }
        RegistryKey ShellEx { get; }

        string FullPath { get; }
    }

    public class WinContextMenu : IContextLocation {

        private WinContextMenu background;

        private RegistryKey parent,
                            shell,
                            shellex;

        public Dictionary<string, Entry> Entries {
            get;
            private set;
        }

        public WinContextMenu Background {
            get { return background; }
        }

        public RegistryKey ParentKey {
            get { return parent; }
        }

        public RegistryKey Shell {
            get { return shell; }
        }

        public RegistryKey ShellEx {
            get { return shellex; }
        }

        public WinContextMenu(string menuKey) {
            Entries = new Dictionary<string, Entry>();
            parent = Registry.ClassesRoot.OpenSubKey(menuKey);
            fullPath = "HKCR\\" + menuKey;
            shell = parent.OpenSubKey("shell");
            if (shell != null) {
                if (shell.GetSubKeyNames().Contains("ContextMenuHandlers")) {
                    shell.Close();
                    shell = parent.OpenSubKey("shell\\ContextMenuHandlers");
                    init(parent, "shell\\ContextMenuHandlers");
                }
                else {
                    shell.Close();
                    shell = null;
                }
            }
            shellex = parent.OpenSubKey("shellex");
            if (shellex != null) {
                if (shellex.GetSubKeyNames().Contains("ContextMenuHandlers")) {
                    shellex.Close();
                    shellex = parent.OpenSubKey("shellex\\ContextMenuHandlers");
                    init(parent, "shellex\\ContextMenuHandlers");
                }
                else {
                    shellex.Close();
                    shellex = null;
                }
            }
            background = BackgroundMenu.CreateFromParent(menuKey, this);
        }

        private void init(RegistryKey _base, string path) {
            using (RegistryKey _menu = _base.OpenSubKey(path)) {
                foreach (string name in _menu.GetSubKeyNames()) {
                    Entry e = Entry.LoadFromKey(_base, path + "\\" + name);
                    Entries.Add(e.GetName(), e);
                }
            }
        }

        public void Dispose() {
            parent.Close();
            if (shell != null) {
                shell.Close();
            }
            if (shellex != null) {
                shellex.Close();
            }
            if (background != null) {
                background.Dispose();
            }
        }

        internal class BackgroundMenu : WinContextMenu {
            
            public static BackgroundMenu CreateFromParent(string parentName, WinContextMenu parent) {
                if (parent.ParentKey.GetSubKeyNames().Contains("background")) {
                    return new BackgroundMenu(parentName);
                }
                else {
                    return null;
                }
            }

            private BackgroundMenu(string parent) :
                base(parent + "\\background") {
            }
        }

        public class Entry : IDisposable {
            private RegistryKey key;

            private Entry(RegistryKey _base, string path) {
                key = _base.OpenSubKey(path);
                RelativePath = path;
            }

            public string RelativePath { get; private set; }

            public object GetValue() {
                return GetValue("");
            }

            public object GetValue(string name) {
                return key.GetValue(name);
            }

            public string GetName() {
                int end = RelativePath.LastIndexOf('\\');
                if (end > 0) {
                    return RelativePath.Substring(end + 1);
                }
                else {
                    return RelativePath;
                }
            }

            public RegistryKey Key {
                get {
                    return key;
                }
            }

            public void Close() {
                key.Close();
            }

            public void Dispose() {
                Close();
            }

            public static Entry LoadFromKey(RegistryKey _base, string path) {
                return new Entry(_base, path);
            }

            public System.Windows.Forms.ListViewItem ToListViewItem() {
                return ToListViewItem(RelativePath);
            }

            public System.Windows.Forms.ListViewItem ToListViewItem(string path) {
                System.Windows.Forms.ListViewItem item = new System.Windows.Forms.ListViewItem();
                item.Text = GetName();
                object value = GetValue();
                item.SubItems.Add(value == null ? "" : value.ToString());
                item.SubItems.Add(path);
                return item;
            }
        }

        IContextLocation IContextLocation.Background {
            get { return background; }
        }


        private string fullPath;
        public string FullPath {
            get {
                return fullPath;
            }
        }
    }
}
