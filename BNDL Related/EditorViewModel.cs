using System.Collections.ObjectModel;
using System.Windows.Input;
using Chameleon_Hub.Core;

namespace Chameleon_Hub.ViewModels
{
    public class EditorViewModel : ObservableObject
    {
        private readonly FileLoaderService _fileLoaderService = new();

        private ObservableCollection<GameFolder> _gameFolders;
        public ObservableCollection<GameFolder> GameFolders
        {
            get => _gameFolders;
            set => SetProperty(ref _gameFolders, value);
        }

        private object _selectedItem;
        public object SelectedItem
        {
            get => _selectedItem;
            set => SetProperty(ref _selectedItem, value);
        }

        public EditorViewModel()
        {
            // You could load data on initialization or with a command
            GameFolders = _fileLoaderService.LoadGameFolders("PathToGameFilesRoot");
        }

        // Add commands and methods to handle selection changes, searches, etc.
    }
}
