using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ESRI.ArcGIS.Geometry;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Carto;
using System.IO;
using System.Windows.Forms;

namespace Wind
{
    class AlgorithmClass
    {
        //风向角度
        public double sita;
        //风向矢量表示
        public IPoint winddirection;
        //
        public IFeatureLayer pBuildingLayer;
        //
        public string pBuildingId="FID";
        //
        public string pBuildingHeight = "";
        //
        public string pBuildingBlockField = "";
        //path
        public string path = "";
        //图层名
        public string lyrname = "";
        //采样间隔
        public double sampleinterval = 0;

        //将所有数据都加载到内存里，减少后续查找带来的耗时
        public Dictionary<string, List<Build>> DYHZ;

        public AlgorithmClass()
        {
            winddirection = new PointClass();
            DYHZ = new Dictionary<string, List<Build>>();
        }

        //--------方法--------//

        //读入数据
        public void caldanyuan()
        {
            if (DYHZ.Count != 0)
                return;

            IQueryFilter pQueryFilter1 = new QueryFilterClass();
            IFeatureCursor tCursor;
            IFeature pFeature;

            tCursor = pBuildingLayer.Search(pQueryFilter1, false);
            pFeature = tCursor.NextFeature();
            while (pFeature != null)
            {
                string bid = Convert.ToString(pFeature.get_Value(pFeature.Fields.FindField(pBuildingBlockField)));
                double height = Convert.ToDouble(pFeature.get_Value(pFeature.Fields.FindField(pBuildingHeight)));
                Build b = new Build();
                IPolygon pc = pFeature.Shape as IPolygon;
                b.p = pc;
                b.height = height;

                if (DYHZ.ContainsKey(bid))
                    DYHZ[bid].Add(b);
                else
                {
                    List<Build> ls = new List<Build>();
                    ls.Add(b);
                    DYHZ.Add(bid, ls);
                }
                pFeature = tCursor.NextFeature();
            }
            tCursor = null;
        }

        //距离计算
        private double Distance(IPoint p, IPoint p1)
        {
            return Math.Sqrt((p.X - p1.X) * (p.X - p1.X) + (p.Y - p1.Y) * (p.Y - p1.Y));
        }

        //路径覆盖检查
        public string pathisover(string pname)
        {
            int num = 0;
            string name = pname;
            string[] files = Directory.GetFiles(path);
            //Dictionary<string, int> nn = new Dictionary<string, int>();

            while (true)
            {
                string file=path+"\\"+name + num.ToString() + ".txt";
                if (File.Exists(file))
                {
                    num++;
                }
                else
                    break;
            }
            return "\\" + name + num.ToString() + ".txt";
        }

        //计算投影面法向量
        private void calSita(double da)
        {
            double si = sita;
            if (si+da > 360)
            {
                si = si - 360+da;
            }
            winddirection.X = Math.Cos(si / 360.0 * 2*Math.PI);
            winddirection.Y = Math.Sin(si / 360.0 * 2*Math.PI);
        }
       
        //判断点是否在矩形内部
        private bool IsinRect(MyRectangle mr, IPoint p)
        {
            bool isin = true;
            if (p.X > mr.maxx || p.Y < mr.miny || p.Y > mr.maxy)
                isin = false;
            return isin;
        }


