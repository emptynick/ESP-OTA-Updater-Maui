using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Net.Sockets;
using ESP_OTA_Updater_Maui.Helper;
using ESP_OTA_Updater_Maui.OTA;

namespace ESP_OTA_Updater_Maui {
    public partial class MainPage : ContentPage, IDisposable {
        public ObservableCollection<Models.Device> Devices { get; set; } = new();
        DeviceListener listener;

        // Firmware
        private string FirmwarePath { get; set; } = string.Empty;
        private byte[] FirmwareData { get; set; } = null;
        private string FirmwareHash { get;set; } = string.Empty;

        public MainPage () {
            InitializeComponent();
            BindingContext = this;
        }

        private async Task<bool> EnsureFirmwareExists () {
            try {
                PickOptions options = new() {
                    PickerTitle = "Please select a comic file",
                    FileTypes = new FilePickerFileType(new Dictionary<DevicePlatform, IEnumerable<string>> {
                        { DevicePlatform.Android, new[] { "application/octet-stream", "application/x-binary" } }, // MIME type
                        { DevicePlatform.WinUI, new[] { ".bin" } }, // file extension
                    }),
                };

                var result = await FilePicker.Default.PickAsync(options);
                if (result != null) {
                    FirmwarePath = result.FullPath;
                    FirmwareData = File.ReadAllBytes(FirmwarePath);
                    FirmwareHash = FirmwareData.MD5Hash().ToLower();
                    return true;
                }
            } catch (Exception) { }

            return false;
        }

        private async void App_Loaded (object sender, EventArgs e) {
            listener = new();

            var timer = Application.Current.Dispatcher.CreateTimer();
            timer.Interval = TimeSpan.FromSeconds(5);
            timer.Tick += Timer_Tick;
            timer.Start();

            await listener.DiscoverDevices();
            await listener.StartListening(this);

        }

        private async void Timer_Tick (object sender, EventArgs e) {
            await listener.DiscoverDevices();

            // TODO: Clean up devices that haven't been seen in 1 minute (?)
        }

        public void Dispose () {
            if (listener != null) {
                listener.Dispose();
            }
        }
    }

}
