using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace EtiquetaFORNew.Forms
{
    public partial class Faq : Form
    {
        public Faq()
        {
            InitializeComponent();
        }

        private void lbl8046_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            Process.Start("https://helptools.softcomsistemas.com.br/core/promover/detalhe/id/8046");
        }

        private void linklbl8002_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            Process.Start("https://helptools.softcomsistemas.com.br/core/promover/detalhe/id/8002");
        }

        private void linklbl8001_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            Process.Start("https://helptools.softcomsistemas.com.br/core/promover/detalhe/id/8001");
        }

        private void linklbl8047_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            Process.Start("https://helptools.softcomsistemas.com.br/core/promover/detalhe/id/8047");
        }

        private void linklbl9320_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            Process.Start("https://helptools.softcomsistemas.com.br/core/promover/detalhe/id/9320");
        }

        private void linklbl9321_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            Process.Start("https://helptools.softcomsistemas.com.br/core/promover/detalhe/id/9321");
        }
    }
}
