namespace RequestBuilder.ViewModels
{
    public class FormParamViewModel : BaseViewModel
    {
        private string key = "";
        private string val = "";

        public event Action? Changed;

        public string Key
        {
            get => key;
            set { key = value; OnPropertyChanged(); Changed?.Invoke(); }
        }

        public string Value
        {
            get => val;
            set { val = value; OnPropertyChanged(); Changed?.Invoke(); }
        }
    }
}
