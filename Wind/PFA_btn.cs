using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

namespace Wind
{
    public class PFA_btn : ESRI.ArcGIS.Desktop.AddIns.Button
    {
        public PFA_btn()
        {
        }

        protected override void OnClick()
        {
            //
            //  TODO: Sample code showing how to access button host
            
            //
            PFA_Form f = new PFA_Form();
            f.Show();


            ArcMap.Application.CurrentTool = null;
        }
        protected override void OnUpdate()
        {
            Enabled = ArcMap.Application != null;
        }
    }

}
