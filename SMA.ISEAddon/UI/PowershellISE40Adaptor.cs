using Microsoft.PowerShell.Host.ISE;
using System;
using System.Collections;
using System.Collections.Specialized;
using System.ComponentModel;
using System.IO;
using System.Linq;

namespace SMA.ISEAddon.UI
{
    class PowershellISE40Adaptor: IViewAdaptor
    {
        private ObjectModelRoot hostObject;

        public event EventHandler<ViewPathEventArgs> FileClosed;
        public event EventHandler<ViewPathEventArgs> ActiveFileChanged;

        public object HostObject
        {
            get { return hostObject; }
            set
            {
                if (hostObject != null)
                {
                    throw new InvalidOperationException("Host object has been already attached.");
                }

                hostObject = (ObjectModelRoot)value;
                hostObject.PowerShellTabs.CollectionChanged += PowerShellTabs_CollectionChanged;
                StartTrackingNewTabs(hostObject.PowerShellTabs);
            }
        }

        public void Open(string path)
        {
            ISEFile file = null;
            foreach (var tab in hostObject.PowerShellTabs)
            {
                file = tab.Files.FirstOrDefault(x => string.Equals(x.FullPath, path, StringComparison.InvariantCultureIgnoreCase));
                if (file != null)
                {
                    hostObject.PowerShellTabs.SetSelectedPowerShellTab(tab);
                    tab.Files.SetSelectedFile(file);

                    return;
                }
            }

            hostObject.CurrentPowerShellTab.Files.Add(path);
        }

        public void Save()
        {
            var currentFile = hostObject.CurrentPowerShellTab.Files.SelectedFile;
            if (!currentFile.IsSaved)
                if (currentFile.IsUntitled)
                    currentFile.SaveAs(Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("D") + ".ps1"));
                else
                    currentFile.Save();
        }

        private void PowerShellTabs_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    StartTrackingNewTabs(e.NewItems);
                    break;
                case NotifyCollectionChangedAction.Replace:
                    StopTrackingOldTabs(e.OldItems);
                    StartTrackingNewTabs(e.NewItems);
                    break;
                case NotifyCollectionChangedAction.Remove:
                    StopTrackingOldTabs(e.OldItems);
                    break;
                case NotifyCollectionChangedAction.Reset:
                    StopTrackingOldTabs(e.OldItems);
                    break;
            }
        }

        private void Files_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Replace:
                    StopTrackingOldFiles(e.OldItems);
                    break;
                case NotifyCollectionChangedAction.Remove:
                    StopTrackingOldFiles(e.OldItems);
                    break;
                case NotifyCollectionChangedAction.Reset:
                    StopTrackingOldFiles(e.OldItems);
                    break;
            }
        }

        private void tab_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "LastEditorWithFocus")
            {
                if (ActiveFileChanged != null)
                    ActiveFileChanged.Invoke(this, new ViewPathEventArgs(hostObject.CurrentPowerShellTab.Files.SelectedFile.FullPath));
            }
        }

        private void StopTrackingOldTabs(IList e)
        {
            foreach (PowerShellTab tab in e)
            {
                tab.PropertyChanged -= tab_PropertyChanged;
                tab.Files.CollectionChanged -= Files_CollectionChanged;
            }
        }

        private void StartTrackingNewTabs(IList e)
        {
            foreach (PowerShellTab tab in e)
            {
                tab.PropertyChanged += tab_PropertyChanged;
                tab.Files.CollectionChanged += Files_CollectionChanged;
            }
        }

        private void StopTrackingOldFiles(IList e)
        {
            foreach (ISEFile file in e)
            {
                if (FileClosed != null)
                    FileClosed.Invoke(this, new ViewPathEventArgs(file.FullPath));
            }
        }
    }
}
