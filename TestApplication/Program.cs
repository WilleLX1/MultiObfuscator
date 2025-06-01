using System;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace TestApplication
{
    class C_OvLIrI
    {
        static async Task Main(string[] P_xROrPd)
        {
            string V_gHXgcK = M_mgbkf("MTI3Lg==") + M_mgbkf("MC4wLjE=");
            const int V_bJnHVZ = 1234;
            Console.WriteLine($"[*] Connecting to {V_gHXgcK}:{V_bJnHVZ}…");
            using var V_EDiXpz = new TcpClient();
            try
            {
                await V_EDiXpz.ConnectAsync(V_gHXgcK, V_bJnHVZ);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[!] Could not connect: {ex.Message}");
                return;
            }

            Console.WriteLine(M_mgbkf("WytdIENvbm5lY3RlZC4gWW91IGNhbiBubw==") + M_mgbkf("dyB0eXBlIGFuZCBzZW5kIG1lc3NhZ2VzLg=="));
            Console.WriteLine("    Type `/exit` and press Enter to quit.\n");
            using NetworkStream V_JdkMbG = V_EDiXpz.GetStream();
            var V_wfDxpt = new CancellationTokenSource();
            // Task 1: Read from server → print to console
            var V_bPwfVC = Task.Run(async () =>
            {
                var V_Yoxofd = new byte[4096];
                while (!V_wfDxpt.Token.IsCancellationRequested)
                {
                    int V_EuIdEz;
                    try
                    {
                        V_EuIdEz = await V_JdkMbG.ReadAsync(V_Yoxofd, 0, V_Yoxofd.Length, V_wfDxpt.Token);
                    }
                    catch
                    {
                        // Connection closed or error
                        break;
                    }

                    if (V_EuIdEz == 0)
                        break; // peer closed
                    string V_LuGxfv = Encoding.UTF8.GetString(V_Yoxofd, 0, V_EuIdEz);
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.Write(V_LuGxfv);
                    Console.ResetColor();
                }
            }, V_wfDxpt.Token);
            // Task 2: Read from console → send to server
            var V_DPZlAi = Task.Run(async () =>
            {
                while (!V_wfDxpt.Token.IsCancellationRequested)
                {
                    string? V_aSFyMJ = Console.ReadLine();
                    if (V_aSFyMJ == null || V_aSFyMJ.Trim() == M_mgbkf("L2U=") + M_mgbkf("eGl0"))
                    {
                        // user wants to quit
                        V_wfDxpt.Cancel();
                        break;
                    }

                    byte[] V_KvYBGm = Encoding.UTF8.GetBytes(V_aSFyMJ + "\n");
                    try
                    {
                        await V_JdkMbG.WriteAsync(V_KvYBGm, 0, V_KvYBGm.Length, V_wfDxpt.Token);
                    }
                    catch
                    {
                        // broken pipe or connection closed
                        V_wfDxpt.Cancel();
                        break;
                    }
                }
            }, V_wfDxpt.Token);
            // Wait for either side to finish
            await Task.WhenAny(V_bPwfVC, V_DPZlAi);
            // Signal cancellation and close
            V_wfDxpt.Cancel();
            try
            {
                V_EDiXpz.Close();
            }
            catch
            {
            }

            Console.WriteLine("\n[*] Disconnected.");
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