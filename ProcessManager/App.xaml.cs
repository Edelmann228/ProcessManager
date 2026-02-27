using System;
using System.Diagnostics;
using System.Security.Principal;
using System.Windows;
using System.Windows.Input;

namespace ProcessManager
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // Проверка прав администратора
            bool isAdmin = IsAdministrator();

            if (!isAdmin)
            {
                // Предупреждение о правах
                Console.WriteLine("Программа запущена без прав администратора. Некоторые функции могут быть ограничены.");
            }

            // Глобальная обработка клавиш
            EventManager.RegisterClassHandler(typeof(Window),
                UIElement.PreviewKeyDownEvent, new KeyEventHandler(OnPreviewKeyDown));
        }

        private bool IsAdministrator()
        {
            try
            {
                var identity = WindowsIdentity.GetCurrent();
                var principal = new WindowsPrincipal(identity);
                return principal.IsInRole(WindowsBuiltInRole.Administrator);
            }
            catch
            {
                return false;
            }
        }

        private void OnPreviewKeyDown(object sender, KeyEventArgs e)
        {
            var window = sender as Window;
            if (window?.DataContext is ViewModels.MainViewModel vm)
            {
                if (e.Key == Key.Delete && vm.SelectedProcess != null)
                {
                    if (vm.KillCommand.CanExecute(null))
                    {
                        vm.KillCommand.Execute(null);
                        e.Handled = true;
                    }
                }
                else if (e.Key == Key.F5)
                {
                    if (vm.RefreshCommand.CanExecute(null))
                    {
                        vm.RefreshCommand.Execute(null);
                        e.Handled = true;
                    }
                }
                else if (e.Key == Key.F1)
                {
                    if (vm.ShowHelpCommand.CanExecute(null))
                    {
                        vm.ShowHelpCommand.Execute(null);
                        e.Handled = true;
                    }
                }
            }
        }
    }
}