using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace io.nodekit.NKElectro
{
    public partial class NKE_Window : Form
    {

        public NKE_Window()
        {
            InitializeComponent();
        }

        public NKE_Window(Control item)
        {
            InitializeComponent();
            this.SuspendLayout();
            item.Dock = System.Windows.Forms.DockStyle.Fill;
            item.Location = new System.Drawing.Point(0, 0);
            item.MinimumSize = new System.Drawing.Size(20, 20);
            item.Name = "webView";
            item.Size = this.ClientSize; ;
            item.TabIndex = 0;
            this.Controls.Add(item);
            this.ResumeLayout(false);
        }

    }
}
