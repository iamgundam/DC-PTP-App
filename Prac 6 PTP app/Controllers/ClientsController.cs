using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web;
using System.Web.Http;
using API_classes;
using Prac_6_PTP_app.Models;

namespace Prac_6_PTP_app.Controllers
{
    public class ClientsController : ApiController
    {
        // GET List of all clients
        [Route("api/clients")]
        [HttpGet]
        public HttpResponseMessage GetClients()
        {
            DataModel db = DataModel.get();
            ClientListData output = new ClientListData(db.getClients());
            return Request.CreateResponse(HttpStatusCode.OK, output);
        }

        //POST New client
        [Route("api/clients/add")]
        [HttpPost]
        public HttpResponseMessage AddClient(ClientData data)
        {
            DataModel db = DataModel.get();
            HttpResponseMessage resp;

            try
            {
                db.addClient(data.ipAddress, data.port);
                resp = new HttpResponseMessage(HttpStatusCode.OK);
            }
            catch(ClientAlreadyExistsException e)
            {
                resp = new HttpResponseMessage(HttpStatusCode.Forbidden);
            }

            return resp;
        }

        //DELETE Remove client
        [Route("api/clients/remove")]
        [HttpDelete]
        public HttpResponseMessage RemoveClient(ClientData data)
        {
            DataModel db = DataModel.get();
            HttpResponseMessage resp;

            try
            {
                db.removeClient(data.ipAddress, data.port);
                resp = new HttpResponseMessage(HttpStatusCode.OK);
            }
            catch (ClientNotFoundException e)
            {
                resp = new HttpResponseMessage(HttpStatusCode.Forbidden);
            }

            return resp;
        }
    }
}
