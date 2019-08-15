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
        private RedisDatabase redisDatabase;

        public HomeController(RedisDatabase redisDatabase)
        {
            this.redisDatabase = redisDatabase;
        }

        private static State defaultState = null;

        public IActionResult Index()
        {
            if(defaultState == null)
            {
                defaultState = new State();
                defaultState.GenerateDefaultStorage();
            }

            return View(defaultState);
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
            // TODO: We really should be using a local in-memory cache here so that we don't always have to hit the database.
            //       Yes, Redis is fast, but it's silly for us to be re-evaluating the operand storage and other things for every calculation.

            State state = new State();
            if(!this.redisDatabase.GetState(calculatorID, state))
                return defaultState;
            return state;
        }

        private void SetState(string calculatorID, State state)
        {
            this.redisDatabase.SetState(calculatorID, state);
        }
    }
}
