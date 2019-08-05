using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.LinearAlgebra.Double;

namespace GeometricAlgebra
{
    public class Matrix : Operand
    {
        private Operand[,] operandArray;
        private int rows, cols;

        public int Rows { get { return rows; } }
        public int Cols { get { return cols; } }

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

        public Matrix(Matrix<double> matrix) : base()
        {
            this.rows = matrix.RowCount;
            this.cols = matrix.ColumnCount;

            operandArray = new Operand[rows, cols];
            for (int i = 0; i < rows; i++)
                for (int j = 0; j < cols; j++)
                    operandArray[i, j] = new NumericScalar(matrix.At(i, j));
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
                        {
                            Operand element = operandArray[i, j];
                            if(element != null)
                                colList.Add(operandArray[i, j].Print(format, context));
                            else
                                colList.Add("?");
                        }

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

        public void SetElement(int row, int col, Operand operand)
        {
            operandArray[row, col] = operand;
        }

        public Operand GetElement(int row, int col)
        {
            return operandArray[row, col];
        }

        public IEnumerable<Operand> YieldAllElements()
        {
            for(int i = 0; i < rows; i++)
                for(int j = 0; j < cols; j++)
                    yield return operandArray[i, j];
        }

        public Matrix<double> GenerateNumericMatrix()
        {
            try
            {
                Matrix<double> numericMatrix = DenseMatrix.Create(rows, cols, 0.0);
                for (int i = 0; i < rows; i++)
                    for (int j = 0; j < cols; j++)
                        numericMatrix.At(i, j, (operandArray[i, j] as NumericScalar).value);

                return numericMatrix;
            }
            catch(Exception)    // TODO: What's the type-cast exception?
            {
                return null;
            }
        }

        public override Operand Inverse(Context context)
        {
            if(YieldAllElements().All(operand => operand is NumericScalar))
            {
                Matrix<double> numericMatrix = GenerateNumericMatrix();

                double det = numericMatrix.Determinant();
                if(Math.Abs(det) < context.epsilon)
                    throw new MathException("Cannot invert singular matrix or very-near singular matrices.");

                try
                {
                    double detReciprical = 1.0 / det;
                }
                catch(DivideByZeroException)
                {
                    throw new MathException("Cannot take recriprical of determinant.");
                }

                Matrix<double> numericMatrixInverse;
                if (rows == cols)
                    numericMatrixInverse = numericMatrix.Inverse();
                else
                    numericMatrixInverse = numericMatrix.PseudoInverse();
                
                return new Matrix(numericMatrixInverse);
            }

            if (rows != cols)
                throw new MathException("Cannot invert non-square matrices.");  // TODO: Psuedo-inverse?

            // TODO: Use QR factorization for symbolic matrices of large size.

            // Note that this, while algebraically correct, is completely impractical for large matrices.  It's probably also numerically unstable.
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
            Matrix matrix = null;

            if(this.rows > 1 && this.cols > 1)
            {
                matrix = new Matrix(this.cols, this.rows);
                for (int i = 0; i < matrix.rows; i++)
                    for(int j = 0; j < matrix.cols; j++)
                        matrix.operandArray[i, j] = Cofactor(j, i);
            }
            else
            {
                matrix = this.Copy() as Matrix;
            }

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
                    if(!operandArray[bestRow, j].IsAdditiveIdentity)
                        determinant.operandList.Add(new GeometricProduct(new List<Operand>() { operandArray[bestRow, j].Copy(), Cofactor(bestRow, j) }));
            }
            else
            {
                for (int i = 0; i < rows; i++)
                    if(!operandArray[i, bestCol].IsAdditiveIdentity)
                        determinant.operandList.Add(new GeometricProduct(new List<Operand>() { operandArray[i, bestCol].Copy(), Cofactor(i, bestCol) }));
            }

            return determinant;
        }

        public Matrix Scale(Operand scalar)
        {
            Matrix matrix = new Matrix(rows, cols);
            for(int i = 0; i < rows; i++)
                for(int j = 0; j < cols; j++)
                    matrix.operandArray[i, j] = new GeometricProduct(new List<Operand>() { scalar.Copy(), operandArray[i, j].Copy() });

            return matrix;
        }

        public static Matrix Multiply(Matrix matrixA, Matrix matrixB, Type productType)
        {
            if(matrixA.cols != matrixB.rows)
                throw new MathException($"Cannot multiply {matrixA.rows}x{matrixA.cols} matrix with a {matrixB.rows}x{matrixB.cols} matrix.");

            int count = matrixA.cols;
            Matrix matrix = new Matrix(matrixA.rows, matrixB.cols);

            for(int i = 0; i < matrix.rows; i++)
            {
                for(int j = 0; j < matrix.cols; j++)
                {
                    Sum sum = new Sum();

                    for(int k = 0; k < count; k++)
                    {
                        Product product = Activator.CreateInstance(productType) as Product;
                        product.operandList.Add(matrixA.operandArray[i, k].Copy());
                        product.operandList.Add(matrixB.operandArray[k, j].Copy());
                        sum.operandList.Add(product);
                    }

                    matrix.operandArray[i, j] = sum;
                }
            }

            return matrix;
        }

        public static Matrix Add(Matrix matrixA, Matrix matrixB)
        {
            if(matrixA.rows != matrixB.rows || matrixA.cols != matrixB.cols)
                throw new MathException($"Cannot add {matrixA.rows}x{matrixA.cols} matrix with a {matrixB.rows}x{matrixB.cols} matrix.");

            Matrix matrix = new Matrix(matrixA.rows, matrixA.cols);

            for(int i = 0; i < matrix.rows; i++)
            {
                for(int j = 0; j < matrix.cols; j++)
                {
                    Sum sum = new Sum();
                    sum.operandList.Add(matrixA.operandArray[i, j].Copy());
                    sum.operandList.Add(matrixB.operandArray[i, j].Copy());
                    matrix.operandArray[i, j] = sum;
                }
            }

            return matrix;
        }
    }
}
