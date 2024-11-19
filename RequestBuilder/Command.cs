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

        public event EventHandler CanExecuteChanged;
        public Command(Action action)
        {
            Action = action;
        }
        public bool CanExecute(object parameter)
        {
            return true;
        }

        public void Execute(object parameter)
        {
            Action?.Invoke();
        }
    }
}
