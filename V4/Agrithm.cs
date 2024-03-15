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
        public IPoint winddirection = new PointClass();
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
        //采样间隔
        public double sampleinterval = 0;

        public AlgorithmClass()
        {

        }

        //--------方法--------//
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
        private void calSita()
        {
            //sita =  sita + 90;
            if (sita > 360)
            {
                sita = sita - 360;
            }


            //if ( sita == 0 ||  sita == 180)
            //{
            //     winddirection.X = 1;
            //     winddirection.Y = 0;
            //}
            //if ( sita == 90)
            //{
            //     winddirection.X = 0;
            //     winddirection.Y = 1;
            //}
            //else
            //{

            //    if ( sita < 90)
            //    {
            winddirection.X = Math.Cos(sita / 360.0 * 2*Math.PI);
            winddirection.Y = Math.Sin(sita / 360.0 * 2*Math.PI);
            //}
            //else
            //{
            //     winddirection.X = -Math.Sin( sita / 180 * Math.PI);
            //     winddirection.Y = Math.Cos( sita / 180 * Math.PI);
            //}

        }
        //计算投影面法向量
        private void calSita(double sita1)
        {
            //sita = sita + 90;
            if (sita1 > 360)
            {
                sita1 = sita1 - 360;
            }

            //if (sita == 0)
            //{
            //    winddirection.X = 1;
            //    winddirection.Y = 0;
            //}
            //else if (sita == 180)
            //{
            //    winddirection.X = -1;
            //    winddirection.Y = 0;
            //}
            //else if (sita == 90)
            //{
            //    winddirection.X = 0;
            //    winddirection.Y = 1;
            //}
            //else if (sita == 270)
            //{
            //    winddirection.X = 0;
            //    winddirection.Y = -1;
            //}
            //else
            //{
            winddirection.X = Math.Cos(sita1 / 360.0 * 2 * Math.PI);
            winddirection.Y = Math.Sin(sita1 / 360.0 * 2 * Math.PI);
            //}
        }

        public Dictionary<string, int> danyuan = new Dictionary<string, int>();
        
        //单元数汇总
        public void caldanyuan()
        {
            if (danyuan.Count != 0)
                return;

            IQueryFilter pQueryFilter1 = new QueryFilterClass();
            IFeatureCursor tCursor;
            IFeature pFeature;

            tCursor = pBuildingLayer.Search(pQueryFilter1, false);
            pFeature = tCursor.NextFeature();
            while (pFeature != null)
            {
                string bid = Convert.ToString(pFeature.get_Value(pFeature.Fields.FindField(pBuildingBlockField)));
                if (danyuan.ContainsKey(bid))
                    danyuan[bid]++;
                else
                    danyuan.Add(bid, 1);
                pFeature = tCursor.NextFeature();
            }
            tCursor = null;
        }

        //将所有数据都加载到内存里，减少后续查找带来的耗时
        //通过定义的类将需要的数据加载到内存，帮助后续的分析
        public Dictionary<string, List<Build>> DYHZ = new Dictionary<string, List<Build>>();
        
        public void caldanyuan1()
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

        //投影计算
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

        //投影计算
        private double Projecting(IPolygon pPolygon)
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
            return Distance(pMaxPoint, pMinPoint);
        }

        //投影计算
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

        private bool IsinRect(MyRectangle mr, IPoint p)
        {
            bool isin = true;
            if ( p.X > mr.maxx || p.Y < mr.miny || p.Y > mr.maxy)
                isin = false;
            return isin;
        }


        //Frontal Area 
        public void Prosess()
        {
            caldanyuan();
            calSita();

            //IGeometryCollection gs = null;
            //ITopologicalOperator unionFeature = null;

            IQueryFilter pQueryFilter1 = new QueryFilterClass();
            IFeatureCursor tCursor;
            IFeature pFeature;

            //MessageBox.Show("2");
            StreamWriter sw = new StreamWriter(path+pathisover("block"));
            
            sw.WriteLine("BBID\tFA");
            //double marea = 0;
            //IArea area;
            //IPolygon pPolygon;

            foreach (var item in danyuan)
            {
                IGeometryCollection gs = new GeometryBagClass();
                ITopologicalOperator unionFeature = new PolygonClass();

                pQueryFilter1.WhereClause = pBuildingBlockField + " = '" + item.Key + "'";
                tCursor = pBuildingLayer.Search(pQueryFilter1, false);
                pFeature = tCursor.NextFeature();
                if (pFeature == null) continue;
                //MessageBox.Show("3");

                while (pFeature != null)
                {
                    IPolygon pPolygon = pFeature.Shape as IPolygon;
                    double height = Convert.ToDouble(pFeature.get_Value(pFeature.Fields.FindField(pBuildingHeight)));
                    //MessageBox.Show("3");
                    IPolygon polygon = Projecting(pPolygon, height);
                    //MessageBox.Show("3");
                    gs.AddGeometry(polygon);
                    pFeature = tCursor.NextFeature();
                }
                //MessageBox.Show("4");
                unionFeature.ConstructUnion(gs as IEnumGeometry);

                IArea area = unionFeature as IArea;

                //marea = area.Area;

                sw.WriteLine(item.Key.ToString() + "\t" + area.Area.ToString("F2"));

            }
            tCursor = null;

            sw.Close();
        }

        //Frontal Area 
        public void ProsessAcc()
        {
            //读取数据
            caldanyuan1();
            //MessageBox.Show(DYHZ.Count.ToString());
            calSita();


            //输出文本的对象
            StreamWriter sw = new StreamWriter(path + pathisover("block"));
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

        
        //Frontal Area 
        public void ProsessWAcc()
        {
            //读取数据
            caldanyuan1();
            //MessageBox.Show(DYHZ.Count.ToString());
            calSita();


            //存放每个单元建筑投影数据
            List<MyRectangle> unionFeature = new List<MyRectangle>();
            //构造判断是否在迎风面中的点
            IPoint p = new PointClass();
            //打开文件，写入列名
            StreamWriter sw = new StreamWriter(path + pathisover("Mong"));
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

        //Frontal Area 
        public void Prosess0()
        {
            caldanyuan();
            calSita();

            IQueryFilter pQueryFilter1 = new QueryFilterClass();
            IFeatureCursor tCursor;
            IFeature pFeature;

            //存放每个单元建筑投影数据
            List<MyRectangle> unionFeature = new List<MyRectangle>();

            //打开文件，写入列名
            StreamWriter sw = new StreamWriter(path + pathisover("Mong"));
            sw.WriteLine("BBID\tFA");

            //获取高度
            double height = 0;
            //构造判断是否在迎风面中的点
            IPoint p = new PointClass();

            //开始计算
            foreach (var item in danyuan)
            {

                pQueryFilter1.WhereClause = pBuildingBlockField +
                    " = '" + item.Key + "'";
                tCursor = pBuildingLayer.Search(pQueryFilter1, false);
                pFeature = tCursor.NextFeature();
                if (pFeature == null) continue;
                //MessageBox.Show("3");
                double miny = double.MaxValue;
                double maxy = double.MinValue;
                double maxh = double.MinValue;
                while (pFeature != null)
                {
                    IPolygon pPolygon = pFeature.Shape as IPolygon;
                    height = Convert.ToDouble(pFeature.get_Value(
                        pFeature.Fields.FindField(pBuildingHeight)));
                    //MessageBox.Show("3");
                    MyRectangle polygon = Projecting1(pPolygon);
                    polygon.maxx = height;

                    if (miny > polygon.miny) miny = polygon.miny;
                    if (maxy < polygon.maxy) maxy = polygon.maxy;
                    if (maxh < height) maxh = height;

                    unionFeature.Add(polygon);

                    pFeature = tCursor.NextFeature();
                }

                //计数
                int count = 0;
                //横列计算
                int row=(int)Math.Abs((maxy - miny )/ sampleinterval);
                int col=(int)Math.Abs(maxh / sampleinterval);

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

                //计算面积
                double area = count * sampleinterval * sampleinterval;
                //MessageBox.Show(item.Key.ToString()+"\r\nrow:" + row.ToString() + "  col:" + col.ToString()+"count:" + count.ToString()+" interval:"+sampleinterval.ToString()+"  area:"+area.ToString());
                sw.WriteLine(item.Key.ToString() + "\t" + area.ToString("F2"));

                unionFeature.Clear();
            }
            tCursor = null;
            //MessageBox.Show("2");        
            sw.Close();
        }

        //八方向FA计算
        public void Prosess8N()
        {
            caldanyuan();

            IGeometryCollection gs = null;
            ITopologicalOperator unionFeature = new PolygonClass();

            IQueryFilter pQueryFilter1 = new QueryFilterClass();
            IFeatureCursor tCursor;
            IFeature pFeature;

            //MessageBox.Show("2");
            StreamWriter sw = new StreamWriter(path + pathisover("N8block"));

            sw.WriteLine("BBID\tN\tEN\tE\tES\tS\tWS\tW\tWN");
            double[] marea = new double[8];
            IArea area;
            IPolygon pPolygon;

            foreach (var item in danyuan)
            {
                pQueryFilter1.WhereClause = pBuildingBlockField + " = '" + item.Key + "'";
                //MessageBox.Show("3");
                for (int i = 0; i < 8; i++)
                {
                    tCursor = pBuildingLayer.Search(pQueryFilter1, false);
                    pFeature = tCursor.NextFeature();
                    if (pFeature == null) continue;

                    gs = new GeometryBagClass();
                    calSita(i * 45);
                    while (pFeature != null)
                    {
                        pPolygon = pFeature.Shape as IPolygon;
                        double height = Convert.ToDouble(pFeature.get_Value(pFeature.Fields.FindField(pBuildingHeight)));
                        //MessageBox.Show("3");
                        IPolygon polygon = Projecting(pPolygon, height);
                        //MessageBox.Show("3");
                        gs.AddGeometry(polygon);
                        pFeature = tCursor.NextFeature();
                    }
                    //MessageBox.Show("4");
                    unionFeature.ConstructUnion(gs as IEnumGeometry);

                    area = unionFeature as IArea;

                    marea[i] = area.Area;
                }

                sw.WriteLine(item.Key.ToString() + "\t" + marea[0].ToString("F2") +
                    "\t" + marea[1].ToString("F2") + "\t" + marea[2].ToString("F2") +
                    "\t" + marea[3].ToString("F2") + "\t" + marea[4].ToString("F2") +
                    "\t" + marea[5].ToString("F2") + "\t" + marea[6].ToString("F2") +
                    "\t" + marea[7].ToString("F2"));

            }
            tCursor = null;

            sw.Close();
        }

        //建筑参数计算
        public void Prosess1()
        {
            calSita();

            //用来存储结果
            //Dictionary<int, List<double>> results = new Dictionary<int, List<double>>();
            StreamWriter sw = new StreamWriter(path + pathisover("building"));
            sw.WriteLine("BID\tBBID\tHeight\tWindSArea\tArea\tFacadeArea\tSurfaceArea");
            //List<List<double>> results = new List<List<double>>();

            IQueryFilter pQueryFilter1 = new QueryFilterClass();
            IFeatureCursor tCursor1;
            IFeature pFeature;

            tCursor1 = pBuildingLayer.Search(pQueryFilter1, false);
            pFeature = tCursor1.NextFeature();
            
            while (pFeature != null)
            {
                //List<double> temp = new List<double>();
                IPolygon pPolygon = pFeature.Shape as IPolygon;
                double height = Convert.ToDouble(pFeature.get_Value(pFeature.Fields.FindField(pBuildingHeight)));
                long bid = Convert.ToInt64(pFeature.get_Value(pFeature.Fields.FindField(pBuildingId)));
                string bbid = Convert.ToString(pFeature.get_Value(pFeature.Fields.FindField(pBuildingBlockField)));
                double fa = Projecting(pPolygon)*height;
                IArea area = pPolygon as IArea;

                double mj=area.Area;
                double cmj=pPolygon.Length * height;
                double bmj=pPolygon.Length * height + area.Area;

                sw.WriteLine(bid.ToString() + "\t" + bbid.ToString() + "\t" +height.ToString()+ "\t" + fa.ToString("F2") + "\t" + 
                    mj.ToString("F2") + "\t" + cmj.ToString("F2") + "\t" + bmj.ToString("F2"));

                pFeature = tCursor1.NextFeature();
            }
            tCursor1 = null;
            sw.Close();
        }
    }
}
