using JustMVVM;
using System.Windows.Input;

namespace FileDialogSample
{
    public class MainWindowViewModel : MVVMBase
    {
        private string _path;
        public string Path
        {
            get { return _path; }
            set
            {
                _path = value;
                OnPropertyChanged();
            }
        }

        public MainWindowViewModel()
        {
            Path = @"C:\";
        }
    }
}
