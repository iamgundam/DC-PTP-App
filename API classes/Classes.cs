﻿using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace API_classes
{
    public class ClientData
    {
        public string ipAddress;
        public uint port;

        public ClientData(string inputIp, uint inputPort)
        {
            ipAddress = inputIp;
            port = inputPort;
        }

        public bool Equals(ClientData other)
        {
            bool result = false;

            if(other.ipAddress.Equals(ipAddress))
            {
                if(other.port.Equals(port))
                {
                    result = true;
                }
            }

            return result;
        }
    }

    public class ClientListData
    {
        public List<ClientData> clients;

        public ClientListData(List<ClientData> inList)
        {
            clients = inList;
        }
    }

    [DataContractAttribute]
    public class Job
    {
        [DataMember]
        public uint localID;

        [DataMember]
        public string pythonCode;

        [DataMember]
        public string solution;

        public Job(uint id, string code, string sol)
        {
            localID = id;
            pythonCode = code;
            solution = sol;
        }

        public bool Equals(Job other)
        {
            bool isEqual = false;

            if (localID == other.localID)
            {
                if (String.Equals(pythonCode, other.pythonCode))
                {
                    isEqual = true;
                }
            }

            return isEqual;
        }
    }
}
