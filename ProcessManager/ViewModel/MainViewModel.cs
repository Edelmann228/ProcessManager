using LiveCharts;
using LiveCharts.Wpf;
using ProcessManager.Models;
using ProcessManager.Services;
using ProcessManager.Utilities;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;

namespace ProcessManager.ViewModels
{
    public class MainViewModel : INotifyPropertyChanged
    {
        private ProcessService _service = new ProcessService();
        private DispatcherTimer _updateTimer;
        private DispatcherTimer _cpuTimer;
        private bool _isUpdating = false;
        private PerformanceCounter _totalCpuCounter;

        public ObservableCollection<ProcessInfo> Processes { get; set; }
            = new ObservableCollection<ProcessInfo>();

        public ObservableCollection<ProcessInfo> ProcessTree { get; set; }
            = new ObservableCollection<ProcessInfo>();

        public ObservableCollection<bool> Cores { get; set; }

        public int CoreCount { get; } = Environment.ProcessorCount;

        public SeriesCollection CpuSeries { get; set; } = new SeriesCollection();
        public SeriesCollection MemorySeries { get; set; } = new SeriesCollection();

        private string _searchText;
        public string SearchText
        {
            get => _searchText;
            set
            {
                _searchText = value;
                OnPropertyChanged(nameof(SearchText));
                ApplyFilters();
            }
        }

        private bool _onlyGui;
        public bool OnlyGui
        {
            get => _onlyGui;
            set
            {
                _onlyGui = value;
                OnPropertyChanged(nameof(OnlyGui));
                ApplyFilters();
            }
        }

        private bool _onlySystem;
        public bool OnlySystem
        {
            get => _onlySystem;
            set
            {
                _onlySystem = value;
                OnPropertyChanged(nameof(OnlySystem));
                ApplyFilters();
            }
        }

        private int _updateInterval = 2;
        public int UpdateInterval
        {
            get => _updateInterval;
            set
            {
                if (int.TryParse(value.ToString(), out int newValue) && newValue > 0)
                {
                    _updateInterval = newValue;
                    OnPropertyChanged(nameof(UpdateInterval));
                    UpdateTimerInterval();
                }
            }
        }

        private ProcessInfo _selected;
        public ProcessInfo SelectedProcess
        {
            get => _selected;
            set
            {
                _selected = value;
                OnPropertyChanged(nameof(SelectedProcess));
                if (value != null)
                {
                    LoadAffinity();
                }
            }
        }

        private string _affinityHex;
        public string AffinityHex
        {
            get => _affinityHex;
            set
            {
                _affinityHex = value;
                OnPropertyChanged(nameof(AffinityHex));
            }
        }

        private string _affinityBin;
        public string AffinityBin
        {
            get => _affinityBin;
            set
            {
                _affinityBin = value;
                OnPropertyChanged(nameof(AffinityBin));
            }
        }

        private string _statusMessage;
        public string StatusMessage
        {
            get => _statusMessage;
            set
            {
                _statusMessage = value;
                OnPropertyChanged(nameof(StatusMessage));
            }
        }

        public ICommand RefreshCommand { get; }
        public ICommand KillCommand { get; }
        public ICommand SetHighPriorityCommand { get; }
        public ICommand SetRealtimePriorityCommand { get; }
        public ICommand SetNormalPriorityCommand { get; }
        public ICommand ApplyAffinityCommand { get; }
        public ICommand ShowHelpCommand { get; }

        private ObservableCollection<ProcessInfo> _allProcesses = new ObservableCollection<ProcessInfo>();

        public MainViewModel()
        {
            // Инициализация Cores
            Cores = new ObservableCollection<bool>();
            for (int i = 0; i < CoreCount; i++)
            {
                Cores.Add(true);
            }

            // Команды
            RefreshCommand = new RelayCommand(async _ => await LoadProcessesAsync());
            KillCommand = new RelayCommand(_ => KillProcess());
            SetHighPriorityCommand = new RelayCommand(_ => SetPriority(ProcessPriorityClass.High));
            SetRealtimePriorityCommand = new RelayCommand(_ => SetPriority(ProcessPriorityClass.RealTime));
            SetNormalPriorityCommand = new RelayCommand(_ => SetPriority(ProcessPriorityClass.Normal));
            ApplyAffinityCommand = new RelayCommand(_ => ApplyAffinity());
            ShowHelpCommand = new RelayCommand(_ => ShowHelp());

            // Инициализация графиков
            InitCharts();

            // Инициализация счетчика CPU
            try
            {
                _totalCpuCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total");
                _totalCpuCounter.NextValue();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error initializing CPU counter: {ex.Message}");
                StatusMessage = "Ошибка инициализации CPU счетчика";
            }

            // Таймер для обновления процессов
            _updateTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(UpdateInterval)
            };
            _updateTimer.Tick += async (s, e) => await LoadProcessesAsync();
            _updateTimer.Start();

