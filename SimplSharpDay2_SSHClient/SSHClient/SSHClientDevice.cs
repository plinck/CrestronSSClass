using System;
using System.Text;
using System.Text.RegularExpressions;
using Crestron.SimplSharp;                          				// For Basic SIMPL# Classes
using Crestron.SimplSharp.Ssh;

namespace SSHClient
{
    public delegate void CommandEventHandler(string stringVal);

    public class SSHClientDevice
    {
        #region Global Variables
        private SshClient myClient;
        private ShellStream myStream;
        private CrestronQueue<String> myQueue = new CrestronQueue<string>();
        public event CommandEventHandler myEventToSsp = delegate { }; // This ensures there's at least one delegate so it's never null
        #endregion
        //Default Constructor
        public SSHClientDevice()
        {
            CrestronInvoke.BeginInvoke(ProcessResponses);
        }
        public ushort Connect(String Host, ushort Port, String UserName, String Password)
        {
            try
            {
                if (myClient != null && myClient.IsConnected)
                    return 0;
                myClient = new SshClient(Host, (int)Port, UserName, Password);
                myClient.Connect();
                // Create a new shellstream
                try
                {
                    myStream = myClient.CreateShellStream("terminal", 80, 24, 800, 600, 1024);
                    myStream.DataReceived += new EventHandler<Crestron.SimplSharp.Ssh.Common.ShellDataEventArgs>(myStream_DataReceived);
                }
                catch (Exception e)
                {
                    ErrorLog.Exception("Exception creating stream", e);
                }
                return 1;
            }
            catch (Exception ex)
            {
                ErrorLog.Error(String.Format("Error Connecting: {0}", ex.Message));
                return 0;
            }
        }
        public ushort SendCommand(String Command)
        {
            try
            {
                SshCommand myCmd = myClient.RunCommand(Command);
                myQueue.Enqueue(myCmd.Execute());
                return 1;
            }
            catch (Exception e)
            {
                ErrorLog.Error("Error Sending Command: {0}", e.Message);
                return 0;
            }
        }
        void myStream_DataReceived(object sender, Crestron.SimplSharp.Ssh.Common.ShellDataEventArgs e)
        {
            var stream = (ShellStream)sender;
            // Loop as long as there is data on the stream
            while (stream.DataAvailable)
            {
                // Read the stream and pass it to SSP
                myEventToSsp(stream.Read());
            }
        }
        private void ProcessResponses(object o)
        {
            String str;
            while (true)
            {
                try
                {
                    str = myQueue.Dequeue();
                    //Send Command response to Simpl Sharp Pro
                    myEventToSsp(str);
                    parseData(str);
                }
                catch (Exception e)
                {
                    ErrorLog.Error("Process Response Error: {0}", e.Message);
                }
            }
        }
        public void parseData(String str)
        {
            String matchString = @"(^.*$)*DHCP.*:\ ON.*$\n(^.*$\n)*^.*IP\ Address.*:\ (?<ipaddress>.*$).*\n(^.*$\n)*^.*DHCP\ Server.*:\ (?<DHCPServer>.*$).*\n(^.*$\n)*^.*Lease\ Expires On.*:\ (?<LeaseDate>.*$\n)";
            RegexOptions myRegexOptions = new RegexOptions();
            myRegexOptions = RegexOptions.IgnoreCase | RegexOptions.Multiline;
            Regex myRegex = new Regex(matchString, myRegexOptions);
            MatchCollection myCollection;
            myCollection = myRegex.Matches(str);
            foreach (Match m in myCollection)
            {
                // Altered print to single parameter to work with event
                myEventToSsp("\r\nIP Address:          " + m.Groups["ipaddress"].Value);
                myEventToSsp("DHCP Server:         " + m.Groups["DHCPServer"].Value);
                myEventToSsp("Lease Expiration:    " + m.Groups["LeaseDate"].Value);
            }
        }
    }
}
