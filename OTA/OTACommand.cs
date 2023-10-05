using System;

namespace ESP_OTA_Updater_Maui.OTA
{
    [Flags]
    public enum OTACommand
    {
        FLASH = 0,
        SPIFFS = 100,
        AUTH = 200
    }
}
