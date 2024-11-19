using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Threading;

namespace RequestBuilder.ViewModels
{
    public class PrimrayViewModel : BaseViewModel
    {
        private RequestSessionViewModel currentSession;
        private ObservableCollection<HttpVerb> verbs;
        private Dispatcher dispatcher;

        public PrimrayViewModel(Dispatcher dispatcher)
        {
            this.dispatcher = dispatcher;
        }

        public RequestSessionViewModel CurrentSession
        {
            get { 
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
    }
}
