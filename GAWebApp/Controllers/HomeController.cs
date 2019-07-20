using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Xml;
using System.Xml.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using GAWebApp.Models;
using GeometricAlgebra;

namespace GAWebApp.Controllers
{
    public class HomeController : Controller
    {
        private static State defaultState = new State();

        // TODO: We can't store the cache here, because the controller is instantiated for each request.
        //       I'm trying to use System.Runtime.Caching.MemoryCache.Default, but running into wall after wall after wall.
        private Dictionary<string, State > stateCache = new Dictionary<string, State>();

        public IActionResult Index()
        {
            State state = this.GetState("");
            return View(state);
        }

        [HttpGet]
        public IActionResult History(string calculatorID)
        {
            State state = GetState(calculatorID);
            return PartialView("HistoryView", state);
        }

        [HttpGet]
        public IActionResult ClearHistory(string calculatorID)
        {
            State state = GetState(calculatorID);
            state.history.Clear();
            SetState(calculatorID, state);
            return PartialView("HistoryView", state);
        }

        [HttpGet]
        public IActionResult ShowLatex(string calculatorID, bool showLatex)
        {
            State state = GetState(calculatorID);
            state.showLatex = showLatex;
            SetState(calculatorID, state);
            return PartialView("HistoryView", state);
        }

        [HttpGet]
        public IActionResult Calculate(string calculatorID, string expression)
        {
            State state = GetState(calculatorID);
            state.Calculate(expression);
            SetState(calculatorID, state);
            return PartialView("HistoryView", state);
        }

        private CloudBlobContainer GetCloudBlobContainer()
        {
            // Here's the connection string in plain text, which is not recommended.
            // I couldn't find the Azure namespace, though, so until then, here it is.
            // I guess it will be part of the git history too.
            string connectionString = "DefaultEndpointsProtocol=https;AccountName=gawebappstorage;AccountKey=tWQkquTxPG8y+7BsfQ8tNajEsdfxrK0o4v6h1DWhyOPY84ZyP3ZAHiLtifso4Hsy10gUJ4K0uPM228ACKEja+w==;EndpointSuffix=core.windows.net";
            //string connectionString = CloudConfigurationManager.GetSetting("gawebappstorage_AzureStorageConnectionString");

            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(connectionString);

            CloudBlobClient blobClient = storageAccount.CreateCloudBlobClient();
            CloudBlobContainer container = blobClient.GetContainerReference("ga-calc-container");

            return container;
        }

        private State GetState(string calculatorID)
        {
            Task<State> task = GetStateAsync(calculatorID);
            //task.Wait(5000);
            task.Wait();

            if(task.IsCompletedSuccessfully)
                return task.Result;

            return defaultState;
        }

        private async Task<State> GetStateAsync(string calculatorID)
        {
            if(calculatorID.Length == 0)
                return defaultState;

            if(stateCache.ContainsKey(calculatorID))
                return stateCache[calculatorID];

            try
            {
                CloudBlobContainer container = GetCloudBlobContainer();
                await container.CreateIfNotExistsAsync();

                CloudBlockBlob blob = container.GetBlockBlobReference(calculatorID);

                State state = new State();

                try
                {
                    string blobXml = await blob.DownloadTextAsync();
                    XElement rootElement = XElement.Parse(blobXml);
                    state.DeserializeFromXml(rootElement);
                }
                catch(Exception)        // What's the specific exception?
                {
                    XElement rootElement = new XElement("State");
                    state.context.GenerateDefaultStorage();
                    state.SerializeToXml(rootElement);
                    string blobXml = rootElement.ToString();
                    await blob.UploadTextAsync(blobXml);
                }
                
                stateCache.Add(calculatorID, state);

                return state;
            }
            catch(Exception exc)
            {
                string error = exc.Message;

                return defaultState;
            }
        }

        private void SetState(string calculatorID, State state)
        {
            // Just start the task and return immediately; don't wait for it.
            Task<bool> task = SetStateAsync(calculatorID, state);
            task.Start();
        }

        private async Task<bool> SetStateAsync(string calculatorID, State state)
        {
            try
            {
                CloudBlobContainer container = GetCloudBlobContainer();
                await container.CreateIfNotExistsAsync();

                CloudBlockBlob blob = container.GetBlockBlobReference(calculatorID);

                XElement rootElement = new XElement("State");
                state.SerializeToXml(rootElement);
                string blobXml = rootElement.ToString();
                await blob.UploadTextAsync(blobXml);
            }
            catch(Exception exc)
            {
                string error = exc.Message;
                //...
            }

            return true;
        }
    }
}
