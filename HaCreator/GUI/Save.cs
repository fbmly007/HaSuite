﻿/* Copyright (C) 2015 haha01haha01

* This Source Code Form is subject to the terms of the Mozilla Public
* License, v. 2.0. If a copy of the MPL was not distributed with this
* file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using HaCreator.MapEditor;
using HaCreator.Wz;
using MapleLib.WzLib.WzStructure.Data;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace HaCreator.GUI
{
    public partial class Save : Form
    {
        private Board board;

        public Save(Board board)
        {
            this.board = board;
            InitializeComponent();
            idBox.Text = board.MapInfo.id == -1 ? "" : board.MapInfo.id.ToString();
            idBox_TextChanged(null, null);
        }

        private void idBox_TextChanged(object sender, EventArgs e)
        {
            int id = 0;
            if (idBox.Text == "")
            {
                statusLabel.Text = "Please choose an ID";
                saveButton.Enabled = false;
            }
            else if (!int.TryParse(idBox.Text, out id))
            {
                statusLabel.Text = "Must enter a number";
                saveButton.Enabled = false;
            }
            else if (id < WzConstants.MinMap || id > WzConstants.MaxMap)
            {
                statusLabel.Text = "Out of range";
                saveButton.Enabled = false;
            }
            else if (WzInfoTools.GetMapStringProp(id.ToString()) != null)
            {
                statusLabel.Text = "WARNING: Will overwrite existing map";
                saveButton.Enabled = true;
            }
            else
            {
                statusLabel.Text = "";
                saveButton.Enabled = true;
            }
        }

        private void saveButton_Click(object sender, EventArgs e)
        {
            // Flush the UserObjects cache because the map we are saving might use of those images
            if (MessageBox.Show("You have unsaved user objects (images from your computer that you added to the editor). If you proceed, the following images will be written to the WZ file:\r\n\r\n" + board.ParentControl.UserObjects.NewObjects.Select(x => x.l2).Aggregate((x, y) => x + "\r\n" + y) + "\r\n\r\nIf you want to remove some or all of them, exit the saving dialog and remove them first.\r\nProceed?", "Unsaved Objects", MessageBoxButtons.YesNo, MessageBoxIcon.Information) != DialogResult.Yes)
                return;
            board.ParentControl.UserObjects.Flush();
            MapSaver saver = new MapSaver(board);
            int newId = int.Parse(idBox.Text);
            saver.ChangeMapID(newId);
            saver.SaveMapImage();
            saver.UpdateMapLists();
            MessageBox.Show("Saved map with ID: " + newId.ToString());
            Close();
        }
    }
}
