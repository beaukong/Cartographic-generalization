using System;
using System.Drawing;
using System.Runtime.InteropServices;
using ESRI.ArcGIS.ADF.BaseClasses;
using ESRI.ArcGIS.ADF.CATIDs;
using ESRI.ArcGIS.Controls;
using System.Windows.Forms;


using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using ESRI.ArcGIS.Carto;
using ESRI.ArcGIS.Framework;
using ESRI.ArcGIS.Editor;
using ESRI.ArcGIS.Display;
using ESRI.ArcGIS.ArcMapUI;
using WHU.Common;
using WHUTools;

using System.Collections.Generic;
using System.IO;
namespace testTool
{
    /// <summary>
    /// Summary description for HighlightStroke.
    /// </summary>
    [Guid("0278716e-757b-4f8d-a411-9bb4896e24dc")]
    [ClassInterface(ClassInterfaceType.None)]
    [ProgId("testTool.绘制Stroke.HighlightStroke")]
    public sealed class HighlightStroke : BaseTool
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
            ControlsCommands.Register(regKey);
        }
        /// <summary>
        /// Required method for ArcGIS Component Category unregistration -
        /// Do not modify the contents of this method with the code editor.
        /// </summary>
        private static void ArcGISCategoryUnregistration(Type registerType)
        {
            string regKey = string.Format("HKEY_CLASSES_ROOT\\CLSID\\{{{0}}}", registerType.GUID);
            MxCommands.Unregister(regKey);
            ControlsCommands.Unregister(regKey);
        }

        #endregion
        #endregion

        private IHookHelper m_hookHelper = null;
        private IApplication application = null;
        private IMap map = null;
        private IActiveView activew = null;
        private ILayer layer = null;
        private IFeatureClass fClass = null;
        private INewEnvelopeFeedback newEnvelopeFb = null;
        private Form form = null;//点击按钮时的弹窗。待改进点
        public HighlightStroke()
        {
            //
            // TODO: Define values for the public properties
            //
            base.m_category = "HighlightStroke"; //localizable text 
            base.m_caption = "高亮Stroke";  //localizable text 
            base.m_message = "Draw envelope and HighlightStroke";  //localizable text
            base.m_toolTip = "HighlightStroke";  //localizable text
            base.m_name = "HighlightStroke";   //unique id, non-localizable (e.g. "MyCategory_MyTool")
            try
            {
                //
                // TODO: change resource name if necessary
                //
                string bitmapResourceName = GetType().Name + ".bmp";
                base.m_bitmap = new Bitmap(GetType(), bitmapResourceName);
                base.m_cursor = new System.Windows.Forms.Cursor(GetType(), GetType().Name + ".cur");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.WriteLine(ex.Message, "Invalid Bitmap");
            }
        }

        #region Overridden Class Methods

        /// <summary>
        /// Occurs when this tool is created
        /// </summary>
        /// <param name="hook">Instance of the application</param>
        public override void OnCreate(object hook)
        {
            application = hook as IApplication;
            try
            {
                m_hookHelper = new HookHelperClass();
                m_hookHelper.Hook = hook;
                if (m_hookHelper.ActiveView == null)
                {
                    m_hookHelper = null;
                }
            }
            catch
            {
                m_hookHelper = null;
            }

            if (m_hookHelper == null)
                base.m_enabled = false;
            else
                base.m_enabled = true;

            // TODO:  Add other initialization code
        }

        /// <summary>
        /// Occurs when this tool is clicked
        /// </summary>
        public override void OnClick()
        {
            // TODO: Add HighlightStroke.OnClick implementation
            map = (application.Document as IMxDocument).ActiveView.FocusMap;
            activew = map as IActiveView;
            layer = DApplication.App.currentLayer;
            if (layer==null)
            {
                MessageBox.Show("地图文档中无数据！");
            }
            fClass = (layer as FeatureLayer).FeatureClass;
            //弹出窗体
            //form = new Form();
            //将fClass的字段展示出来
            //选择高亮依据的字段
        }

        public override void OnMouseDown(int Button, int Shift, int X, int Y)
        {
            // TODO:  Add HighlightStroke.OnMouseDown implementation
            if (activew == null) activew = m_hookHelper.FocusMap as IActiveView;
            else
            {
                newEnvelopeFb = null;
                newEnvelopeFb = new NewEnvelopeFeedbackClass();
                IPoint point = activew.ScreenDisplay.DisplayTransformation.ToMapPoint(X, Y);
                newEnvelopeFb.Display = activew.ScreenDisplay;
                newEnvelopeFb.Start(point);
            }
        }

        public override void OnMouseMove(int Button, int Shift, int X, int Y)
        {
            // TODO:  Add HighlightStroke.OnMouseMove implementation
            if (newEnvelopeFb!=null)
            {
                if (activew == null) activew = m_hookHelper.FocusMap as IActiveView;
                else
                {
                    newEnvelopeFb.Display = activew.ScreenDisplay;
                    IPoint point = activew.ScreenDisplay.DisplayTransformation.ToMapPoint(X, Y);
                    newEnvelopeFb.MoveTo(point);
                }
            }
        }

        public override void OnMouseUp(int Button, int Shift, int X, int Y)
        {
            // TODO:  Add HighlightStroke.OnMouseUp implementation
            IEnvelope envelope = newEnvelopeFb.Stop();
            if (envelope!=null)
            {
                try
                {
                    string  sValue= getStrokeValue(envelope);//获取选中线要素的StrokeValue
                    Highlight(fClass, sValue);
                }
                catch (Exception e)
                {
                    MessageBox.Show(e.Message);
                    MessageBox.Show(e.ToString());
                    return;
                }
            }
            newEnvelopeFb = null;
            activew.Refresh();       
        }
        #endregion

        //获取envelope内线要素的StrokeValue
        //这里的envelope是矩形
        private string getStrokeValue(IEnvelope envelope)
        {
            //空间查询
            ISpatialFilter sFilter = new SpatialFilterClass();
            sFilter.Geometry = envelope;
            sFilter.GeometryField = "SHAPE";//Shape也可以
            sFilter.SpatialRel = esriSpatialRelEnum.esriSpatialRelIntersects;//查询的空间关系 为Intersect（x相交）

            IFeatureCursor featureCursor = fClass.Search(sFilter, false);
            IFeature preFeature = featureCursor.NextFeature();
            if (preFeature == null)
            {
                MessageBox.Show("未选中要素！");
                return null;
            }
            string ID1 = getSingleStrokeValue(preFeature);//第一个线要素的StrokeValue
            IFeature latterFeature;
            while ((latterFeature = featureCursor.NextFeature()) != null)
            {
                //如果第一个线要素的StrokeValue与其它线要素的StrokeValue不同，则MessageBox.Show（）;
                if (ID1 != getSingleStrokeValue(latterFeature))
                {
                    MessageBox.Show("选中了多个Stroke的线要素，请重新绘制矩形！");
                    newEnvelopeFb = null;
                    activew.Refresh();
                    return null;
                }
            }
            featureCursor.Flush();
            Marshal.FinalReleaseComObject(featureCursor);
            return getSingleStrokeValue(preFeature);
        }

        //获取单个线要素的Stroke值
        private string getSingleStrokeValue(IFeature feature)
        {
            string strokeValue;//单个线要素的Stroke值
            int index_Stroke = feature.Fields.FindField("Stroke");
            try
            {
                strokeValue = Convert.ToString(feature.get_Value(index_Stroke));//根据Stroke字段的索引获取字段值
                return strokeValue;
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message);
                MessageBox.Show(e.ToString());
                return null;
            }
        }

        //根据车辆ID高亮其轨迹点
        private void Highlight(IFeatureClass fClass, string strokeValue, string fieldName = "Stroke")
        {
            //框选多个线要素时，将传入空的StrokeValue，直接结束执行该函数。
            if (strokeValue == null)
            {
                //MessageBox.Show("车辆ID为空！");
                return;
            }
            //判断待查询字段是否存在
            int index_Stroke = fClass.Fields.FindField(fieldName);
            if (index_Stroke == -1)
            {
                MessageBox.Show("未查询到" + fieldName + "字段！", "警告", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            //属性查询
            IQueryFilter qFilter = new QueryFilterClass();//空间和属性查询对象都是由 类 实例化
            qFilter.WhereClause = fieldName + "=" + strokeValue;//"\"" + fieldName + "\"" + "=" + strokeValue;//fieldName + "=" + "\'" + strokeValue + "\'";
            IFeatureSelection fSelection = layer as IFeatureSelection;
            //该查询方法能使查询结果 持续高亮（缩放漫游地图仍高亮）
            fSelection.SelectFeatures(qFilter, esriSelectionResultEnum.esriSelectionResultNew, false);
            activew.PartialRefresh(esriViewDrawPhase.esriViewGeoSelection, null, null);
        }
    }
}
