using System;
using System.Diagnostics;
using System.Text.Json;

namespace Fortnite_Replay_Parser_GUI
{
    /// <summary>
    /// サーバー起動時に PowerShell の Get-ComputerInfo コマンドを実行し、
    /// システム情報をキャッシュして提供するヘルパークラス。
    /// </summary>
    public static class SystemInfoHelper
    {
        private static string _os = "取得中...";
        private static string _cpu = "取得中...";
        private static string _memory = "取得中...";
        private static string _availableMemory = "取得中...";
        private static string _gpu = "取得中...";
        private static string _resolution = "取得中...";
        private static bool _initialized = false;

        /// <summary>
        /// サーバー起動時にバックグラウンドでシステム情報を取得します。
        /// </summary>
        public static void InitializeAsync()
        {
            Task.Run(async () =>
            {
                try
                {
                    // Get-ComputerInfo でシステム情報を取得
                    var computerInfoJson = await RunPowerShellAsync(
                        "Get-ComputerInfo | Select-Object OsName, OsVersion, CsProcessors, CsTotalPhysicalMemory, OsFreePhysicalMemory | ConvertTo-Json -Depth 2"
                    );

                    if (!string.IsNullOrEmpty(computerInfoJson))
                    {
                        ParseComputerInfo(computerInfoJson);
                    }

                    // GPU 情報は Get-ComputerInfo に含まれないため、別途 CIM で取得
                    var gpuJson = await RunPowerShellAsync(
                        "Get-CimInstance Win32_VideoController | Select-Object -First 1 -Property Name | ConvertTo-Json"
                    );

                    if (!string.IsNullOrEmpty(gpuJson))
                    {
                        ParseGpuInfo(gpuJson);
                    }

                    // 画面解像度は .NET の Screen クラスで取得
                    var resolutionJson = await RunPowerShellAsync(
                        "Add-Type -AssemblyName System.Windows.Forms; $s = [System.Windows.Forms.Screen]::PrimaryScreen.Bounds; @{ Width = $s.Width; Height = $s.Height } | ConvertTo-Json"
                    );

                    if (!string.IsNullOrEmpty(resolutionJson))
                    {
                        ParseResolutionInfo(resolutionJson);
                    }

                    _initialized = true;
                    Console.WriteLine("[SystemInfoHelper] システム情報の取得が完了しました。");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[SystemInfoHelper] システム情報の取得に失敗しました: {ex.Message}");
                    SetFallbackValues();
                    _initialized = true;
                }
            });
        }

        private static void ParseComputerInfo(string json)
        {
            try
            {
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;

                // OS
                if (root.TryGetProperty("OsName", out var osName))
                {
                    _os = osName.GetString() ?? "Unknown OS";
                }

                // CPU（CsProcessors は配列）
                if (root.TryGetProperty("CsProcessors", out var processors) &&
                    processors.ValueKind == JsonValueKind.Array &&
                    processors.GetArrayLength() > 0)
                {
                    var firstProcessor = processors[0];
                    if (firstProcessor.TryGetProperty("Name", out var cpuName))
                    {
                        _cpu = cpuName.GetString() ?? "Unknown CPU";
                    }
                }

                // メモリ（CsTotalPhysicalMemory はバイト単位）
                if (root.TryGetProperty("CsTotalPhysicalMemory", out var totalMem))
                {
                    var totalBytes = totalMem.GetDouble();
                    var totalGB = Math.Round(totalBytes / (1024.0 * 1024.0 * 1024.0), 2);
                    _memory = $"{totalGB} GB RAM";
                }

                // 利用可能メモリ（OsFreePhysicalMemory は KB 単位）
                if (root.TryGetProperty("OsFreePhysicalMemory", out var freeMem))
                {
                    var freeKB = freeMem.GetDouble();
                    var freeGB = Math.Round(freeKB / (1024.0 * 1024.0), 2);
                    _availableMemory = $"{freeGB} GB RAM は使用可能です";
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[SystemInfoHelper] ComputerInfo のパースに失敗: {ex.Message}");
            }
        }

        private static void ParseGpuInfo(string json)
        {
            try
            {
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;

                if (root.TryGetProperty("Name", out var gpuName))
                {
                    _gpu = gpuName.GetString() ?? "Unknown GPU";
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[SystemInfoHelper] GPU 情報のパースに失敗: {ex.Message}");
            }
        }

        private static void ParseResolutionInfo(string json)
        {
            try
            {
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;

                if (root.TryGetProperty("Width", out var width) && root.TryGetProperty("Height", out var height))
                {
                    _resolution = $"{width.GetInt32()} x {height.GetInt32()}";
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[SystemInfoHelper] 解像度情報のパースに失敗: {ex.Message}");
            }
        }

        private static void SetFallbackValues()
        {
            _os = "Unknown OS";
            _cpu = "Unknown CPU";
            _memory = "Unknown Memory";
            _availableMemory = "";
            _gpu = "Unknown GPU";
            _resolution = "Unknown Resolution";
        }

        /// <summary>
        /// PowerShell コマンドを実行して標準出力を返します。
        /// </summary>
        private static async Task<string> RunPowerShellAsync(string command)
        {
            var psi = new ProcessStartInfo
            {
                FileName = "powershell",
                Arguments = $"-NoProfile -NonInteractive -Command \"{command}\"",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = Process.Start(psi);
            if (process == null) return "";

            var output = await process.StandardOutput.ReadToEndAsync();
            await process.WaitForExitAsync();

            return output.Trim();
        }

        public static string GetOS() => _os;
        public static string GetCPU() => _cpu;
        public static string GetMemory() => _memory;
        public static string GetAvailableMemory() => _availableMemory;
        public static string GetGPU() => _gpu;
        public static string GetResolution() => _resolution;

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
