using System;
using System.Drawing;
using System.Drawing.Drawing2D;

namespace JA.UI
{
    public interface IGeometry
    {
        PointF Center { get; }
        PointF Direction { get; }
        PointF Normal { get; }
        float DistanceToOrigin { get; }
        float Magnitude { get; }
        float SumSquare { get; }
        Point2 LocalPoint(float away, float along);
    }

    public interface ISelectable
    {
        bool Hit(PointF point, float width);
    }
}
