/*
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
 *      MODULE OPERATIONS
 *     --------------------
 *      MotherBuilder has the functionality to spawn child process and allocate 
 *      the requests to child process when they are ready
 *     
 *     PUBLIC INTERFACE
 *    --------------------
 *      ---> kill
 *      ---> sort
 *      ---> spawnProcess
 *      ---> createChildBuilders
 *      ---> DelegateRequest
 *      
 *     BUILD PROCESS
 *   ------------------
 *   Dependencies:
 *      IMPCommService.cs
 *      MPCommService.cs
 *   Build:
 *      csc IMPCommService.cs MPCommService.cs MotherBuilder.cs 
 *      
 *     MAINTAINENCE HISTORY
 *   ------------------------
 *     ver 1.0 : 25 October 2017
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using MessagePassingComm;
using SWTools;
using System.Text.RegularExpressions;

namespace MotherBuilder
{
    class MotherBuilder
    {
        private int port = 7000;
        Comm channel;
        private static BlockingQueue<CommMessage> readyQueue;
        private static BlockingQueue<CommMessage> requestQueue;
        private int instances;
        private bool quit;

        public MotherBuilder()
        {
            Console.WriteLine("\n_______________________________________________");
            Console.WriteLine("\n\tMotherBuilder running");
            Console.WriteLine("\n+++++++++++++++++++++++++++++++++++++++++++++++");
            quit = false;
            channel = new Comm("http://localhost", port);
            readyQueue = new BlockingQueue<CommMessage>();
            requestQueue = new BlockingQueue<CommMessage>();
        }
        static void Main(string[] args)
        {
            MotherBuilder mb = new MotherBuilder();
            mb.instances = Int32.Parse(args[0]);
            mb.CreateChildBuilders(mb.instances, "" + mb.port);
            Thread sortThread = new Thread(mb.sort);
            sortThread.Start();
            Thread requestDelegateThread = new Thread(mb.delegateRequest);
            requestDelegateThread.Start();
        }

        /*
         * Kills the child builders by sending quit message and then laters
         * shuts down
         */
        private void kill()
        {
            CommMessage comm = new CommMessage(CommMessage.MessageType.closeReceiver);
            comm.from = "http://localhost:7000/MessagePassingComm.Receiver";
            comm.author = "MotherBuilder";
            while (instances != 0)
            {
                Console.WriteLine("Sending quit message to {0}", (port+instances));
                comm.to = "http://localhost:" + (port + instances) + "/MessagePassingComm.Receiver";
                comm.command = "Quit";
                channel.postMessage(comm);
                instances--;
                Thread.Sleep(1000);
            }
        
            CommMessage quit = new CommMessage(CommMessage.MessageType.closeSender);
            quit.to = "http://localhost:" + port + "/MessagePassingComm.Receiver";
            quit.from = "http://localhost:" + port + "/MessagePassingComm.Receiver";
            channel.postMessage(quit);
        }

        /*
         * This function sorts the ready requests and the requests from the rest
         * of the requests
         */
        private void sort()
        {
            CommMessage msg;
            while (true)
            {
                msg = channel.getMessage();
                if (msg.command == "xml" && !quit)
                {
                    Console.WriteLine("Received an xml request");
                    requestQueue.enQ(msg);
                }
                else if (msg.command == "ready" && !quit)
                {
                    Console.WriteLine("child process ready from {0}", msg.author);
                    readyQueue.enQ(msg);
                }
                else if (msg.command == "Quit")
                {
                    Console.WriteLine("Qutting child Builders");
                    quit = true;
                    kill();
                    break;
                }
            }
        }

        /*
         * This function delegates the requests to the ready child builders
         */
        private void delegateRequest()
        {
            CommMessage requestMsg, readyMsg;
            while (!quit)
            {
                if (requestQueue.size() > 0 && readyQueue.size() > 0)
                {
                    readyMsg = readyQueue.deQ();
                    requestMsg = requestQueue.deQ();
                    requestMsg.to = readyMsg.from;
                    requestMsg.from = "http://localhost:7000/MessagePassingComm.Receiver";
                    channel.postMessage(requestMsg);
                }
            }
        }

        /*
         * This function creates the childBuilders by calling the spawn process method
         */
        private void CreateChildBuilders(int instances, string motherAddress)
        {
            for (int i = 0; i < instances; i++)
            {
                spawnProcess((Int32.Parse(motherAddress) + i + 1).ToString(), motherAddress);
            }

        }

        /*
         * This function creates a process
         */
        private void spawnProcess(string address, string motherAddress)
        {
            System.Diagnostics.Process cmd = new System.Diagnostics.Process
            {
                StartInfo = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = @"BuildServer\bin\Debug\BuildServer.exe",
                    Arguments = address + " " + motherAddress,
                    UseShellExecute = true,
                    CreateNoWindow = false,
                }
            };
            cmd.Start();

        }
    }
}
