using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Threading;
using Microsoft.Win32;

namespace RequestBuilder.ViewModels
{
    public class PrimaryViewModel : BaseViewModel
    {
        private RequestSessionViewModel currentSession;
        private ObservableCollection<HttpVerb> verbs;
        private Dispatcher dispatcher;
        private double width;
        private double height;
        private ObservableCollection<RequestHistoryItem> history;
        private RequestHistoryItem selectedHistoryItem;

        public PrimaryViewModel(Dispatcher dispatcher)
        {
            this.dispatcher = dispatcher;
            foreach (var item in HistoryPersistenceService.Load())
                History.Add(item);
        }

        public RequestSessionViewModel CurrentSession
        {
            get
            {
                if (currentSession == null)
                {
                    currentSession = new RequestSessionViewModel(dispatcher);
                    currentSession.RequestCompleted += OnRequestCompleted;
                }
                return currentSession;
            }
            set
            {
                currentSession = value;
                OnPropertyChanged();
            }
        }

        public ObservableCollection<RequestHistoryItem> History
        {
            get => history ??= new ObservableCollection<RequestHistoryItem>();
        }

        public RequestHistoryItem SelectedHistoryItem
        {
            get => selectedHistoryItem;
            set
            {
                selectedHistoryItem = value;
                OnPropertyChanged();
                if (value != null)
                {
                    CurrentSession.Url = value.Url;
                    CurrentSession.HttpVerb = value.HttpVerb;
                    CurrentSession.Headers = value.Headers;
                    CurrentSession.Body = value.Body;
                    CurrentSession.ResponseString = value.ResponseString;
                    CurrentSession.ResponseHeaders = value.ResponseHeaders;
                    CurrentSession.Status = value.Status;
                }
            }
        }

        private void OnRequestCompleted(RequestHistoryItem item)
        {
            HistoryPersistenceService.SaveItem(item);
            History.Insert(0, item);
        }

        public Command DeleteHistoryItemCommand => new Command(obj =>
        {
            if (obj is RequestHistoryItem item)
            {
                HistoryPersistenceService.DeleteItem(item);
                History.Remove(item);
            }
        });

        public Command SaveRequestCommand => new Command(obj =>
        {
            if (obj is RequestHistoryItem item)
                SaveTextToFile(HistoryPersistenceService.FormatRequest(item), "request");
        });

        public Command SaveResponseCommand => new Command(obj =>
        {
            if (obj is RequestHistoryItem item)
                SaveTextToFile(HistoryPersistenceService.FormatResponse(item), "response");
        });

        public Command SaveRequestAndResponseCommand => new Command(obj =>
        {
            if (obj is RequestHistoryItem item)
            {
                var sb = new StringBuilder();
                sb.AppendLine("===== REQUEST =====");
                sb.AppendLine();
                sb.Append(HistoryPersistenceService.FormatRequest(item));
                sb.AppendLine();
                sb.AppendLine("===== RESPONSE =====");
                sb.AppendLine();
                sb.Append(HistoryPersistenceService.FormatResponse(item));
                SaveTextToFile(sb.ToString(), "request_response");
            }
        });

        private void SaveTextToFile(string content, string defaultName)
        {
            var dialog = new SaveFileDialog
            {
                Filter = "Text files (*.txt)|*.txt|All files (*.*)|*.*",
                DefaultExt = "txt",
                FileName = defaultName
            };
            if (dialog.ShowDialog() == true)
                File.WriteAllText(dialog.FileName, content);
        }

        public ObservableCollection<HttpVerb> Verbs
        {
            get
            {
                if (verbs == null)
                {
                    verbs = new ObservableCollection<HttpVerb>();
                    var values = Enum.GetValues<HttpVerb>();
                    verbs.AddRange(values);
                }
                return verbs;
            }
        }

        public double Width
        {
            get => width;
            set
            {
                width = value;
                OnPropertyChanged();
            }
        }

        public double Height
        {
            get => height;
            set
            {
                height = value;
                OnPropertyChanged();
            }
        }
    }
}
