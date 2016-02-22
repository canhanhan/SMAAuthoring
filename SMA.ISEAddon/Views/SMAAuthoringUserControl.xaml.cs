using Microsoft.PowerShell.Host.ISE;
using SMA.ISEAddon.UI;
using SMA.ISEAddon.ViewModels;
using System;
using System.ComponentModel;
using System.Windows.Controls;

namespace SMA.ISEAddon.Views
{
    /// <summary>
    /// Interaction logic for UserControl1.xaml
    /// </summary>
    public partial class SMAAuthoringUserControl : UserControl, IAddOnToolHostObject, IAuthoringView
    {
        private readonly SMAAuthoringViewModel viewModel;
        private string activeFile;
        private ObjectModelRoot hostObject;
        private IViewAdaptor ise;

        public event PropertyChangedEventHandler PropertyChanged;

        public ObjectModelRoot HostObject
        {
            get { throw new InvalidOperationException("Do not use this method!"); }
            set { 
                hostObject = value;
                if (ise != null) { ise.HostObject = hostObject; } 
            }
        }

        public IViewAdaptor IseAdaptor
        {
            get { return ise; }
            set { 
                if (ise != null)
                {
                    ise.ActiveFileChanged -= ise_ActiveFileChanged;
                    ise.FileClosed -= ise_FileClosed;
                }

                ise = value;
                if (hostObject != null) { ise.HostObject = hostObject; }

                ise.ActiveFileChanged += ise_ActiveFileChanged;
                ise.FileClosed += ise_FileClosed;
            }
        }

        public string ActiveFile
        {
            get { return activeFile; }
            set { activeFile = value; InvokePropertyChanged("ActiveFile"); }
        }

        public SMAAuthoringUserControl()
        {
            InitializeComponent();

            IseAdaptor = new PowershellISE40Adaptor();
            viewModel = new SMAAuthoringViewModel(this);
            DataContext = viewModel;
        }

        public void Open(string path)
        {
            ise.Open(path);
        }

        public void Save()
        {
            ise.Save();
        }

        protected void InvokePropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
                PropertyChanged.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private void ise_FileClosed(object sender, ViewPathEventArgs e)
        {
            viewModel.RemoveFile(e.Path);
        }

        private void ise_ActiveFileChanged(object sender, ViewPathEventArgs e)
        {
            if (ActiveFile != e.Path)
                ActiveFile = e.Path;
        }

        private void RunbooksCombobox_DropDownOpened(object sender, EventArgs e)
        {
            viewModel.RefreshRunbooks();
        }
    }
}
