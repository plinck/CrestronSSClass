using System;
using Crestron.SimplSharp;                          	// For Basic SIMPL# Classes
using Crestron.SimplSharpPro;                       	// For Basic SIMPL#Pro classes
using Crestron.SimplSharpPro.CrestronThread;        	// For Threading
using Crestron.SimplSharpPro.Diagnostics;		    	// For System Monitor Access
using Crestron.SimplSharpPro.DeviceSupport;         	// For Generic Device Support
using SSHClient;                                        // Adding the reference to the .clz or .dll

namespace SSHClientProcessorCode
{
    public class ControlSystem : CrestronControlSystem
    {
        #region Global Variables
        private SSHClientDevice mySSHClientDevice;
        private string  sshHost = "127.0.0.1",
                        sshUser = "Crestron",
                        sshPass = "";
        private ushort  sshPort = 22;
        #endregion
        public ControlSystem()
            : base()
        {
            try
            {
                Thread.MaxNumberOfUserThreads = 20;

                //Subscribe to the controller events (System, Program, and Ethernet)
                CrestronEnvironment.SystemEventHandler += new SystemEventHandler(ControlSystem_ControllerSystemEventHandler);
                CrestronEnvironment.ProgramStatusEventHandler += new ProgramStatusEventHandler(ControlSystem_ControllerProgramEventHandler);
                CrestronEnvironment.EthernetEventHandler += new EthernetEventHandler(ControlSystem_ControllerEthernetEventHandler);

                //Console Commands to Trigger Connect and send command
                CrestronConsole.AddNewConsoleCommand(ConnectSSH, "SSHConnect", "Connect to the SSH server", ConsoleAccessLevelEnum.AccessOperator);
                CrestronConsole.AddNewConsoleCommand(SendSSHCommand, "SSHCommand", "Send a string as a command to the SSH server", ConsoleAccessLevelEnum.AccessOperator);

                mySSHClientDevice = new SSHClientDevice();
                mySSHClientDevice.myEventToSsp += new CommandEventHandler(mySSHClientDevice_myEventToSsp);
            }
            catch (Exception e)
            {
                ErrorLog.Error("Error in the constructor: {0}", e.Message);
            }
        }

        //SSH Methods
        void mySSHClientDevice_myEventToSsp(string stringVal)
        {
            CrestronConsole.PrintLine(stringVal);
        }
        void ConnectSSH(string unused)
        {
            if (mySSHClientDevice.Connect(sshHost, sshPort, sshUser, sshPass) == 1)
                CrestronConsole.ConsoleCommandResponse("Connection Successful");
            else
                CrestronConsole.ConsoleCommandResponse("Connection Failed");
        }
        void SendSSHCommand(string cmd)
        {
            if (mySSHClientDevice.SendCommand(cmd) != 1)
                CrestronConsole.ConsoleCommandResponse("Command Failed");
        }

        //Default Logic
        public override void InitializeSystem()
        {
            try
            {

            }
            catch (Exception e)
            {
                ErrorLog.Error("Error in InitializeSystem: {0}", e.Message);
            }
        }
        void ControlSystem_ControllerEthernetEventHandler(EthernetEventArgs ethernetEventArgs)
        {
            switch (ethernetEventArgs.EthernetEventType)
            {//Determine the event type Link Up or Link Down
                case (eEthernetEventType.LinkDown):
                    //Next need to determine which adapter the event is for. 
                    //LAN is the adapter is the port connected to external networks.
                    if (ethernetEventArgs.EthernetAdapter == EthernetAdapterType.EthernetLANAdapter)
                    {
                        //
                    }
                    break;
                case (eEthernetEventType.LinkUp):
                    if (ethernetEventArgs.EthernetAdapter == EthernetAdapterType.EthernetLANAdapter)
                    {

                    }
                    break;
            }
        }
        void ControlSystem_ControllerProgramEventHandler(eProgramStatusEventType programStatusEventType)
        {
            switch (programStatusEventType)
            {
                case (eProgramStatusEventType.Paused):
                    //The program has been paused.  Pause all user threads/timers as needed.
                    break;
                case (eProgramStatusEventType.Resumed):
                    //The program has been resumed. Resume all the user threads/timers as needed.
                    break;
                case (eProgramStatusEventType.Stopping):
                    //The program has been stopped.
                    //Close all threads. 
                    //Shutdown all Client/Servers in the system.
                    //General cleanup.
                    //Unsubscribe to all System Monitor events
                    break;
            }

        }
        void ControlSystem_ControllerSystemEventHandler(eSystemEventType systemEventType)
        {
            switch (systemEventType)
            {
                case (eSystemEventType.DiskInserted):
                    //Removable media was detected on the system
                    break;
                case (eSystemEventType.DiskRemoved):
                    //Removable media was detached from the system
                    break;
                case (eSystemEventType.Rebooting):
                    //The system is rebooting. 
                    //Very limited time to preform clean up and save any settings to disk.
                    break;
            }

        }
    }
}