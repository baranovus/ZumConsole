﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Threading;
using System.Timers;
using System.Net;
using System.Net.Sockets;
using ZumConsole.Properties;
//using System.Diagnostics;             
//using System.Runtime.InteropServices; // for DllImportAttribute


namespace ZumConsole
{

    public partial class Form1 : Form
    {
        System.IO.StreamWriter logfile;
        System.IO.StreamReader scriptfile;
        String Hostname = String.Empty;
        String log_file_path_str = String.Empty;
        String script_file_path_str = String.Empty;
        String Timestamp = String.Empty;
        String scriptline;
        String inputline = String.Empty;
        static String port_str = String.Empty;
        Int32 PortNumber = 41795;
        TcpClient client;
        NetworkStream tcp_stream;
        delegate void SetTextCallback(string text);
        NetworkConn net_conn = new NetworkConn();
        private String tcpresponse = String.Empty;
        private byte[] txbuffer = new byte[2000];           //transmit buffer for tcp
        private byte[] rxbuffer = new byte[5000];           //receive buffer for tcp
        bool script_file_opened = false;
        bool tcp_connected = false;
        bool log_file_created = false;
        bool append_crlf = true;
        bool save_ascii = true;
        public Form1()
        {
            InitializeComponent();
            ZumConsole.Properties.Settings.Default.Reload();
            PortNumber = ZumConsole.Properties.Settings.Default.Port;
            log_file_path_str = ZumConsole.Properties.Settings.Default.LogFile;
            script_file_path_str = "";
 
            this.ConsOutput.Text = "";
            backgroundWorker1.RunWorkerCompleted += new RunWorkerCompletedEventHandler(backgroundWorker1_RunWorkerCompleted);
            backgroundWorker1.ProgressChanged += new ProgressChangedEventHandler(backgroundWorker1_ProgressChanged);
            this.backgroundWorker1.WorkerReportsProgress = true;
            this.backgroundWorker1.WorkerSupportsCancellation = true;
            saveFileDialog1.Filter = "Text file|*.txt";
            saveFileDialog1.Title = "Save Log File";
            ascii.Checked = true;
            hex.Checked = false;
            

  
        }
        private void SetDiagText(string text)
        {
            // InvokeRequired required compares the thread ID of the
            // calling thread to the thread ID of the creating thread. 
            // If these threads are different, it returns true.
            if (this.DiagLabel.InvokeRequired)
            {
                SetTextCallback d = new SetTextCallback(SetDiagText);
                this.Invoke(d, new object[] { text });
            }
            else
            {
                this.DiagLabel.Text = text;
            }
       }

        private void SetHostNameText(string text)
        {
            // InvokeRequired required compares the thread ID of the
            // calling thread to the thread ID of the creating thread. 
            // If these threads are different, it returns true.
            if (this.HostNameLabel.InvokeRequired)
            {
                SetTextCallback d = new SetTextCallback(SetHostNameText);
                this.Invoke(d, new object[] { text });
            }
            else
            {
                this.HostNameLabel.Text = text;
            }
        }
        private void SetConsoleText(string text)
        {
            // InvokeRequired required compares the thread ID of the
            // calling thread to the thread ID of the creating thread. 
            // If these threads are different, it returns true.
            if (this.ConsOutput.InvokeRequired)
            {
                SetTextCallback d = new SetTextCallback(SetConsoleText);
                this.Invoke(d, new object[] { text });
            }
            else
            {
                this.ConsOutput.Text += text;
                this.ConsOutput.SelectionStart = this.ConsOutput.TextLength;
                this.ConsOutput.ScrollToCaret();
            }

        }
        private void ClearConsole()
        {
            // InvokeRequired required compares the thread ID of the
            // calling thread to the thread ID of the creating thread. 
            // If these threads are different, it returns true.
            if (this.ConsOutput.InvokeRequired)
            {
                SetTextCallback d = new SetTextCallback(SetConsoleText);
                this.Invoke(d, new object[] { "" });
            }
            else
            {
                this.ConsOutput.Text = "";
            }
        }

        private string ByteArrayToHexString(byte[] data, ushort index, int length)
        {
            ushort i = index;
            StringBuilder sb = new StringBuilder(data.Length * 3);
            while (i < length)
            {
                sb.Append(Convert.ToString(data[i], 16).PadLeft(2, '0'));
                i++;
            }
            return sb.ToString().ToUpper();
        }
        
