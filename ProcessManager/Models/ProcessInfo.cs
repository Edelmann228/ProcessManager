using System;
using System.Diagnostics;
using System.Collections.ObjectModel;

namespace ProcessManager.Models
{
    public class ProcessInfo
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public ProcessPriorityClass Priority { get; set; }
        public long MemoryUsage { get; set; }
        public int ThreadCount { get; set; }
        public TimeSpan CpuTime { get; set; }
        public bool HasWindow { get; set; }
        public int ParentId { get; set; }

        public ObservableCollection<ProcessInfo> Children { get; set; }
            = new ObservableCollection<ProcessInfo>();

        public string MemoryMb => (MemoryUsage / 1024 / 1024) + " MB";

       
        public string PriorityDisplay
        {
            get
            {
                switch (Priority)
                {
                    case ProcessPriorityClass.High:
                        return "Высокий";
                    case ProcessPriorityClass.RealTime:
                        return "Реального времени";
                    case ProcessPriorityClass.Idle:
                        return "Низкий";
                    case ProcessPriorityClass.BelowNormal:
                        return "Ниже среднего";
                    case ProcessPriorityClass.AboveNormal:
                        return "Выше среднего";
                    default:
                        return "Обычный";
                }
            }
        }
    }
}