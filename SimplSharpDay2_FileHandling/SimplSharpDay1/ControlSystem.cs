using System;
using Crestron.SimplSharp;                          	// For Basic SIMPL# Classes
using Crestron.SimplSharpPro;                       	// For Basic SIMPL#Pro classes
using Crestron.SimplSharpPro.CrestronThread;        	// For Threading
using Crestron.SimplSharpPro.Diagnostics;		    	// For System Monitor Access
using Crestron.SimplSharpPro.DeviceSupport;         	// For Generic Device Support
using Crestron.SimplSharpPro.Keypads;                   // For the Keypad (need to add the reference)
using Crestron.SimplSharp.CrestronIO;                   // For Accessing Files
using Crestron.SimplSharp.Net.Http;                     // For talking over Http
using Crestron.SimplSharp.Ssh;                          // For communication over SSH

namespace SimplSharpDay2
{
    public class ControlSystem : CrestronControlSystem
    {
        private C2nCbdP myKeypad; //Keypad accessible to methods within the class
        private FileControls mFC; //Accessing the new class we made
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
                CrestronConsole.AddNewConsoleCommand(StringToUpper, "toUpper", "converts string to upper case", ConsoleAccessLevelEnum.AccessOperator);
                #region Keypad Static
                //Define Keypad Statically
                if (this.SupportsCresnet)
                {
                    myKeypad = new C2nCbdP(0x25, this);
                    myKeypad.ButtonStateChange += new ButtonEventHandler(myKeypad_ButtonStateChange);
                    if (myKeypad.Register() != eDeviceRegistrationUnRegistrationResponse.Success)
                        ErrorLog.Error("Error Registering Keypad: {0}", myKeypad.RegistrationFailureReason);
                }
                #endregion

