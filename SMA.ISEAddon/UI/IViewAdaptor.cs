using System;

namespace SMA.ISEAddon.UI
{
    public interface IViewAdaptor
    {
        event EventHandler<ViewPathEventArgs> ActiveFileChanged;
        event EventHandler<ViewPathEventArgs> FileClosed;
        object HostObject { get; set; }
        void Open(string path);
        void Save();
    }
}
