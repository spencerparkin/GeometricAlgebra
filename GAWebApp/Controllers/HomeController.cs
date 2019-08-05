using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Xml;
using System.Xml.Linq;
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

        private State GetState(string calculatorID)
        {
            return defaultState;
        }

        private void SetState(string calculatorID, State state)
        {
        }
    }
}
