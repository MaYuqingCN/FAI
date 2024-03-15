using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using System.Windows.Forms;
using System.Drawing.Drawing2D;

namespace Wind
{
    class DrawClass
    {
        PictureBox pictureBox1;

        public DrawClass(PictureBox picturebox)
        {
            pictureBox1 = picturebox;
        }

        /// <summary>
        /// 绘制图像
        /// </summary>
        /// <param name="sigma"></param>
        /// <param name="g"></param>
        public void drawdirction(double sigma)
        {
            Graphics g = this.pictureBox1.CreateGraphics();
            Color c = this.pictureBox1.BackColor;
            g.Clear(c);
            Pen p = new Pen(Color.Gray, 1);
            g.DrawEllipse(p, new Rectangle(10, 10, this.pictureBox1.Width - 20, this.pictureBox1.Height - 20));

            p = new Pen(Color.Black, 1);
            p.EndCap = LineCap.ArrowAnchor;
            g.DrawLine(p, new System.Drawing.Point(5, this.pictureBox1.Height / 2),
                          new System.Drawing.Point(this.pictureBox1.Width - 5, this.pictureBox1.Height / 2));

            g.DrawLine(p, new System.Drawing.Point(this.pictureBox1.Width / 2, this.pictureBox1.Height - 5),
                          new System.Drawing.Point(this.pictureBox1.Width / 2, 5));


            p = new Pen(Color.Red, 1);
            p.EndCap = LineCap.ArrowAnchor;
            System.Drawing.Point p1 = new System.Drawing.Point();
            System.Drawing.Point p2 = new System.Drawing.Point();
            p1.X = (int)(this.pictureBox1.Width / 2 + pictureBox1.Width / 4 * Math.Cos((sigma - 90) / 360.0 * 2 * Math.PI));
            p1.Y = (int)(this.pictureBox1.Height / 2 + pictureBox1.Width / 4 * Math.Sin((sigma - 90) / 360.0 * 2 * Math.PI));
            p2.X = (int)(this.pictureBox1.Width / 2 + pictureBox1.Width / 2 * Math.Cos((sigma - 90) / 360.0 * 2 * Math.PI));
            p2.Y = (int)(this.pictureBox1.Height / 2 + pictureBox1.Width / 2 * Math.Sin((sigma - 90) / 360.0 * 2 * Math.PI));

            g.DrawLine(p, p2, p1);
            SolidBrush b = new SolidBrush(Color.Black);
            Font f = new Font("宋体", 10);
            g.DrawString("x", f, b, new System.Drawing.Point(this.pictureBox1.Width/2+2, 5));
            g.DrawString("y", f, b, new System.Drawing.Point(this.pictureBox1.Width-7, this.pictureBox1.Height/2 - 5));

            p.Dispose();
            b.Dispose();
            g.Dispose();
        }
    }
}
