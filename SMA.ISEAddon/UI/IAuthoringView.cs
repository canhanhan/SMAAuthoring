using System.ComponentModel;

namespace SMA.ISEAddon.UI
{
    public interface IAuthoringView : INotifyPropertyChanged
    {
        void Save();
        void Open(string path);
        string ActiveFile { get; }
    }

}
