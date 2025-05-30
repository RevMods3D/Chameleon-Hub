using System.Collections.ObjectModel;
using static Chameleon_Hub.EditorWindow;

namespace Chameleon_Hub.Core
{
    public class BndlFile : ObservableObject
    {
        private string _name;
        public string Name
        {
            get => _name;
            set => SetProperty(ref _name, value);
        }

        public ObservableCollection<DatFileReference> DatFiles { get; set; } = new();

        public BndlFile(string name)
        {
            Name = name;
        }
    }
}
