using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace launch.Views.Other
{
    public static class EventService
    {
        public static event EventHandler SubscriptionChanged;
        public static event EventHandler SubscriptionListChanged;
        public static event EventHandler AssemblyDownloadCompleted;
        public static event EventHandler NewsChanged;

        public static void RaiseSubscriptionChanged()
        {
            SubscriptionChanged?.Invoke(null, EventArgs.Empty);
        }

        public static void RaiseSubscriptionListChanged()
        {
            SubscriptionListChanged?.Invoke(null, EventArgs.Empty);
        }

        public static void RaiseAssemblyDownloadCompleted()
        {
            AssemblyDownloadCompleted?.Invoke(null, EventArgs.Empty);
        }

        public static void RaiseNewsChanged()
        {
            NewsChanged?.Invoke(null, EventArgs.Empty);
        }
    }
}
