using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GeometricAlgebra;

namespace GAWebApp.Models
{
    public class State
    {
        public EvaluationContext context;
        public Dictionary<string, string> history;

        public State()
        {
            context = new EvaluationContext();
            history = new Dictionary<string, string>();
        }
    }
}
