using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Security.Cryptography.X509Certificates;
using System.ServiceModel;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Caching;
using System.Web.UI.WebControls.WebParts;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using API_classes;
using Newtonsoft.Json;
using RestSharp;

namespace Client_GUI
{
    // User interface and handles Networking thread
    // Networking will check the Web Server for new clients,
    // and connect to each client's Client Server to look for jobs to do.

    //GUI ELEMENTS
    // CodeBox       - User typed Python code
    // SubmitButton  - Button to submit the code to the Client's job server.
    // JobStatusBox  - Displays whether the networking thread is working in the background or not
    // WorkedJobsBox - Number of worked external jobs
    // ResultsBox    - Displays externally worked out output

    public partial class MainWindow : Window
    {
        private uint port;

        public MainWindow()
        {
            InitializeComponent();

            port = 6000;

            //Start server thread, lock 'port' uint for initial setting.
            Server serverWorker = new Server();
            Thread serverThread = new Thread(() => serverWorker.DoWork(ref port));
            serverThread.Start();
            
            //Loop while port is not set
            while(!serverWorker.isPortSet()){ }

            //Start networking thread
            Networking worker = new Networking();
            Thread networkingThread = new Thread(() => worker.DoWork(port));
            networkingThread.Start();

            Console.WriteLine(String.Concat("CLIENT PORT: ", port.ToString()));

            /*
            //Cleanup
            serverWorker.Stop();
            serverThread.Join();

            worker.Stop();
            networkingThread.Join();*/
        }

        private void SubmitButton_Click(object sender, RoutedEventArgs e)
        {
            string code = CodeBox.Text;
            Console.WriteLine(String.Concat("CLIENT PORT: ", port.ToString()));

        }

        
    }//end class

    public class Networking
    {
        private volatile bool shouldStop; //'volatile' as multiple threads may access this bool
        private List<ClientData> clients;

        public void DoWork(uint clientPort)
        {
            RestClient webServer;
            shouldStop = false;

            //Connect to web server
            ClientListData updatedClients = null;
            webServer = new RestClient("https://localhost:44370/");


            while (!shouldStop)
            {
                //Attempt to grab/update client list from web server
                RestRequest req = new RestRequest("api/clients");
                IRestResponse resp = webServer.Get(req);
                if (resp.IsSuccessful)
                {
                    //Update client list.
                    updatedClients = JsonConvert.DeserializeObject<ClientListData>(resp.Content);
                    clients = new List<ClientData>(updatedClients.clients);

                    //Look for a job to do.
                    IEnumerator<ClientData> enumerator = clients.GetEnumerator();
                    enumerator.MoveNext();
                    ClientData curr = enumerator.Current;

                    bool keepSearching = true;
                    while(enumerator.Current != null && keepSearching)
                    {
                        //Ignore the current client if it is the server's owning client
                        if(curr.port != clientPort)
                        {
                            //Connect to that client's server
                            NetTcpBinding tcp = new NetTcpBinding();
                            string URL = String.Concat("net.tcp://localhost:", curr.port.ToString(), "/ClientServer");
                            ChannelFactory<JobServerInterface> jobServerFactory;
                            jobServerFactory = new ChannelFactory<JobServerInterface>(tcp, URL);
                            JobServerInterface clientServer = jobServerFactory.CreateChannel();

                            /*Do job if found, then stop searching.
                            Job foundJob = clientServer.GetJob();
                            if (foundJob != null)
                            {
                                //do job
                                clientServer.SubmitSolution(foundJob.localID, "testSolution");

                                keepSearching = false;
                            }*/

                            //Check list of available jobs
                            List<Job> jobs = clientServer.GetJobs();
                            Job foundJob = jobs.Find(x => x.solution == null);

                            //Do job if found, then stop searching.
                            if (foundJob != null)
                            {
                                //do job
                                clientServer.SubmitSolution(foundJob.localID, "testSolution");

                                keepSearching = false;
                            }
                        }

                        //Progress client enumerator
                        enumerator.MoveNext();
                        curr = enumerator.Current;
                    }//end while

                }
                else //If some problem occurs...
                {
                    //Don't update list.

                    if(clients == null) //List will be empty on first request. If an error occurred, close thread and log error.
                    {
                        shouldStop = true;
                        Console.WriteLine("ERROR: Clients list could not be retrieved after connecting to the web server.");
                    }
                }

                //Wait half a second before updating the list again and looking for a job to do.
                Thread.Sleep(500);
            }//Loop runs until Stop() is called by another thread


        }

        public void Stop()
        {
            shouldStop = true;
        }

    }//end class

    public class Server
    {
        public volatile bool shouldStop = false;
        public volatile bool portSet = false;

        public void DoWork(ref uint port)
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
            //uint port = 6000;
            bool connected = false;
            while (port <= 7000 && !connected)
            {
                try
                {
                    host = new ServiceHost(server);
                    host.AddServiceEndpoint(typeof(JobServerInterface), tcp, String.Concat("net.tcp://127.0.0.1:", port.ToString(), "/ClientServer"));
                    host.Open();
                    connected = true;
                }
                catch (AddressAlreadyInUseException e)
                {
                    port++;
                }
            }
            portSet = true;

            //Add this client to Web Server list of clients
            RestClient webServer = new RestClient("https://localhost:44370/");
            RestRequest req = new RestRequest("api/clients/add");
            req.AddJsonBody(new ClientData("127.0.0.1", port));
            IRestResponse resp = webServer.Post(req);

            Console.WriteLine(String.Concat("...come! System online on port ", port.ToString()));

            /*DEBUGGING: Console control over job posting
            bool exit = false;
            while (exit != true)
            {
                string input = Console.ReadLine();

                if (input.Equals("exit") || input.Equals("close"))
                {
                    exit = true;
                }
                else if (input.Equals("post job"))
                {
                    server.SubmitJob("test");
                }
            }*/

            //server.SubmitJob("test");
            //server.SubmitJob("test2");

            while (!shouldStop)
            {
                Console.ReadLine();
            }
            
            host.Close();
        }

        public void Stop()
        {
            shouldStop = true;
        }

        public bool isPortSet()
        {
            return portSet;
        }
    }

    [ServiceContract]
    public interface JobServerInterface
    {
        [OperationContract]
        List<Job> GetJobs();

        //[OperationContract]
        //Job GetJob();

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
            localJobIdIncrement = 1;

            jobs.Add(new Job(0, "test", null));
        }

        public static JobServer get()
        {
            if (instance == null)
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

        /*
        public Job GetJob()
        {
            return jobs.First();
        }*/

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
            if (solved.solution == null)
            {
                solved.solution = sol;
                accepted = true;
                jobs.Remove(solved);
            }

            Log(String.Concat("Solution submitted by another client. Solution was: ", sol));
            return accepted;
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        private void Log(string logString)
        {
            logCount += 1;
            Console.WriteLine(String.Concat(logCount.ToString(), ": ", logString));
        }

    }//end DataServer

}//end namespace
