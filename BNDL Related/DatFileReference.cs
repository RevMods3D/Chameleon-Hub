namespace Chameleon_Hub.Core
{
    public class DatFileReference : ObservableObject
    {
        private string _name;
        private long _offset;
        private int _size;

        public string Name
        {
            get => _name;
            set => SetProperty(ref _name, value);
        }

        public long Offset
        {
            get => _offset;
            set => SetProperty(ref _offset, value);
        }

        public int Size
        {
            get => _size;
            set => SetProperty(ref _size, value);
        }

        public string BndlPath { get; set; }  // Path to the BNDL file container

        public DatFileReference(string name, long offset, int size, string bndlPath)
        {
            Name = name;
            Offset = offset;
            Size = size;
            BndlPath = bndlPath;
        }
    }
}
