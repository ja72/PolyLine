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
    /// A line defined by equation <c>a*x+b*y+c=0</c> with coefficients a, b, c
    /// </summary>
    [TypeConverter(typeof(ExpandableObjectConverter))]
    [DebuggerDisplay("{Center}->{Direction}")]
    public struct Line2 : IGeometry, ISelectable
    {
        public readonly float a, b, c;

        public Line2(float a, float b, float c)
        {
            float m2=(float)Math.Sqrt(a*a+b*b);
            this.a=a/m2;
            this.b=b/m2;
            this.c=c/m2;
        }

        public Line2(PointF point1, PointF point2)
            : this(
            point1.Y-point2.Y,
            point2.X-point1.X,
            point1.X*point2.Y-point2.X*point1.Y)
        { }
        public Line2(Point2 point1, Point2 point2)
            : this(
                point1.b*point2.c-point2.b*point1.c,
                point2.a*point1.c-point1.a*point2.c,
                point1.a*point2.b-point2.a*point1.b)
        { }

        public float SumSquare { get { return a*a+b*b; } }
        public float Magnitude { get { return (float)Math.Sqrt(a*a+b*b); } }

        public float DistanceToOrigin
        {
            get { return c/Magnitude; }
        }

        public PointF Center
        {
            get
            {
                return new PointF(-a*c/SumSquare, -b*c/SumSquare);
            }
        }

        public PointF Direction
        {
            get
            {
                return new PointF(-b/Magnitude, a/Magnitude);
            }
        }

        public PointF Normal
        {
            get
            {
                return new PointF(a/Magnitude, b/Magnitude);
            }
        }

        public float DistanceFromPointAlong(PointF point)
        {
            return (a*point.Y-b*point.X)/Magnitude;
        }
        public float DistanceFromPointNormal(PointF point)
        {
            return (a*point.X+b*point.Y+c)/Magnitude;
        }
        public float DistanceFromPointAlong(Point2 point)
        {
            return (a*point.b-b*point.a)/(Magnitude*point.c);
        }
        public float DistanceFromPointNormal(Point2 point)
        {
            return (a*point.a+b*point.b+c*point.c)/(Magnitude*point.c);
        }
        public Line2 Offset(float distance)
        {
            return new Line2(a, b, c+distance*Magnitude);
        }
        public void Render(Graphics g, Pen pen)
        {
            // Draw infinite line between two far away points
            // on the line.
            g.DrawLine(pen, LocalPoint(0, -1000).Center, LocalPoint(0, 1000).Center);
        }

        public Point2 Intersect(Line2 other)
        {
            float x=b*other.c-other.b*c;
            float y=other.a*c-a*other.c;
            float w=a*other.b-other.a*b;
            return new Point2(x, y, w);
        }

        public LineSeg TrimBetween(Line2 line1, Line2 line2)
        {
            PointF p1=Intersect(line1).Center;
            PointF p2=Intersect(line2).Center;
            return new LineSeg(p1, p2);
        }

        public Point2 LocalPoint(float away, float along)
        {
            // r + n*u + e*v
            //
            // -a*c + m*(a*u-b*v)
            // -b*c + m*(b*u+a*v)
            //  m^2
            
            float m=Magnitude;

            return new Point2(
                -a*c+m*(a*away-b*along),
                -b*c+m*(b*away+a*along),
                m*m);
        }


        #region IDrawable Members


        public bool Hit(PointF point, float width)
        {
            float y=DistanceFromPointNormal(point);
            if(Math.Abs(y)<=(width/2))
            {
                return true;
            }
            return false;
        }

        #endregion
    }
    /// <summary>
    /// Line segment defined by a <see cref="Line"/> and two distances from the line center.
    /// </summary>
    public struct LineSeg : JA.UI.ISelectable
    {
        readonly Line2 line;
        readonly float x1, x2;

        public LineSeg(Line2 line, float x1, float x2)
        {
            this.line=line;
            this.x1=x1;
            this.x2=x2;
        }
        public LineSeg(Point2 start, Point2 end)
        {
            this.line=new Line2(start, end);
            this.x1=line.DistanceFromPointAlong(start);
            this.x2=line.DistanceFromPointAlong(end);
        }
        public LineSeg(PointF start, PointF end)
        {
            this.line=new Line2(start, end);
            this.x1=line.DistanceFromPointAlong(start);
            this.x2=line.DistanceFromPointAlong(end);
        }
        public Line2 Line { get { return line; } }
        public float StartDistance { get { return x1; } }
        public float EndDistance { get { return x2; } }
        public Point2 StartPoint
        {
            get { return line.LocalPoint(0, x1); }
        }
        public Point2 EndPoint
        {
            get { return line.LocalPoint(0, x2); }
        }

        public LineSeg Offset(float amount)
        {
            return new LineSeg(line.Offset(amount), x1, x2);
        }

        #region IDrawable Members

        public bool Hit(PointF point, float width)
        {
            if (line.Hit(point, width))
            {
                float z=line.DistanceFromPointAlong(point);
                return (z>=x1&&z<=x2)||(z>=x2&&z<=x1);
            }
            return false;
        }
        #endregion
    }

    /// <summary>
    /// A list of LineSeg definiting a polyline.
    /// </summary>
    [TypeConverter(typeof(ExpandableObjectConverter))]
    public class SegmentList
    {
        readonly List<LineSeg> lines;

        public SegmentList()
        {
            this.lines=new List<LineSeg>();
        }
        public SegmentList(params LineSeg[] segments)
        {
            this.lines=new List<LineSeg>(segments);
        }
        public SegmentList(bool closed, params PointF[] points)
        {
            this.lines=new List<LineSeg>();
            if (points.Length>0)
            {
                PointF p2=points[0];
                for (int i=1; i<points.Length; i++)
                {
                    PointF p1=p2; // remember last point
                    p2=points[i]; // store currect point
                    lines.Add(new LineSeg(p1, p2)); //add segment
                }
                if (closed)
                {
                    // add line to start if a closed line
                    lines.Add(new LineSeg(p2, points[0]));
                }
            }
        }
        public List<LineSeg> Lines { get { return lines; } }

    public SegmentList Offset(float distance)
    {
        List<Line2> new_lines=new List<Line2>();
        int N=lines.Count;
        // Take each line and offset by distance, and store in
        // new list
        for (int i=0; i<N; i++)
        {
            Line2 offset_line=lines[i].Line.Offset(distance);
            new_lines.Add(offset_line);
        }
        List<LineSeg> result=new List<LineSeg>();
        for (int i=0; i<N; i++)
        {
            // i-th line
            Line2 this_line=new_lines[i];
            // Find index of previous line. If i-th is fist line,
            // the j points to last line
            int j=i>0?i-1:N-1;
            // Find index to next line. If i-th is the last line,
            // then k points to fist line
            int k=i<N-1?i+1:0;
            Line2 prev_line=new_lines[j];
            Line2 next_line=new_lines[k];
            // Trim infinate line based on intersection with
            // previous and next line.
            LineSeg offset_seg=this_line.TrimBetween(prev_line, next_line);
            result.Add(offset_seg);
        }
        // Create new polyline from array of line segments
        return new SegmentList(result.ToArray());
    }

    }
}
