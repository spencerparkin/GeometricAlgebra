using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GeometricAlgebra.ConformalModel
{
    public class Conformal3D_EvaluationContext : EvaluationContext
    {
        public Conformal3D_EvaluationContext() : base()
        {
            operandStorage.Add("i", Operand.FullyEvaluate("e1^e2^e3", this));
            operandStorage.Add("I", Operand.FullyEvaluate("e1^e2^e3^no^ni", this));
        }

        public override Operand BilinearForm(string vectorNameA, string vectorNameB)
        {
            if (vectorNameA == "e1" || vectorNameA == "e2" || vectorNameA == "e3")
            {
                if (vectorNameB == "no" || vectorNameB == "ni")
                    return new NumericScalar(0.0);
            }

            if (vectorNameA == "no" || vectorNameA == "ni")
            {
                if (vectorNameB == "e1" || vectorNameB == "e2" || vectorNameB == "e3")
                    return new NumericScalar(0.0);

                if (vectorNameB == "no" || vectorNameB == "ni")
                {
                    if (vectorNameA == vectorNameB)
                        return new NumericScalar(0.0);
                    else
                        return new NumericScalar(-1.0);
                }
            }

            return base.BilinearForm(vectorNameA, vectorNameB);
        }

        public override Operation CreateFunction(string name)
        {
            if (name == "vec")
                return new EuclidVector();
            else if (name == "point")
                return new Point();
            else if (name == "sphere")
                return new Sphere();
            else if (name == "isphere")
                return new Sphere(true);
            else if (name == "plane")
                return new Plane();
            else if (name == "line")
                return new Line();
            else if (name == "circle")
                return new Circle();
            else if (name == "icircle")
                return new Circle(true);
            else if (name == "pointpair")
                return new PointPair();
            else if (name == "ipointpair")
                return new PointPair(true);
            else if (name == "flatpoint")
                return new FlatPoint();
            else if (name == "tangentpoint")
                return new TangentPoint();
            else if (name == "decompose")
                return new Decompose();

            return null;
        }
    }

    public abstract class Function : Operation
    {
        public Function() : base()
        {
        }

        public Function(List<Operand> operandList) : base(operandList)
        {

        }

        public override bool IsDistributiveOver(Operation operation)
        {
            return false;
        }

        public override bool IsAssociative()
        {
            return false;
        }

        public Operand GrabArg(string name, EvaluationContext context, Operand defaultOperand)
        {
            if (operandList.Count != 1 || !(operandList[0] is Variable variable))
                throw new EvaluationException("Expected varaible argument.");

            string key = variable.name + name;
            if (!context.operandStorage.ContainsKey(key))
                return defaultOperand;

            return context.operandStorage[key];
        }
    }

    public class EuclidVector : Function
    {
        public EuclidVector() : base()
        {
        }

        public override Operand New()
        {
            return new EuclidVector();
        }

        public override Operand Evaluate(EvaluationContext context)
        {
            if (operandList.Count != 3)
                throw new EvaluationException(string.Format("Expected 3 arguments, got {0}.", operandList.Count));

            Operand operand = base.Evaluate(context);
            if (operand != null)
                return operand;

            Operand x = operandList[0];
            Operand y = operandList[1];
            Operand z = operandList[2];

            if (x.Grade != 0 || y.Grade != 0 || z.Grade != 0)
                throw new EvaluationException("Scalars expected for eucliden vector components.");

            Conformal3D_EvaluationContext tempContext = new Conformal3D_EvaluationContext();
            tempContext.operandStorage.Add("x", x);
            tempContext.operandStorage.Add("y", y);
            tempContext.operandStorage.Add("z", z);
            return Operand.FullyEvaluate("@x*e1 + @y*e2 + @z*e3", tempContext);
        }
    }

    public abstract class RoundFunction : Function
    {
        public bool imaginary;

        public RoundFunction(bool imaginary = false) : base()
        {
            this.imaginary = imaginary;
        }

        public RoundFunction(List<Operand> operandList, bool imaginary = false) : base(operandList)
        {
            this.imaginary = imaginary;
        }

        public override Operand Copy()
        {
            RoundFunction func = base.Copy() as RoundFunction;
            func.imaginary = this.imaginary;
            return func;
        }
    }

    public class Point : Function
    {
        public Point() : base()
        {
        }

        public Point(List<Operand> operandList) : base(operandList)
        {
        }

        public override Operand New()
        {
            return new Point();
        }

        public override Operand Evaluate(EvaluationContext context)
        {
            Operand operand = base.Evaluate(context);
            if (operand != null)
                return operand;

            Operand euclidPoint = GrabArg("Center", context, new NumericScalar(0.0));
            Operand weight = GrabArg("Weight", context, new NumericScalar(1.0));

            EvaluationContext tempContext = new Conformal3D_EvaluationContext();
            tempContext.operandStorage.Add("c", euclidPoint);
            tempContext.operandStorage.Add("w", weight);
            return Operand.FullyEvaluate("@w*(no + @c + 0.5*(@c.@c)*ni)", tempContext);
        }
    }

    public class Sphere : RoundFunction
    {
        public Sphere(bool imaginary = false) : base(imaginary)
        {
        }

        public Sphere(List<Operand> operandList, bool imaginary = false) : base(operandList, imaginary)
        {
        }

        public override Operand New()
        {
            return new Sphere();
        }

        public override Operand Evaluate(EvaluationContext context)
        {
            Operand operand = base.Evaluate(context);
            if (operand != null)
                return operand;

            Operand euclidPoint = GrabArg("Center", context, new NumericScalar(0.0));
            Operand radius = GrabArg("Radius", context, new NumericScalar(1.0));
            Operand weight = GrabArg("Weight", context, new NumericScalar(1.0));

            EvaluationContext tempContext = new Conformal3D_EvaluationContext();
            tempContext.operandStorage.Add("c", euclidPoint);
            tempContext.operandStorage.Add("r", radius);
            tempContext.operandStorage.Add("w", weight);
            return Operand.FullyEvaluate(string.Format("@w*(no + @c + 0.5*(@c.@c {0} @r*@r)*ni)", imaginary ? "+" : "-"), tempContext);
        }
    }

    public class Plane : Function
    {
        public Plane() : base()
        {
        }

        public Plane(List<Operand> operandList) : base(operandList)
        {
        }

        public override Operand New()
        {
            return new Plane();
        }

        public override Operand Evaluate(EvaluationContext context)
        {
            Operand operand = base.Evaluate(context);
            if (operand != null)
                return operand;

            Operand euclidPoint = GrabArg("Center", context, new NumericScalar(0.0));
            Operand euclidNormal = GrabArg("Normal", context, new NumericScalar(0.0));
            Operand weight = GrabArg("Weight", context, new NumericScalar(1.0));

            EvaluationContext tempContext = new Conformal3D_EvaluationContext();
            tempContext.operandStorage.Add("c", euclidPoint);
            tempContext.operandStorage.Add("n", euclidNormal);
            tempContext.operandStorage.Add("w", weight);
            return Operand.FullyEvaluate("@w*(@n + (@c.@n)*ni)", tempContext);
        }
    }

    public class Circle : RoundFunction
    {
        public Circle(bool imaginary = false) : base(imaginary)
        {
        }

        public Circle(List<Operand> operandList, bool imaginary = false) : base(operandList, imaginary)
        {
        }

        public override Operand New()
        {
            return new Circle();
        }

        public override Operand Evaluate(EvaluationContext context)
        {
            Operand operand = base.Evaluate(context);
            if (operand != null)
                return operand;

            Operand euclidPoint = GrabArg("Center", context, new NumericScalar(0.0));
            Operand euclidNormal = GrabArg("Normal", context, new NumericScalar(0.0));
            Operand radius = GrabArg("Radius", context, new NumericScalar(1.0));
            Operand weight = GrabArg("Weight", context, new NumericScalar(1.0));

            OuterProduct circle = new OuterProduct();
            circle.operandList.Add(new Sphere(new List<Operand>() { euclidPoint, radius, weight }, imaginary));
            circle.operandList.Add(new Plane(new List<Operand>() { euclidPoint, euclidNormal }));
            return circle;
        }
    }

    public class TangentPoint : Function
    {
        public TangentPoint() : base()
        {
        }

        public TangentPoint(List<Operand> operandList) : base(operandList)
        {
        }

        public override Operand New()
        {
            return new TangentPoint();
        }

        public override Operand Evaluate(EvaluationContext context)
        {
            Operand operand = base.Evaluate(context);
            if (operand != null)
                return operand;

            Operand euclidPoint = GrabArg("Center", context, new NumericScalar(0.0));
            Operand euclidNormal = GrabArg("Normal", context, new NumericScalar(0.0));
            Operand weight = GrabArg("Weight", context, new NumericScalar(1.0));

            return new Circle(new List<Operand>() { euclidPoint, euclidNormal, new NumericScalar(0.0), weight });
        }
    }

    public class Line : Function
    {
        public Line() : base()
        {
        }

        public Line(List<Operand> operandList) : base(operandList)
        {
        }

        public override Operand New()
        {
            return new Line();
        }

        public override Operand Evaluate(EvaluationContext context)
        {
            Operand operand = base.Evaluate(context);
            if (operand != null)
                return operand;

            Operand euclidPoint = GrabArg("Center", context, new NumericScalar(0.0));
            Operand euclidNormal = GrabArg("Normal", context, new NumericScalar(0.0));
            Operand weight = GrabArg("Weight", context, new NumericScalar(1.0));

            EvaluationContext tempContext = new Conformal3D_EvaluationContext();
            tempContext.operandStorage.Add("c", euclidPoint);
            tempContext.operandStorage.Add("n", euclidNormal);
            tempContext.operandStorage.Add("w", weight);
            return Operand.FullyEvaluate("@w*(@n + (@c^@n)*ni)*i", tempContext);
        }
    }

    public class PointPair : RoundFunction
    {
        public PointPair(bool imaginary = false) : base(imaginary)
        {
        }

        public PointPair(List<Operand> operandList, bool imaginary = false) : base(operandList, imaginary)
        {
        }

        public override Operand New()
        {
            return new PointPair();
        }

        public override Operand Evaluate(EvaluationContext context)
        {
            Operand operand = base.Evaluate(context);
            if (operand != null)
                return operand;

            Operand euclidPoint = GrabArg("Center", context, new NumericScalar(0.0));
            Operand euclidNormal = GrabArg("Normal", context, new NumericScalar(0.0));
            Operand radius = GrabArg("Radius", context, new NumericScalar(1.0));
            Operand weight = GrabArg("Weight", context, new NumericScalar(1.0));

            OuterProduct pointPair = new OuterProduct();
            pointPair.operandList.Add(new Sphere(new List<Operand>() { euclidPoint, radius, weight }, imaginary));
            pointPair.operandList.Add(new Line(new List<Operand>() { euclidPoint, euclidNormal }));
            return pointPair;
        }
    }

    public class FlatPoint : Function
    {
        public FlatPoint() : base()
        {
        }

        public FlatPoint(List<Operand> operandList) : base(operandList)
        {
        }

        public override Operand New()
        {
            return new FlatPoint();
        }

        public override Operand Evaluate(EvaluationContext context)
        {
            Operand operand = base.Evaluate(context);
            if (operand != null)
                return operand;

            Operand euclidPoint = GrabArg("Center", context, new NumericScalar(0.0));
            Operand weight = GrabArg("Weight", context, new NumericScalar(1.0));

            // This is the outer product of a plane and a line.  The normal cancels.
            EvaluationContext tempContext = new Conformal3D_EvaluationContext();
            tempContext.operandStorage.Add("c", euclidPoint);
            tempContext.operandStorage.Add("w", weight);
            return Operand.FullyEvaluate("@w*(1 - @c^ni)*i", tempContext);
        }
    }

    public class Decompose : Function
    {
        public Decompose() : base()
        {
        }

        public Decompose(List<Operand> operandList) : base(operandList)
        {
        }

        public override Operand New()
        {
            return new Decompose();
        }

        public override Operand Evaluate(EvaluationContext context)
        {
            Operand operand = base.Evaluate(context);
            if (operand != null)
                return operand;

            // TODO: Here we take an operand to decompose and then a variable to use as a prefix.
            //       Here we identify the given element and store its decomposition.  So if the
            //       variable given was @_, we create the variables @_Center, @_Radius, @_Normal, @_Weight,
            //       setting each to a value appropriate to the given element.  Creating these variable is convenient,
            //       because all composition functions take their input in the same form as our output here.
            //       We might also add a log to the evaluation context where we can spew the decomposition data.
            //       In this log I would give, in addition to the decomposition parameters, the type of geometry.

            return null;
        }
    }
}