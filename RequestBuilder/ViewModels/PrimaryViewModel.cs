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
                }
                return currentSession;
            }
            set
            {
                currentSession = value;
                OnPropertyChanged();
            }
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
