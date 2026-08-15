namespace RequestBuilder.ViewModels
{
    public enum MultipartParamType { Text, File }

    public class MultipartParamViewModel : BaseViewModel
    {
        private string key = "";
        private string textValue = "";
        private string filePath = "";
        private MultipartParamType paramType = MultipartParamType.Text;

        public event Action Changed;

        public string Key
        {
            get => key;
            set { key = value; OnPropertyChanged(); Changed?.Invoke(); }
        }

        public string TextValue
        {
            get => textValue;
            set { textValue = value; OnPropertyChanged(); Changed?.Invoke(); }
        }

        public string FilePath
        {
            get => filePath;
            set { filePath = value; OnPropertyChanged(); Changed?.Invoke(); }
        }

        public MultipartParamType ParamType
        {
            get => paramType;
            set
            {
                paramType = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(IsText));
                OnPropertyChanged(nameof(IsFile));
                Changed?.Invoke();
            }
        }

        public bool IsText => paramType == MultipartParamType.Text;
        public bool IsFile => paramType == MultipartParamType.File;
    }
}
