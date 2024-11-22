using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace RequestBuilder
{
    public class Command : ICommand
    {
        private readonly Action Action;
        private readonly Action<object> ActionWithParam;

        public event EventHandler CanExecuteChanged;
        public Command(Action action)
        {
            Action = action;
        }
        public Command(Action<object> actionWithParam)
        {
            ActionWithParam = actionWithParam;
        }
        public bool CanExecute(object parameter)
        {
            return true;
        }

        public void Execute(object parameter)
        {
            Action?.Invoke();
            ActionWithParam?.Invoke(parameter);
        }
    }
}
