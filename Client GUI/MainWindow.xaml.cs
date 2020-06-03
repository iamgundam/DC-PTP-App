using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Threading;
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
using API_classes;
using Client_Server;
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
        public MainWindow()
        {
            InitializeComponent();

            //Start server
            //Do nothing, assume THIS client is being run on port 6000 for testing purposes

            //Start networking thread
            Networking worker = new Networking();
            Thread networkingThread = new Thread(worker.DoWork);
            networkingThread.Start();

            /*
            //Cleanup
            worker.Stop();
            networkingThread.Join();*/
        }

        private void SubmitButton_Click(object sender, RoutedEventArgs e)
        {
            string code = CodeBox.Text;


        }

        
    }//end class

    public class Networking
    {
        private volatile bool shouldStop; //'volatile' as multiple threads may access this bool
        private List<ClientData> clients;

        public void DoWork()
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
                        //Connect to that client's server
                        NetTcpBinding tcp = new NetTcpBinding();
                        string URL = String.Concat("net.tcp://localhost:", curr.port.ToString(), "/ClientServer");
                        ChannelFactory<Client_Server.JobServerInterface> jobServerFactory;
                        jobServerFactory = new ChannelFactory<JobServerInterface>(tcp, URL);
                        JobServerInterface clientServer = jobServerFactory.CreateChannel();

                        //Check list of available jobs
                        List<Job> jobs = clientServer.GetJobs();
                        Job foundJob = jobs.Find(x => x.solution == null);

                        //Do job if found, then stop searching.
                        if(foundJob != null)
                        {
                            //do job
                            clientServer.SubmitSolution(foundJob.localID, "testSolution");

                            keepSearching = false;
                        }

                        //Progress client enumerator
                        enumerator.MoveNext();
                        curr = enumerator.Current;
                    }

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

}//end namespace