        //投影计算，返回矩形
        private IPolygon Projecting(IPolygon pPolygon, double height)
        {
            //将多边形转成点进行投影
            IPointCollection pc = pPolygon as IPointCollection;
            //
            IPoint pProjectPoint = new PointClass();
            IPoint pMaxPoint = null;
            IPoint pMinPoint = null;

            for (int i = 0; i < pc.PointCount; i++)
            {
                IPoint p = pc.get_Point(i);

                //这里使用平面直线的参数方程，方向是风的方向（线地法向量），过（0，0，0）点
                double t = -winddirection.X * p.X - winddirection.Y * p.Y;
                //t = -t / (winddirection.X * winddirection.X + winddirection.Y * winddirection.Y);
                pProjectPoint.X = winddirection.X * t + p.X;
                pProjectPoint.Y = winddirection.Y * t + p.Y;

                //MessageBox.Show("3！");
                if (pMaxPoint == null)
                {
                    pMaxPoint = new PointClass();
                    pMinPoint = new PointClass();
                    pMaxPoint.X = pProjectPoint.X; pMaxPoint.Y = pProjectPoint.Y;
                    pMinPoint.X = pProjectPoint.X; pMinPoint.Y = pProjectPoint.Y;
                    //pMaxPoint = pProjectPoint;
                    //pMinPoint = pProjectPoint;
                    continue;
                }

                //当风向＞0and ≤±45°，用在Y轴对比大小
                if (Math.Abs(winddirection.Y) <= Math.Abs(winddirection.X))
                {
                    if (pProjectPoint.Y > pMaxPoint.Y)
                    {
                        pMaxPoint.X = pProjectPoint.X;
                        pMaxPoint.Y = pProjectPoint.Y;
                    }
                    if (pProjectPoint.Y < pMinPoint.Y)
                    {
                        pMinPoint.X = pProjectPoint.X;
                        pMinPoint.Y = pProjectPoint.Y;
                    }
                }
                //当风向＞±45°and ≤±90°，用在X轴对比大小
                if (Math.Abs(winddirection.Y) > Math.Abs(winddirection.X))
                {
                    if (pProjectPoint.X > pMaxPoint.X)
                    {
                        pMaxPoint.X = pProjectPoint.X;
                        pMaxPoint.Y = pProjectPoint.Y;
                    }
                    if (pProjectPoint.X < pMinPoint.X)
                    {
                        pMinPoint.X = pProjectPoint.X;
                        pMinPoint.Y = pProjectPoint.Y;
                    }
                }
            }
            // MessageBox.Show("4！");
            //新建要素
            IPolygon polygon = new PolygonClass();
            IPointCollection polygonpts = polygon as IPointCollection;
            IPoint t1 = new PointClass();
            IPoint t2 = new PointClass();

            //MessageBox.Show((maxdis - mindis).ToString());

            //构建点
            double dis = Distance(pMaxPoint, pMinPoint);
            pMinPoint.X = 0;
            pMaxPoint.X = 0;
            pMaxPoint.Y = pMinPoint.Y + dis;
            t1.X = height;
            t1.Y = pMaxPoint.Y;
            t2.X = height;
            t2.Y = pMinPoint.Y;

            //构建面
            polygonpts.AddPoint(pMinPoint);
            polygonpts.AddPoint(pMaxPoint);
            polygonpts.AddPoint(t1);
            polygonpts.AddPoint(t2);
            polygonpts.AddPoint(pMinPoint);
            //MessageBox.Show("34！");

            return polygon;
        }

        //投影计算，返回投影边
        private double Projecting(IPolygon pPolygon)
        {
            //将多边形转成点进行投影
            IPointCollection pc = pPolygon as IPointCollection;
            //
            IPoint pProjectPoint = new PointClass();
            IPoint pMaxPoint = null;
            IPoint pMinPoint = null;
            for (int i = 0; i < pc.PointCount; i++)
            {
                IPoint p = pc.get_Point(i);

                //这里使用平面直线的参数方程，方向是风的方向（线地法向量），过（0，0，0）点
                double t = -winddirection.X * p.X - winddirection.Y * p.Y;
                //t = -t / mo;
                pProjectPoint.X = winddirection.X * t + p.X;
                pProjectPoint.Y = winddirection.Y * t + p.Y;

                //MessageBox.Show("3！");
                if (pMaxPoint == null)
                {
                    pMaxPoint = new PointClass();
                    pMinPoint = new PointClass();
                    pMaxPoint.X = pProjectPoint.X; pMaxPoint.Y = pProjectPoint.Y;
                    pMinPoint.X = pProjectPoint.X; pMinPoint.Y = pProjectPoint.Y;
                    continue;
                }

                //当风向＞0and ≤±45°，用在Y轴对比大小
                if (Math.Abs(winddirection.Y) <= Math.Abs(winddirection.X))
                {
                    if (pProjectPoint.Y > pMaxPoint.Y)
                    {
                        pMaxPoint.X = pProjectPoint.X;
                        pMaxPoint.Y = pProjectPoint.Y;
                    }
                    if (pProjectPoint.Y < pMinPoint.Y)
                    {
                        pMinPoint.X = pProjectPoint.X;
                        pMinPoint.Y = pProjectPoint.Y;
                    }
                }
                //当风向＞±45°and ≤±90°，用在X轴对比大小
                if (Math.Abs(winddirection.Y) > Math.Abs(winddirection.X))
                {
                    if (pProjectPoint.X > pMaxPoint.X)
                    {
                        pMaxPoint.X = pProjectPoint.X;
                        pMaxPoint.Y = pProjectPoint.Y;
                    }
                    if (pProjectPoint.X < pMinPoint.X)
                    {
                        pMinPoint.X = pProjectPoint.X;
                        pMinPoint.Y = pProjectPoint.Y;
                    }
                }
            }
            return Distance(pMaxPoint, pMinPoint);
        }

