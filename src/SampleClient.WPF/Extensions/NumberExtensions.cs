using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SampleClient.WPF.Extensions
{
    public static class NumberExtensions
    {
        public static string HumanReadableSpeed(this int speed) => HumanReadableSpeed((long)speed);
        public static string HumanReadableSpeed(this long speed)
        {
            if (speed > 1024 * 1024 * 1024)
            {
                return string.Format("{0:F2} GiB/s", speed / (1024.0 * 1024.0 * 1024.0));
            }
            else if (speed > 1024 * 1024)
            {
                return string.Format("{0:F2} MiB/s", speed / (1024.0 * 1024.0));
            }
            else if (speed > 1024)
            {
                return string.Format("{0:F2} kiB/s", speed / 1024.0);
            }
            else
            {
                return string.Format("{0} B/s", speed);
            }
        }
        public static string HumanReadableSize(this long size)
        {
            if (size > 1024 * 1024 * 1024)
            {
                return string.Format("{0:F2} GiB", size / (1024.0 * 1024.0 * 1024.0));
            }
            else if (size > 1024 * 1024)
            {
                return string.Format("{0:F2} MiB", size / (1024.0 * 1024.0));
            }
            else if (size > 1024)
            {
                return string.Format("{0:F2} kiB", size / 1024.0);
            }
            else
            {
                return string.Format("{0} B", size);
            }
        }
    }
}
