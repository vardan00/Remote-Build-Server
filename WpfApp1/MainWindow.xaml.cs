/*`
 * 
 * ###############################################################################
 * ###  Program.cs - demonstrating functioning of build server prototype       ###
 * ###  Language:     C#                                                       ###
 * ###  Platform:     Lenovo B51, Windows 10, SP2                              ###
 * ###  Application:  Working of the Build Server                              ###
 * ###  Author:       Sai Vardhan Lella, Syracuse University                   ###
 * ###                                                                         ###
 * ###############################################################################
 * 
 *     MODULE OPERATIONS
 *     --------------------
 *     This provides the  grapical user interface.
 *     
 *     PUBLIC INTERFACE
 *    --------------------
 *      ---> receiveRequestThreadFunction
 *      ---> postMessage
 *      ---> quitMsg
 *      ---> clear
 *      ---> generateXMLString
 *      ---> Build_Click
 *      ---> quitThreadProc
 *      ---> QuitButton_click
 *      ---> AddTest_Click
 *      ---> ClearTest_Click
 *      ---> demoRequest
 *      
 *      
 *     BUILD PROCESS
 *   ------------------
 *   Dependencies:
 *      IMPCommService.cs
 *      MPCommService.cs
 *      MainWindow.xaml
 *      
 *   Build:
 *      csc MainWindow.xaml.cs
 *      
 *   
 *     MAINTAINENCE HISTORY
 *   ------------------------
 *     ver 1.0 : 25 October 2017
 */
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
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
using System.Xml.Linq;
using MessagePassingComm;
using System.Threading;
using System.Diagnostics;
using System.Text.RegularExpressions;

namespace WpfApp1
{

    //Interaction logic for MainWindow.xaml
    public partial class MainWindow : Window
    {

        private string startDir = Properties.Settings.Default.StartPath;
        private Dictionary<string, List<string>> files = new Dictionary<string, List<string>>();
        private StringBuilder xmlStructure = new StringBuilder("");

        private int port = 5000;

        private Comm channel;
        public MainWindow()
        {
            channel = new Comm("http://localhost", port);
            InitializeComponent();
            postMessage(6000, "", "navigation", new List<string>(), "");
            Thread recieveRequest = new Thread(receiveRequestThreadFunction);
            recieveRequest.Start();
            Thread demo = new Thread(demoRequest);
            demo.Start();    
        }

        /**
         * A function to demonstrate the functioning of the project.
         */
        private void demoRequest()
        {
            XDocument xd = XDocument.Load("../../sampletest/test1.xml");
            string timestamp = "" + DateTime.Now.ToFileTimeUtc();
            postMessage(6000, timestamp, "ClientRequest", new List<string>(), xd.ToString());
            Thread.Sleep(6000);
            timestamp = "" + DateTime.Now.ToFileTimeUtc();
            xd = XDocument.Load("../../sampletest/test2.xml");
            postMessage(6000, timestamp, "ClientRequest", new List<string>(), xd.ToString());
            Thread.Sleep(6000);
            timestamp = "" + DateTime.Now.ToFileTimeUtc();
            xd = XDocument.Load("../../sampletest/test3.xml");
            postMessage(6000, timestamp, "ClientRequest", new List<string>(), xd.ToString());
        }
        /**
         * This function is to process the requests received from other servers.
         */
        private void receiveRequestThreadFunction()
        {
            CommMessage comm;
            while (true)
            {
                comm = channel.getMessage();
                if (comm.command == "navigation")
                {
                    foreach (string arg in comm.arguments)
                    {
                        Dispatcher.Invoke(() => DriverSelected.Items.Add(System.IO.Path.GetFileName(arg)));
                        Dispatcher.Invoke(() => FilesSelected.Items.Add(System.IO.Path.GetFileName(arg)));
                    }
                }
                else if (comm.command == "msg")
                {
                    Dispatcher.Invoke(() => { Results.Text += comm.content; });
                }
                else if (comm.command == "Quit")
                {
                    Dispatcher.Invoke(() => Results.Text += "\n quitting gui");

                    CommMessage quit = new CommMessage(CommMessage.MessageType.closeSender);
                    quit.to = "http://localhost:5000/MessagePassingComm.Receiver";
                    quit.from = "http://localhost:5000/MessagePassingComm.Receiver";
                    quit.author = "Client";
                    quit.command = "Quit";
                    channel.postMessage(quit);
                    break;
                }
            }
            Dispatcher.Invoke(() => Results.Text += "closing interface");
            Process.GetCurrentProcess().CloseMainWindow();
        }

