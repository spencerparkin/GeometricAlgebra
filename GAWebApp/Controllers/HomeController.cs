using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using GAWebApp.Models;
using GeometricAlgebra;

namespace GAWebApp.Controllers
{
    public class HomeController : Controller
    {
        private static State defaultState = new State();

        public IActionResult Index()
        {
            State state = this.GetState();
            return View(state);
        }

        [HttpGet]
        public IActionResult ClearHistory()
        {
            State state = GetState();
            state.history.Clear();
            return PartialView("HistoryView", state);
        }

        [HttpGet]
        public IActionResult ShowLatex(bool showLatex)
        {
            State state = GetState();
            state.showLatex = showLatex;
            return PartialView("HistoryView", state);
        }

        [HttpGet]
        public IActionResult Calculate(string expression)
        {
            State state = GetState();
            state.Calculate(expression);
            return PartialView("HistoryView", state);
        }

        [HttpGet]
        public IActionResult RunScript(string script)
        {
            State state = GetState();
            state.RunScript(script);
            return PartialView("HistoryView", state);
        }

        private State GetState()
        {
            // TODO: For correctness, we need to retrieve the correct state for the client based on
            //       an identification of the client.  For now, just return whatever state we have.
            //       This could have lots of weird consequences depending on how the web-server application
            //       is scaled horizontally or otherwise allocated for clients.
            return defaultState;
        }
    }
}
