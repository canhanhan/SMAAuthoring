using System;

namespace SMA.ISEAddon
{
    public class ViewPathEventArgs : EventArgs
    {
        public string Path { get; set; }
        public ViewPathEventArgs(string path)
        {
            Path = path;
        }
    }

}
