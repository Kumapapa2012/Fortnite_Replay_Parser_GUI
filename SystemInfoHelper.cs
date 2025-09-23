using System;
using System.Linq;
using System.Management;
using System.Drawing; // 参照追加必要
using System.Windows.Forms; // 参照追加必要
using Microsoft.Win32;

namespace Fortnite_Replay_Parser_GUI
{
    public static class SystemInfoHelper
    {
        public static string GetGPU()
        {
            try
            {
                using var searcher = new ManagementObjectSearcher("select * from Win32_VideoController");
                foreach (var obj in searcher.Get())
                {
                    var name = obj["Name"]?.ToString();
                    if (!string.IsNullOrEmpty(name))
                        return name;
                }
            }
            catch { }
            return "Unknown GPU";
        }

        public static string GetCPU()
        {
            try
            {
                using var searcher = new ManagementObjectSearcher("select * from Win32_Processor");
                foreach (var obj in searcher.Get())
                {
                    var name = obj["Name"]?.ToString();
                    if (!string.IsNullOrEmpty(name))
                        return name;
                }
            }
            catch { }
            return "Unknown CPU";
        }

        public static string GetMemory()
        {
            try
            {
                using var searcher = new ManagementObjectSearcher("select * from Win32_ComputerSystem");
                foreach (var obj in searcher.Get())
                {
                    var total = Convert.ToDouble(obj["TotalPhysicalMemory"]) / (1024 * 1024 * 1024);
                    return $"{Math.Round(total, 2)} GB RAM";
                }
            }
            catch { }
            return "Unknown Memory";
        }

        public static string GetAvailableMemory()
        {
            try
            {
                using var searcher = new ManagementObjectSearcher("select * from Win32_OperatingSystem");
                foreach (var obj in searcher.Get())
                {
                    var available = Convert.ToDouble(obj["FreePhysicalMemory"]) / (1024 * 1024);
                    return $"{Math.Round(available, 2)} GB RAM は使用可能です";
                }
            }
            catch { }
            return "";
        }

        public static string GetResolution()
        {
            try
            {
                var primaryScreen = Screen.PrimaryScreen;
                if (primaryScreen == null)
                    return "Unknown Resolution";
                // RefreshRate プロパティは存在しないため、解像度のみ返す
                return $"{primaryScreen.Bounds.Width} x {primaryScreen.Bounds.Height}";
            }
            catch { }
            return "Unknown Resolution";
        }

        public static string GetOS()
        {
            try
            {
                string[] OsDescription = System.Runtime.InteropServices.RuntimeInformation.OSDescription.Split(".");
                if (OsDescription.Length >= 2 && OsDescription[0].Contains("Windows 10"))
                {
                    // Windows 10, Windows 11 の場合、メジャーバージョンとマイナーバージョンを使用して OS 名を特定
                    if ( int.TryParse(OsDescription[2], out int value))
                    {
                        if(value >= 22000) {
                            return "Windows 11";
                        }
                        else {
                            return "Windows 10 or earlier";
                        }
                    }
                    else
                    {
                        // OS情報を返す
                        return "Windows 10 or earlier";
                    }
                }
            }
            catch { }
            return "Unknown OS";
        }

        public static string GetSystemInfoText()
        {
            return
                $"GPU：{GetGPU()}\n" +
                $"CPU：{GetCPU()}\n" +
                $"メモリ：{GetMemory()} ({GetAvailableMemory()})\n" +
                $"現在の解像度：{GetResolution()}\n" +
                $"オペレーティング システム： {GetOS()}";
        }
    }
}