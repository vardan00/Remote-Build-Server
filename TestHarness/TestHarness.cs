/*
 * ###############################################################################
 * ###  Program.cs - demonstrating functioning of Mock TestHarness             ###
 * ###  Language:     C#                                                       ###
 * ###  Platform:     Lenovo B51, Windows 10, SP2                              ###
 * ###  Application:  Working of the Test Harness                              ###
 * ###  Author:       Sai Vardhan Lella, Syracuse University                   ###
 * ###                                                                         ###
 * ###############################################################################
 * 
 *      MODULE OPERATIONS
 *     --------------------
 *     This module loads the dll file and run to test the results.
 *     
 *     PUBLIC INTERFACE
 *    --------------------
 *      ---> runTest
 *      ---> runTestHarntess
 *      ---> parse
 *      ---> postMessage
 *      ---> checkRequests
 *      ---> fetchPort
 *      
 *    
 *     BUILD PROCESS
 *   ------------------
 *     Dependent Files :
 *      IMPCommService.cs
 *      MPCommService.cs
 *     
 *     Build:       
 *     		csc IMPCommService.cs MPCommService.cs TestHarness.cs
 *      
 *        REFERNCE
 *   ------------------
 *     AMAR (TA's Logging test demo)
 *  
 *     MAINTAINENCE HISTORY
 *   ------------------------
 *     ver 1.0 : 5 December 2017
 */

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using MessagePassingComm;
using System.Text.RegularExpressions;

namespace TestHarness
{
    public class TestHarness
    {
        Comm channel;
        private int port = 5500;
        List<string> files;
        public TestHarness()
        {
            channel = new Comm("http://localhost", port);
            Console.WriteLine("\n_______________________________________________");
            Console.WriteLine("\n\tTestharness running");
            Console.WriteLine("\n+++++++++++++++++++++++++++++++++++++++++++++++");
        }

        /*
         * The function is responsible to call necessary functions
         * to carry out test harness tasks
         */
        public void runTestHarness(string timestamp, List<string> files)
        {
            Console.WriteLine("\n\tParsing the xml file");
            string path = "TestHarness/files/" + timestamp + "/";
            foreach (string s in files)
            {
                runTest(path + s, timestamp);
            }
        }

        /*
         * The parse function is responsible to parse the xml file and return
         * a list of file names
         */
        private List<string> parse(string timestamp, string xmlString)
        {
            Console.WriteLine(xmlString);
            List<string> files = new List<string>();
            string location = "TestHarness/files/" + timestamp + "/";
            try
            {
                XDocument xd = XDocument.Parse(xmlString);

                var tests = from elements in xd.Descendants().Elements("TestElement") select elements;
                foreach (XElement xl in tests)
                {
                    string key = xl.Element("testStubs").Value;
                    files.Add(key);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("Error parsing the xml file\n" + e.ToString());
            }
            return files;
        }

        /*
         * This function is used to send messages.
         */
        private void postMessage(int to, string timestamp, string command, List<string> arguments, string content)
        {
            CommMessage comm = new CommMessage(CommMessage.MessageType.reply);
            comm.to = "http://localhost:" + to + "/MessagePassingComm.Receiver";
            comm.from = "http://localhost:" + port + "/MessagePassingComm.Receiver";
            comm.author = "ChilBuilder : " + port;
            comm.command = command;
            foreach (string s in arguments)
            {
                comm.arguments.Add(s);
            }
            comm.timestamp = timestamp;
            comm.content = content;
            channel.postMessage(comm);
        }
        /*
         * The runTest loads the dll files and execute the files
         */
        private void runTest(string Path, string timestamp)
        {
            try
            {
                Console.WriteLine("\tLoading the test file");
                Assembly asm = Assembly.LoadFrom(Path);
                Type[] types = asm.GetTypes();
                bool Result;

                foreach (Type type in types)
                {
                    MethodInfo testMethod = type.GetMethod("test");
                    if (testMethod != null && type.IsClass)
                    {
                        Console.WriteLine("\tInvoking the test method");
                        Result = (bool)testMethod.Invoke(Activator.CreateInstance(type), null);

                        if (Result)
                        {
                            postMessage(5000, timestamp, "msg", new List<string>(), "\nTest success");
                            postMessage(6000, timestamp, "log", new List<string>(), timestamp + " test success");
                            Console.Write("\n\n\tTest Passed.");
                        }
                        else
                        {
                            Console.WriteLine("\n\n \tTest Failed.");
                            postMessage(5000, timestamp, "msg", new List<string>(), "\nTests failed");
                            postMessage(6000, timestamp, "log", new List<string>(), timestamp + " test failed");
                        }
                        break;
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("\t!!!Test failed. \n\t!!!Error occured while Testing\n");
                Console.WriteLine(e.ToString());
                postMessage(5000, timestamp, "msg", new List<string>(), "\nTests failed");
                postMessage(6000, timestamp, "log", new List<string>(), timestamp + e.ToString());
            }
        }

        /**
         * This function is to run check the messages received and 
         * act accordingly.
         */
        private void checkRequests()
        {
            CommMessage msg;
            while (true)
            {
                msg = channel.getMessage();
                if (msg.command == "xml")
                {
                    Console.WriteLine("Received an test request from {0}", msg.timestamp.Clone());
                    Directory.CreateDirectory("TestHarness/files/" + (string)msg.timestamp.Clone());
                    files = parse((string)msg.timestamp.Clone(), (string)msg.content.Clone());
                    postMessage(fetchPort(msg.from), (string)msg.timestamp.Clone(), "testFiles", files, "");
                }
                else if (msg.command == "file")
                {
                    runTestHarness(msg.timestamp, files);
                }
                else if (msg.command == "Quit")
                {
                    Console.WriteLine("quit received");
                    CommMessage quitMsgGui = new CommMessage(CommMessage.MessageType.closeReceiver);
                    quitMsgGui.to = "http://localhost:5000/MessagePassingComm.Receiver";
                    quitMsgGui.from = "http://localhost:5500/MessagePassingComm.Receiver";
                    quitMsgGui.command = "Quit";
                    quitMsgGui.author = "testHarness";
                    channel.postMessage(quitMsgGui);
                    Thread.Sleep(3000);
                    CommMessage quit = new CommMessage(CommMessage.MessageType.closeSender);
                    quit.to = "http://localhost:5500/MessagePassingComm.Receiver";
                    quit.from = "http://localhost:5500/MessagePassingComm.Receiver";
                    quit.author = "testHarness";
                    channel.postMessage(quit);
                    break;
                }
            }
        }

        static void Main(string[] args)
        {
            TestHarness th = new TestHarness();
            Thread checkRequestsThread = new Thread(th.checkRequests);
            checkRequestsThread.Start();
        }
        /**
         * This function is used to fetch the port number from the url
         */
        private int fetchPort(string address)
        {
            return Int32.Parse(Regex.Match(address, @"\d+").Value);
        }
    }
}
