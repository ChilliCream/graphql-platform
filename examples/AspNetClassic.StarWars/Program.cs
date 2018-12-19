using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Microsoft.Owin.Hosting;

namespace StarWars
{
    public static class Program
    {
        public static void Main(string[] args)
        {
            var baseUrl = "http://localhost:5678/";
            var options = new StartOptions(baseUrl);

            using (WebApp.Start<Startup>(options))
            {
                Console.WriteLine($"The server is up and running under \"{baseUrl}\"...");
                Console.WriteLine("Press any key to exit...");
                OpenUrl(baseUrl);
                Console.ReadKey();
            }
        }

        private static void OpenUrl(string url)
        {
            try
            {
                Process.Start(url);
            }
            catch
            {
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    url = url.Replace("&", "^&");
                    Process.Start(new ProcessStartInfo("cmd", $"/c start {url}") { CreateNoWindow = true });
                }
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                {
                    Process.Start("xdg-open", url);
                }
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                {
                    Process.Start("open", url);
                }
                else
                {
                    throw;
                }
            }
        }
    }
}
