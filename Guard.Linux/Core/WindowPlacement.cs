using System.Text.Json;
using Avalonia;
using Avalonia.Controls;
using Guard.Core;

namespace Guard.Linux.Core
{
    public static class WindowPlacement
    {
        private class Placement
        {
            public int X { get; set; }
            public int Y { get; set; }
            public double Width { get; set; }
            public double Height { get; set; }
            public bool Maximized { get; set; }
        }

        private static string GetFilePath() =>
            Path.Combine(InstallationContext.GetAppDataFolderPath(), "window-placement");

        public static void SavePlacement(Window window)
        {
            try
            {
                Placement placement = new()
                {
                    X = window.Position.X,
                    Y = window.Position.Y,
                    Width = window.Width,
                    Height = window.Height,
                    Maximized = window.WindowState == WindowState.Maximized,
                };
                File.WriteAllText(GetFilePath(), JsonSerializer.Serialize(placement));
            }
            catch (Exception e)
            {
                Log.Logger.Error("Couldn't save position for window: {0}", e.Message);
            }
        }

        public static void ApplyPlacement(Window window)
        {
            try
            {
                string path = GetFilePath();
                if (!File.Exists(path))
                {
                    return;
                }
                Placement? placement = JsonSerializer.Deserialize<Placement>(File.ReadAllText(path));
                if (placement == null || placement.Width < window.MinWidth || placement.Height < window.MinHeight)
                {
                    return;
                }
                window.WindowStartupLocation = WindowStartupLocation.Manual;
                window.Width = placement.Width;
                window.Height = placement.Height;
                window.Position = new PixelPoint(Math.Max(0, placement.X), Math.Max(0, placement.Y));
                if (placement.Maximized)
                {
                    window.WindowState = WindowState.Maximized;
                }
            }
            catch (Exception e)
            {
                Log.Logger.Error("Couldn't apply position for window: {0}", e.Message);
            }
        }
    }
}
