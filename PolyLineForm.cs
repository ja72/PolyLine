using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Drawing.Drawing2D;

namespace JA.UI
{

    public partial class PolyLineForm : Form
    {
        class MoveInfo
        {
            public int index;
            public LineSeg seg;
            public Point StartMouseMovePoint;
        }

        #region Factory
        public PolyLineForm()
        {
            this.DoubleBuffered=true;

            InitializeComponent();
            DrawScale=new SizeF(1, -1);
            DrawOrigin=new PointF(pictureBox1.Width/2, pictureBox1.Height/2);
            Selected=-1;
            Poly=new SegmentList(true,
                new PointF(-90, -90),
                new PointF(0, -30),
                new PointF(90, -90),
                new PointF(60, 60),
                new PointF(0, 30),
                new PointF(-60, 60));

            this.pictureBox1.Paint+=new PaintEventHandler(pictureBox1_Paint);
            this.pictureBox1.Resize+=new EventHandler(pictureBox1_Resize);
            this.pictureBox1.MouseDown+=new MouseEventHandler(pictureBox1_MouseDown);
            this.pictureBox1.MouseUp+=new MouseEventHandler(pictureBox1_MouseUp);
            this.pictureBox1.MouseMove+=new MouseEventHandler(pictureBox1_MouseMove);
        }
        #endregion

        #region Properties
        public int Selected { get; set; }
        public int Count { get { return Poly.Lines.Count; } }
        public SegmentList Poly { get; set; }
        MoveInfo Moving { get; set; }
        public SizeF DrawScale { get; set; }
        public PointF DrawOrigin { get; set; }
        public bool HasCapturedLine
        {
            get
            {
                return Moving!=null;
            }
        }
        #endregion

        #region Methods
        public void CaptureLine(int selection, Point pt)
        {
            if (selection>=0&&selection<Poly.Lines.Count)
            {
                base.Capture=true;
                this.Moving=new MoveInfo()
                {
                    index=selection,
                    seg=Poly.Lines[selection],
                    StartMouseMovePoint=pt
                };
            }
        }
        public void ReleaseLine()
        {
            base.Capture=false;
            this.Moving=null;
        }
        void RefreshSelection(Point point)
        {
            var pt=new PointF(point.X-DrawOrigin.X, point.Y-DrawOrigin.Y);
            pt=new PointF(DrawScale.Width*pt.X, DrawScale.Height*pt.Y);

            int sel=Selected;
            Selected=-1;
            for (int i=0; i<Count; i++)
            {
                if (Poly.Lines[i].Hit(pt, 4))
                {
                    Selected=i;
                    break;
                }
            }
            if (sel!=Selected)
            {
                pictureBox1.Refresh();
            }
            this.Cursor=
                HasCapturedLine?Cursors.Hand:
                Selected>=0?Cursors.SizeAll:
                Cursors.Default;
        }
        #endregion

        #region Events
        void pictureBox1_MouseMove(object sender, MouseEventArgs e)
        {
            Point pt=e.Location;
            if (HasCapturedLine)
            {
                int sel=Moving.index;

                int dx=pt.X-Moving.StartMouseMovePoint.X;
                int dy=pt.Y-Moving.StartMouseMovePoint.Y;
                Point2 del=new Point2(DrawScale.Width*dx, DrawScale.Height*dy);

                int prev_index=sel>0?sel-1:Count-1;
                int next_index=sel<Count-1?sel+1:0;

                Point2 prev_pt=Poly.Lines[prev_index].StartPoint;
                Point2 next_pt=Poly.Lines[next_index].EndPoint;

                LineSeg tseg=new LineSeg(Moving.seg.StartPoint+del, Moving.seg.EndPoint+del);

                Poly.Lines[prev_index]=new LineSeg(prev_pt, tseg.StartPoint);
                Poly.Lines[sel]=tseg;
                Poly.Lines[next_index]=new LineSeg(tseg.EndPoint, next_pt);

                pictureBox1.Refresh();
                this.Text=string.Format("x={0} y={1} Dragging Line={2}", pt.X, pt.Y, Selected);
            }
            else
            {
                if (Selected>=0)
                {
                    this.Text=string.Format("x={0} y={1} Hover Line={2}", pt.X, pt.Y, Selected);
                }
                else
                {
                    this.Text=string.Format("x={0} y={1}", pt.X, pt.Y);
                }
            }
            RefreshSelection(pt);
        }

        void pictureBox1_MouseUp(object sender, MouseEventArgs e)
        {
            Point pt=e.Location;
            if (HasCapturedLine)
            {
                ReleaseLine();
            }
            RefreshSelection(pt);
        }

        void pictureBox1_MouseDown(object sender, MouseEventArgs e)
        {
            Point pt=e.Location;
            RefreshSelection(pt);

            if (Selected>=0&&!HasCapturedLine)
            {
                CaptureLine(Selected, pt);
            }

            RefreshSelection(pt);
        }

