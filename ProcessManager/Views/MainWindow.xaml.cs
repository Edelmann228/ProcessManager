using System.Windows;
using System.Windows.Input;
using System.Windows.Controls;

namespace ProcessManager.Views
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

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

        private void TreeView_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (e.NewValue is Models.ProcessInfo process)
            {
                var vm = DataContext as ViewModels.MainViewModel;
                if (vm != null)
                {
                    vm.SelectedProcess = process;
                }
            }
        }

        private void TreeViewItem_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            var treeViewItem = sender as TreeViewItem;
            if (treeViewItem?.DataContext is Models.ProcessInfo process)
            {
                var vm = DataContext as ViewModels.MainViewModel;
                if (vm != null)
                {
                    vm.SelectedProcess = process;
                    treeViewItem.IsSelected = true;
                }
            }
        }
    }
}