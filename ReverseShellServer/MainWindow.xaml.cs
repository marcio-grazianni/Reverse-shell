using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Management.Automation.Runspaces;
using System.Management.Automation;
using System.Net.Sockets;
using System.Security;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Data.SqlTypes;
using ReverseShellServer.Models;

namespace ReverseShellServer
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public int connectedClients;
        List<RemoteMachine> remoteMachines;
        List<Runspace> runspaces;
        public MainWindow()
        {
            InitializeComponent();
            remoteMachines = new List<RemoteMachine>();
           runspaces = new List<Runspace>();
            Loaded += MainWindow_Loaded;
           
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            initializeComputers();
        }

        private void initializeComputers()
        {
            remoteMachines.Add(new RemoteMachine { ip = "192.168.100.145", username = "Admin", password = "Password123!" });
            remoteMachines.Add(new RemoteMachine { ip = "192.168.100.157", username = "Admin", password = "Password123!" });
            OutputConsole.AppendText("\nConnecting Computers\n");
            foreach (var comp in remoteMachines)
            {
                new Thread(() =>
                {
                    ConnectRemote(comp);
        }).Start();
            }
        }

       
        private void InputConsole_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                InputConsole.Focus();
            }
        }

        private void InputConsole_TextChanged(object sender, TextChangedEventArgs e)
        {

        }

        private void RunCommands(Runspace runspace)
        {
            this.Dispatcher.Invoke(() => {
                OutputConsole.AppendText("\n\nResults for " + runspace.ConnectionInfo.ComputerName + "\n");
            });
            using (PowerShell ps = PowerShell.Create())
            {
                ps.Runspace = runspace;
                this.Dispatcher.Invoke(() => {
                    ps.AddScript(InputConsole.Text);
                });
              
                StringBuilder sb = new StringBuilder();
                try
                {
                    var results = ps.Invoke();
                    foreach (var x in results)
                    {
                        this.Dispatcher.Invoke(() => {
                            OutputConsole.AppendText(x.ToString() + "\n");
                        });
                        
                    }
                }
                catch (Exception e)
                {
                    this.Dispatcher.Invoke(() => {
                        OutputConsole.AppendText("The following problem occured when running the script \n");
                        OutputConsole.AppendText(e.Message+"\n");
                    });
                }
            }
            //runspace.Close();
            Console.ReadLine();
        }

        private void SendButton_Click(object sender, RoutedEventArgs e)
        {
            if (String.IsNullOrEmpty(InputConsole.Text))
            {
                OutputConsole.AppendText("\n\nYou must provide a command!!\n\n");
                return;
            }
            //RemoteMachine remoteMachine = new RemoteMachine {ip = "192.168.100.149", username = "Admin",password="Password123!" };
            //remoteMachines.Add(remoteMachine);
            //ClientList.Items.Add(remoteMachine);
            foreach (var runSpace in runspaces)
            {
                new Thread(() =>
                {
                    RunCommands(runSpace);
                }).Start();
              
            }
        }
        public void ConnectRemote(RemoteMachine remoteMachine)
        {
            try
            {
                var securestring = new SecureString();
                foreach (Char c in remoteMachine.password)
                {
                    securestring.AppendChar(c);
                }
                PSCredential creds = new PSCredential(remoteMachine.username, securestring);
                WSManConnectionInfo connectionInfo = new WSManConnectionInfo();

                connectionInfo.ComputerName = remoteMachine.ip;// "192.168.100.149";
                connectionInfo.Credential = creds;
                Runspace runspace = RunspaceFactory.CreateRunspace(connectionInfo);
                runspace.Open();
                runspaces.Add(runspace);
                
             
                this.Dispatcher.Invoke(() => {
                    ClientList.Items.Add(remoteMachine);
                    ClientNumLabel.Content = "Connected Clients : " + runspaces.Count;
                    OutputConsole.AppendText("\nNow connected to " + remoteMachine.ip + "\n");
                });
            }
            catch(Exception e)
            {
                this.Dispatcher.Invoke(() => {
                    OutputConsole.AppendText("\nCould not connect to " + remoteMachine.ip + " for the following reason\n");
                    OutputConsole.AppendText(e.Message + "\n");
                });
                
            }
            
        }
    }
}
