using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using API_classes;

namespace Prac_6_PTP_app.Models
{
    //Singleton static data model
    public class DataModel
    {
        private static DataModel instance;
        private List<ClientData> clients;

        private DataModel()
        {
            clients = new List<ClientData>();
        }

        public static DataModel get()
        {
            if (instance == null)
            {
                instance = new DataModel();
            }

            return instance;
        }

        public List<ClientData> getClients()
        {
            //Return a copy of the client list
            return new List<ClientData>(clients);
        }

        public void addClient(string ip, uint port)
        {
            ClientData newClient = new ClientData(ip, port);

            if (clients.Contains(newClient))
            {
                throw new ClientAlreadyExistsException();
            }
            else
            {
                clients.Add(newClient);
            }
        }

        public void removeClient(string ip, uint port)
        {
            ClientData toDelete = new ClientData(ip, port);
            int placeInList;

            //Find index of client to remove. Returned index is -1 if client is not found.
            placeInList = clients.FindIndex(x => x.Equals(toDelete));

            if(placeInList == -1)
            {
                throw new ClientNotFoundException();
            }
            else
            {
                clients.RemoveAt(placeInList);
            }
        }
    }

    //Custom exceptions ---------------------------------------------------------------------------
    public class ClientAlreadyExistsException: Exception
    {
        public ClientAlreadyExistsException()
        {
        }

        public ClientAlreadyExistsException(string message)
            : base(message)
        {
        }

        public ClientAlreadyExistsException(string message, Exception inner)
            : base(message, inner)
        {
        }
    }
    public class ClientNotFoundException : Exception
    {
        public ClientNotFoundException()
        {
        }

        public ClientNotFoundException(string message)
            : base(message)
        {
        }

        public ClientNotFoundException(string message, Exception inner)
            : base(message, inner)
        {
        }
    }
}
