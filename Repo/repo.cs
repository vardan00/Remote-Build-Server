/*
 * 
 * ###############################################################################
 * ###  Program.cs - demonstrating functioning of Mock Reposerver              ###
 * ###  Language:     C#                                                       ###
 * ###  Platform:     Lenovo B51, Windows 10, SP2                              ###
 * ###  Application:  Working of the Mock Repository                           ###
 * ###  Author:       Sai Vardhan Lella, Syracuse University                   ###
 * ###                                                                         ###
 * ###############################################################################
 * 
 *      MODULE OPERATIONS
 *     --------------------
 *     This is a Mock Repository that maintains the logs and the files to be tested
 *     and built.
 *     
 *     PUBLIC INTERFACE
 *    --------------------
 *       ---> parse
 *       ---> requests
 *       ---> postMessage
 *       ---> fetchPort
 *       ---> fileMsgHandler
 *       
 *    
 *     BUILD PROCESS
 *   ------------------
 *   Dependencies:
 *      IMPCommService.cs
 *      MPCommService.cs
 *      
 *      
 *   Build:
 *      csc IMPCommService.cs MPCommService.cs repo.cs Helper.cs
 *      
 *   
 *     MAINTAINENCE HISTORY
 *   ------------------------
 *     ver 1.0 : 25 October 2017
 *
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using System.IO;
using System.Text.RegularExpressions;
using MessagePassingComm;
using System.Threading;
namespace Repo
{
    public class repo
    {
        private static Comm channel;
        private static int port = 6000;

        public repo()
        {
            channel = new Comm("http://localhost", port);
            Console.WriteLine("\n_______________________________________________");
            Console.WriteLine("\n\tRepository running ");
            Console.WriteLine("\n+++++++++++++++++++++++++++++++++++++++++++++++");
        }


        /*
         * The parse function is responsible to parse the xml file and fetch
         * the files
         */
        private List<string> parse(string timestamp)
        {
            List<string> files = new List<string>();
            string location = "Repo/files/";
            files.Add(location + timestamp + ".xml");
            try
            {
                XDocument xd = XDocument.Load(location + timestamp + ".xml");
                Console.WriteLine("\tXml File Obtained:" + @Regex.Replace(xd.ToString(), @"\n|^", "\n\t") + "\n\n");
                var tests = from elements in xd.Descendants().Elements("TestElement") select elements;
                foreach (XElement xl in tests)
                {
                    files.Add(location + xl.Element("testDriver").Value);
                    foreach (XElement xe in from el in xl.Element("testCodes").Elements("string") select el)
                    {
                        files.Add(location + xe.Value);
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("\n\terrror parsing the file {0}", e.ToString());
            }
            return files;
        }
        
        /**
         * This function is used to check the incoming messgaes and do
         * necessary functions.
         */
        private void requests()
        {
            CommMessage comm;
            do
            {
                comm = channel.getMessage();
                if (comm.command == "Quit")
                {
                    Console.WriteLine("Quitting");
                    CommMessage quit = new CommMessage(CommMessage.MessageType.closeSender);
                    quit.to = "http://localhost:6000/MessagePassingComm.Receiver";
                    quit.from = "http://localhost:6000/MessagePassingComm.Receiver";
                    quit.author = "repo";
                    channel.postMessage(quit);
                    break;
                }
                else if (comm.command == "ClientRequest")
                {
                    Console.WriteLine("Received a Client request");
                    XDocument xd = XDocument.Parse(comm.content);
                    xd.Save("Repo/files/xml/"+comm.timestamp+".xml");
                    postMessage(7000, (string)comm.timestamp.Clone(), "xml", new List<string>(), (string)(comm.content).Clone());
                }
                else if (comm.command == "file")
                {
                    fileMsgHandler(comm);
                }
                else if (comm.command == "navigation")
                {
                    string[] listFiles = Directory.GetFiles("Repo/files/");
                    postMessage(5000, (string)comm.timestamp.Clone(), "navigation", new List<string>(listFiles), "");
                }
                else if (comm.command == "log")
                {
                    File.AppendAllText("Repo/logs/" + "log.txt", comm.content + Environment.NewLine);
                }
            } while (true);
        }

        /**
         * A function to handle file message.
         */
        private void fileMsgHandler(CommMessage comm) {
            int folder = fetchPort(comm.from);
            try
            {
                foreach (string f in comm.arguments)
                {
                    if (File.Exists("Repo/files/" + f))
                    {
                        Console.WriteLine("Transfering the file {0}", f);
                        channel.postFile("Repo/files/" + f, comm.from, "BuildServer/files/" + folder + "/" + (string)comm.timestamp.Clone());
                    }
                    else
                    {
                        throw new FileNotFoundException();
                    }
                }
                postMessage(fetchPort(comm.from), (string)comm.timestamp.Clone(), "file", new List<string>(), "");
            }
            catch (Exception e)
            {
                Console.WriteLine("error at repo");
                postMessage(5000, "", "file", new List<string>(), "\nError at repo check logs");
                File.AppendAllText("Repo/logs/" + "log.txt", e.ToString()+ Environment.NewLine);
            }
        }
        /**
         * This function helps in getting the port number from the 
         * url 
         */
        private int fetchPort(string address)
        {
            return Int32.Parse(Regex.Match(address, @"\d+").Value);
        }

        /*
         * A function to send messages
         */
        private void postMessage(int to, string timestamp, string command, List<string> arguments, string content)
        {
            CommMessage comm = new CommMessage(CommMessage.MessageType.reply);
            comm.to = "http://localhost:" + to + "/MessagePassingComm.Receiver";
            comm.from = "http://localhost:6000/MessagePassingComm.Receiver";
            comm.author = "repo";
            comm.command = command;
            comm.content = content;
            foreach (string s in arguments)
            {
                comm.arguments.Add(s);
            }
            comm.timestamp = timestamp;
            channel.postMessage(comm);
        }

        static void Main(string[] args)
        {
            repo r = new repo();
            Thread checkRequests = new Thread(r.requests);
            checkRequests.Start();
        }
    }
}
