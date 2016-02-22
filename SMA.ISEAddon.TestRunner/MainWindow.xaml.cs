using SMA.ISEAddon.UI;
using System;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace SMA.ISEAddon.TestRunner
{
    class TabFile
    {
        public string Path { get; set; }
        public string Content { get; set; }
    }
    class TestISEAdaptor : IViewAdaptor
    {
        private MainWindow window;
        
        public event EventHandler<ViewPathEventArgs> ActiveFileChanged;
        public event EventHandler<ViewPathEventArgs> FileClosed;        

        public object HostObject
        {
            get
            {
                return window;
            }
            set
            {
                this.window = (MainWindow)value;
                this.window.tabs.SelectionChanged += tabs_SelectionChanged;
            }
        }

        public TestISEAdaptor()
        {
        }

        public void Open(string path)
        {
            var tab = window.tabs.Items.Cast<TabFile>().FirstOrDefault(x => x.Path == path);
            if (tab == null)
            {
                window.tabs.Items.Add(new TabFile { Path = path, Content = File.ReadAllText(path) });
                window.tabs.SelectedIndex = window.tabs.Items.Count - 1;
            }
            else
            {
                window.tabs.SelectedItem = tab;
            }
        }

        public void Save()
        {

        }

        private void tabs_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ActiveFileChanged != null)
                ActiveFileChanged.Invoke(this, new ViewPathEventArgs(((TabFile)window.tabs.SelectedItem).Path));
        }

    }

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            control.IseAdaptor = new TestISEAdaptor();
            control.IseAdaptor.HostObject = this;
        }
    }
}
