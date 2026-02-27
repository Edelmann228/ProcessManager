using System.Diagnostics;

namespace ProcessManager.Models
{
    public class ThreadInfo
    {
        public int Id { get; set; }
        public ThreadPriorityLevel Priority { get; set; }
        public ThreadState State { get; set; }
        public System.TimeSpan CpuTime { get; set; }
    }
}