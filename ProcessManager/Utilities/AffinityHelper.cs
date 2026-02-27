using System;

namespace ProcessManager.Utilities
{
    public static class AffinityHelper
    {
        public static IntPtr BuildMask(bool[] cores)
        {
            long mask = 0;
            for (int i = 0; i < cores.Length; i++)
                if (cores[i])
                    mask |= (1L << i);

            return new IntPtr(mask);
        }

        public static bool IsEnabled(IntPtr mask, int core)
        {
            return (mask.ToInt64() & (1L << core)) != 0;
        }

        public static string ToBinary(IntPtr mask)
        {
            long value = mask.ToInt64();
            return Convert.ToString(value, 2).PadLeft(Environment.ProcessorCount, '0');
        }

        public static string ToHex(IntPtr mask)
            => "0x" + mask.ToInt64().ToString("X");
    }
}