        //投影计算，返回矩形
        private MyRectangle Projecting1(IPolygon pPolygon)
        {
            //将多边形转成点进行投影
            IPointCollection pc = pPolygon as IPointCollection;
            //
            IPoint pProjectPoint = new PointClass();
            IPoint pMaxPoint = null;
            IPoint pMinPoint = null;
            //double mo = (winddirection.X * winddirection.X + winddirection.Y * winddirection.Y);
            for (int i = 0; i < pc.PointCount; i++)
            {
                IPoint p = pc.get_Point(i);

                //这里使用平面直线的参数方程，方向是风的方向（线地法向量），过（0，0，0）点
                double t = -winddirection.X * p.X - winddirection.Y * p.Y;
                //t = -t / mo;
                pProjectPoint.X = winddirection.X * t + p.X;
                pProjectPoint.Y = winddirection.Y * t + p.Y;

                //MessageBox.Show("3！");
                if (pMaxPoint == null)
                {
                    pMaxPoint = new PointClass();
                    pMinPoint = new PointClass();
                    pMaxPoint.X = pProjectPoint.X; pMaxPoint.Y = pProjectPoint.Y;
                    pMinPoint.X = pProjectPoint.X; pMinPoint.Y = pProjectPoint.Y;
                    //pMaxPoint = pProjectPoint;
                    //pMinPoint = pProjectPoint;
                    continue;
                }

                //当风向＞0and ≤±45°，用在Y轴对比大小
                if (Math.Abs(winddirection.Y) <= Math.Abs(winddirection.X))
                {
                    if (pProjectPoint.Y > pMaxPoint.Y)
                    {
                        pMaxPoint.X = pProjectPoint.X;
                        pMaxPoint.Y = pProjectPoint.Y;
                    }
                    if (pProjectPoint.Y < pMinPoint.Y)
                    {
                        pMinPoint.X = pProjectPoint.X;
                        pMinPoint.Y = pProjectPoint.Y;
                    }
                }
                //当风向＞±45°and ≤±90°，用在X轴对比大小
                if (Math.Abs(winddirection.Y) > Math.Abs(winddirection.X))
                {
                    if (pProjectPoint.X > pMaxPoint.X)
                    {
                        pMaxPoint.X = pProjectPoint.X;
                        pMaxPoint.Y = pProjectPoint.Y;
                    }
                    if (pProjectPoint.X < pMinPoint.X)
                    {
                        pMinPoint.X = pProjectPoint.X;
                        pMinPoint.Y = pProjectPoint.Y;
                    }
                }
            }

            MyRectangle mr = new MyRectangle();
            mr.minx = 0;
            mr.miny = pMinPoint.Y;
            mr.maxy = pMinPoint.Y + Distance(pMaxPoint, pMinPoint);
            //mr.maxy = 0;
            return mr;
        }


        //Frontal Area（ours）
        public void ProsessOurs()
        {
            //读取数据
            caldanyuan();
            //MessageBox.Show(DYHZ.Count.ToString());
            calSita(0);


            //输出文本的对象
            StreamWriter sw = new StreamWriter(path + pathisover(lyrname + "_block"));
            //输出表头
            sw.WriteLine("BBID\tFA");

            //进度条
            PFA_Form.pb.Value = 0;
            int cnt = 0;
            foreach (var item in DYHZ)
            {
                if (cnt >= DYHZ.Count / PFA_Form.pb.Maximum)
                {
                    PFA_Form.pb.Value++;
                    cnt = 0;
                }
                else
                    cnt++;

                //求并集使用的接口
                IGeometryCollection gs=new GeometryBagClass();;
                ITopologicalOperator unionFeature = new PolygonClass();

                foreach(var build in item.Value)
                {
                    //MessageBox.Show("3");
                    //投影后构造的矩形加入并集计算中
                    IPolygon polygon = Projecting(build.p, build.height);
                    gs.AddGeometry(polygon);
                }
                //求并集，这步最耗时
                unionFeature.ConstructUnion(gs as IEnumGeometry);
                //while (gs.GeometryCount > 0) ;
                //gs.RemoveGeometries(0, 1);
                //计算面积
                IArea area = unionFeature as IArea;
                //输出
                sw.WriteLine(item.Key.ToString() + "\t" + area.Area.ToString("F2"));
            }
            PFA_Form.pb.Value=100;
            sw.Close();
        }
      
