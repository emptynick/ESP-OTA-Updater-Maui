using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.Json.Nodes;
using System.Threading.Tasks;

namespace ESP_OTA_Updater_Maui.Helper {
    static class Extensions {
        public static void Sort<T> (this ObservableCollection<T> collection) where T : IComparable {
            List<T> sorted = collection.OrderBy(x => x).ToList();
            for (int i = 0; i < sorted.Count(); i++)
                collection.Move(collection.IndexOf(sorted[i]), i);
        }
        public static string SortableIPString (IPAddress ipAddress) {
            return string.Join(".", ipAddress.ToString().Split('.').Select(part => part.PadLeft(3, '0')));
        }

        public static bool IsValidJson(string input) {
            try {
                var tmpObj = JsonNode.Parse(input);
                return true;
            } catch (Exception) { }

            return false;
        }

        public static async void GetHostName (IPAddress ipAddress, Models.Device device) {
            try {
                IPHostEntry entry = await Dns.GetHostEntryAsync(ipAddress);
                if (entry != null) {
                    device.Name = entry.HostName;
                }
            } catch (Exception) { }
        }
    }
}
