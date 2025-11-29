using System;
using System.Windows.Input;

namespace TFitnessApp.ViewModels
{
    // Triển khai ICommand để hỗ trợ MVVM
    public class RelayCommand : ICommand
    {
        private readonly Action<object> _execute;
        private readonly Func<bool> _canExecute;
        private readonly Action _executeNoParam;

        // Cần cho WPF/MVVM khi trạng thái của control thay đổi (như SelectedItem)
        public event EventHandler CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }

        // Constructor cho lệnh có tham số và kiểm tra CanExecute
        public RelayCommand(Action<object> execute, Func<bool> canExecute = null)
        {
            if (execute == null)
                throw new ArgumentNullException(nameof(execute));

            _execute = execute;
            _canExecute = canExecute;
        }

        // Constructor cho lệnh không tham số
        public RelayCommand(Action execute, Func<bool> canExecute = null)
        {
            if (execute == null)
                throw new ArgumentNullException(nameof(execute));

            _executeNoParam = execute;
            _canExecute = canExecute;
        }

        public bool CanExecute(object parameter)
        {
            return _canExecute == null || _canExecute();
        }

        public void Execute(object parameter)
        {
            if (_execute != null)
            {
                _execute(parameter);
            }
            else if (_executeNoParam != null)
            {
                _executeNoParam();
            }
        }
    }
}