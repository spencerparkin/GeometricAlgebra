using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GeometricAlgebra
{
    public class Token
    {
        public enum Kind
        {
            SYMBOL,
            NUMBER,
            OPERATOR,
            LEFT_PARAN,
            RIGHT_PARAN,
            DELIMITER
        }

        public Kind kind;
        public string data;

        public Token(Kind kind, string data = "")
        {
            this.kind = kind;
            this.data = data;
        }
    }

    public class ParseException : Exception
    {
        public ParseException(string error)
            : base(error)
        {
        }
    }

    public class Parser
    {
        private EvaluationContext context;
        private bool basisVectorsOnly;
        public bool generatedSymbolicVector;

        public Parser(EvaluationContext context = null, bool basisVectorsOnly = false)
        {
            this.context = context;
            this.basisVectorsOnly = basisVectorsOnly;
            this.generatedSymbolicVector = false;
        }

        public Operand Parse(string expression)
        {
            generatedSymbolicVector = false;
            List<Token> tokenList = Tokenize(expression);
            Operand root = BuildOperandTree(tokenList);
            return root;
        }

        public List<Token> Tokenize(string expression)
        {
            var tokenList = new List<Token>();

            expression = expression.Replace(" ", "");

            while (expression.Length > 0)
                tokenList.Add(EatToken(ref expression));

            return tokenList;
        }

        private Token EatToken(ref string expression)
        {
            Token token = null;

            List<string> operatorList = new List<string>() { "+", "-", "*", "/", "^", ".", "~", "!", "=", ":=" };
            foreach(string opName in operatorList)
            {
                if(expression.Length >= opName.Length && expression.Substring(0, opName.Length) == opName)
                {
                    token = new Token(Token.Kind.OPERATOR, opName);
                    break;
                }
            }

            if(token == null)
            {
                if (expression[0] == '(')
                {
                    token = new Token(Token.Kind.LEFT_PARAN, expression[0].ToString());
                }
                else if (expression[0] == ')')
                {
                    token = new Token(Token.Kind.RIGHT_PARAN, expression[0].ToString());
                }
                else if(expression[0] == ',')
                {
                    token = new Token(Token.Kind.DELIMITER, expression[0].ToString());
                }
                else if (Char.IsLetterOrDigit(expression[0]) || expression[0] == '_' || expression[0] == '$' || expression[0] == '@')
                {
                    if(Char.IsDigit(expression[0]))
                        token = new Token(Token.Kind.NUMBER);
                    else if(Char.IsLetter(expression[0]) || expression[0] == '$' || expression[0] == '@')
                        token = new Token(Token.Kind.SYMBOL);

                    for(int i = 0; i < expression.Length; i++)
                    {
                        if (Char.IsLetterOrDigit(expression[i]) || expression[i] == '_' || (token.kind == Token.Kind.NUMBER && expression[i] == '.') || (token.kind == Token.Kind.SYMBOL && (expression[i] == '$' || expression[i] == '@')))
                            token.data += expression[i];
                        else
                            break;
                    }
                }
            }

            if(token != null)
                expression = expression.Substring(token.data.Length, expression.Length - token.data.Length);
            else
                throw new ParseException(string.Format("Failed to tokenize at \"{0}\".", expression));

            return token;
        }

        private int PrecedenceLevel(string operation)
        {
            if (operation == "=" || operation == ":=")
                return 0;
            if (operation == "+" || operation == "-")
                return 1;
            if (operation == ".")
                return 2;
            if (operation == "^")
                return 3;
            if (operation == "*" || operation == "/")
                return 4;

            throw new ParseException(string.Format("Cannot determine precedence level of \"{0}\".", operation));
        }

        enum Associativity
        {
            LEFT_TO_RIGHT,
            RIGHT_TO_LEFT
        }

        private Associativity OperatorAssociativity(string operation)
        {
            if (operation == "=" || operation == ":=")
                return Associativity.RIGHT_TO_LEFT;
            else if (operation == "+" || operation == "-")
                return Associativity.LEFT_TO_RIGHT;
            else if (operation == "*" || operation == "/")
                return Associativity.LEFT_TO_RIGHT;
            else if (operation == "." || operation == "^")
                return Associativity.LEFT_TO_RIGHT;

            throw new ParseException(string.Format("Cannot determine associativity of \"{0}\".", operation));
        }

        public Operand BuildOperandTree(List<Token> tokenList)
        {
            while (tokenList.Count > 0)
            {
                int count = tokenList.Count;

                if (tokenList[0].kind == Token.Kind.LEFT_PARAN)
                {
                    int i = FindMatchingParan(tokenList, 0);
                    if(i == tokenList.Count - 1)
                    {
                        tokenList.RemoveAt(0);
                        tokenList.RemoveAt(tokenList.Count - 1);
                    }
                }

                if(tokenList.Count == count)
                    break;
            }

            if (tokenList.Count == 0)
                throw new ParseException("Encountered empty token list.");

            if (tokenList.Count == 1)
            {
                Token token = tokenList[0];
                switch (token.kind)
                {
                    case Token.Kind.SYMBOL:
                    {
                        if(token.data[0] == '@')
                            return new Variable(token.data.Substring(1));

                        if(token.data[0] == '$')
                            return new SymbolicScalarTerm(token.data.Substring(1));

                        if(basisVectorsOnly)
                        {
                            string vectorName = token.data;
                            List<string> basisVectorList = context.ReturnBasisVectors();
                            if(!basisVectorList.Contains(vectorName))
                            {
                                Sum sum = new Sum();

                                foreach (string basisVectorName in basisVectorList)
                                {
                                    InnerProduct dot = new InnerProduct(new List<Operand>() { new Blade(vectorName), new Blade(basisVectorName) });
                                    sum.operandList.Add(new GeometricProduct(new List<Operand>() { dot, new Blade(basisVectorName) }));
                                }

                                return sum;
                            }
                        }

                        generatedSymbolicVector = true;
                        return new Blade(token.data);
                    }
                    case Token.Kind.NUMBER:
                    {
                        double number;
                        if (!double.TryParse(token.data, out number))
                            throw new ParseException(string.Format("Encountered non-parsable number ({0}).", token.data));

                        return new Blade(number);
                    }
                    default:
                    {
                        throw new ParseException(string.Format("Encountered lone token ({0}) that isn't handled.", token.data));
                    }
                }
            }
            else if (tokenList[0].kind == Token.Kind.OPERATOR && ((tokenList[1].kind == Token.Kind.LEFT_PARAN && tokenList[tokenList.Count - 1].kind == Token.Kind.RIGHT_PARAN) || tokenList.Count == 2))
            {
                Token token = tokenList[0];

                if (token.data == "-")
                    return new GeometricProduct(new List<Operand>() { new Blade(-1.0), BuildOperandTree(tokenList.Skip(1).ToList()) });

                throw new ParseException(string.Format("Encounterd unary operator ({0}) that isn't recognized on the left.", token.data));
            }
            else if (tokenList[tokenList.Count - 1].kind == Token.Kind.OPERATOR && ((tokenList[0].kind == Token.Kind.LEFT_PARAN && tokenList[tokenList.Count - 2].kind == Token.Kind.RIGHT_PARAN) || tokenList.Count == 2))
            {
                Token token = tokenList[tokenList.Count - 1];

                if(token.data == "~")
                    return new Reverse(new List<Operand>() { BuildOperandTree(tokenList.Take(tokenList.Count - 1).ToList()) });

                throw new ParseException(string.Format("Encountered unary operator ({0}) that isn't recognized on the right.", token.data));
            }
            else if (tokenList[0].kind == Token.Kind.SYMBOL && tokenList[1].kind == Token.Kind.LEFT_PARAN && tokenList[tokenList.Count - 1].kind == Token.Kind.RIGHT_PARAN)
            {
                Token token = tokenList[0];

                Operation operation = null;
                if (token.data == "reverse" || token.data == "rev")
                    operation = new Reverse();
                else if (token.data == "inverse" || token.data == "inv")
                    operation = new Inverse();
                else if (token.data == "grade")
                    operation = new GradePart();
                else if (token.data == "assign")
                    operation = new Assignment();
                else if (token.data == "equiv")
                    operation = new Assignment(false);
                else if (context != null)
                    operation = context.CreateFunction(token.data);

                if(operation == null)
                    throw new ParseException(string.Format("Encountered unknown function \"{0}\".", token.data));

                List<int> argBoundaryList = new List<int>() { 1 };
                
                foreach(Token delimeterToken in WalkTokensSkipSubexpressions(tokenList.Skip(2).Take(tokenList.Count - 3).ToList()))
                {
                    if(delimeterToken.kind == Token.Kind.DELIMITER)
                    {
                        int i = tokenList.IndexOf(delimeterToken);
                        argBoundaryList.Add(i);
                    }
                }

                argBoundaryList.Add(tokenList.Count - 1);

                for(int i = 0; i < argBoundaryList.Count - 1; i++)
                {
                    List<Token> argTokenList = tokenList.Skip(argBoundaryList[i] + 1).Take(argBoundaryList[i + 1] - argBoundaryList[i] - 1).ToList();
                    if(argTokenList.Count > 0)
                        operation.operandList.Add(BuildOperandTree(argTokenList));
                }

                return operation;
            }
            else
            {
                // Our goal here is to find an operator of lowest precedence.  It will never be
                // at the very beginning or end of the entire token sequence.
                List<Token> opTokenList = null;
                foreach(Token token in WalkTokensSkipSubexpressions(tokenList))
                {
                    if (token.kind == Token.Kind.OPERATOR && tokenList.IndexOf(token) != 0 && tokenList.IndexOf(token) != tokenList.Count - 1)
                    {
                        if (opTokenList == null || PrecedenceLevel(opTokenList[0].data) > PrecedenceLevel(token.data))
                            opTokenList = new List<Token>() { token };
                        else if(opTokenList != null && PrecedenceLevel(opTokenList[0].data) == PrecedenceLevel(token.data))
                            opTokenList.Add(token);
                    }
                }

                if (opTokenList == null)
                    throw new ParseException("Did not encounter binary operator token.");

                Token operatorToken = null;
                switch(OperatorAssociativity(opTokenList[0].data))
                {
                    case Associativity.LEFT_TO_RIGHT:
                        operatorToken = opTokenList[opTokenList.Count - 1];
                        break;
                    case Associativity.RIGHT_TO_LEFT:
                        operatorToken = opTokenList[0];
                        break;
                }

                Operation operation = null;

                if (operatorToken.data == "+" || operatorToken.data == "-")
                    operation = new Sum();
                else if (operatorToken.data == "*" || operatorToken.data == "/")
                    operation = new GeometricProduct();
                else if (operatorToken.data == ".")
                    operation = new InnerProduct();
                else if (operatorToken.data == "^")
                    operation = new OuterProduct();
                else if (operatorToken.data == "=")
                    operation = new Assignment();
                else if (operatorToken.data == ":=")
                    operation = new Assignment(false);

                if (operation == null)
                    throw new ParseException(string.Format("Did not recognized operator token ({0}).", operatorToken.data));

                int i = tokenList.IndexOf(operatorToken);

                Operand leftOperand = BuildOperandTree(tokenList.Take(i).ToList());
                Operand rightOperand = BuildOperandTree(tokenList.Skip(i + 1).Take(tokenList.Count - 1 - i).ToList());

                operation.operandList.Add(leftOperand);

                if(operatorToken.data == "-")
                {
                    operation.operandList.Add(new GeometricProduct(new List<Operand>() { new NumericScalar(-1.0), rightOperand }));
                }
                else if(operatorToken.data == "/")
                {
                    operation.operandList.Add(new Inverse(new List<Operand>() { rightOperand }));
                }
                else
                {
                    operation.operandList.Add(rightOperand);
                }

                return operation;
            }
        }

        public IEnumerable<Token> WalkTokensSkipSubexpressions(List<Token> tokenList)
        {
            for(int i = 0; i < tokenList.Count; i++)
            {
                Token token = tokenList[i];

                if(token.kind == Token.Kind.LEFT_PARAN)
                {
                    i = FindMatchingParan(tokenList, i);
                    continue;
                }

                yield return token;
            }
        }

        public int FindMatchingParan(List<Token> tokenList, int i)
        {
            int j = 0;

            while(true)
            {
                Token token = tokenList[i];

                if(token.kind == Token.Kind.LEFT_PARAN)
                    j++;
                else if(token.kind == Token.Kind.RIGHT_PARAN)
                {
                    j--;

                    if(j <= 0)
                        break;
                }

                if(i < tokenList.Count - 1)
                    i++;
                else
                    break;
            }

            if (j != 0)
                throw new ParseException("Encountered unbalanced parenthesis.");

            return i;
        }
    }
}
