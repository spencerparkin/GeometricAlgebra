﻿using System;
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
                    operandArray[i, j] = new NumericScalar(0.0);
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
                    catch(Exception exc)    // TODO: Catch specific exception here.
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
                    string printedMatrix = @"\left[\begin{array}{" + new string('c', this.cols) + "}";
                    for(int i = 0; i < rows; i++)
                    {
                        List<string> printedRowList = new List<string>();
                        for(int j = 0; j < cols; j++)
                            printedRowList.Add(operandArray[i, j].Print(format, context));

                        printedMatrix += string.Join("&", printedRowList);
                        if(i + 1 < rows)
                            printedMatrix += @"\\";
                    }

                    printedMatrix += @"\end{array}\right]";
                    return printedMatrix;
                }
            }

            return "?";
        }

        public override Operand Inverse(Context context)
        {
            if(rows != cols)
                throw new MathException("Cannot yet invert non-square matrices.");

            // TODO: Return adjoint over determinant.
            return null;
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
    }
}
