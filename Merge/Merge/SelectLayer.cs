using ESRI.ArcGIS.ADF.BaseClasses;
using ESRI.ArcGIS.ADF.CATIDs;
using ESRI.ArcGIS.ArcMapUI;
using ESRI.ArcGIS.Carto;
using ESRI.ArcGIS.Framework;
using ESRI.ArcGIS.SystemUI;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace Merge
{
    /// <summary>
    /// Summary description for SelectLayer.
    /// </summary>
    [Guid("b6e1c737-47b8-47c0-8012-9275637de102")]
    [ClassInterface(ClassInterfaceType.None)]
    [ProgId("Merge.SelectLayer")]
    public sealed class SelectLayer : BaseCommand, IToolControl
    {
        #region COM Registration Function(s)
        [ComRegisterFunction()]
        [ComVisible(false)]
        static void RegisterFunction(Type registerType)
        {
            // Required for ArcGIS Component Category Registrar support
            ArcGISCategoryRegistration(registerType);

            //
            // TODO: Add any COM registration code here
            //
        }

        [ComUnregisterFunction()]
        [ComVisible(false)]
        static void UnregisterFunction(Type registerType)
        {
            // Required for ArcGIS Component Category Registrar support
            ArcGISCategoryUnregistration(registerType);

            //
            // TODO: Add any COM unregistration code here
            //
        }

        #region ArcGIS Component Category Registrar generated code
        /// <summary>
        /// Required method for ArcGIS Component Category registration -
        /// Do not modify the contents of this method with the code editor.
        /// </summary>
        private static void ArcGISCategoryRegistration(Type registerType)
        {
            string regKey = string.Format("HKEY_CLASSES_ROOT\\CLSID\\{{{0}}}", registerType.GUID);
            MxCommands.Register(regKey);

        }
        /// <summary>
        /// Required method for ArcGIS Component Category unregistration -
        /// Do not modify the contents of this method with the code editor.
        /// </summary>
        private static void ArcGISCategoryUnregistration(Type registerType)
        {
            string regKey = string.Format("HKEY_CLASSES_ROOT\\CLSID\\{{{0}}}", registerType.GUID);
            MxCommands.Unregister(regKey);

        }

        #endregion
        #endregion

        private IApplication m_application;
        private ComboBox cb;
        IMap map = null;
        private Dictionary<string, ILayer> dclayers;
        public static ILayer selectLayer = null;
        public SelectLayer()
        {
            //
            // TODO: Define values for the public properties
            //
            base.m_category = "选择待处理的图层"; //localizable text
            base.m_caption = "选择待处理的图层";  //localizable text
            base.m_message = "选择待处理的图层";  //localizable text 
            base.m_toolTip = "选择待处理的图层";  //localizable text 
            base.m_name = "选择待处理的图层";   //unique id, non-localizable (e.g. "MyCategory_ArcMapCommand")
            cb = new ComboBox();
            cb.DropDownStyle = ComboBoxStyle.DropDownList;
            cb.Size = new System.Drawing.Size(160, 27);
            cb.SelectedIndexChanged += cb_SelectedIndexChanged;
            cb.Click += cb_Click;
            try
            {
                //
                // TODO: change bitmap name if necessary
                //
                string bitmapResourceName = GetType().Name + ".bmp";
                base.m_bitmap = new Bitmap(GetType(), bitmapResourceName);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.WriteLine(ex.Message, "Invalid Bitmap");
            }
        }

        void cb_Click(object sender, EventArgs e)
        {
            if (m_application != null)
            {
                map = (m_application.Document as IMxDocument).ActiveView.FocusMap;
                IEnumLayer layers = map.get_Layers();
                ILayer layer = null;
                layers.Reset();//将迭代器重置为集合中的第一层。
                dclayers = new Dictionary<string, ILayer>();
                this.cb.Items.Clear();
                while ((layer = layers.Next()) != null)
                {
                    if (!layer.Visible)
                        continue;
                    if (!dclayers.ContainsKey(layer.Name))
                    {
                        dclayers.Add(layer.Name, layer);
                        this.cb.Items.Add(layer.Name);//向下拉框中添加地图中的图层
                    }
                }
                //if (this.cb.Items.Count > 0)
                //{
                //    this.cb.SelectedIndex = 0;
                //}
            }
        }

        void cb_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (this.cb.SelectedItem != null)
            {
                selectLayer = dclayers[this.cb.SelectedItem.ToString()] as ILayer;//获得下拉框选中的图层Layer
            }
        }

        #region Overridden Class Methods

        /// <summary>
        /// Occurs when this command is created
        /// </summary>
        /// <param name="hook">Instance of the application</param>
        public override void OnCreate(object hook)
        {
            if (hook == null)
                return;

            m_application = hook as IApplication;

            //Disable if it is not ArcMap
            if (hook is IMxApplication)
                base.m_enabled = true;
            else
                base.m_enabled = false;
            map = (m_application.Document as IMxDocument).ActiveView.FocusMap;
            // TODO:  Add other initialization code
        }

        /// <summary>
        /// Occurs when this command is clicked
        /// </summary>
        public override void OnClick()
        {
            // TODO: Add SelectLayer.OnClick implementation
            if (m_application != null)
            {
                map = (m_application.Document as IMxDocument).ActiveView.FocusMap;

                IEnumLayer layers = map.get_Layers();
                ILayer layer = null; layers.Reset();
                dclayers = new Dictionary<string, ILayer>();
                this.cb.Items.Clear();
                while ((layer = layers.Next()) != null)
                {
                    if (!layer.Visible)
                        continue;
                    if (!dclayers.ContainsKey(layer.Name))
                    {
                        dclayers.Add(layer.Name, layer);
                        this.cb.Items.Add(layer.Name);
                    }
                }

                if (this.cb.Items.Count > 0)
                {
                    this.cb.SelectedIndex = 0;
                    cb_SelectedIndexChanged(this.cb, new EventArgs());
                }
            }
        }

        #endregion
        #region IToolControl 成员

        public bool OnDrop(esriCmdBarType barType)
        {
            return true;
            //throw new NotImplementedException();
        }

        public void OnFocus(ICompletionNotify complete)
        {
            //throw new NotImplementedException();
        }

        public int hWnd
        {
            get { return cb.Handle.ToInt32(); }
        }

        #endregion
    }
}
