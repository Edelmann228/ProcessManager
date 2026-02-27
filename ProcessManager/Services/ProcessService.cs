using ProcessManager.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Management;
using System.Threading.Tasks;

namespace ProcessManager.Services
{
    public class ProcessService
    {
        private Dictionary<int, int> _parentCache = new Dictionary<int, int>();

        public async Task<List<ProcessInfo>> GetProcessesAsync()
        {
            var processes = Process.GetProcesses();
            var list = new List<ProcessInfo>();

            await LoadParentProcessIdsAsync();

            foreach (var p in processes)
            {
                try
                {
                    var processInfo = new ProcessInfo
                    {
                        Id = p.Id,
                        Name = p.ProcessName,
                        Priority = GetSafePriority(p),
                        MemoryUsage = p.WorkingSet64,
                        ThreadCount = p.Threads.Count,
                        CpuTime = GetSafeTotalProcessorTime(p),
                        HasWindow = GetSafeMainWindowHandle(p) != IntPtr.Zero,
                        ParentId = _parentCache.ContainsKey(p.Id) ? _parentCache[p.Id] : 0
                    };

                    list.Add(processInfo);
                }
                catch
                {
                    // Пропускаем процессы, к которым нет доступа
                }
                finally
                {
                    p.Dispose();
                }
            }

            return list;
        }

        private ProcessPriorityClass GetSafePriority(Process p)
        {
            try { return p.PriorityClass; }
            catch { return ProcessPriorityClass.Normal; }
        }

        private TimeSpan GetSafeTotalProcessorTime(Process p)
        {
            try { return p.TotalProcessorTime; }
            catch { return TimeSpan.Zero; }
        }

        private IntPtr GetSafeMainWindowHandle(Process p)
        {
            try { return p.MainWindowHandle; }
            catch { return IntPtr.Zero; }
        }

        private async Task LoadParentProcessIdsAsync()
        {
            _parentCache.Clear();

            await Task.Run(() =>
            {
                try
                {
                    using (var searcher = new ManagementObjectSearcher(
                        "SELECT ProcessId, ParentProcessId FROM Win32_Process"))
                    {
                        foreach (var obj in searcher.Get())
                        {
                            try
                            {
                                int pid = Convert.ToInt32(obj["ProcessId"]);
                                int ppid = Convert.ToInt32(obj["ParentProcessId"]);
                                _parentCache[pid] = ppid;
                            }
                            catch { }
                        }
                    }
                }
                catch { }
            });
        }

        public bool KillProcess(int id)
        {
            try
            {
                var process = Process.GetProcessById(id);
                process.Kill();
                process.WaitForExit(1000); // Ждем завершения
                return true;
            }
            catch
            {
                return false;
            }
        }

        public bool SetPriority(int id, ProcessPriorityClass priority)
        {
            try
            {
                using (var p = Process.GetProcessById(id))
                {
                    p.PriorityClass = priority;
                    return true;
                }
            }
            catch
            {
                return false;
            }
        }

        public IntPtr GetAffinity(int id)
        {
            try
            {
                using (var p = Process.GetProcessById(id))
                {
                    return p.ProcessorAffinity;
                }
            }
            catch
            {
                return IntPtr.Zero;
            }
        }

        public bool SetAffinity(int id, IntPtr mask)
        {
            try
            {
                using (var p = Process.GetProcessById(id))
                {
                    p.ProcessorAffinity = mask;
                    return true;
                }
            }
            catch
            {
                return false;
            }
        }
    }
}