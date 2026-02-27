using System.Windows;
using System.Windows.Input;

namespace ProcessManager.Views
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            // Дополнительная привязка команд
            CommandBindings.Add(new CommandBinding(
                new RoutedCommand("DeleteCommand", typeof(MainWindow)),
                (s, e) => ExecuteDelete(),
                (s, e) => e.CanExecute = (DataContext as ViewModels.MainViewModel)?.SelectedProcess != null
            ));
        }

        private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Delete)
            {
                var vm = DataContext as ViewModels.MainViewModel;
                if (vm?.SelectedProcess != null)
                {
                    vm.KillCommand.Execute(null);
                    e.Handled = true;
                }
            }
        }

        private void ExecuteDelete()
        {
            var vm = DataContext as ViewModels.MainViewModel;
            vm?.KillCommand.Execute(null);
        }
    }
}