using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Xml;
using System.Xml.Linq;
using System.Threading.Tasks;
using System.Runtime.Caching;
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

            State state = MemoryCache.Default.Get(calculatorID) as State;
            if(state != null)
                return state;

            try
            {
                CloudBlobContainer container = GetCloudBlobContainer();
                await container.CreateIfNotExistsAsync();

                CloudBlockBlob blob = container.GetBlockBlobReference(calculatorID);

                state = new State();

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
                
                MemoryCache.Default.Set(calculatorID, state, null);

                return state;
            }
            catch(Exception exc)
            {
                string error = exc.Message;

                return defaultState;
            }
        }

        private async void SetState(string calculatorID, State state)
        {
            await SetStateAsync(calculatorID, state);
        }

        private async Task SetStateAsync(string calculatorID, State state)
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
        }
    }
}