            // Таймер для обновления CPU (каждую секунду)
            _cpuTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(1)
            };
            _cpuTimer.Tick += (s, e) => UpdateCpuUsage();
            _cpuTimer.Start();

            // Первоначальная загрузка
            Task.Run(async () => await LoadProcessesAsync());

            StatusMessage = "Программа запущена";
        }

        private async Task LoadProcessesAsync()
        {
            if (_isUpdating) return;

            _isUpdating = true;

            try
            {
                var list = await _service.GetProcessesAsync();

                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    _allProcesses.Clear();
                    foreach (var p in list)
                        _allProcesses.Add(p);

                    ApplyFilters();
                    StatusMessage = $"Обновлено: {DateTime.Now:HH:mm:ss} | Процессов: {list.Count}";
                });
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error loading processes: {ex.Message}");
                StatusMessage = "Ошибка обновления";
            }
            finally
            {
                _isUpdating = false;
            }
        }

        private void ApplyFilters()
        {
            var filtered = _allProcesses.AsEnumerable();

            if (!string.IsNullOrWhiteSpace(SearchText))
                filtered = filtered.Where(p => p.Name.IndexOf(SearchText, StringComparison.OrdinalIgnoreCase) >= 0);

            if (OnlyGui)
                filtered = filtered.Where(p => p.HasWindow);

            if (OnlySystem)
                filtered = filtered.Where(p => p.Id < 1000 || p.Id == 0 || p.Id == 4); // Системные процессы

            Processes.Clear();
            foreach (var p in filtered.OrderByDescending(p => p.MemoryUsage))
                Processes.Add(p);

            BuildTree(filtered.ToList());
            UpdateMemoryChart(filtered.ToList());
        }

        private void BuildTree(List<ProcessInfo> list)
        {
            ProcessTree.Clear();
            var dict = list.ToDictionary(p => p.Id);
            var added = new HashSet<int>();

            // Сначала добавляем процессы без родителей или с некорректными родителями
            foreach (var p in list.Where(p => !dict.ContainsKey(p.ParentId) || p.ParentId == 0))
            {
                if (!added.Contains(p.Id))
                {
                    ProcessTree.Add(p);
                    added.Add(p.Id);
                    AddChildrenRecursive(p, dict, added);
                }
            }

            // Добавляем оставшиеся процессы как корневые
            foreach (var p in list.Where(p => !added.Contains(p.Id)))
            {
                ProcessTree.Add(p);
                added.Add(p.Id);
                AddChildrenRecursive(p, dict, added);
            }
        }

        private void AddChildrenRecursive(ProcessInfo parent, Dictionary<int, ProcessInfo> dict, HashSet<int> added)
        {
            var children = dict.Values.Where(p => p.ParentId == parent.Id && !added.Contains(p.Id));
            foreach (var child in children)
            {
                parent.Children.Add(child);
                added.Add(child.Id);
                AddChildrenRecursive(child, dict, added);
            }
        }

        private void KillProcess()
        {
            if (SelectedProcess == null)
            {
                MessageBox.Show("Выберите процесс для завершения", "Информация",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            if (SelectedProcess.Id == Process.GetCurrentProcess().Id)
            {
                MessageBox.Show("Нельзя завершить текущий процесс", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            var result = MessageBox.Show($"Завершить процесс {SelectedProcess.Name} (PID: {SelectedProcess.Id})?\n" +
                "Все несохраненные данные будут потеряны!", "Подтверждение",
                MessageBoxButton.YesNo, MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    if (_service.KillProcess(SelectedProcess.Id))
                    {
                        StatusMessage = $"Процесс {SelectedProcess.Name} завершен";
                        Task.Run(async () => await LoadProcessesAsync());
                    }
                    else
                    {
                        MessageBox.Show("Не удалось завершить процесс. Возможно, требуются права администратора.",
                            "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка при завершении процесса: {ex.Message}",
                        "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void SetPriority(ProcessPriorityClass priority)
        {
            if (SelectedProcess == null)
            {
                MessageBox.Show("Выберите процесс", "Информация",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            if (priority == ProcessPriorityClass.RealTime)
            {
                var result = MessageBox.Show("RealTime приоритет может дестабилизировать систему!\n" +
                    "Продолжить?", "Предупреждение", MessageBoxButton.YesNo, MessageBoxImage.Warning);

                if (result == MessageBoxResult.No)
                    return;
            }

            try
            {
                if (_service.SetPriority(SelectedProcess.Id, priority))
                {
                    StatusMessage = $"Приоритет процесса {SelectedProcess.Name} изменен";
                    Task.Run(async () => await LoadProcessesAsync());
                }
                else
                {
                    MessageBox.Show("Не удалось изменить приоритет. Возможно, требуются права администратора.",
                        "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при изменении приоритета: {ex.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadAffinity()
        {
            if (SelectedProcess == null) return;

            try
            {
                var mask = _service.GetAffinity(SelectedProcess.Id);

                for (int i = 0; i < CoreCount && i < Cores.Count; i++)
                {
                    Cores[i] = AffinityHelper.IsEnabled(mask, i);
                }

                AffinityHex = AffinityHelper.ToHex(mask);
                AffinityBin = AffinityHelper.ToBinary(mask);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error loading affinity: {ex.Message}");
            }
        }

        private void ApplyAffinity()
        {
            if (SelectedProcess == null)
            {
                MessageBox.Show("Выберите процесс", "Информация",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            try
            {
                var cores = new bool[CoreCount];
                for (int i = 0; i < CoreCount && i < Cores.Count; i++)
                    cores[i] = Cores[i];

                var mask = AffinityHelper.BuildMask(cores);

                if (_service.SetAffinity(SelectedProcess.Id, mask))
                {
                    StatusMessage = $"Привязка к ядрам для {SelectedProcess.Name} применена";
                    LoadAffinity();
                }
                else
                {
                    MessageBox.Show("Не удалось изменить привязку к ядрам.",
                        "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при применении affinity: {ex.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void UpdateCpuUsage()
        {
            try
            {
                if (_totalCpuCounter != null)
                {
                    float cpuValue = _totalCpuCounter.NextValue();

                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        UpdateCpuChart(Math.Round(cpuValue, 1));
                    });
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error updating CPU usage: {ex.Message}");
            }
        }

        private void InitCharts()
        {
            CpuSeries.Clear();

            var cpuSeries = new LineSeries
            {
                Title = "Загрузка CPU (%)",
                Values = new ChartValues<double> { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 },
                PointGeometry = null,
                LineSmoothness = 0.4,
                StrokeThickness = 3,
                Fill = System.Windows.Media.Brushes.Transparent,
                Stroke = System.Windows.Media.Brushes.DodgerBlue
            };

            CpuSeries.Add(cpuSeries);
        }

        private void UpdateCpuChart(double value)
        {
            if (CpuSeries.Count > 0)
            {
                var series = CpuSeries[0] as LineSeries;
                if (series != null)
                {
                    var values = series.Values as ChartValues<double>;
                    if (values != null)
                    {
                        values.Add(value);
                        if (values.Count > 30)
                            values.RemoveAt(0);
                    }
                }
            }
        }

        private void UpdateMemoryChart(List<ProcessInfo> list)
        {
            var top = list.OrderByDescending(p => p.MemoryUsage).Take(10); // Изменено с 8 на 10

            MemorySeries.Clear();
            foreach (var p in top)
            {
                MemorySeries.Add(new PieSeries
                {
                    Title = p.Name.Length > 15 ? p.Name.Substring(0, 12) + "..." : p.Name,
                    Values = new ChartValues<double> { Math.Round(p.MemoryUsage / 1024.0 / 1024.0, 1) },
                    DataLabels = true,
                    LabelPoint = point => $"{point.Y} MB"
                });
            }
        }

        private void UpdateTimerInterval()
        {
            if (_updateTimer != null)
            {
                _updateTimer.Interval = TimeSpan.FromSeconds(UpdateInterval);
                StatusMessage = $"Интервал обновления: {UpdateInterval} сек";
            }
        }

        private void ShowHelp()
        {
            string help = @"=== ДИСПЕТЧЕР ПРОЦЕССОВ ===

                ОСНОВНЫЕ ВОЗМОЖНОСТИ:
                • Просмотр всех запущенных процессов
                • Просмотр дерева процессов (иерархия родитель-дочерние)
                • Завершение процессов (ПКМ → Завершить или Delete)
                • Изменение приоритета (ПКМ → Приоритет)
                • Настройка привязки к ядрам процессора (Affinity)
                • Фильтрация процессов (только GUI/системные)
                • Поиск по имени процесса
                • Визуализация загрузки CPU и использования памяти
                • Автообновление (интервал настраивается)
                • Возможность просмотреть дерево процессов для лучшей навигации
                СТОЛБЕЦ 'ОКНО':
                • ✓ - процесс имеет графическое окно (GUI)
                • Пусто - процесс без окна (фоновый/системный)
                

                ГОРЯЧИЕ КЛАВИШИ:
                • F5 - Обновить список
                • Delete - Завершить выбранный процесс
                • F1 - Справка

                ЦВЕТОВЫЕ ОБОЗНАЧЕНИЯ:
                • Салатовый - Высокий приоритет
                • Розовый - Realtime приоритет";

            MessageBox.Show(help, "Справка", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public class RelayCommand : ICommand
    {
        private readonly Action<object> _execute;
        private readonly Func<object, bool> _canExecute;

        public RelayCommand(Action<object> execute, Func<object, bool> canExecute = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

        public bool CanExecute(object parameter)
        {
            return _canExecute == null || _canExecute(parameter);
        }

        public void Execute(object parameter)
        {
            _execute(parameter);
        }

        public event EventHandler CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }
    }
}