using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using System.Diagnostics;
using System.ComponentModel;

namespace JA.UI
{

    /// <summary>
    /// A point defined by the coefficients a, b, c with coordinates <c>x=-b/c</c> and <c>y=a/c</c>
    /// </summary>
    [TypeConverter(typeof(ExpandableObjectConverter))]
    [DebuggerDisplay("{Center}")]
    public struct Point2 : IGeometry, ISelectable
    {
        public readonly float a, b, c;

        public Point2(float a, float b, float c)
        {
            this.a = a;
            this.b = b;
            this.c = c;
        }
        public Point2(float x, float y) : this(x,y,1) 
        { }
        public Point2(PointF point) : this(point.X, point.Y)
        { }

        public PointF Center
        {
            get { return new PointF(a/c, b/c); }
        }
        public PointF Normal
        {
            get { return new PointF(a/Magnitude, b/Magnitude); }
        }
        public PointF Direction
        {
            get { return new PointF(-b/Magnitude, a/Magnitude); }
        }
        public float Magnitude { get { return (float)Math.Sqrt(SumSquare); } }
        public float SumSquare { get { return a*a+b*b; } }

        public float DistanceToOrigin { get { return Magnitude/c; } }

        public Point2 Add(Point2 other)
        {
            return new Point2(a*other.c+other.a*c, b*other.c+other.b*c, c*other.c);
        }
        public Point2 Subtract(Point2 other)
        {
            return new Point2(a*other.c-other.a*c, b*other.c-other.b*c, c*other.c);
        }

        public static Point2 operator+(Point2 lhs, Point2 rhs)
        {
            return lhs.Add(rhs);
        }
        public static Point2 operator-(Point2 lhs, Point2 rhs)
        {
            return lhs.Subtract(rhs);
        }

        public Point2 LocalPoint(float away, float along)
        {
            // r + n*u + e*v
            //
            // m*a + c*(a*u - b*v)
            // m*b + c*(b*u + a*v)
            // m*c

            float m = Magnitude;

            return new Point2(
                m*a+c*(a*away-b*along),
                m*b+c*(b*away+a*along),
                m*c);
        }

        #region IDrawable Members


        public bool Hit(PointF point, float width)
        {
            var cen=Center;
            var del=new PointF(point.X-cen.X, point.Y-cen.Y);
            return Math.Sqrt(del.X*del.X+del.Y*del.Y)<=width/2;
        }

        #endregion
    }

}
