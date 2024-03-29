﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using ESRI.ArcGIS.Carto;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;

namespace Wind
{
    public partial class PFA_Form : Form
    {

        //自定义类实例
        DrawClass dc;
        //自定义类实例
        AlgorithmClass ac;

        public static ProgressBar pb;

        //public List<string> select = new List<string>();

        public PFA_Form()
        {
            InitializeComponent();
        }

        //初始化
        private void PFA_Form_Load(object sender, EventArgs e)
        {
            dc = new DrawClass(this.pictureBox1);
            ac = new AlgorithmClass();
            pb = this.progressBar1;
            pb.Maximum = 100;
            pb.Minimum = 0;

            //加载图层
            for (int i = 0; i < ArcMap.Document.FocusMap.LayerCount; i++)
            {
                if (ArcMap.Document.FocusMap.get_Layer(i) is IFeatureLayer)
                    //建筑图层
                    this.BL_cmb.Items.Add(ArcMap.Document.FocusMap.get_Layer(i).Name);
            }
            //风向初始化为45度
            double.TryParse(this.textBox1.Text, out ac.sita);
            //ac.sita = ac.sita + 90;   
            double.TryParse(this.textBox2.Text, out ac.sampleinterval);
        }

        //输入建筑图层
        private void BL_cmb_SelectedIndexChanged(object sender, EventArgs e)
        {
            ac.pBuildingLayer = null;
            //ac.danyuan.Clear();
            ac.DYHZ.Clear();

            //加载图层
            for (int i = 0; i < ArcMap.Document.FocusMap.LayerCount; i++)
            {
                if (ArcMap.Document.FocusMap.get_Layer(i).Name == this.BL_cmb.SelectedItem.ToString())
                {
                    ac.pBuildingLayer = ArcMap.Document.FocusMap.get_Layer(i) as IFeatureLayer;
                    this.BL_cmb.Text = this.BL_cmb.SelectedItem.ToString();                  
                    break;
                }
            }

            //给输出定义名字
            ac.lyrname = this.BL_cmb.Text;
            if (ac.lyrname.Length > 10)
                ac.lyrname = ac.lyrname.Substring(0, 10);


            //获取建筑物字段
            IFields pFields = ac.pBuildingLayer.FeatureClass.Fields;
            //这里写死了
            ac.pBuildingId = pFields.get_Field(0).Name;

            BHF_cmb.Items.Clear();
            BBlock_cmb.Items.Clear(); 

            for (int i = 0; i < pFields.FieldCount; i++)
            {
                //只加载数字类型
                //if (pFields.get_Field(i).VarType <= 6)
                //{
                    BHF_cmb.Items.Add(pFields.get_Field(i).Name);
                    BBlock_cmb.Items.Add(pFields.get_Field(i).Name);
                    //cmbEvaluateField.Items.Add(pFields.get_Field(i).Name);
                //}
                //BHF_cmb.Items.Add(pFields.get_Field(i).Name);
            }
        }

        //输入建筑高度字段
        private void BHF_cmb_SelectedIndexChanged(object sender, EventArgs e)
        {
            ac.pBuildingHeight = this.BHF_cmb.SelectedItem.ToString();
            this.BHF_cmb.Text = ac.pBuildingHeight;
            //ac.danyuan.Clear();
            ac.DYHZ.Clear();
        }

        //建筑图层中对应位置区域字段选择
        private void BBlock_cmb_SelectedIndexChanged(object sender, EventArgs e)
        {
            ac.pBuildingBlockField = this.BBlock_cmb.SelectedItem.ToString();
            this.BBlock_cmb.Text = ac.pBuildingBlockField;
            //ac.danyuan.Clear();
            ac.DYHZ.Clear();
        }

        //输入风向
        private void textBox1_TextChanged(object sender, EventArgs e)
        {
            bool bbb = double.TryParse(this.textBox1.Text, out ac.sita);
            if (bbb == false)
            {
                MessageBox.Show("请输入数字");
                this.textBox1.Text = "";

                return;
            }

            if (ac.sita < 0 || ac.sita > 360)
            {
                MessageBox.Show("请输入0-360范围的数字");
                this.textBox1.Text = "";
                this.Focus();
                return;
            }

            //sigma = ac.sita;

            dc.drawdirction(ac.sita);

        }

        //输入间隔
        private void textBox2_TextChanged(object sender, EventArgs e)
        {
            bool bbb = double.TryParse(this.textBox2.Text, out ac.sampleinterval);
            if (bbb == false)
            {
                MessageBox.Show("请输入数字");
                this.textBox2.Text = "";
                return;
            }
        }

        //坐标系帮助
        private void label10_Click(object sender, EventArgs e)
        {
            Form1 f = new Form1();
            f.ShowDialog();
        }

        // select path
        private void button3_Click(object sender, EventArgs e)
        {
            FolderBrowserDialog fbd = new FolderBrowserDialog();
            fbd.Description = "选择TXT文件路径";
            if (fbd.ShowDialog() == DialogResult.OK)
            {
                ac.path = fbd.SelectedPath;
                textBox3.Text = fbd.SelectedPath;
            }
        }