        //Frontal Area （wong）
        public void ProsessWong()
        {
            //读取数据
            caldanyuan();
            //MessageBox.Show(DYHZ.Count.ToString());
            calSita(0);


            //存放每个单元建筑投影数据
            List<MyRectangle> unionFeature = new List<MyRectangle>();
            //构造判断是否在迎风面中的点
            IPoint p = new PointClass();

            //打开文件，写入列名
            StreamWriter sw = new StreamWriter(path + pathisover(lyrname+"_Mong"));
            sw.WriteLine("BBID\tFA");

            //进度条

            PFA_Form.pb.Value = 0;
            int cnt = 0;
            foreach (var item in DYHZ)
            {
                if (cnt > DYHZ.Count / PFA_Form.pb.Maximum)
                {
                    PFA_Form.pb.Value++;
                    cnt = 0;
                }
                else
                    cnt++;

                double miny = double.MaxValue;
                double maxy = double.MinValue;
                double maxh = double.MinValue;
                foreach (var build in item.Value)
                {
                    //MessageBox.Show("3");
                    //投影后构造的矩形加入并集计算中
                    //IPolygon polygon = Projecting(build.p, build.height);
                    MyRectangle polygon = Projecting1(build.p);
                    polygon.maxx = build.height;

                    if (miny > polygon.miny) miny = polygon.miny;
                    if (maxy < polygon.maxy) maxy = polygon.maxy;
                    if (maxh < build.height) maxh = build.height;

                    unionFeature.Add(polygon);
                }

                //计数
                int count = 0;
                //横列计算
                int row = (int)Math.Abs((maxy - miny) / sampleinterval);
                int col = (int)Math.Abs(maxh / sampleinterval);

                //MessageBox.Show("row:" + row.ToString() + "  col:" + col.ToString());

                //开始判断
                for (int i = 0; i < row; i++)
                    for (int j = 0; j < col; j++)
                    {
                        //
                        p.Y = i * sampleinterval + miny;
                        p.X = j * sampleinterval;
                        foreach (var k in unionFeature)
                        {
                            //如果不在内部就继续，否则加1
                            if (p.X > k.maxx || p.Y < k.miny || p.Y > k.maxy)
                                continue;
                            else
                            {
                                count++;
                                break;
                            }

                        }
                    }
                double area = count * sampleinterval * sampleinterval;
                //输出
                sw.WriteLine(item.Key.ToString() + "\t" + area.ToString("F2"));

                unionFeature.Clear();

            }
            sw.Close();

            PFA_Form.pb.Value = 100;

        }

        //八方向FA计算
        public void Prosess8D()
        {
            //读取数据
            caldanyuan();

            //输出文本的对象
            StreamWriter sw = new StreamWriter(path + pathisover(lyrname + "_block"));
            //输出表头
            sw.WriteLine("BBID\tFA" + sita.ToString() +"\tFA" + (sita + 22.5).ToString() + "\tFA" + (sita + 45).ToString() 
                + "\tFA" + (sita + 67.5).ToString()+"\tFA"+(sita+90).ToString()+"\tFA"+(sita+112.5).ToString()
                + "\tFA" + (sita + 135).ToString() + "\tFA" + (sita + 157.5).ToString());

            //进度条
            PFA_Form.pb.Value = 0;
            int cnt = 0;

            double[] marea = new double[8];
            foreach (var item in DYHZ)
            {
                if (cnt >= DYHZ.Count / PFA_Form.pb.Maximum)
                {
                    PFA_Form.pb.Value++;
                    cnt = 0;
                }
                else
                    cnt++;

                //计算8方向
                for (int i = 0; i < 8; i++)
                {

                    //求并集使用的接口
                    IGeometryCollection gs = new GeometryBagClass();
                    ITopologicalOperator unionFeature = new PolygonClass();

                    calSita(i * 22.5);
                    foreach (var build in item.Value)
                    {
                        //投影后构造的矩形加入并集计算中
                        IPolygon polygon = Projecting(build.p, build.height);
                        gs.AddGeometry(polygon);
                    }
                    //求并集，这步最耗时
                    unionFeature.ConstructUnion(gs as IEnumGeometry);

                    //计算面积
                    IArea area = unionFeature as IArea;

                    marea[i] = area.Area;
                }
                //输出
                sw.WriteLine(item.Key.ToString() + "\t" + marea[0].ToString("F2") +
                    "\t" + marea[1].ToString("F2") + "\t" + marea[2].ToString("F2") +
                    "\t" + marea[3].ToString("F2") + "\t" + marea[4].ToString("F2") +
                    "\t" + marea[5].ToString("F2") + "\t" + marea[6].ToString("F2") +
                    "\t" + marea[7].ToString("F2"));
            }

            PFA_Form.pb.Value = 100;
            sw.Close();

        }

