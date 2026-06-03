using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Threading;

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
            History.Insert(0, item);
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
