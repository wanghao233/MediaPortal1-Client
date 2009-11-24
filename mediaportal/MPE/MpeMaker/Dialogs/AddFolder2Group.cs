﻿using System;
using System.IO;
using System.Windows.Forms;
using MpeCore;
using MpeCore.Classes;
using MpeCore.Classes.Project;

namespace MpeMaker.Dialogs
{
    public partial class AddFolder2Group : Form
    {
        private GroupItem _groupItem;
        private PackageClass Package; 

        public AddFolder2Group(PackageClass packageClass, GroupItem groupItem)
        {
            Init();
            _groupItem = groupItem;
            Package= packageClass;
        }

        public AddFolder2Group(PackageClass packageClass, GroupItem groupItem, string folder)
        {
            Init();
            _groupItem = groupItem;
            txt_folder.Text = folder;
            Package = packageClass;
        }

        private void Init()
        {
            InitializeComponent();
            foreach (var keyValuePair in MpeInstaller.InstallerTypeProviders)
            {
                cmb_installtype.Items.Add(keyValuePair.Key);
            }
            cmb_installtype.SelectedIndex = 0;
            cmb_overwrite.SelectedIndex = 2;
        }

        private void add_folder_Click(object sender, EventArgs e)
        {
            folderBrowserDialog1.SelectedPath = txt_folder.Text;
            if(folderBrowserDialog1.ShowDialog()==DialogResult.OK)
            {
                txt_folder.Text = folderBrowserDialog1.SelectedPath;
            }
        }

        private void btn_add_template_Click(object sender, EventArgs e)
        {
            PathTemplateSelector dlg = new PathTemplateSelector();
            if (dlg.ShowDialog() == DialogResult.OK)
            {
                txt_template.Text = dlg.Result +"\\"+ Path.GetFileName(txt_folder.Text);
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.Cancel;
            Close();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(txt_folder.Text))
            {
                MessageBox.Show("Source folder not specified !");
                return;
            }

            if (string.IsNullOrEmpty(txt_template.Text))
            {
                MessageBox.Show("Template not specified !");
                return;
            }

            if (!Directory.Exists(txt_folder.Text))
            {
                MessageBox.Show("Folder not found !");
                return;
            }

            FolderGroup group = new FolderGroup
                                    {
                                        DestinationFilename = txt_template.Text,
                                        Folder = txt_folder.Text,
                                        InstallType = cmb_installtype.Text,
                                        UpdateOption = (UpdateOptionEnum)cmb_overwrite.SelectedIndex,
                                        Param1 = txt_param1.Text,
                                        Recursive = chk_recurs.Checked,
                                        Group = _groupItem.Name
                                    };

            Package.ProjectSettings.Add(group);
            ProjectSettings.UpdateFiles(Package, group);

            DialogResult = DialogResult.OK;
            Close();
        }
    }
}
