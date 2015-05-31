﻿/* Copyright (C) 2015 haha01haha01

* This Source Code Form is subject to the terms of the Mozilla Public
* License, v. 2.0. If a copy of the MPL was not distributed with this
* file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using HaCreator.MapEditor.Info;
using HaCreator.MapEditor.Input;
using HaCreator.MapEditor.Instance;
using HaCreator.MapEditor.UndoRedo;
using MapleLib.WzLib.WzStructure.Data;
using XNA = Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HaCreator.Exceptions;

namespace HaCreator.MapEditor
{
    public abstract class BoardItem : ISerializable
    {
        protected XNA.Vector3 position;
        private Dictionary<BoardItem, XNA.Point> boundItems = new Dictionary<BoardItem, XNA.Point>();//key = BoardItem; value = point (distance)
        private List<BoardItem> boundItemsList = new List<BoardItem>();
        private BoardItem parent = null;
        private bool selected = false;
        protected Board board;

        /*temporary fields used by other functions*/
        public BoardItem tempParent = null; //for mouse drag-drop
        public XNA.Point moveStartPos = new XNA.Point(); //for undo of drag-drop

        public BoardItem(Board board, int x, int y, int z)
        {
            position = new XNA.Vector3(x, y, z);
            this.board = board;
        }

        #region Methods
        public virtual void InsertItem()
        {
            lock (Board.ParentControl)
            {
                Board.BoardItems.Add(this, true);
            }
        }

        public virtual void OnItemPlaced(List<UndoRedoAction> undoPipe)
        {
            lock (Board.ParentControl)
            {
                List<BoardItem> items = boundItems.Keys.ToList();
                foreach (BoardItem item in items)
                    item.OnItemPlaced(undoPipe);

                if (undoPipe != null)
                {
                    undoPipe.Add(UndoRedoManager.ItemAdded(this));
                }
                if (parent != null)
                {
                    if (!(parent is Mouse) && undoPipe != null)
                    {
                        undoPipe.Add(UndoRedoManager.ItemsLinked(parent, this, (Microsoft.Xna.Framework.Point)parent.boundItems[this]));
                    }
                }
            }
        }

        public virtual void RemoveItem(List<UndoRedoAction> undoPipe)
        {
            lock (Board.ParentControl)
            {
                List<BoardItem> items = boundItems.Keys.ToList();
                foreach (BoardItem item in items)
                    item.RemoveItem(undoPipe);

                if (undoPipe != null)
                {
                    undoPipe.Add(UndoRedoManager.ItemDeleted(this));
                }
                if (parent != null)
                {
                    if (!(parent is Mouse) && undoPipe != null)
                    {
                        undoPipe.Add(UndoRedoManager.ItemsUnlinked(parent, this, (Microsoft.Xna.Framework.Point)parent.boundItems[this]));
                    }
                    parent.ReleaseItem(this);
                }
                Selected = false;
                board.BoardItems.Remove(this);
            }
        }

        public static Texture2D TextureFromBitmap(GraphicsDevice device, System.Drawing.Bitmap bitmap)
        {
            Texture2D texture;
            using (System.IO.MemoryStream s = new System.IO.MemoryStream())
            {
                bitmap.Save(s, System.Drawing.Imaging.ImageFormat.Png);
                s.Seek(0, System.IO.SeekOrigin.Begin);
                texture = Texture2D.FromStream(device, s);
                //texture = Texture2D.FromFile(device, s);
            }
            return texture;
        }

        public virtual void BindItem(BoardItem item, XNA.Point distance)
        {
            lock (Board.ParentControl)
            {
                if (boundItems.ContainsKey(item)) return;
                boundItems[item] = distance;
                boundItemsList.Add(item);
                item.parent = this;
            }
        }

        public virtual void ReleaseItem(BoardItem item)
        {
            lock (Board.ParentControl)
            {
                if (boundItems.ContainsKey(item))
                {
                    boundItems.Remove(item);
                    boundItemsList.Remove(item);
                    item.parent = null;
                }
            }
        }

        public virtual void Move(int x, int y)
        {
            lock (Board.ParentControl)
            {
                position.X = x;
                position.Y = y;
                List<BoardItem> items = boundItems.Keys.ToList();
                foreach (BoardItem item in items)
                {
                    XNA.Point value = boundItems[item];
                    item.Move(value.X + x, value.Y + y);
                }
                if (this.parent != null && !(parent is Mouse))
                {
                    parent.boundItems[this] = new XNA.Point(this.X - parent.X, this.Y - parent.Y);
                }
                if (this.tempParent != null && !tempParent.Selected) //to fix a certain mouse selection bug
                {
                    tempParent.boundItems[this] = new XNA.Point(this.X - tempParent.X, this.Y - tempParent.Y);
                }
            }
        }

        public void SnapMove(int x, int y)
        {
            lock (Board.ParentControl)
            {
                position.X = x;
                position.Y = y;
                List<BoardItem> items = boundItems.Keys.ToList();
                foreach (BoardItem item in items)
                {
                    XNA.Point value = boundItems[item];
                    item.Move(value.X + x, value.Y + y);
                }
                if (this.tempParent != null && !tempParent.Selected) //to fix a certain mouse selection bug
                {
                    tempParent.boundItems[this] = new XNA.Point(this.X - tempParent.X, this.Y - tempParent.Y);
                }
            }
        }

        public virtual void Draw(SpriteBatch sprite, XNA.Color color, int xShift, int yShift)
        {
            if (ApplicationSettings.InfoMode)
            {
                Board.ParentControl.FillRectangle(sprite, new XNA.Rectangle(this.X - UserSettings.DotWidth + xShift, this.Y - UserSettings.DotWidth + yShift, UserSettings.DotWidth * 2, UserSettings.DotWidth * 2), UserSettings.OriginColor);
            }
        }

        public virtual bool CheckIfLayerSelected(SelectionInfo sel)
        {
            // By default, item is nonlayered
            return true;
        }
        public virtual XNA.Color GetColor(SelectionInfo sel, bool selected)
        {
            if ((sel.editedTypes & Type) == Type && CheckIfLayerSelected(sel))
                return selected ? UserSettings.SelectedColor : XNA.Color.White;
            else return MultiBoard.InactiveColor;
        }

        public virtual bool IsPixelTransparent(int x, int y)
        {
            lock (Board.ParentControl)
            {
                System.Drawing.Bitmap image = this.Image;
                if (this is IFlippable && ((IFlippable)this).Flip)
                    x = image.Width - x;
                return image.GetPixel(x, y).A == 0;
            }
        }

        public bool BoundToSelectedItem(Board board)
        {
            lock (Board.ParentControl)
            {
                BoardItem currItem = Parent;
                while (currItem != null)
                {
                    if (board.SelectedItems.Contains(currItem)) return true;
                    else currItem = currItem.Parent;
                }
            }
            return false;
        }
        #endregion

        #region Properties
        public abstract System.Drawing.Bitmap Image { get; }
        public abstract System.Drawing.Point Origin { get; }
        public abstract ItemTypes Type { get; }
        public abstract MapleDrawableInfo BaseInfo { get; }

        public abstract int Width { get; }
        public abstract int Height { get; }
        public virtual int X
        {
            get
            {
                return (int)position.X;
            }
            set
            {
                Move(value, (int)position.Y);
            }
        }

        public virtual int Y
        {
            get
            {
                return (int)position.Y;
            }
            set
            {
                Move((int)position.X, value);
            }
        }

        public virtual int Z
        {
            get
            {
                return (int)position.Z;
            }
            set
            {
                position.Z = Math.Max(0, value);
                /*if (this is LayeredItem || this is BackgroundInstance)
                    board.BoardItems.Sort();*/
            }
        }

        public virtual int Left
        {
            get { return (int)X - Origin.X; }
        }

        public virtual int Top
        {
            get { return (int)Y - Origin.Y; }
        }

        public virtual int Right
        {
            get { return (int)X - Origin.X + Width; }
        }

        public virtual int Bottom
        {
            get { return (int)Y - Origin.Y + Height; }
        }

        public virtual bool Selected
        {
            get
            {
                return selected;
            }
            set
            {
                lock (Board.ParentControl)
                {
                    if (selected == value) return;
                    selected = value;
                    if (value && !board.SelectedItems.Contains(this))
                        board.SelectedItems.Add(this);
                    else if (!value && board.SelectedItems.Contains(this))
                        board.SelectedItems.Remove(this);
                    if (board.SelectedItems.Count == 1)
                        board.ParentControl.OnSelectedItemChanged(board.SelectedItems[0]);
                    else if (board.SelectedItems.Count == 0)
                        board.ParentControl.OnSelectedItemChanged(null);
                }
            }
        }

        public Board Board
        {
            get
            {
                return board;
            }
        }

        public virtual Dictionary<BoardItem, XNA.Point> BoundItems
        {
            get
            {
                return boundItems;
            }
        }

        public virtual List<BoardItem> BoundItemsList
        {
            get
            {
                return boundItemsList;
            }
        }

        public virtual BoardItem Parent
        {
            get { return parent; }
            set { parent = value; }
        }
        #endregion

        #region ISerializable Implementation
        public virtual bool ShouldSerialize
        {
            get
            {
                return true;
            }
        }

        public virtual bool ShouldSerializeChildren
        {
            get
            {
                return boundItems.Count > 0;
            }
        }

        public virtual List<ISerializable> SelectSerialized()
        {
            List<ISerializable> serList = new List<ISerializable>();
            foreach (BoardItem item in BoundItems.Keys)
            {
                serList.Add((ISerializable)item);
            }
            return serList;
        }

        public virtual dynamic Serialize()
        {
            dynamic result = new ExpandoObject();
            result.x = position.X;
            result.y = position.Y;
            result.z = position.Z;
            return result;
        }

        public virtual IDictionary<string, object> SerializeBindings(Dictionary<ISerializable, int> refDict)
        {
            Dictionary<string, object> result = new Dictionary<string, object>();
            List<int> bindOrder = new List<int>();
            foreach (BoardItem item in boundItemsList)
            {
                int refNum = refDict[item];
                result.Add(refNum.ToString(), SerializationManager.SerializePoint(boundItems[item]));
                bindOrder.Add(refNum);
            }
            if (bindOrder.Count > 0)
                result.Add("bindOrder", bindOrder.ToArray());
            return result;
        }

        public BoardItem(Board board, dynamic json)
        {
            this.board = board;
            position = new XNA.Vector3((float)json.x, (float)json.y, (float)json.z);
        }

        public virtual void DeserializeBindings(IDictionary<string, object> bindSer, Dictionary<int, ISerializable> refDict)
        {
            if (!bindSer.ContainsKey("bindOrder"))
                return; // No bindings were serialized
            int[] bindOrder = (int[])bindSer["bindOrder"];
            foreach (int id in bindOrder)
            {
                BoardItem item = (BoardItem)refDict[id];
                XNA.Point offs = (XNA.Point)bindSer[id.ToString()];
                boundItems.Add(item, offs);
                boundItemsList.Add(item);
                item.parent = this;
            }
        }

        public virtual void AddToBoard(List<UndoRedoAction> undoPipe)
        {
            OnItemPlaced(undoPipe);
            board.BoardItems.Add(this, false);
        }
        #endregion
    }
}
