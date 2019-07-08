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

            try
            {
                // TODO: We should probably do this in a task and then wait with time-out.
                Parser parser = new Parser();
                Operand operand = parser.Parse(expression);
                operand = Operand.FullyEvaluate(operand, state.context);
                string result = operand.Print(Operand.Format.PARSEABLE);
                state.history.Add(expression, result);
            }
            catch(Exception exc)
            {
                state.history.Add(expression, exc.Message);
            }

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