        private void pictureBox1_Paint(object sender, PaintEventArgs e)
        {
            e.Graphics.InterpolationMode=System.Drawing.Drawing2D.InterpolationMode.High;
            e.Graphics.SmoothingMode=System.Drawing.Drawing2D.SmoothingMode.HighQuality;
            e.Graphics.TranslateTransform(DrawOrigin.X, DrawOrigin.Y);
            e.Graphics.ScaleTransform(DrawScale.Width, DrawScale.Height);

            SegmentList L1=Poly.Offset(7);
            SegmentList L2=Poly.Offset(-7);

            for (int i=0; i<Count; i++)
            {
                using (Pen pen=new Pen(Color.Black, 1))
                {
                    pen.DashStyle=System.Drawing.Drawing2D.DashStyle.Solid;
                    e.Graphics.DrawPoint(Poly.Lines[i].StartPoint, pen);
                    e.Graphics.DrawPoint(Poly.Lines[i].EndPoint, pen);
                    pen.Width=2;
                    e.Graphics.DrawLine(L1.Lines[i], pen);
                    e.Graphics.DrawLine(L2.Lines[i], pen);
                    pen.DashStyle=System.Drawing.Drawing2D.DashStyle.Dash;
                    pen.Width=1;
                    //e.Graphics.DrawPipe(12f, Poly.Lines[i], Color.Yellow, Color.Black);
                    //e.Graphics.DrawLine(Poly.Lines[i], pen);
                }
                if (i==Selected)
                {
                    using (Pen pen=new Pen(Color.FromArgb(92, 250, 92, 92), 2))
                    {
                        pen.DashStyle=System.Drawing.Drawing2D.DashStyle.Solid;
                        e.Graphics.DrawLine(L1.Lines[i].Offset(2), pen);
                        e.Graphics.DrawLine(L2.Lines[i].Offset(-2), pen);
                        //e.Graphics.DrawLine(Poly.Lines[i], pen);                                  
                    }
                }
            }
        }

        private void pictureBox1_Resize(object sender, EventArgs e)
        {

            this.DrawOrigin=new PointF(
                pictureBox1.Width/2,
                pictureBox1.Height/2);

            pictureBox1.Refresh();
        }

        #endregion
    }

    public static class GdiEx
    {
        public static void DrawPoint(this Graphics g, Point2 point, Pen pen)
        {
            var cen=point.Center;
            g.DrawEllipse(pen, cen.X-2, cen.Y-2, 4f, 4f);
        }


        public static void DrawLine(this Graphics g, LineSeg seg, Pen pen)
        {
            g.DrawLine(pen, seg.StartPoint.Center, seg.EndPoint.Center);
        }
        public static void DrawPipe(this Graphics g, float width, LineSeg seg, Color mid_color, Color edge_color)
        {
            DrawPipe(g, width, seg.StartPoint.Center, seg.EndPoint.Center, mid_color, edge_color);
        }
        public static void DrawPipe(this Graphics g, float width, PointF p1, PointF p2, Color mid_color, Color edge_color)
        {
            SizeF along=new SizeF(p2.X-p1.X, p2.Y-p1.Y);
            float mag=(float)Math.Sqrt(along.Width*along.Width+along.Height*along.Height);
            along=new SizeF(along.Width/mag, along.Height/mag);
            SizeF perp=new SizeF(-along.Height, along.Width);

            PointF p1L=new PointF(p1.X+width/2*perp.Width, p1.Y+width/2*perp.Height);
            PointF p1R=new PointF(p1.X-width/2*perp.Width, p1.Y-width/2*perp.Height);
            PointF p2L=new PointF(p2.X+width/2*perp.Width, p2.Y+width/2*perp.Height);
            PointF p2R=new PointF(p2.X-width/2*perp.Width, p2.Y-width/2*perp.Height);

            GraphicsPath gp=new GraphicsPath();
            gp.AddLines(new PointF[] { p1L, p2L, p2R, p1R });
            gp.CloseFigure();

            Region region=new Region(gp);
            using (LinearGradientBrush brush=new LinearGradientBrush(p1L, p1R, Color.Black, Color.Black))
            {
                ColorBlend color_blend=new ColorBlend();
                color_blend.Colors=new Color[] { 
                            Color.FromArgb(0, edge_color), edge_color, mid_color, 
                            edge_color, Color.FromArgb(0, edge_color) };
                color_blend.Positions=new float[] { 0f, 0.1f, 0.5f, 0.9f, 1f };
                brush.InterpolationColors=color_blend;
                g.FillRegion(brush, region);
            }
        }
        public static void DrawPipes(this Graphics g, float width, SegmentList poly, Color mid_color, Color edge_color)
        {
            for (int i=0; i<poly.Lines.Count; i++)
            {
                using (GraphicsPath gp=new GraphicsPath())
                {
                    var start=poly.Lines[i].StartPoint;
                    gp.AddEllipse(start.Center.X-width/2, start.Center.Y-width/2, width, width);
                    if (i==poly.Lines.Count-1)
                    {
                        var end=poly.Lines[i].EndPoint;
                        gp.AddEllipse(end.Center.X-width/2, end.Center.Y-width/2, width, width);
                    }

                    using (PathGradientBrush brush=new PathGradientBrush(gp))
                    {
                        brush.CenterColor=mid_color;
                        brush.SurroundColors=new Color[] { edge_color };
                        brush.CenterPoint=start.Center;
                        g.FillPath(brush, gp);
                    }
                }
                if (i>0)
                {
                    DrawPipe(g, width, poly.Lines[i], mid_color, edge_color);
                }
            }
        }
        public static void DrawPipes(this Graphics g, float width, PointF[] points, Color mid_color, Color edge_color)
        {
            for (int i=0; i<points.Length; i++)
            {
                using (GraphicsPath gp=new GraphicsPath())
                {
                    gp.AddEllipse(points[i].X-width/2, points[i].Y-width/2, width, width);

                    using (PathGradientBrush brush=new PathGradientBrush(gp))
                    {
                        brush.CenterColor=mid_color;
                        brush.SurroundColors=new Color[] { edge_color };
                        brush.CenterPoint=points[i];
                        g.FillPath(brush, gp);
                    }
                }
                if (i>0)
                {
                    DrawPipe(g, width, points[i-1], points[i], mid_color, edge_color);
                }
            }
        }
    }
}