                mFC = new FileControls(); // Instanciate the Class for File Reading

            }
            catch (Exception e)
            {
                ErrorLog.Error("Error in the constructor: {0}", e.Message);
            }
        }

        void myKeypad_ButtonStateChange(GenericBase device, ButtonEventArgs args)
        {
            switch (args.Button.Number)
            {
                case 1:
                    {
                        if (args.Button.State == eButtonState.Pressed)
                            if (mFC.OpenLocalFile(@"\NVRAM\Books.xml") == 0)
                                CrestronConsole.PrintLine("Error Loading Books.xml");
                        break;
                    }
                case 2:
                    {
                        if (args.Button.State == eButtonState.Pressed)
                            if (mFC.OpenHTTPFile(@"http://textfiles.com/computers/1pt4mb.inf") == 0)
                                CrestronConsole.PrintLine("Error Opening HTTP File");
                        break;
                    }
                case 3:
                    {
                        if (args.Button.State == eButtonState.Pressed)
                            if (mFC.OpenSFTPFile("Crestron", "", "127.0.0.1", @"/NVRAM/Books.xml") == 0)
                                CrestronConsole.PrintLine("Error Transferring File via sFTP");
                        break;
                    }
                default:
                    {
                        CrestronConsole.PrintLine("Keypad Button Triggered: {0}, {1}, {2}", device.ID, args.Button.Number, args.Button.State);
                        break;
                    }
            }
        }
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
        // My Console Command Delegate
        void StringToUpper(string response)
        {
            CrestronConsole.ConsoleCommandResponse("Converted to Caps: {0}", response.ToUpper());
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
    public class FileControls
    {
        private FileStream myStream;
        private StreamReader myReader;

        //Default Constructor
        public FileControls()
        {
        }

        public ushort OpenLocalFile(String strPath)
        {
            ushort returnvalue = 1;
            try
            {
                myStream = new FileStream(strPath, FileMode.Open);
            }
            catch (FileNotFoundException e)
            {
                ErrorLog.Error("FileNotFoundException: {0}", e.Message);
                return 0;
            }
            catch (IOException e)
            {
                ErrorLog.Error("IOException: {0}", e.Message);
                return 0;
            }
            catch (DirectoryNotFoundException e)
            {
                ErrorLog.Error("DirectoryNotFoundException: {0}", e.Message);
                return 0;
            }
            catch (PathTooLongException e)
            {
                ErrorLog.Error("PathTooLongException: {0}", e.Message);
                return 0;
            }
            catch (Exception e)
            {
                ErrorLog.Error("Exception: {0}", e.Message);
                return 0;
            }
            try
            {
                myReader = new StreamReader(myStream);
                while (!myReader.EndOfStream)
                {
                    CrestronConsole.PrintLine(myReader.ReadLine());
                }
            }
            catch (Exception e)
            {
                ErrorLog.Error(String.Format("Exception: {0}", e.Message));
                returnvalue = 0;
            }
            finally
            {
                myStream.Close();
            }
            return returnvalue;
        }
        public ushort OpenHTTPFile(String strURL)
        {
            ushort returnvalue = 1;
            HttpClient myClient = new HttpClient();
            try
            {
                string tempUrl = @"\nvram\httptmp.txt";
                myClient.FgetFile(strURL, tempUrl);
                OpenLocalFile(tempUrl);
            }
            catch (Exception e)
            {
                ErrorLog.Error(String.Format("Error Getting HTTP file: {0}", e.Message));
                returnvalue = 0;
            }
            return returnvalue;
        }
        public ushort OpenSFTPFile(String strUser, String strPassword, String strHost, String strPath)
        {
            ushort returnvalue = 1;
            FileStream myStream;
            SftpClient myClient;
            try
            {
                myStream = new FileStream(@"\nvram\temp.txt", FileMode.Create);
                myClient = new SftpClient(strHost, 22, strUser, strPassword);
                myClient.Connect();
                //Action<ulong> myAction = DownloadDone; // Defines that myAction is a delegate that takes a single ulong input and is the same as the function download done.
                myClient.DownloadFile(strPath, myStream, DownloadDone); // Replace DownloadDone with myAction if using.
                myClient.Disconnect();
                myStream.Close();
            }
            catch (Exception e)
            {
                ErrorLog.Error(String.Format("Error Loading SFTP file: {0}", e.Message));
                returnvalue = 0;
            }
            finally
            {
                if (returnvalue == 1)
                    OpenLocalFile(@"\nvram\temp.txt");
            }

            return returnvalue;
        }
        //Triggered on Download Completed
        private void DownloadDone(ulong size)
        {
            CrestronConsole.PrintLine("Downloaded file: {0}", size);
        }
        #region Optional Version of OpenSFTPFile
        //public ushort OpenSFTPFile(String strUser, String strPassword, String strHost, String strPath)
        //{
        //    ushort returnvalue = 1;
        //    MemoryStream myTest = null;
        //    SftpClient myClient;
        //    try
        //    {
        //        myTest = new MemoryStream();
        //        myClient = new SftpClient(strHost, 22, strUser, strPassword);
        //        myClient.Connect();
        //        //Action<ulong> myAction = DownloadDone; // Defines that myAction is a delegate that takes a single ulong input and is the same as the function download done.
        //        myClient.DownloadFile(strPath, myTest, DownloadDone); // Replace DownloadDone with myAction if using.
        //        myClient.Disconnect();
        //    }
        //    catch (Exception e)
        //    {
        //        ErrorLog.Error(String.Format("Error Loading SFTP file: {0}", e.Message));
        //        returnvalue = 0;
        //    }
        //    finally
        //    {
        //        if (myTest != null)
        //        {
        //            if (returnvalue == 1)
        //            {
        //                myReader = new StreamReader(myTest);
        //                while (!myReader.EndOfStream)
        //                {
        //                    CrestronConsole.PrintLine(myReader.ReadLine());
        //                }
        //                myTest.Close();
        //            }
        //        }
        //    }

        //    return returnvalue;
        //}
        #endregion
    }

}
