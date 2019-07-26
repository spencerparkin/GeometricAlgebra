using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GeometricAlgebra
{
    public class Matrix : Operand
    {
        private Operand[,] operandArray;
        private int rows, cols;

        public Matrix() : base()
        {
            operandArray = null;
            rows = 0;
            cols = 0;
        }

        public Matrix(int rows, int cols) : base()
        {
            this.rows = rows;
            this.cols = cols;

            operandArray = new Operand[rows, cols];
            for(int i = 0; i < rows; i++)
                for(int j = 0; j < cols; j++)
                    operandArray[i, j] = null;
        }

        public Matrix(Matrix matrix, int row, int col) : base()
        {
            if(matrix.rows <= 1 || matrix.cols <= 1)
                throw new MathException($"Cannot take proper sub-matrix of {matrix.rows}x{matrix.cols} matrix in both dimensions.");

            this.rows = matrix.rows - 1;
            this.cols = matrix.cols - 1;
            
            operandArray = new Operand[rows, cols];
            for (int i = 0; i < rows; i++)
                for(int j = 0; j < cols; j++)
                    operandArray[i, j] = matrix.operandArray[i < row ? i : i + 1, j < col ? j : j + 1].Copy();
        }

        public Matrix(Matrix matrix, Type type) : base()
        {
            this.rows = matrix.rows;
            this.cols = matrix.cols;

            operandArray = new Operand[rows, cols];
            for (int i = 0; i < rows; i++)
            {
                for (int j = 0; j < cols; j++)
                {
                    Operation operation = Activator.CreateInstance(type) as Operation;
                    operation.operandList.Add(matrix.operandArray[i, j].Copy());
                    operandArray[i, j] = operation;
                }
            }
        }

        public Matrix(List<List<Operand>> listOfOperandLists)
        {
            this.rows = listOfOperandLists.Count;
            this.cols = 0;
            for(int i = 0; i < rows; i++)
                if(listOfOperandLists[i].Count > cols)
                    cols = listOfOperandLists[i].Count;

            operandArray = new Operand[rows, cols];
            for(int i = 0; i < rows; i++)
            {
                for(int j = 0; j < cols; j++)
                {
                    try
                    {
                        operandArray[i, j] = listOfOperandLists[i][j];
                    }
                    catch(Exception)    // TODO: Catch specific out-of-bounds exception here.
                    {
                        operandArray[i, j] = new NumericScalar(0.0);
                    }
                }
            }
        }

        public override Operand Copy()
        {
            Matrix matrix = new Matrix(this.rows, this.cols);
            for(int i = 0; i < rows; i++)
                for(int j = 0; j < cols; j++)
                    matrix.operandArray[i, j] = operandArray[i, j].Copy();

            return matrix;
        }

        public override Operand New()
        {
            return new Matrix();
        }

        public override Operand EvaluationStep(Context context)
        {
            int count = 0;

            for(int i = 0; i < rows; i++)
            {
                for(int j = 0; j < cols; j++)
                {
                    Operand operand = operandArray[i, j].EvaluationStep(context);
                    if(operand != null)
                    {
                        operandArray[i, j] = operand;
                        count++;
                    }
                }
            }

            if(count > 0)
                return this;

            return null;
        }

        public override string Print(Format format, Context context = null)
        {
            switch(format)
            {
                case Format.PARSEABLE:
                {
                    List<string> rowList = new List<string>();
                    for(int i = 0; i < rows; i++)
                    {
                        List<string> colList = new List<string>();
                        for(int j = 0; j < cols; j++)
                            colList.Add(operandArray[i, j].Print(format, context));

                        rowList.Add("[" + string.Join(", ", colList) + "]");
                    }

                    return "[" + string.Join(", ", rowList) + "]";
                }
                case Format.LATEX:
                {
                    return PrintLatex(@"\left[", @"\right]", context);
                }
            }

            return "?";
        }

        public string PrintLatex(string leftBracket, string rightBracket, Context context)
        {
            string printedMatrix = leftBracket + @"\begin{array}{" + new string('c', this.cols) + "}";
            for (int i = 0; i < rows; i++)
            {
                List<string> printedRowList = new List<string>();
                for (int j = 0; j < cols; j++)
                    printedRowList.Add(operandArray[i, j].Print(Format.LATEX, context));

                printedMatrix += string.Join("&", printedRowList);
                if (i + 1 < rows)
                    printedMatrix += @"\\";
            }

            printedMatrix += @"\end{array}" + rightBracket;
            return printedMatrix;
        }

        public override Operand Inverse(Context context)
        {
            if(rows != cols)
                throw new MathException("Cannot invert non-square matrices.");  // TODO: Psuedo-inverse?

            return new GeometricProduct(new List<Operand>() { Adjugate(), new Inverse(new List<Operand>() { Determinant() }) });
        }

        public override Operand Reverse()
        {
            // You should always be able to multiply a matrix by its reverse, so I think we need the transpose.
            Matrix matrix = this.Transpose() as Matrix;
            for(int i = 0; i < matrix.rows; i++)
                for(int j = 0; j < matrix.cols; j++)
                    matrix.operandArray[i, j] = new Reverse(new List<Operand>() { matrix.operandArray[i, j] });

            return matrix;
        }

        public Operand Transpose()
        {
            Matrix matrix = new Matrix(this.cols, this.rows);
            for (int i = 0; i < rows; i++)
                for (int j = 0; j < cols; j++)
                    matrix.operandArray[j, i] = operandArray[i, j].Copy();

            return matrix;
        }

        public Operand Adjugate()
        {
            Matrix matrix = new Matrix(this.cols, this.rows);
            for(int i = 0; i < rows; i++)
                for(int j = 0; j < cols; j++)
                    matrix.operandArray[i, j] = Cofactor(i, j);

            return matrix;
        }

        public Operand Cofactor(int row, int col)
        {
            Matrix matrix = new Matrix(this, row, col);
            if((row + col) % 2 == 1)
                return new GeometricProduct(new List<Operand>() { new NumericScalar(-1.0), matrix.Determinant() });

            return matrix.Determinant();
        }

        public Operand Determinant()
        {
            if(rows != cols)
                throw new MathException("Cannot take determinant of non-square matrix.");

            if(rows == 1)
                return operandArray[0, 0].Copy();

            int bestRow = -1;
            int largestRowCount = -1;

            for(int i = 0; i < rows; i++)
            {
                int count = 0;
                for(int j = 0; j < cols; j++)
                    if(operandArray[i, j].IsAdditiveIdentity)
                        count++;

                if(count == cols)
                    return new NumericScalar(0.0);

                if(count > largestRowCount)
                {
                    largestRowCount = count;
                    bestRow = i;
                }
            }

            int bestCol = -1;
            int largestColCount = -1;

            for(int j = 0; j < cols; j++)
            {
                int count = 0;
                for(int i = 0; i < rows; i++)
                    if(operandArray[i, j].IsAdditiveIdentity)
                        count++;

                if(count == rows)
                    return new NumericScalar(0.0);

                if(count > largestColCount)
                {
                    largestColCount = j;
                    bestCol = j;
                }
            }

            Sum determinant = new Sum();

            if(largestRowCount >= largestColCount)
            {
                for (int j = 0; j < cols; j++)
                    determinant.operandList.Add(new GeometricProduct(new List<Operand>() { operandArray[bestRow, j].Copy(), Cofactor(bestRow, j) }));
            }
            else
            {
                for (int i = 0; i < rows; i++)
                    determinant.operandList.Add(new GeometricProduct(new List<Operand>() { operandArray[i, bestCol].Copy(), Cofactor(i, bestCol) }));
            }

            return determinant;
        }
    }
}