        /*
         * This function is used to generate the xml request    
         */
        private string generateXMLString(string parsableString)
        {
            string[] testRequests = parsableString.Split(';');
            XDocument xd = new XDocument();
            XElement testRequest = new XElement("TestRequest");
            xd.Add(testRequest);
            int i = 1;
            foreach (string tr in testRequests)
            {
                if (tr != "")
                {
                    XElement testElement = new XElement("TestElement");
                    testRequest.Add(testElement);
                    XElement testName = new XElement("testName");
                    testName.SetValue("test" + i++);
                    testElement.Add(testName);
                    string[] trSplit = tr.Split(':');
                    string[] testStubs = trSplit[1].Split(',');
                    XElement testDriverTag = new XElement("testDriver");
                    testDriverTag.SetValue(trSplit[0]);
                    testElement.Add(testDriverTag);
                    XElement testStubsTag = new XElement("testStubs");
                    foreach (string ts in testStubs)
                    {
                        if (ts != "")
                        {
                            XElement testCase = new XElement("testCase");
                            testCase.SetValue(ts);
                            testStubsTag.Add(testCase);
                        }
                    }
                    testElement.Add(testStubsTag);
                }
            }
            return xd.ToString();
        }

        /*
         * A function to send the messages
         */
        private void postMessage(int to, string timestamp, string command, List<string> arguments, string content)
        {
            CommMessage comm = new CommMessage(CommMessage.MessageType.reply);
            comm.to = "http://localhost:" + to + "/MessagePassingComm.Receiver";
            comm.from = "http://localhost:5000/MessagePassingComm.Receiver";
            comm.author = "Client";
            comm.command = command;
            foreach (string s in arguments)
            {
                comm.arguments.Add(s);
            }
            comm.timestamp = timestamp;
            comm.content = content;
            channel.postMessage(comm);
        }

        /**
         * A quit function to send quit messages.
         */
        private void quitMsg(int to, string command)
        {
            CommMessage comm = new CommMessage(CommMessage.MessageType.closeReceiver);
            comm.to = "http://localhost:" + to + "/MessagePassingComm.Receiver";
            comm.from = "http://localhost:5000/MessagePassingComm.Receiver";
            comm.author = "Client";
            comm.command = command;
            channel.postMessage(comm);
        }

        /**
         * To clear and disable all buttons when quit button is clicked.
         */
        private void clear()
        {
            AddTest.IsEnabled = false;
            Clear.IsEnabled = false;
            Build.IsEnabled = false;
            DriverSelected.Items.Clear();
            FilesSelected.Items.Clear();
        }

        /*
         * Kills all the process running.
         */
        private void QuitButton_click(object sender, RoutedEventArgs e)
        {
            clear();
            Thread quitThread = new Thread(quitThreadProc);
            quitThread.Start();
        }

        /**
         * Thread procedure to send quit msgs to all servers.
         */
        private void quitThreadProc()
        {
            Dispatcher.Invoke(() => Results.Text += "\nQuiting TestHarness");
            quitMsg(5500, "Quit");
            Dispatcher.Invoke(() => Results.Text += "\nQuiting Builder");
            quitMsg(7000, "Quit");
            Thread.Sleep(3000);
            Dispatcher.Invoke(() => Results.Text += "\nQuiting Reposirtory");
            quitMsg(6000, "Quit");
        }
        /**
         * A event handler to add test button.
         */
        private void AddTest_Click(object sender, RoutedEventArgs e)
        {
            string testDriver = DriverSelected.SelectedItem.ToString();
            xmlStructure.Append(testDriver);
            xmlStructure.Append(":");
            XmlPreview.Text += "\n" + DriverSelected.SelectedItem.ToString();
            foreach (var tf in FilesSelected.SelectedItems)
            {
                xmlStructure.Append(tf);
                xmlStructure.Append(",");
                XmlPreview.Text += "\n\t" + tf.ToString();
            }
            FilesSelected.UnselectAll();
            DriverSelected.UnselectAll();
            Clear.IsEnabled = true;
            Build.IsEnabled = true;
            xmlStructure.Append(";");
        }

        /**
         * An event handler to clear button. To clear the selected test requests.
         */
        private void ClearTest_Click(object sender, RoutedEventArgs e)
        {
            XmlPreview.Text = "";
            xmlStructure.Clear();
            Clear.IsEnabled = false;
            Build.IsEnabled = false;
        }

        /**
         * A event handler to build button. To initiate the build process
         * by sending the build request.
         */
        private void Build_Click(object sender, RoutedEventArgs e)
        {
            XmlPreview.Text = "";
            string timestamp = "" + DateTime.Now.ToFileTimeUtc();
            postMessage(6000, timestamp, "ClientRequest", new List<string>(), generateXMLString(xmlStructure.ToString()));
            xmlStructure.Clear();
            Clear.IsEnabled = false;
            Build.IsEnabled = false;
        }

    }


}
