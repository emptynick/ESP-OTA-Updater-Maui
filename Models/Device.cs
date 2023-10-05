using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.Intrinsics.Arm;
using System.Text;
using System.Text.Json.Nodes;
using System.Threading.Tasks;

namespace ESP_OTA_Updater_Maui.Models {
    public enum FlashStatusEnum {
        Pending,
        Initializing,
        Flashing,
        Failed,
        Finished
    }
    public partial class Device : ObservableObject, IComparable {
        // Public
        [ObservableProperty]
        private IPAddress ip;

        [ObservableProperty]
        private string type;

        [ObservableProperty]
        private string name;

        [ObservableProperty]
        private string version;

        [ObservableProperty]
        private JsonNode status1;

        [ObservableProperty]
        private JsonNode status2;

        [ObservableProperty]
        private string temperature;

        // Internal

        [ObservableProperty]
        private bool known = false;

        [ObservableProperty]
        private bool selected = false;

        [ObservableProperty]
        private string flashText = "Flash me";

        [ObservableProperty]
        private float flashProgress = 0;

        [ObservableProperty]
        private FlashStatusEnum flashStatus = FlashStatusEnum.Pending;

        // LED related
        [ObservableProperty]
        private int red = 0;

        [ObservableProperty]
        private int green = 0;

        [ObservableProperty]
        private int blue = 0;

        [ObservableProperty]
        private int white = 0;

        [ObservableProperty]
        private int strobe = 0;

        private MainPage mainpage;

        public Device (UdpClient udp, MainPage mainpage) {
            this.udp = udp;
            this.mainpage = mainpage;
        }

        public bool IsLED {
            get {
                return Type.ToUpper() == "LED";
            }
        }

        public bool IsXMO {
            get {
                return Type.ToUpper() == "XMO";
            }
        }

        public async Task Restart () {
            await udp.SendAsync(Encoding.ASCII.GetBytes("restart"), 7, Ip.ToString(), 8426);
        }

        public async Task Identify () {
            await udp.SendAsync(Encoding.ASCII.GetBytes("identify"), 8, Ip.ToString(), 8426);
        }

        public string sortBy = "ip";
        public bool sortAsc = true;

        public int CompareTo (object o) {
            Device a = this;
            Device b = (Device)o;

            /*if (mainpage.sortBy == "ip") {
                if (!mainpage.sortAsc) {
                    return string.Compare(Extensions.IpAddressLabel(b.Ip), Extensions.IpAddressLabel(a.Ip));
                }
            } else if (mainpage.sortBy == "name") {
                if (mainpage.sortAsc) {
                    return string.Compare(a.Name, b.Name);
                } else {
                    return string.Compare(b.Name, a.Name);
                }
            } else if (mainpage.sortBy == "type") {
                if (mainpage.sortAsc) {
                    return string.Compare(a.Type, b.Type);
                } else {
                    return string.Compare(b.Type, a.Type);
                }
            }*/


            return string.Compare(Helper.Extensions.SortableIPString(a.Ip), Helper.Extensions.SortableIPString(b.Ip));
        }

        private UdpClient udp;
    }
}
