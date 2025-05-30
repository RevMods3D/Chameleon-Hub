using System.Collections.ObjectModel;
using System.IO;

namespace Chameleon_Hub.Core
{
    public class FileLoaderService
    {
        public ObservableCollection<GameFolder> LoadGameFolders(string rootPath)
        {
            var rootFolder = new GameFolder("Root");
            LoadFoldersRecursive(rootPath, rootFolder);
            var collection = new ObservableCollection<GameFolder> { rootFolder };
            return collection;
        }

        private void LoadFoldersRecursive(string path, GameFolder parentFolder)
        {
            // Load subfolders
            foreach (var dir in Directory.GetDirectories(path))
            {
                var folder = new GameFolder(Path.GetFileName(dir));
                parentFolder.SubFolders.Add(folder);
                LoadFoldersRecursive(dir, folder);
            }

            // Load .bndl files in this folder
            foreach (var file in Directory.GetFiles(path, "*.bndl"))
            {
                var bndl = new BndlFile(Path.GetFileName(file));
                parentFolder.BndlFiles.Add(bndl);

                // TODO: Load .dat files inside this .bndl
                // For now, maybe dummy data:
                bndl.DatFiles.Add(new DatFileReference("example.dat", 0, 1024, file));
            }
        }
    }
}
