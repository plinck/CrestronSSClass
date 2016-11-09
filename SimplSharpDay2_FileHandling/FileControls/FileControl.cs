using System;
using System.Text;
using Crestron.SimplSharp;                          				// For Basic SIMPL# Classes
using Crestron.SimplSharp.CrestronIO;
using Crestron.SimplSharp.Net.Http;
using Crestron.SimplSharp.Ssh;

namespace FileControls
{
    public class FileControl
    {
        private FileStream myStream;
        private StreamReader myReader;

        //Default Constructor
        public FileControl()
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
    }
}

