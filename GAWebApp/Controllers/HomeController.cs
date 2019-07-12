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

        // TODO: Add parameter that identifies client, then retrieve state from a database or something based on that identification.
        // TODO: Use the async/await stuff for this.
        private State GetState()
        {
            return defaultState;
        }
    }
}
