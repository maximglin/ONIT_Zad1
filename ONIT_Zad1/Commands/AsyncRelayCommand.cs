using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;

namespace ONIT_Zad1
{
    internal class AsyncRelayCommand : ICommand
    {
        public event EventHandler CanExecuteChanged;

        bool isExecuting = false;

        Func<Task> action;
        Action<Exception> onExceptionAction;
        Action<TaskCanceledException> onTaskCancelledAction;
        public AsyncRelayCommand(Func<Task> action, Action<Exception> onExceptionAction = null, Action<TaskCanceledException> onTaskCancelledAction = null)
        {
            this.action = action;
            this.onExceptionAction = onExceptionAction;
            this.onTaskCancelledAction = onTaskCancelledAction;
        }

        public bool IsExecuting { get => isExecuting; set { isExecuting = value; CanExecuteChanged?.Invoke(this, new EventArgs()); } }

        public bool CanExecute(object parameter)
        {
            return !IsExecuting;
        }

        public async void Execute(object parameter)
        {
            try
            {
                IsExecuting = true;
                if (action != null)
                    await action.Invoke();
            }
            catch (TaskCanceledException cancelException)
            {
                onTaskCancelledAction?.Invoke(cancelException);
            }
            catch (Exception ex)
            {
                onExceptionAction?.Invoke(ex);
            }
            finally
            {
                IsExecuting = false;
            }
        }
    }


}
