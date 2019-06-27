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
            RIGHT_PARAN
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
        public Parser()
        {
        }

        public Operand Parse(string expression)
        {
            List<Token> tokenList = Tokenize(expression);
            Operand root = BuildOperandTree(tokenList);
            return root;
        }

        public List<Token> Tokenize(string expression)
        {
            var tokenList = new List<Token>();

            expression = expression.Replace(" ", "");

            List<char> charList = expression.ToArray().ToList();

            while (charList.Count > 0)
                tokenList.Add(EatToken(charList));

            return tokenList;
        }

        private Token EatToken(List<char> charList)
        {
            List<char> operatorList = new List<char>() { '+', '-', '*', '^', '.', '~', '!' };

            char ch = charList[0];

            if (operatorList.Contains(ch))
            {
                charList.RemoveAt(0);
                return new Token(Token.Kind.OPERATOR, ch.ToString());
            }

            if (ch == '(')
            {
                charList.RemoveAt(0);
                return new Token(Token.Kind.LEFT_PARAN, ch.ToString());
            }

            if (ch == ')')
            {
                charList.RemoveAt(0);
                return new Token(Token.Kind.RIGHT_PARAN, ch.ToString());
            }

            if (Char.IsLetterOrDigit(ch))
            {
                Token token = null;

                if(Char.IsDigit(ch))
                    token = new Token(Token.Kind.NUMBER);
                else if(Char.IsLetter(ch))
                    token = new Token(Token.Kind.SYMBOL);

                while(charList.Count > 0)
                {
                    ch = charList[0];
                    if (Char.IsLetterOrDigit(ch))
                    {
                        token.data += ch;
                        charList.RemoveAt(0);
                    }
                    else
                        break;
                }

                return token;
            }

            throw new ParseException(string.Format("Failed to tokenize at \"{0}\".", string.Join("", charList)));
        }

        private int PrecedenceLevel(string operation)
        {
            if (operation == "+")
                return 0;
            if (operation == ".")
                return 1;
            if (operation == "^")
                return 2;
            if (operation == "*")
                return 3;

            throw new ParseException(string.Format("Cannot determine precedence level of \"{0}\".", operation));
        }

        public Operand BuildOperandTree(List<Token> tokenList)
        {
            while (tokenList.Count > 0)
            {
                if (tokenList[0].kind == Token.Kind.LEFT_PARAN && tokenList[tokenList.Count - 1].kind == Token.Kind.RIGHT_PARAN)
                {
                    tokenList.RemoveAt(0);
                    tokenList.RemoveAt(tokenList.Count - 1);
                }
                else
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
                        // TODO: Here we might differentiate between symbolic scalars and symbolic vectors.
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
            else if (tokenList[0].kind == Token.Kind.OPERATOR && (tokenList[1].kind == Token.Kind.LEFT_PARAN || tokenList.Count == 2))
            {
                Token token = tokenList[0];

                if (token.data == "-")
                    return new GeometricProduct(new List<Operand>() { new Blade(-1.0), BuildOperandTree(tokenList.Skip(1).ToList()) });

                throw new ParseException(string.Format("Encounterd unary operator ({0}) that isn't recognized on the left.", token.data));
            }
            else if (tokenList[tokenList.Count - 1].kind == Token.Kind.OPERATOR && (tokenList[tokenList.Count - 2].kind == Token.Kind.RIGHT_PARAN || tokenList.Count == 2))
            {
                Token token = tokenList[tokenList.Count - 1];

                // TODO: Should handle ~ unary operator here.

                throw new ParseException(string.Format("Encountered unary operator ({0}) that isn't recognized on the right.", token.data));
            }
            else
            {
                // Our goal here is to find the operator of lowest precedence.  It will never be
                // at the beginning or end of the entire token sequence.

                int j;
                Token operatorToken = null;

                for(int i = 1; i < tokenList.Count - 1; i++)
                {
                    Token token = tokenList[i];

                    if (token.kind == Token.Kind.OPERATOR)
                    {
                        if (operatorToken == null || PrecedenceLevel(operatorToken.data) > PrecedenceLevel(token.data))
                            operatorToken = token;
                    }
                    else if (token.kind == Token.Kind.LEFT_PARAN)
                    {
                        j = 0;
                        while (true)
                        {
                            if (token.kind == Token.Kind.LEFT_PARAN)
                                j++;
                            else if (token.kind == Token.Kind.RIGHT_PARAN)
                                j--;

                            if (j == 0)
                                break;

                            if (i == tokenList.Count - 1)
                                throw new ParseException("Encountered unbalanced parenthesis.");

                            token = tokenList[++i];
                        }
                    }
                }

                if (operatorToken == null)
                    throw new ParseException("Did not encounter operator token.");

                Operation operation = null;

                if (operatorToken.data == "+")
                    operation = new Sum();
                else if (operatorToken.data == "*")
                    operation = new GeometricProduct();
                else if (operatorToken.data == ".")
                    operation = new InnerProduct();
                else if (operatorToken.data == "^")
                    operation = new OuterProduct();

                if (operation == null)
                    throw new ParseException(string.Format("Did not recognized operator token ({0}).", operatorToken.data));

                j = tokenList.IndexOf(operatorToken);

                Operand leftOperand = BuildOperandTree(tokenList.Take(j).ToList());
                Operand rightOperand = BuildOperandTree(tokenList.Skip(j + 1).Take(tokenList.Count - 1 - j).ToList());

                operation.operandList.Add(leftOperand);
                operation.operandList.Add(rightOperand);

                return operation;
            }
        }
    }
}
