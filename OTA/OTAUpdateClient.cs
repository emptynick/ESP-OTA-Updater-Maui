using Microsoft.Maui.Controls;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Numerics;
using System.Text;

using ESP_OTA_Updater_Maui.Models;

namespace ESP_OTA_Updater_Maui.OTA {
    public class OTAUpdateClient : IDisposable {
        UdpClient cmdSocket;
        TcpListener dataListener;
        TcpClient dataSocket;
        public enum FLASH_Options {
            FLASH = 0,
            SPIFFS = 100,
            AUTH = 200,
        }
        public OTAUpdateClient () {

        }

        public void Dispose() {
            dataListener.Stop();
            cmdSocket.Close();
            cmdSocket.Dispose();
            dataSocket.Close();
            dataSocket.Dispose();
        }

        public async void UploadFirmware (string remoteAddress, int remotePort, string password, byte[] firmwareData, Models.Device device) {
            try {
                device.FlashStatus = FlashStatusEnum.Initializing;
                device.FlashText = "Starting";
                device.FlashProgress = 0;
                IPAddress address = IPAddress.Any;

                dataListener = new TcpListener(address, 0);
                dataListener.Server.NoDelay = true;
                dataListener.Start();
                int localPort = ((IPEndPoint)dataListener.LocalEndpoint).Port;
                device.FlashText = $"Server started. Listening to TCP clients at 0.0.0.0:{localPort}";
                long content_size = firmwareData.Length;
                string file_md5 = firmwareData.MD5Hash().ToLower();
                device.FlashText = $"Upload size {content_size}";

                string message = string.Format("0 {1} {2} {3}\n", 0, localPort, content_size, file_md5);
                byte[] message_bytes = Encoding.ASCII.GetBytes(message);
                device.FlashText = "Sending invitation to " + remoteAddress;


                cmdSocket = new UdpClient();
                var ep = new IPEndPoint(IPAddress.Parse(remoteAddress), remotePort);
                await cmdSocket.SendAsync(message_bytes, message_bytes.Length, ep);
                var t = cmdSocket.ReceiveAsync();
                t.Wait(10000);
                var res = t.Result;
                var res_text = Encoding.ASCII.GetString(res.Buffer);
                if (res_text != "OK") {
                    device.FlashText = "AUTH required and not implemented";
                    device.FlashStatus = FlashStatusEnum.Failed;
                }
                device.FlashText = "Waiting for device ...";
                dataListener.Server.SendTimeout = 10000;
                dataListener.Server.ReceiveTimeout = 10000;
                DateTime startTime = DateTime.Now;
                device.FlashText = "Awaiting Connection";
                while ((DateTime.Now - startTime).TotalSeconds < 10) {
                    if (dataListener.Pending()) {
                        dataSocket = dataListener.AcceptTcpClient();
                        dataSocket.NoDelay = true;
                        break;
                    } else
                        Thread.Sleep(10);
                }
                if (dataSocket == null) {
                    device.FlashText = "No response from device";
                    device.FlashStatus = FlashStatusEnum.Failed;
                }
                device.FlashText = "Got Connection";
                using (MemoryStream fs = new MemoryStream(firmwareData)) {
                    int offset = 0;
                    byte[] chunk = new byte[1460];
                    int chunk_size = 0;
                    int read_count = 0;
                    string resp = "";
                    while (content_size > offset) {
                        chunk_size = fs.Read(chunk, 0, 1460);
                        offset += chunk_size;
                        if (dataSocket.Available > 0) {
                            resp = Encoding.ASCII.GetString(chunk, 0, read_count);
                            Console.Write(resp);
                        }
                        device.FlashProgress = (float)(offset) / (float)(content_size) * 100;
                        device.FlashText = $"Flashing {(int)device.FlashProgress}%";
                        device.FlashStatus = FlashStatusEnum.Flashing;
                        dataSocket.Client.Send(chunk, 0, chunk_size, SocketFlags.None);
                        dataSocket.ReceiveTimeout = 10000;

                    }
                    dataSocket.ReceiveTimeout = 15000;
                    read_count = dataSocket.Client.Receive(chunk);
                    resp = Encoding.ASCII.GetString(chunk, 0, read_count);
                    while (!resp.Contains("O")) {
                        if (resp.Contains("E")) {
                            device.FlashText = "Flashing failed";
                            device.FlashStatus = FlashStatusEnum.Failed;
                            device.FlashProgress = 0;
                        }
                        read_count = dataSocket.Client.Receive(chunk);
                        resp = Encoding.ASCII.GetString(chunk, 0, read_count);
                    }
                    device.FlashText = "Done";
                    device.FlashStatus = FlashStatusEnum.Finished;
                }
            } catch (SocketException ex) {
                device.FlashStatus = FlashStatusEnum.Failed;
                device.FlashProgress = 0;
                if (ex.SocketErrorCode == SocketError.TimedOut) {
                    device.FlashText = $"Device timed out";
                } else {
                    device.FlashText = ex.Message;
                }
            } catch (Exception ex) {
                device.FlashText = ex.Message;
                device.FlashStatus = FlashStatusEnum.Failed;
                device.FlashProgress = 0;
            }
        }
    }
}