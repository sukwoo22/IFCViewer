using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using IFCViewer;

namespace WindowsFormsApplication2
{
    public partial class Form1 : Form
    {
        Scene scene = null;

        public Form1()
        {
            InitializeComponent();

        }

        private void openGLControl1_OpenGLDraw(object sender, SharpGL.RenderEventArgs args)
        {
            scene.Update();

            scene.Render(openGLControl1.OpenGL);
        }

        private void openGLControl1_OpenGLInitialized(object sender, EventArgs e)
        {
            scene = new Scene();

            scene.InitScene(openGLControl1.OpenGL, this.Width, this.Height);
        }

        private void 파일열기ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            OpenFileDialog oFD = new OpenFileDialog();
            oFD.Filter = "IFC FIles (*.ifc)|*.ifc;*.IFC|ALL Files|*.*";

            if (oFD.ShowDialog(this) != System.Windows.Forms.DialogResult.OK)
            {
                MessageBox.Show("Not a valid IFC file");
                return;
            }
            
            scene.ParseIFCFile(oFD.FileName);

            scene.InitDeviceBuffer(openGLControl1.OpenGL, this.panel1.Width, this.panel1.Height);

        }

        private void 파일초기화ToolStripMenuItem_Click(object sender, EventArgs e)
        {

            scene.ClearScene();
        }
    }
}
