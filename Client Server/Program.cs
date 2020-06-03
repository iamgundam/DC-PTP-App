using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ServiceModel;
using System.Runtime.CompilerServices;
using API_classes;
using RestSharp;
using System.Dynamic;

namespace Client_Server
{
    //Stores THIS client's jobs it wants done
    //Allows for other clients to take jobs and submit solutions.

    public class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Wel...");

            ServiceHost host = null;
            NetTcpBinding tcp = new NetTcpBinding();
            JobServer server = JobServer.get();

            tcp.MaxBufferPoolSize = int.MaxValue;
            tcp.MaxBufferSize = int.MaxValue;
            tcp.MaxConnections = 100;
            tcp.MaxReceivedMessageSize = int.MaxValue;

            //Find a free port
            uint port = 6000;
            bool connected = false;
            while(port <= 7000 && !connected)
            {
                try
                {
                    host = new ServiceHost(server);
                    host.AddServiceEndpoint(typeof(JobServerInterface), tcp, String.Concat("net.tcp://127.0.0.1:", port.ToString(), "/ClientServer"));
                    host.Open();
                    connected = true;
                }
                catch(AddressAlreadyInUseException e)
                {
                    port++;
                }
            }
            
            //Add this client to Web Server list of clients
            RestClient webServer = new RestClient("https://localhost:44370/");
            RestRequest req = new RestRequest("api/clients/add");
            req.AddJsonBody(new ClientData("127.0.0.1", port));
            IRestResponse resp = webServer.Post(req);

            Console.WriteLine(String.Concat("...come! System online on port ", port.ToString()));

            
            //DEBUGGING: Console control over job posting
            bool exit = false;
            while(exit != true)
            {
                string input = Console.ReadLine();

                if(input.Equals("exit") || input.Equals("close"))
                {
                    exit = true;
                }
                else if(input.Equals("post job"))
                {
                    server.SubmitJob("test");
                }
            }

            //server.SubmitJob("test");
            //server.SubmitJob("test2");

            Console.ReadLine();
            host.Close();
        }
    }

    [ServiceContract]
    public interface JobServerInterface
    {
        [OperationContract]
        List<Job> GetJobs();

        [OperationContract]
        bool SubmitJob(string job);

        [OperationContract]
        bool SubmitSolution(uint id, string sol);

    }//end DataServerInterface

    [ServiceBehavior(ConcurrencyMode = ConcurrencyMode.Multiple, UseSynchronizationContext = false, InstanceContextMode = InstanceContextMode.Single)]
    internal class JobServer : JobServerInterface
    {
        private static JobServer instance;
        private List<Job> jobs;
        private uint localJobIdIncrement; //Incremented by 1, starting from 0, to identify jobs.
        private uint logCount;

        private JobServer()
        {
            jobs = new List<Job>();
            localJobIdIncrement = 0;
        }

        public static JobServer get()
        {
            if(instance == null)
            {
                instance = new JobServer();
            }

            return instance;
        }

        public List<Job> GetJobs()
        {
            Log("Job list retrieved.");
            List<Job> output = jobs;
            return output;
        }

        //Only the server's owning client will submit jobs using this method
        [MethodImpl(MethodImplOptions.Synchronized)]
        public bool SubmitJob(string job)
        {
            bool success = false;
            Job newJob = new Job(localJobIdIncrement, job, null);
            jobs.Add(newJob);

            localJobIdIncrement++;

            Log("Job posted by GUI.");
            return success;
        }

        //Other clients will submit a solution using this method
        [MethodImpl(MethodImplOptions.Synchronized)]
        public bool SubmitSolution(uint id, string sol)
        {
            bool accepted = false;
            Job solved = jobs.Find(x => x.localID == id);
            if(solved.solution == null)
            {
                solved.solution = sol;
                accepted = true;
            }

            Log(String.Concat("Solution submitted by another client. Solution was: ",sol));
            return accepted;
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        private void Log(string logString)
        {
            logCount += 1;
            Console.WriteLine(String.Concat(logCount.ToString(), ": ", logString));
        }

    }//end DataServer
}