        // parameters help
        private void button2_Click(object sender, EventArgs e)
        {
            ParaHelpForm ph = new ParaHelpForm();
            ph.ShowDialog();
        }


        //Building Parameters
        private void OK_btn_Click(object sender, EventArgs e)
        {
            if (this.textBox1.Text == "")
            {
                MessageBox.Show("未输入风向");
                return;
            }

            if (BL_cmb.Text == "")
            {
                MessageBox.Show("未选择建筑图层");
                return;
            }

            if (BHF_cmb.Text == "")
            {
                MessageBox.Show("未选择建筑高度");
                return;
            }

            if (textBox3.Text == "")
            {
                MessageBox.Show("无输出路径");
                return;
            }
            this.label5.Text = "Running......";

            ac.Prosess0();

            MessageBox.Show("已完成！！！");
            label5.Text = "Finished!!!";

        }

        //Building Parameters(汇总)
        private void button6_Click(object sender, EventArgs e)
        {
            if (this.textBox1.Text == "")
            {
                MessageBox.Show("未输入风向");
                return;
            }

            if (BL_cmb.Text == "")
            {
                MessageBox.Show("未选择建筑图层");
                return;
            }

            if (BHF_cmb.Text == "")
            {
                MessageBox.Show("未选择建筑高度");
                return;
            }

            if (textBox3.Text == "")
            {
                MessageBox.Show("无输出路径");
                return;
            }
            this.label5.Text = "Running......";

            ac.Prosess1();

            MessageBox.Show("已完成！！！");
            label5.Text = "Finished!!!";
        }

        //FA ours
        private void button1_Click(object sender, EventArgs e)
        {
            if (this.textBox1.Text == "")
            {
                MessageBox.Show("未输入风向");
                return;
            }

            if (BL_cmb.Text == "")
            {
                MessageBox.Show("未选择建筑图层");
                return;
            }

            if (BHF_cmb.Text == "")
            {
                MessageBox.Show("未选择建筑高度");
                return;
            }

            if (BBlock_cmb.Text == "")
            {
                MessageBox.Show("未选择建筑所在单元ID");
                return;
            }

            if (textBox3.Text == "")
            {
                MessageBox.Show("无输出路径");
                return;
            }

            this.label5.Text = "Running......";

            DateTime start = DateTime.Now;

            ac.ProsessOurs();

            DateTime end = DateTime.Now;
            TimeSpan t = new TimeSpan(start.Ticks);
            TimeSpan t2 = new TimeSpan(end.Ticks);
            double interval=t2.TotalMilliseconds-t.TotalMilliseconds;
            MessageBox.Show("已完成！！！\r\n耗时："+interval.ToString()+"毫秒");
            label5.Text = "Finished!!!";
        }

        //FA N8
        private void button2_Click_1(object sender, EventArgs e)
        {
            if (this.textBox1.Text == "")
            {
                MessageBox.Show("未输入风向");
                return;
            }

            if (BL_cmb.Text == "")
            {
                MessageBox.Show("未选择建筑图层");
                return;
            }

            if (BHF_cmb.Text == "")
            {
                MessageBox.Show("未选择建筑高度");
                return;
            }

            if (BBlock_cmb.Text == "")
            {
                MessageBox.Show("未选择建筑所在单元ID");
                return;
            }

            if (textBox3.Text == "")
            {
                MessageBox.Show("无输出路径");
                return;
            }

            this.label5.Text = "Running......";

            ac.Prosess8D();

            MessageBox.Show("已完成！！！");
            label5.Text = "Finished!!!";
        }
        
        //FA wong 
        private void button5_Click(object sender, EventArgs e)
        {
            if (this.textBox1.Text == "")
            {
                MessageBox.Show("未输入风向");
                return;
            }

            if (BL_cmb.Text == "")
            {
                MessageBox.Show("未选择建筑图层");
                return;
            }

            if (BHF_cmb.Text == "")
            {
                MessageBox.Show("未选择建筑高度");
                return;
            }

            if (BBlock_cmb.Text == "")
            {
                MessageBox.Show("未选择建筑所在单元ID");
                return;
            }

            if (textBox3.Text == "")
            {
                MessageBox.Show("无输出路径");
                return;
            }

            if (textBox2.Text == "")
            {
                MessageBox.Show("无采样间隔");
                return;
            }

            this.label5.Text = "Running......";

            DateTime start = DateTime.Now;
            TimeSpan t = new TimeSpan(start.Ticks);

            ac.ProsessWong();

            DateTime end = DateTime.Now;
            TimeSpan t2 = new TimeSpan(end.Ticks);
            double interval = t2.TotalMilliseconds - t.TotalMilliseconds;
            MessageBox.Show("已完成！！！\r\n耗时：" + interval.ToString() + "毫秒");
            label5.Text = "Finished!!!";
        }




    }
}