        private void FileOpen_Click(object sender, EventArgs e)
        {
            string ScriptFile = String.Empty;
            bool ChkFile = false;
  
            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                ScriptFile = openFileDialog1.FileName;
                ChkFile = true;
            }
            if (ChkFile)
            {
                System.IO.FileInfo fInfo = new System.IO.FileInfo(ScriptFile);
                try
                {
                    scriptfile = new System.IO.StreamReader(ScriptFile);
                    script_file_opened = true;
                 }
                catch (Exception ex)
                {
                    SetDiagText( "Could not open script file" +ex);
                    script_file_opened = false;
                }
 
            }
        }
        private void Log_button_Click(object sender, EventArgs e)
        {
            string logpath = String.Empty;
            string timesatamp = DateTime.Now.ToString("yyyy-MM-dd-hh-mm-ss");
            // Set the properties on SaveFileDialog1 so the user is 
            // prompted to create the file if it doesn't exist 
            // or overwrite the file if it does exist.
            saveFileDialog1.CreatePrompt = true;
            saveFileDialog1.OverwritePrompt = true;
            // Set the file name, set the type filter
            // to text files, and set the initial directory to the 
            // MyDocuments folder.
            saveFileDialog1.FileName = "log" + timesatamp;//DateTime.Now.ToString(;
            saveFileDialog1.Filter =   "Text files (*.txt)|*.txt|All files (*.*)|*.*";
            if (log_file_path_str == String.Empty)
            {
                saveFileDialog1.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            }
            else
            {
                saveFileDialog1.InitialDirectory = log_file_path_str;
            }
            // Call ShowDialog and check for a return value of DialogResult.OK,
            // which indicates that the file was saved. 
            try
            {
                DialogResult result = saveFileDialog1.ShowDialog();

                if (result == DialogResult.OK)
                {
                    Stream logstream = saveFileDialog1.OpenFile();
                    logpath = saveFileDialog1.FileName;
                    System.IO.FileInfo fInfo = new System.IO.FileInfo(logpath);
                    ZumConsole.Properties.Settings.Default.LogFile = logpath;  //save log file path to settings
                    ZumConsole.Properties.Settings.Default.Save();
                    logfile = new System.IO.StreamWriter(logstream);
                    log_file_created = true;
                }
            }
            catch (Exception ex)
            {
                SetDiagText("Could not create log file" + ex);
                log_file_created = false;
            }
 
        }
        private void Form1_Load(object sender, EventArgs e)
        {

        }

