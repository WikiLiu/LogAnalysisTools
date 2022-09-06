using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace LogAnalysisTools
{
    public partial class FormHistory : Form
    {
        public FormHistory()
        {
            InitializeComponent();
        }

        private void FormHistory_Load(object sender, EventArgs e)
        {
            ckboxList.Items.Clear();
            EntityLog log = new EntityLog();
            List<string> data = log.GetDate();
            foreach (string item in data)
            {
                ckboxList.Items.Add(item);
            }
            Form1 frm1 = (Form1)this.Owner;
            if (frm1.CurrentDateList != null && frm1.CurrentDateList.Count > 0)
            {
                for (int i = 0; i < ckboxList.Items.Count; i++)
                {
                    if (frm1.CurrentDateList.Contains(ckboxList.Items[i].ToString()))
                    {
                        ckboxList.SetItemChecked(i, true);
                    }
                }
            }
        }

        private void see_button_Click(object sender, EventArgs e)
        {
            List<string> selist = getSelectedItem();

            Form1 frm1 = (Form1)this.Owner;
            frm1.CurrentDateList = selist;
            frm1.SeeHistoryData();
        }

        private List<string> getSelectedItem()
        {
            List<string> selist = new List<string>();

            for (int i = 0; i < ckboxList.Items.Count; i++)
            {
                if (ckboxList.GetItemChecked(i))
                {
                    selist.Add(ckboxList.Items[i].ToString());
                }
            }
            return selist;
        }

        private void del_hostory_button_Click(object sender, EventArgs e)
        {
            List<string> selist = getSelectedItem();
            if (selist.Count > 0)
            {
                DialogResult dr = MessageBox.Show("您确定要删除吗？删除后不可恢复.", "系统提示", MessageBoxButtons.OKCancel, MessageBoxIcon.Warning);
                if (dr == DialogResult.OK)
                {
                    EntityLog el = new EntityLog();
                    el.DeleteData(selist);

                    //刷新列表
                    FormHistory_Load(sender, e);
                }
            }
            else
            {
                MessageBox.Show("请先选择要删除的项目！");
            }
        }
    }
}
