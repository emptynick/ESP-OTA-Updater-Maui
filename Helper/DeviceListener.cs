using CommunityToolkit.Maui.Alerts;
using CommunityToolkit.Maui.Core;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.Intrinsics.Arm;
using System.Text;
using System.Text.Json.Nodes;

namespace ESP_OTA_Updater_Maui.Helper {
   class DeviceListener : IDisposable {
        UdpClient udp = new UdpClient();

        public DeviceListener() {
            try {
                udp.Client.Bind(new IPEndPoint(IPAddress.Any, 8426));
                Toast.Make("ESP Updater listening on port 8426", ToastDuration.Short, 14).Show(CancellationToken.None);
            } catch (Exception) {
                Toast.Make("Port 8426 is already in use", ToastDuration.Long, 14).Show(CancellationToken.None);

                return;
            }
        }

        public void Dispose () {
            udp.Close();
            udp.Dispose();
        }

        public async Task StartListening(MainPage mainpage) {
            while (true) {
                var msg = await udp.ReceiveAsync();
                Models.Device dev = mainpage.Devices.Where(X => X.Ip != null && X.Ip.ToString() == msg.RemoteEndPoint.Address.ToString()).FirstOrDefault();
                string data = Encoding.ASCII.GetString(msg.Buffer);
                if (data != "data") {
                    if (dev == null) {
                        dev = new Models.Device(udp, mainpage) { Ip = msg.RemoteEndPoint.Address };
                        mainpage.Devices.Add(dev);
                        Extensions.GetHostName(msg.RemoteEndPoint.Address, dev);
                        mainpage.Devices.Sort();
                    }

                    // Check if data is JSON
                    if (Extensions.IsValidJson(data)) {
                        JsonNode json = JsonNode.Parse(data);

                        if (json != null) {
                            dev.Known = true;
                            dev.Type = json["type"]?.GetValue<string>() ?? "Unknown";
                            dev.Version = json["version"]?.GetValue<string>() ?? "Unknown";
                            dev.Temperature = (double)((json["temperature"]?.GetValue<double>() ?? 0.00) / 10.00) + "°";
                            //dev.Status1 = json["status1"]?.GetValue<JsonNode>() ?? JsonNode.Parse("{}");
                            //dev.WebSupport = json["web_support"]?.GetValue<bool>() ?? false;
                        }
                    }
                }
            }
        }

        public async Task DiscoverDevices () {
            await udp.SendAsync(Encoding.ASCII.GetBytes("data"), 4, "255.255.255.255", 8426);
        }

        public async void DiscoverDevices (object sender, EventArgs e) {
            await DiscoverDevices();
        }
    }
}