        //建筑参数计算（未汇总）
        public void Prosess0()
        {
            calSita(0);

            //用来存储结果
            StreamWriter sw = new StreamWriter(path + pathisover(lyrname + "_building"));
            sw.WriteLine("BBID\tCount\tHeight\tFrontalArea\tArea\tFacadeArea\tSurfaceArea");

            IQueryFilter pQueryFilter1 = new QueryFilterClass();
            IFeatureCursor tCursor1;
            IFeature pFeature;
            tCursor1 = pBuildingLayer.Search(pQueryFilter1, false);
            pFeature = tCursor1.NextFeature();
            int Fcount=pBuildingLayer.FeatureClass.FeatureCount(pQueryFilter1);

            double height;
            IPolygon pPolygon;
            long bid;
            string bbid;
            double fa;
            IArea area;
            double mj, cmj, bmj;

            //进度条
            PFA_Form.pb.Value = 0;
            int cnt = 0;
            while (pFeature!=null)
            {
                if (cnt >= Fcount / PFA_Form.pb.Maximum)
                {
                    PFA_Form.pb.Value++;
                    cnt = 0;
                }
                else
                    cnt++;

                pPolygon = pFeature.Shape as IPolygon;
                height = Convert.ToDouble(pFeature.get_Value(pFeature.Fields.FindField(pBuildingHeight)));
                bid = Convert.ToInt64(pFeature.get_Value(pFeature.Fields.FindField(pBuildingId)));
                bbid = Convert.ToString(pFeature.get_Value(pFeature.Fields.FindField(pBuildingBlockField)));
                fa = Projecting(pPolygon) * height;
                area = pPolygon as IArea;

                mj = area.Area;
                cmj = pPolygon.Length * height;
                bmj = pPolygon.Length * height + area.Area;

                sw.WriteLine(bid.ToString() + "\t" + bbid.ToString() + "\t" + height.ToString() + "\t" + fa.ToString("F2") + "\t" +
                    mj.ToString("F2") + "\t" + cmj.ToString("F2") + "\t" + bmj.ToString("F2"));

                pFeature = tCursor1.NextFeature();
            }

            tCursor1 = null;

            sw.Close();

            PFA_Form.pb.Value = 100;
        }

        //建筑参数计算(汇总)
        public void Prosess1()
        {
            calSita(0);

            StreamWriter sw = new StreamWriter(path + pathisover(lyrname + "_Other"));
            sw.WriteLine("BBID\tCount\tHeight\tFrontalArea\tArea\tFacadeArea\tSurfaceArea");

            //进度条
            PFA_Form.pb.Value = 0;
            int cnt = 0;
            foreach (var item in DYHZ)
            {
                if (cnt >= DYHZ.Count / PFA_Form.pb.Maximum)
                {
                    PFA_Form.pb.Value++;
                    cnt = 0;
                }
                else
                    cnt++;

                double height = 0;
                double fa = 0;
                IArea area;
                double mj = 0, cmj = 0, bmj = 0;
                int count = 0;
                foreach (var build in item.Value)
                {
                    fa += Projecting(build.p) * build.height;
                    area = build.p as IArea;
                    mj += area.Area;

                    height += build.height;
                    cmj += build.p.Length * build.height;
                    bmj += build.p.Length * build.height + area.Area;
                    count++;
                }
                //输出
                sw.WriteLine(item.Key.ToString() + "\t" + count.ToString() + "\t" + height.ToString() + "\t"
                    + fa.ToString("F2") + "\t" + mj.ToString("F2") + "\t" + cmj.ToString("F2") + "\t" + bmj.ToString("F2"));
            }

            PFA_Form.pb.Value = 100;
            sw.Close();
        }
    }
}
