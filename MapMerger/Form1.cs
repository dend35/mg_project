using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace MapMerger
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            new MapViewer().MergeMaps();
            //new MapViewer().RenderMap();
            new MapViewer().RenderThisMap();
            //new MapViewer().Save(MapViewer.ConvertMap());
        }
    }
}
