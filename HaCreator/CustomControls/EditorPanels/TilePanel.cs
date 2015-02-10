﻿/* Copyright (C) 2015 haha01haha01

* This Source Code Form is subject to the terms of the Mozilla Public
* License, v. 2.0. If a copy of the MPL was not distributed with this
* file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using WeifenLuo.WinFormsUI.Docking;
using HaCreator.MapEditor;
using MapleLib.WzLib;
using MapleLib.WzLib.WzProperties;
using MapleLib.WzLib.WzStructure.Data;
using System.Collections;
using HaCreator.ThirdParty;

namespace HaCreator.CustomControls.EditorPanels
{
    public partial class TilePanel : DockContent
    {
        private HaCreatorStateManager hcsm;

        public TilePanel(HaCreatorStateManager hcsm)
        {
            this.hcsm = hcsm;
            hcsm.SetTilePanel(this);
            InitializeComponent();

            List<string> sortedTileSets = new List<string>();
            foreach (DictionaryEntry tS in Program.InfoManager.TileSets)
                sortedTileSets.Add((string)tS.Key);
            sortedTileSets.Sort();
            foreach (string tS in sortedTileSets)
                tileSetList.Items.Add(tS);
        }

        private void searchResultsBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            SelectedIndexChanged.Invoke(sender, e);
        }

        public event EventHandler SelectedIndexChanged;

        private void tileBrowse_Click(object sender, EventArgs e)
        {
            new TileSetBrowser(tileSetList).ShowDialog();
            hcsm.MultiBoard.RenderFrame();
        }

        private void tileSetList_SelectedIndexChanged(object sender, EventArgs e)
        {
            LoadTileSetList();
            hcsm.MultiBoard.RenderFrame();
        }

        public void LoadTileSetList()
        {
            if (tileSetList.SelectedItem == null) return;
            tileImagesContainer.Controls.Clear();
            WzImage tileSetImage = Program.InfoManager.TileSets[(string)tileSetList.SelectedItem];
            if (tileSetImage == null) return;
            foreach (WzSubProperty tCat in tileSetImage.WzProperties)
            {
                if (tCat.Name == "info") continue;
                if (ApplicationSettings.randomTiles)
                {
                    WzCanvasProperty canvasProp = (WzCanvasProperty)tCat["0"];
                    if (canvasProp == null) continue;
                    ImageViewer item = tileImagesContainer.Add(canvasProp.PngProperty.GetPNG(false), tCat.Name, true);
                    TileInfo[] randomInfos = new TileInfo[tCat.WzProperties.Count];
                    for (int i = 0; i < randomInfos.Length; i++)
                    {
                        WzCanvasProperty tile = (WzCanvasProperty)tCat.WzProperties[i];
                        if (tile.HCTag == null)
                            tile.HCTag = TileInfo.Load(tile, (string)tileSetList.SelectedItem, tCat.Name, tile.Name);
                        randomInfos[i] = (TileInfo)tile.HCTag;
                    }
                    item.Tag = randomInfos;
                    item.MouseDown += new MouseEventHandler(tileItem_Click);
                    item.MouseUp += new MouseEventHandler(ImageViewer.item_MouseUp);
                }
                else
                {
                    foreach (WzCanvasProperty tile in tCat.WzProperties)
                    {
                        if (tile.HCTag == null)
                            tile.HCTag = TileInfo.Load(tile, (string)tileSetList.SelectedItem, tCat.Name, tile.Name);
                        ImageViewer item = tileImagesContainer.Add(tile.PngProperty.GetPNG(false), tCat.Name + "/" + tile.Name, true);
                        item.Tag = tile.HCTag;
                        item.MouseDown += new MouseEventHandler(tileItem_Click);
                        item.MouseUp += new MouseEventHandler(ImageViewer.item_MouseUp);
                    }
                }
            }
        }

        void tileItem_Click(object sender, MouseEventArgs e)
        {
            if (hcsm.MultiBoard.SelectedBoard.SelectedLayerIndex == -1)
            {
                MessageBox.Show("Select a real layer", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            Layer layer = hcsm.MultiBoard.SelectedBoard.SelectedLayer;
            if (layer.tS != null)
            {
                TileInfo infoToAdd = null;
                if (ApplicationSettings.randomTiles)
                    infoToAdd = ((TileInfo[])((ImageViewer)sender).Tag)[0];
                else
                    infoToAdd = (TileInfo)((ImageViewer)sender).Tag;
                if (infoToAdd.tS != layer.tS)
                {
                    MessageBox.Show("Error: layer tS already set to a different tS.\r\nPlease choose a different layer.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
            }
            hcsm.EnterEditMode(ItemTypes.Tiles);
            if (ApplicationSettings.randomTiles)
                hcsm.MultiBoard.SelectedBoard.Mouse.SetRandomTilesMode((TileInfo[])((ImageViewer)sender).Tag);
            else
                hcsm.MultiBoard.SelectedBoard.Mouse.SetHeldInfo((TileInfo)((ImageViewer)sender).Tag);
            hcsm.MultiBoard.Focus();
            hcsm.MultiBoard.RenderFrame();
            ((ImageViewer)sender).IsActive = true;
        }
    }
}
