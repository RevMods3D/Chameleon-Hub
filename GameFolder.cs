using System.Collections.ObjectModel;
using static Chameleon_Hub.EditorWindow;

namespace Chameleon_Hub.Core
{
    public class GameFolder : ObservableObject
    {
        private string _name;
        public string Name
        {
            get => _name;
            set => SetProperty(ref _name, value);
        }

        public ObservableCollection<GameFolder> SubFolders { get; set; } = new();
        public ObservableCollection<BndlFile> BndlFiles { get; set; } = new();

        public GameFolder(string name)
        {
            Name = name;
        }
    }
}
