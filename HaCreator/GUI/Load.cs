﻿/* Copyright (C) 2015 haha01haha01

* This Source Code Form is subject to the terms of the Mozilla Public
* License, v. 2.0. If a copy of the MPL was not distributed with this
* file, You can obtain one at http://mozilla.org/MPL/2.0/. */

//uncomment the line below to create a space-time tradeoff (saving RAM by wasting more CPU cycles)
#define SPACETIME

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.Collections;
using System.Xml;
using System.Linq;
using System.IO;
using MapleLib.WzLib;
using MapleLib.WzLib.WzProperties;
using HaCreator.MapEditor;
using XNA = Microsoft.Xna.Framework;
using MapleLib.WzLib.WzStructure.Data;
using MapleLib.WzLib.WzStructure;
using MapleLib.Helpers;
using HaCreator.Wz;
using MapleLib.WzLib.Serialization;


namespace HaCreator.GUI
{
    public partial class Load : System.Windows.Forms.Form
    {
        public bool usebasepng = false;
        public int bufferzone = 100;
        private MultiBoard multiBoard;
        private System.Windows.Controls.TabControl Tabs;
        private System.Windows.RoutedEventHandler[] rightClickHandler;

        public Load(MultiBoard board, System.Windows.Controls.TabControl Tabs, System.Windows.RoutedEventHandler[] rightClickHandler)
        {
            InitializeComponent();
            DialogResult = DialogResult.Cancel;
            this.multiBoard = board;
            this.Tabs = Tabs;
            this.rightClickHandler = rightClickHandler;

            this.searchBox.TextChanged += this.mapBrowser.searchBox_TextChanged;
        }

        private void Load_Load(object sender, EventArgs e)
        {
            switch (ApplicationSettings.lastRadioIndex)
            {
                case 0:
                    HAMSelect.Checked = true;
                    HAMBox.Text = ApplicationSettings.LastHamPath;
                    break;
                case 1:
                    XMLSelect.Checked = true;
                    XMLBox.Text = ApplicationSettings.LastXmlPath;
                    break;
                case 2:
                    WZSelect.Checked = true;
                    break;
            }
            this.mapBrowser.InitializeMaps(true);
        }

        private void selectionChanged(object sender, EventArgs e)
        {
            if (HAMSelect.Checked)
            {
                ApplicationSettings.lastRadioIndex = 0;
                HAMBox.Enabled = true;
                XMLBox.Enabled = false;
                searchBox.Enabled = false;
                mapBrowser.IsEnabled = false;
                loadButton.Enabled = true;
            }
            else if (XMLSelect.Checked)
            {
                ApplicationSettings.lastRadioIndex = 1;
                HAMBox.Enabled = false;
                XMLBox.Enabled = true;
                searchBox.Enabled = false;
                mapBrowser.IsEnabled = false;
                loadButton.Enabled = XMLBox.Text != "";
            }
            else if (WZSelect.Checked)
            {
                ApplicationSettings.lastRadioIndex = 2;
                HAMBox.Enabled = false;
                XMLBox.Enabled = false;
                searchBox.Enabled = true;
                mapBrowser.IsEnabled = true;
                loadButton.Enabled = mapBrowser.LoadAvailable;
            }
        }

        private void browseXML_Click(object sender, EventArgs e)
        {
            OpenFileDialog dialog = new OpenFileDialog();
            dialog.Title = "Select XML to load...";
            dialog.Filter = "eXtensible Markup Language file (*.xml)|*.xml";
            if (dialog.ShowDialog() != DialogResult.OK)
            {
                return;
            }
            XMLBox.Text = dialog.FileName;
            loadButton.Enabled = true;
        }

        private void browseHAM_Click(object sender, EventArgs e)
        {
            OpenFileDialog dialog = new OpenFileDialog();
            dialog.Title = "Select Map to load...";
            dialog.Filter = "HaCreator Map File (*.ham)|*.ham";
            if (dialog.ShowDialog() != DialogResult.OK)
            {
                return;
            }
            HAMBox.Text = dialog.FileName;
            loadButton.Enabled = true;
        }

        private void loadButton_Click(object sender, EventArgs e)
        {
            //Hide();
            WaitWindow ww = new WaitWindow("Loading...");
            ww.Show();
            Application.DoEvents();

            MapLoader loader = new MapLoader();
            WzImage mapImage = null;
            int mapid = -1;
            string mapName = null, streetName = "", categoryName = "";
            WzSubProperty strMapProp = null;


            if (HAMSelect.Checked)
            {
                loader.CreateMapFromHam(multiBoard, Tabs, File.ReadAllText(HAMBox.Text), rightClickHandler);
                DialogResult = DialogResult.OK;
                ww.EndWait();
                Close();
                return;
            }
            else if (XMLSelect.Checked)
            {
                try
                {
                    mapImage = (WzImage)new WzXmlDeserializer(false, null).ParseXML(XMLBox.Text)[0];
                }
                catch
                {
                    MessageBox.Show("Error while loading XML. Aborted.");
                    ww.EndWait();
                    Show();
                    return;
                }
            }
            else if (WZSelect.Checked)
            {
                if (mapBrowser.SelectedItem == "MapLogin")
                {
                    mapImage = (WzImage)Program.WzManager["ui"]["MapLogin.img"];
                    mapName = streetName = categoryName = "MapLogin";
                }
                else if (mapBrowser.SelectedItem == "MapLogin1")
                {
                    mapImage = (WzImage)Program.WzManager["ui"]["MapLogin1.img"];
                    mapName = streetName = categoryName = "MapLogin1";
                }
                else if (mapBrowser.SelectedItem == "CashShopPreview")
                {
                    mapImage = (WzImage)Program.WzManager["ui"]["CashShopPreview.img"];
                    mapName = streetName = categoryName = "CashShopPreview";
                }
                else
                {
                    string mapid_str = mapBrowser.SelectedItem.Substring(0, 9);
                    int.TryParse(mapid_str, out mapid);

                    string mapcat = "Map" + mapid_str.Substring(0, 1);
                    if (Program.WzManager.wzFiles.ContainsKey("map002"))//i hate nexon so much  
                    {
                        mapImage = (WzImage)Program.WzManager["map002"]["Map"][mapcat][mapid_str + ".img"];
                    }
                    else
                    {
                        mapImage = (WzImage)Program.WzManager["map"]["Map"][mapcat][mapid_str + ".img"];
                    }
                    strMapProp = WzInfoTools.GetMapStringProp(mapid_str);
                    mapName = WzInfoTools.GetMapName(strMapProp);
                    streetName = WzInfoTools.GetMapStreetName(strMapProp);
                    categoryName = WzInfoTools.GetMapCategoryName(strMapProp);
                }
            }
            loader.CreateMapFromImage(mapid, mapImage, mapName, streetName, categoryName, strMapProp, Tabs, multiBoard, rightClickHandler);
            DialogResult = DialogResult.OK;
            ww.EndWait();
            Close();
        }

        private void mapBrowser_SelectionChanged()
        {
            loadButton.Enabled = mapBrowser.LoadAvailable;
        }

        private void Load_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Escape)
            {
                Close();
            }
            else if (e.KeyCode == Keys.Enter)
            {
                loadButton_Click(null, null);
            }
        }

        private void HAMBox_TextChanged(object sender, EventArgs e)
        {
            ApplicationSettings.LastHamPath = HAMBox.Text;
        }

        private void XMLBox_TextChanged(object sender, EventArgs e)
        {
            ApplicationSettings.LastXmlPath = XMLBox.Text;
        }
    }
}