        private void backgroundWorker1_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            SetDiagText("Completed");
            Start.Enabled = true;
            Stop.Enabled = false;
        }
        private void backgroundWorker1_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
             //            this.resultLabel.Text = e.ProgressPercentage.ToString();
        }
        private void backgroundWorker1_DoWork(object sender, DoWorkEventArgs e)
        {
            BackgroundWorker worker = sender as BackgroundWorker;

            if (worker.CancellationPending == true)
            {
                e.Cancel = true;

            }
            else
            {
                e.Result = ReadLinesFromScriptFile(worker, e);
            }
         }

        private int ReadLinesFromScriptFile(BackgroundWorker worker, DoWorkEventArgs e)
         {
            for (; ; )
            {
                if (worker.CancellationPending)
                {
                    e.Cancel = true;
                    break;
                }
                else
                {
                    scriptline = scriptfile.ReadLine();
                    int rem = scriptline.IndexOf("//");             //cutting off comments
                    if (rem >= 0)
                    {
                        scriptline = scriptline.Substring(0, rem);
                    }
                    scriptline = scriptline.Trim();
                    if (scriptline.Length > 0)
                    {
                        scriptline += "\n\r";
                        SendStringToTCP(ref scriptline, ref tcpresponse, 500);
                        Thread.Sleep(2000);
                    }
                    if (scriptfile.EndOfStream)                     //if it is the end of the file
                    {                                               //start from the beginning
                        scriptfile.BaseStream.Position = 0;
                    }
                }
            }
            return 0;
        }
        private int WaitForResponseFromTCP()
        {
            int nTry = 1000;
            Int32 res = -1;
            while (!tcp_stream.DataAvailable)
            {
                Thread.Sleep(5);
                if (--nTry == 0)
                {
                    break;
                }
            }
            if (nTry > 0)
            {
                res = 0;
            }
            return res;
        } 
        private void SendStringToTCP(ref String s_tx, ref String s_rx, int timeout = 150/*miliseconds*/)
        {
            if ((s_rx == null) || (s_tx == null)) { return; }
            int index = 0;
            int tcp_rx_bytes = 0;
            s_rx = String.Empty;
            Timestamp = DateTime.Now.ToString();
            if (log_file_created)
            {
                logfile.WriteLine(Timestamp);   //log timestamp and console command
//                logfile.WriteLine(s_tx);
            }
            Byte[] data = System.Text.Encoding.ASCII.GetBytes(s_tx);    //send console command
            try
            {
                tcp_stream.Write(data, 0, data.Length);                     //to TCP stream
            }
            catch (SocketException e6)
            {

                SetDiagText("Socket is busy" + e6);

            }
            catch(IOException e7)
            {
                SetDiagText("Socket is busy" + e7);
            }
            catch
            {

                 SetDiagText("Server is busy");
            }
            if (timeout > 0)                                            //if response is expected timeout is non-zero
            {
                Thread.Sleep(timeout);
                if (WaitForResponseFromTCP() == 0)      //if there is some response
                {
                    do
                    {
                        //tcp_rx_bytes += tcp_stream.Read(rxbuffer, index, rxbuffer.Length);
                        //index = tcp_rx_bytes;
                        //if (tcp_rx_bytes >= rxbuffer.Length) break;
                        tcp_rx_bytes = tcp_stream.Read(rxbuffer, index, rxbuffer.Length);
                        if (!save_ascii)
                        {
                            s_rx = ByteArrayToHexString(rxbuffer, 0, tcp_rx_bytes);
                        }
                        else
                        {
                            s_rx = System.Text.Encoding.ASCII.GetString(rxbuffer, 0, tcp_rx_bytes); //convert response to text
                        }
                        SetConsoleText(s_rx);
                        if (!save_ascii) { SetConsoleText("\r\n"); }
                        if (log_file_created)
                        {
                            logfile.WriteLine(s_rx);
                            if (!save_ascii) { logfile.WriteLine("\r\n"); }
                        }                      //and write to log file

                    }
                    while (tcp_stream.DataAvailable);
                   
                    //s_rx = System.Text.Encoding.ASCII.GetString(rxbuffer, 0, tcp_rx_bytes); //convert response to text
                    //SetConsoleText(s_rx);                                                   //print at the text window
                    //if (log_file_created) { logfile.WriteLine(s_rx); }                      //and write to log file
                    //Thread.Sleep(10);
                }
            }
        }
        
        private void Start_Click(object sender, EventArgs e)
        {
            if( (script_file_opened)&&(tcp_connected)&&(log_file_created))
            { 
                this.backgroundWorker1.RunWorkerAsync();
                Stop.Enabled = true;
                Start.Enabled = false;
            }
            else
            {
                SetDiagText("Script or log file are not open or TCP connection is not established");
            }
        }

        private void Stop_Click(object sender, EventArgs e)
        {
 
            if( (log_file_created)&&(!script_file_opened)&&(tcp_connected))
            {
                logfile.Close();
                log_file_created = false;
            }
            else
            {
                SetDiagText("Log file is not open or TCP connection is not established");
            }
        }

        private void StopScriptButton_Click(object sender, EventArgs e)
        {
            if ((script_file_opened) && (tcp_connected))
            {
                this.backgroundWorker1.CancelAsync();
                Stop.Enabled = false;
            }
        }

        private void hostname_TextChanged(object sender, EventArgs e)
        {
//            Hostname = hostname.Text;
        }

        private void Port_TextChanged(object sender, EventArgs e)
        {
            //port_str = Port.Text;
            //PortNumber = Convert.ToUInt16(port_str, 10); 
        }

        private void TCP_connect_button_Click(object sender, EventArgs e)
        {
            if ((String.IsNullOrEmpty(Hostname)) || (PortNumber == 0))
            {
                SetDiagText("No host name or Port defined");
                return;
            }

            try
            {
                    IPHostEntry hostInfo = Dns.GetHostEntry(Hostname);
                    client = new TcpClient(hostInfo.HostName, PortNumber);
 
             }
            catch (SocketException e4)
            {
                 SetDiagText("Failure to connect to TCP" + e4);
                 tcp_connected = false;
            }
            if (client.Connected)
            {
                tcp_stream = client.GetStream();
                SetDiagText("Connected to TCP");
                
                tcp_connected = true;
                ZumConsole.Properties.Settings.Default.Hostname = Hostname;
                ZumConsole.Properties.Settings.Default.Port = PortNumber;
                ZumConsole.Properties.Settings.Default.Save();

            }
        }

        
        
        private void Form1Closing(object sender, FormClosingEventArgs e)
        {
            // The form is closing, save the user's preferences
            // Close everything.
            ZumConsole.Properties.Settings.Default.Hostname = Hostname;
            ZumConsole.Properties.Settings.Default.Port = PortNumber;
            ZumConsole.Properties.Settings.Default.LogFile = log_file_path_str;
        }

  
        private void ConnSettings_Click_1(object sender, EventArgs e)
        {
            Form2 form2 = new Form2();
            // Add an event handler to update this form
            // when the ID form is updated (when IdentityUpdated fires).
            form2.NetParametersUpdated += new Form2.NetUpdateHandler(Net_Settings_ButtonClicked);
            form2.Show();
        }
        // handles the event from form2
        private void Net_Settings_ButtonClicked(object sender, NetUpdateEventArgs e)
        {
            // update the forms values from the event args
            Hostname =  e.HostName;
            port_str = e.PortName;
            PortNumber = Convert.ToUInt16(port_str, 10);
            tcp_stream = net_conn.MakeConnection(Hostname, (int)PortNumber);
            tcp_connected = net_conn.GetTcpConnected();
            if(tcp_connected)
            {
                SetHostNameText(Hostname + ":" + PortNumber);
            }
            else
            {
                SetHostNameText("Failed to connect to TCP");
            }
            //if ((String.IsNullOrEmpty(Hostname)) || (PortNumber == 0))
            //{
            //    SetDiagText("No host name or Port defined");
            //    return;
            //}
            //try
            //{
            //    if (IsValidIp(Hostname))
            //    {
            //        IPAddress ip_address = IPAddress.Parse(Hostname);
            //        IPEndPoint ipLocalEndPoint = new IPEndPoint(ip_address, PortNumber);
            //        client = new TcpClient();
            //        client.Connect(ipLocalEndPoint);

            //    }
            //    else
            //    {
            //        IPHostEntry hostInfo = Dns.GetHostEntry(Hostname);
            //        client = new TcpClient(hostInfo.HostName, PortNumber);
            //        if (client.Connected) { tcp_connected = true; }
            //    }
 
            //}
            //catch (SocketException e4)
            //{
            //    //               DiagLabel.Text = "SocketException: " + e4;
            //    SetDiagText("Failed to connect to TCP");
            //    tcp_connected = false;
            //}
            //if (tcp_connected)
            //{
            //    try
            //    {
            //        tcp_stream = client.GetStream();
            //        SetHostNameText(Hostname + ":" + PortNumber);
            //        tcp_connected = true;
            //        ZumConsole.Properties.Settings.Default.Hostname = Hostname;
            //        ZumConsole.Properties.Settings.Default.Port = PortNumber;
            //        ZumConsole.Properties.Settings.Default.Save();
            //    }
            //    catch (SocketException e5)
            //    {
            //        SetDiagText("Failure to assign TCP steam" + e5);
            //        tcp_connected = false;
            //    }
            //}

        }

        private bool IsValidIp(string addr)
        {
            IPAddress ip;
            bool valid = !string.IsNullOrEmpty(addr) && IPAddress.TryParse(addr, out ip);
            return valid;
        }

        private void textBox1_TextChanged(object sender, EventArgs e)
        {
            inputline = textBox1.Text;
            int crlf1 = inputline.IndexOf("\n\r");
            int crlf2 = inputline.IndexOf("\r\n");            
            if (crlf1 >= 0)
            {
                inputline = inputline.Substring(0, crlf1);
            }
            if (crlf2 >= 0)
            {
                inputline = inputline.Substring(0, crlf2);
            }
        }

        private void SensConsCommand_Click(object sender, EventArgs e)
        {
            if (tcp_connected)
            {
                if (append_crlf)
                { 
                    inputline += "\n\r";
                }
                SendStringToTCP(ref inputline, ref tcpresponse, 500);
            }
        }

        private void AppendCRLF_CheckedChanged(object sender, EventArgs e)
        {
            if(AppendCRLF.Checked)
            {
                append_crlf = true;
            }
            else
            {
                append_crlf = false;
            }
        }

        private void ClearScreen_Click(object sender, EventArgs e)
        {
            ClearConsole();
        }

        private void ascii_CheckedChanged(object sender, EventArgs e)
        {
            if(ascii.Checked)
            {
                save_ascii = true;
                hex.Checked = false;
            }
            else
            {
                save_ascii = false;
            }
        }

        private void hex_CheckedChanged(object sender, EventArgs e)
        {
            if (hex.Checked)
            {
                save_ascii = false;
                ascii.Checked = false;
            }
            else
            {
                save_ascii = true;
            }

        }

 
 
    }
}
