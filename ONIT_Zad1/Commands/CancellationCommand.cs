using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace ONIT_Zad1
{
    internal class CancellationCommand : ICommand
    {
        public event EventHandler CanExecuteChanged;


        Action cancelAction;
        ICommand other;
        public CancellationCommand(Action cancelAction, ICommand other)
        {
            this.cancelAction = cancelAction;
            this.other = other;
            other.CanExecuteChanged += (s, e) => this.CanExecuteChanged?.Invoke(s, e);
        }

        public bool CanExecute(object parameter)
        {
            return !other.CanExecute(parameter);
        }

        public void Execute(object parameter)
        {
            cancelAction?.Invoke();
        }
    }
}
