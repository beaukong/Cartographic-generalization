using ESRI.ArcGIS.ADF.BaseClasses;
using ESRI.ArcGIS.ADF.CATIDs;
using ESRI.ArcGIS.ArcMapUI;
using ESRI.ArcGIS.Carto;
using ESRI.ArcGIS.Controls;
using ESRI.ArcGIS.Display;
using ESRI.ArcGIS.Editor;
using ESRI.ArcGIS.esriSystem;
using ESRI.ArcGIS.Framework;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using System.Collections;
using System.Linq;
using System.Text;
using ESRI.ArcGIS.SystemUI;

namespace Merge
{
    /// <summary>
    /// Summary description for MergeOnAttributeTool.
    /// </summary>
    [Guid("764ffa27-4a6a-425a-9305-12c981edae18")]
    [ClassInterface(ClassInterfaceType.None)]
    [ProgId("Merge.MergeOnAttributeTool")]
    public sealed class MergeOnAttributeTool : BaseTool
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
        private IHookHelper m_hookHelper = null;
        public IMap map = null;
        public IActiveView activeView;
        public ILayer selectLayer = null;
        private INewEnvelopeFeedback newEnvelopeFb = null;
        public MergeOnAttributeTool()
        {
            //
            // TODO: Define values for the public properties
            //
            base.m_category = "根据特征合并"; //localizable text 
            base.m_caption = "根据特征合并";  //localizable text 
            base.m_message = "框选,合并相邻且某属性相同的多边形";  //localizable text
            base.m_toolTip = "根据特征合并";  //localizable text
            base.m_name = "根据特征合并";   //unique id, non-localizable (e.g. "MyCategory_ArcMapTool")
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
            m_application = hook as IApplication;
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
            //Disable if it is not ArcMap
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
            // TODO: Add MergeOnAttributeTool.OnClick implementation
            map = (m_application.Document as IMxDocument).ActiveView.FocusMap;
            activeView = map as IActiveView;
            selectLayer = SelectLayer.selectLayer;
            if (selectLayer == null)
            {
                MessageBox.Show("地图文档中无数据！");
                return;
            }
        }

        public override void OnMouseDown(int Button, int Shift, int X, int Y)
        {
            // TODO:  Add BuildingBaseTool1.OnMouseDown implementation
            if (activeView == null)
            {
                activeView = m_hookHelper.FocusMap as IActiveView;//？？干啥用
            }
            else
            {
                newEnvelopeFb = null;//框选的矩形
                newEnvelopeFb = new NewEnvelopeFeedbackClass();
                IPoint point = activeView.ScreenDisplay.DisplayTransformation.ToMapPoint(X, Y);
                newEnvelopeFb.Display = activeView.ScreenDisplay;
                newEnvelopeFb.Start(point);//绘制矩形的起点
            }
        }

        public override void OnMouseMove(int Button, int Shift, int X, int Y)
        {
            // TODO:  Add BuildingBaseTool1.OnMouseMove implementation
            if (newEnvelopeFb != null)
            {
                if (activeView == null)
                {
                    activeView = m_hookHelper.FocusMap as IActiveView;
                }
                IPoint point = activeView.ScreenDisplay.DisplayTransformation.ToMapPoint(X, Y);
                newEnvelopeFb.Display = activeView.ScreenDisplay;
                newEnvelopeFb.MoveTo(point);//绘制矩形移动的路径
            }
        }

        public override void OnMouseUp(int Button, int Shift, int X, int Y)
        {
            // 新建一个编辑器
            IEditor editor = m_application.FindExtensionByName("ESRI Object Editor") as IEditor;
            esriEditState editorState = editor.EditState;
            // 如果不处于编辑状态，则给出提示
            if (editorState != esriEditState.esriStateEditing)
            {
                MessageBox.Show("未打开编辑器！");
                newEnvelopeFb = null;
                return;
                //activeView.Refresh();// 刷新一次屏幕
            }
            else
            {
                editor.EnableUndoRedo(true);
                editor.StartOperation();
                if (newEnvelopeFb != null)
                {
                    IEnvelope envelope = newEnvelopeFb.Stop();//获得绘制的矩形
                    if (envelope.IsEmpty != true)// != null)
                    {
                        try
                        {
                            //空间查询建筑物图层被选中的要素，并执行合并
                            //MergePolygons((selectLayer as IFeatureLayer).FeatureClass,envelope);
                            //MergePolygons((selectLayer as IFeatureLayer).FeatureClass, envelope, "floor");
                            MergePolygonsFroMaxHeight((selectLayer as IFeatureLayer).FeatureClass, envelope, "Elevation");
                        }
                        catch (Exception e)
                        {

                            MessageBox.Show(e.Message);
                            MessageBox.Show(e.ToString());
                            newEnvelopeFb = null;
                            //activeView.Refresh();
                            editor.StopOperation("多边形合并");
                            return;
                        }
                    }
                }
                newEnvelopeFb = null;
                activeView.Refresh();
                editor.StopOperation("多边形合并");

            }
        }
        #endregion


        //
        public void QueryAdjacentBuildings(IFeature feature, IFeatureClass fCls, int index_coordFea, int maxFeaOID, ref int coordFeaValue,
            ref ArrayList pArrayPolygonTouch, ref List<int> toDelIdLst, ref List<int> idLst)
        {
            if (idLst.Contains(feature.OID) || feature.OID > maxFeaOID)
                return;
            else
                idLst.Add(feature.OID);
            int nTouch = 0;
            //设置空间查询规则：与当前建筑物邻接
            ISpatialFilter sFilterTouch = new SpatialFilterClass();
            sFilterTouch.Geometry = feature.Shape;
            sFilterTouch.GeometryField = "SHAPE";//Shape也可以
            sFilterTouch.SpatialRel = esriSpatialRelEnum.esriSpatialRelTouches;// | esriSpatialRelEnum.esriSpatialRelIntersects | esriSpatialRelEnum.esriSpatialRelOverlaps;
            //开始空间查询 邻接的建筑物
            IFeatureCursor feaCursorTouch = fCls.Search(sFilterTouch, false);
            IFeature ifeatureTouch = null;
            //ArrayList pArrayPolygonTouch = new ArrayList();//存储当前建筑物、与当前建筑物touch的建筑物
            //pArrayPolygonTouch.Add(feature.Shape as IPolygon);
            coordFeaValue = Convert.ToInt32(feature.get_Value(index_coordFea));
            //遍历空间查询到邻接的建筑物
            while ((ifeatureTouch = feaCursorTouch.NextFeature()) != null)
            {
                int ifeatureTouchOID = ifeatureTouch.OID;
                if (idLst.Contains(ifeatureTouchOID) || ifeatureTouch.OID > maxFeaOID)
                {
                    continue;
                }
                IPolygon polygonTouch = ifeatureTouch.Shape as IPolygon;
                //PolygonClass plnClsTouch = ifeatureTouch.Shape as PolygonClass;
                //pArrayPolygonTouch.Add(plnClsTouch);
                //int coorFeaValueTouch = Convert.ToInt32(ifeatureTouch.get_Value(index_coordFea));
                ////只把与当前建筑物feature的合并参考特征一致的邻接建筑物添加到
                //if (coordFeaValue == coorFeaValueTouch)
                //{
                if (idLst.Contains(ifeatureTouchOID) || feature.OID > maxFeaOID)
                    continue;
                else
                {
                    idLst.Add(ifeatureTouchOID);
                    pArrayPolygonTouch.Add(polygonTouch);
                    if (!toDelIdLst.Contains(ifeatureTouchOID))
                        toDelIdLst.Add(ifeatureTouchOID);
                    if (coordFeaValue < Convert.ToInt32(ifeatureTouch.get_Value(index_coordFea)))
                        coordFeaValue = Convert.ToInt32(ifeatureTouch.get_Value(index_coordFea));
                    //QueryAdjacentBuildings(ifeatureTouch, fCls, index_coordFea, maxFeaOID,ref  coordFeaValue,ref pArrayPolygonTouch, ref toDelIdLst, ref idLst);
                    nTouch++;
                }
                    //}
                    //pArrayPolygonTouch.Add(feature.Shape as IPolygon);
                //}
                //else
                //    continue;

            }

            if (nTouch > 0)
            {
                if (!toDelIdLst.Contains(feature.OID))
                {

                    pArrayPolygonTouch.Add(feature.Shape as IPolygon);
                    toDelIdLst.Add(feature.OID);
                }
            }



        }


        ////
        //遍历框选的建筑物，判断建筑物是否有与其它建筑物相邻接，若是则合并.j将去合并前最高建筑物的高度给合并后建筑物
        public void MergePolygonsFroMaxHeight(IFeatureClass fCls, IEnvelope envelope, string coordinateFeature)
        {
            for (int j = 0; j < 10; j++)
            {
                int maxFeaOID = 0;// fCls.FeatureCount(null); 
                //1.空间查询
                //ISpatialFilter sFiltermaxFeaOID = new SpatialFilterClass();
                //sFiltermaxFeaOID.Geometry = envelope;
                //sFiltermaxFeaOID.GeometryField = "SHAPE";//Shape也可以
                //sFiltermaxFeaOID.SpatialRel = esriSpatialRelEnum.esriSpatialRelIntersects;//查询的空间关系 为Contains（包含）

                IFeatureCursor featureCursormaxFeaOID = fCls.Search(null, false);//执行空间查询
                IFeature featuremaxFeaOID = null;
                while ((featuremaxFeaOID = featureCursormaxFeaOID.NextFeature()) != null)//遍历查询到的要素
                {
                    if (featuremaxFeaOID.OID > maxFeaOID)
                        maxFeaOID = featuremaxFeaOID.OID;
                }

                int delNum = 0;
                //遍历框选的每个建筑物
                IGeometry polygonRes = null;//合并后的多边形
                //1.空间查询
                ISpatialFilter sFilter = new SpatialFilterClass();
                sFilter.Geometry = envelope;
                sFilter.GeometryField = "SHAPE";//Shape也可以
                sFilter.SpatialRel = esriSpatialRelEnum.esriSpatialRelIntersects;//查询的空间关系 为Contains（包含）

                IFeatureCursor featureCursor = fCls.Search(sFilter, false);//执行空间查询
                //List<IPolygon> polygonLst = new List<IPolygon>();//选中的 多边形列表 
                //ArrayList pArrayPolygon = new ArrayList();
                List<int> idLst = new List<int>(); ;
                List<int> toDelIdLst =  new List<int>();
                //IPointArray pointArray = new PointArrayClass();
                int index_coordFea = fCls.FindField(coordinateFeature);
                IFeature feature = null;
                while ((feature = featureCursor.NextFeature()) != null)//遍历查询到的要素
                {
                    ArrayList pArrayPolygonTouch = new ArrayList();//存储当前建筑物、与当前建筑物touch的建筑物
                    int coordFea = 0;
                    QueryAdjacentBuildings(feature, fCls, index_coordFea, maxFeaOID,ref coordFea, ref pArrayPolygonTouch, ref toDelIdLst, ref idLst);


                    ////设置空间查询规则：与当前建筑物邻接
                    //ISpatialFilter sFilterTouch = new SpatialFilterClass();
                    //sFilterTouch.Geometry = feature.Shape;
                    //sFilterTouch.GeometryField = "SHAPE";//Shape也可以
                    //sFilterTouch.SpatialRel = esriSpatialRelEnum.esriSpatialRelTouches;
                    ////开始空间查询 邻接的建筑物
                    //IFeatureCursor feaCursorTouch = fCls.Search(sFilterTouch,false);
                    //IFeature ifeatureTouch=null;
                    //ArrayList pArrayPolygonTouch = new ArrayList();//存储当前建筑物、与当前建筑物touch的建筑物
                    //pArrayPolygonTouch.Add(feature.Shape as IPolygon);
                    //int coordFeaValue = Convert.ToInt32(feature.get_Value(index_coordFea));
                    ////遍历空间查询到邻接的建筑物
                    //while ((ifeatureTouch =feaCursorTouch.NextFeature())!=null)
                    //{
                    //    IPolygon polygonTouch = ifeatureTouch.Shape as IPolygon;
                    //    //PolygonClass plnClsTouch = ifeatureTouch.Shape as PolygonClass;
                    //    //pArrayPolygonTouch.Add(plnClsTouch);
                    //    int coorFeaValueTouch = Convert.ToInt32(ifeatureTouch.get_Value(index_coordFea));
                    //    //只把与当前建筑物feature的合并参考特征一致的邻接建筑物添加到
                    //    if (coordFeaValue==coorFeaValueTouch)
                    //    {
                    //        pArrayPolygonTouch.Add(polygonTouch);                        
                    //    }
                    //    else
                    //        continue;
                    //}
                    //如果空间查询到邻接且合并参考属性相同的的建筑物，进行合并
                    if (pArrayPolygonTouch.Count > 0)
                    {
                        delNum++;
                        //2 合并
                        polygonRes = UnionOrRemoveRingFromPolygon(pArrayPolygonTouch);//执行合并
                        //feaCursorTouch.Flush();
                        //3 更新feature
                        //设置空间查询规则：与当前建筑物邻接
                        ISpatialFilter sFilterTouch = new SpatialFilterClass();
                        sFilterTouch.Geometry = feature.Shape;
                        sFilterTouch.GeometryField = "SHAPE";//Shape也可以
                        sFilterTouch.SpatialRel = esriSpatialRelEnum.esriSpatialRelTouches;// | esriSpatialRelEnum.esriSpatialRelIntersects | esriSpatialRelEnum.esriSpatialRelOverlaps;
                        IFeatureCursor feaCursorTouch = fCls.Search(sFilterTouch, false);//执行空间查询
                        IFeature ifeatureTouch = feaCursorTouch.NextFeature();//获取空间查询的第一个多边形要素
                        IFeatureCursor feaCursorInsert = fCls.Insert(true);
                        IFeatureBuffer newFeaBuffer = fCls.CreateFeatureBuffer();//创建新要素
                        for (int i = 0; i < feature.Fields.FieldCount; i++)
                        {
                            if (feature.Fields.get_Field(i).Editable == false)
                                continue;
                            if (i == index_coordFea)
                                newFeaBuffer.set_Value(i, coordFea);
                            else
                                newFeaBuffer.set_Value(i, feature.Value[i]);//.Fields.get_Field(i));//为新要素字段赋值
                        }
                        newFeaBuffer.Shape = polygonRes;//将合并后的多边形要素赋值给 新要素
                        feaCursorInsert.InsertFeature(newFeaBuffer);
                        //ifeatureTouch.Delete();
                        //IFeature delFeature = null;
                        //while ((delFeature = feaCursorTouch.NextFeature()) != null)
                        //{
                        //    delFeature.Delete();
                        //    feaCursorTouch.UpdateFeature(delFeature);
                        //    featureCursor.UpdateFeature(delFeature);
                        //}
                        feaCursorTouch.Flush();
                        Marshal.FinalReleaseComObject(feaCursorTouch);
                    }
                    else
                    {
                        continue;
                    }

                }
                featureCursor.Flush();
                Marshal.FinalReleaseComObject(featureCursor);
                featureCursor = fCls.Search(sFilter, false);//执行空间查询
                while ((feature = featureCursor.NextFeature()) != null)
                {
                    if (toDelIdLst.Contains(feature.OID))
                        feature.Delete();
                }
                featureCursor.Flush();
                Marshal.FinalReleaseComObject(featureCursor);
                if (delNum <= 0)
                    break;
            }
            //if (pArrayPolygon.Count < 1)
            //{
            //    MessageBox.Show("框选的多边形数量过少！");
            //    return;
            //}
            ////判断每个建筑物是否有邻接的建筑物

            //////判断有邻接建筑物的合并参考属性是否相同，相同则合并、新增合并后要素、删除合并前要素
        }


        //
        public void QueryAdjacentBuildingsWithSameAttri(IFeature feature, IFeatureClass fCls, int index_coordFea, int maxFeaOID,
            ref ArrayList pArrayPolygonTouch, ref List<int> toDelIdLst, ref List<int> idLst)
        {
            if (idLst.Contains(feature.OID) || feature.OID >= maxFeaOID)
                return;
            else
                idLst.Add(feature.OID);
            int nTouch = 0;
            //设置空间查询规则：与当前建筑物邻接
            ISpatialFilter sFilterTouch = new SpatialFilterClass();
            sFilterTouch.Geometry = feature.Shape;
            sFilterTouch.GeometryField = "SHAPE";//Shape也可以
            sFilterTouch.SpatialRel = esriSpatialRelEnum.esriSpatialRelTouches;// | esriSpatialRelEnum.esriSpatialRelIntersects | esriSpatialRelEnum.esriSpatialRelOverlaps;
            //开始空间查询 邻接的建筑物
            IFeatureCursor feaCursorTouch = fCls.Search(sFilterTouch, false);
            IFeature ifeatureTouch = null;
            //ArrayList pArrayPolygonTouch = new ArrayList();//存储当前建筑物、与当前建筑物touch的建筑物
            //pArrayPolygonTouch.Add(feature.Shape as IPolygon);
            int coordFeaValue = Convert.ToInt32(feature.get_Value(index_coordFea));
            //遍历空间查询到邻接的建筑物
            while ((ifeatureTouch = feaCursorTouch.NextFeature()) != null)
            {
                int ifeatureTouchOID = ifeatureTouch.OID;
                if (idLst.Contains(ifeatureTouchOID) || ifeatureTouch.OID > maxFeaOID)
                {
                    continue;
                }
                IPolygon polygonTouch = ifeatureTouch.Shape as IPolygon;
                //PolygonClass plnClsTouch = ifeatureTouch.Shape as PolygonClass;
                //pArrayPolygonTouch.Add(plnClsTouch);
                int coorFeaValueTouch = Convert.ToInt32(ifeatureTouch.get_Value(index_coordFea));
                //只把与当前建筑物feature的合并参考特征一致的邻接建筑物添加到
                if (coordFeaValue == coorFeaValueTouch)
                {
                    if (idLst.Contains(ifeatureTouchOID) || feature.OID > maxFeaOID)
                        continue;
                    else
                    {
                        idLst.Add(ifeatureTouchOID);
                        pArrayPolygonTouch.Add(polygonTouch);
                        if (!toDelIdLst.Contains(ifeatureTouchOID))
                            toDelIdLst.Add(ifeatureTouchOID);
                        //QueryAdjacentBuildingsWithSameAttri(ifeatureTouch, fCls, index_coordFea, ref pArrayPolygonTouch, ref toDelIdLst, ref idLst);
                        nTouch++;
                    }
                    //}
                    //pArrayPolygonTouch.Add(feature.Shape as IPolygon);
                }
                else
                    continue;

            }

            if (nTouch > 0)
            {
                if (!toDelIdLst.Contains(feature.OID))
                {

                    pArrayPolygonTouch.Add(feature.Shape as IPolygon);
                    toDelIdLst.Add(feature.OID);
                }
            }



        }


        ////
        //遍历框选的建筑物，判断建筑物是否有与其它建筑物相邻接并且高度属性相同，若是则合并.会出现合并后建筑物消失的情况。
        public void MergePolygons(IFeatureClass fCls, IEnvelope envelope, string coordinateFeature)
        {
            for (int j = 0; j < 10; j++)
            {
                int maxFeaOID = 0;// fCls.FeatureCount(null); 
                //1.空间查询
                //ISpatialFilter sFiltermaxFeaOID = new SpatialFilterClass();
                //sFiltermaxFeaOID.Geometry = envelope;
                //sFiltermaxFeaOID.GeometryField = "SHAPE";//Shape也可以
                //sFiltermaxFeaOID.SpatialRel = esriSpatialRelEnum.esriSpatialRelIntersects;//查询的空间关系 为Contains（包含）

                IFeatureCursor featureCursormaxFeaOID = fCls.Search(null, false);//执行空间查询
                IFeature featuremaxFeaOID = null;
                while ((featuremaxFeaOID = featureCursormaxFeaOID.NextFeature()) != null)//遍历查询到的要素
                {
                    if (featuremaxFeaOID.OID > maxFeaOID)
                        maxFeaOID = featuremaxFeaOID.OID;
                }

                int delNum = 0;
                //遍历框选的每个建筑物
                IGeometry polygonRes = null;//合并后的多边形
                //1.空间查询
                ISpatialFilter sFilter = new SpatialFilterClass();
                sFilter.Geometry = envelope;
                sFilter.GeometryField = "SHAPE";//Shape也可以
                sFilter.SpatialRel = esriSpatialRelEnum.esriSpatialRelIntersects;//查询的空间关系 为Contains（包含）

                IFeatureCursor featureCursor = fCls.Search(sFilter, false);//执行空间查询
                //List<IPolygon> polygonLst = new List<IPolygon>();//选中的 多边形列表 
                //ArrayList pArrayPolygon = new ArrayList();
                List<int> idLst = new List<int>(); ;
                List<int> toDelIdLst = new List<int>();
                //IPointArray pointArray = new PointArrayClass();
                int index_coordFea = fCls.FindField(coordinateFeature);
                IFeature feature = null;
                while ((feature = featureCursor.NextFeature()) != null)//遍历查询到的要素
                {
                    ArrayList pArrayPolygonTouch = new ArrayList();//存储当前建筑物、与当前建筑物touch的建筑物
                    QueryAdjacentBuildingsWithSameAttri(feature, fCls, index_coordFea, maxFeaOID, ref pArrayPolygonTouch, ref toDelIdLst, ref idLst);


                    ////设置空间查询规则：与当前建筑物邻接
                    //ISpatialFilter sFilterTouch = new SpatialFilterClass();
                    //sFilterTouch.Geometry = feature.Shape;
                    //sFilterTouch.GeometryField = "SHAPE";//Shape也可以
                    //sFilterTouch.SpatialRel = esriSpatialRelEnum.esriSpatialRelTouches;
                    ////开始空间查询 邻接的建筑物
                    //IFeatureCursor feaCursorTouch = fCls.Search(sFilterTouch,false);
                    //IFeature ifeatureTouch=null;
                    //ArrayList pArrayPolygonTouch = new ArrayList();//存储当前建筑物、与当前建筑物touch的建筑物
                    //pArrayPolygonTouch.Add(feature.Shape as IPolygon);
                    //int coordFeaValue = Convert.ToInt32(feature.get_Value(index_coordFea));
                    ////遍历空间查询到邻接的建筑物
                    //while ((ifeatureTouch =feaCursorTouch.NextFeature())!=null)
                    //{
                    //    IPolygon polygonTouch = ifeatureTouch.Shape as IPolygon;
                    //    //PolygonClass plnClsTouch = ifeatureTouch.Shape as PolygonClass;
                    //    //pArrayPolygonTouch.Add(plnClsTouch);
                    //    int coorFeaValueTouch = Convert.ToInt32(ifeatureTouch.get_Value(index_coordFea));
                    //    //只把与当前建筑物feature的合并参考特征一致的邻接建筑物添加到
                    //    if (coordFeaValue==coorFeaValueTouch)
                    //    {
                    //        pArrayPolygonTouch.Add(polygonTouch);                        
                    //    }
                    //    else
                    //        continue;
                    //}
                    //如果空间查询到邻接且合并参考属性相同的的建筑物，进行合并
                    if (pArrayPolygonTouch.Count > 0)
                    {
                        delNum++;
                        //2 合并
                        polygonRes = UnionOrRemoveRingFromPolygon(pArrayPolygonTouch);//执行合并
                        //feaCursorTouch.Flush();
                        //3 更新feature
                        //设置空间查询规则：与当前建筑物邻接
                        ISpatialFilter sFilterTouch = new SpatialFilterClass();
                        sFilterTouch.Geometry = feature.Shape;
                        sFilterTouch.GeometryField = "SHAPE";//Shape也可以
                        sFilterTouch.SpatialRel = esriSpatialRelEnum.esriSpatialRelTouches;// | esriSpatialRelEnum.esriSpatialRelIntersects | esriSpatialRelEnum.esriSpatialRelOverlaps;
                        IFeatureCursor feaCursorTouch = fCls.Search(sFilterTouch, false);//执行空间查询
                        IFeature ifeatureTouch = feaCursorTouch.NextFeature();//获取空间查询的第一个多边形要素
                        IFeatureCursor feaCursorInsert = fCls.Insert(true);
                        IFeatureBuffer newFeaBuffer = fCls.CreateFeatureBuffer();//创建新要素
                        for (int i = 0; i < feature.Fields.FieldCount; i++)
                        {
                            if (feature.Fields.get_Field(i).Editable == false)
                                continue;
                            newFeaBuffer.set_Value(i, feature.Value[i]);//.Fields.get_Field(i));//为新要素字段赋值
                        }
                        newFeaBuffer.Shape = polygonRes;//将合并后的多边形要素赋值给 新要素
                        feaCursorInsert.InsertFeature(newFeaBuffer);
                        //ifeatureTouch.Delete();
                        //IFeature delFeature = null;
                        //while ((delFeature = feaCursorTouch.NextFeature()) != null)
                        //{
                        //    delFeature.Delete();
                        //    feaCursorTouch.UpdateFeature(delFeature);
                        //    featureCursor.UpdateFeature(delFeature);
                        //}
                        feaCursorTouch.Flush();
                        Marshal.FinalReleaseComObject(feaCursorTouch);
                    }
                    else
                    {
                        continue;
                    }

                }
                featureCursor.Flush();
                Marshal.FinalReleaseComObject(featureCursor);
                featureCursor = fCls.Search(sFilter, false);//执行空间查询
                while ((feature = featureCursor.NextFeature()) != null)
                {
                    if (toDelIdLst.Contains(feature.OID))
                        feature.Delete();
                }
                featureCursor.Flush();
                Marshal.FinalReleaseComObject(featureCursor);
                if (delNum <= 0)
                    break;
            }
            //if (pArrayPolygon.Count < 1)
            //{
            //    MessageBox.Show("框选的多边形数量过少！");
            //    return;
            //}
            ////判断每个建筑物是否有邻接的建筑物

            //////判断有邻接建筑物的合并参考属性是否相同，相同则合并、新增合并后要素、删除合并前要素
        }

        //合并方法
        //参数是：1要素类；2框选矩形
        //方法体：1 空间查询选中的多边形，2（判断是否为普通多边形）合并， 3 更新要素 
        public void MergePolygons(IFeatureClass fCls, IEnvelope envelope)
        {
            IGeometry polygonRes = null;//合并后的多边形
            //1.空间查询
            ISpatialFilter sFilter = new SpatialFilterClass();
            sFilter.Geometry = envelope;
            sFilter.GeometryField = "SHAPE";//Shape也可以
            sFilter.SpatialRel = esriSpatialRelEnum.esriSpatialRelIntersects;//查询的空间关系 为Contains（包含）
            fCls.Search(null, false);
            IFeatureCursor featureCursor = fCls.Search(sFilter, false);//执行空间查询
            List<IPolygon> polygonLst = new List<IPolygon>();//选中的 多边形列表 
            ArrayList pArrayPolygon = new ArrayList();
            IPointArray pointArray = new PointArrayClass();
            IFeature feature = null;
            while ((feature = featureCursor.NextFeature()) != null)//遍历查询到的要素
            {
                IPolygon polygon = feature.Shape as IPolygon;
                PolygonClass plnCls = feature.Shape as PolygonClass;
                pArrayPolygon.Add(feature.Shape);

            }
            if (pArrayPolygon.Count <= 1)
            {
                MessageBox.Show("框选的多边形数量过少！");
                return;
            }
            //2 合并
            polygonRes = UnionOrRemoveRingFromPolygon(pArrayPolygon);//执行合并

            featureCursor.Flush();
            //3 更新feature
            featureCursor = fCls.Search(sFilter, false);//执行空间查询
            feature = featureCursor.NextFeature();//获取空间查询的第一个多边形要素
            IFeatureCursor feaCursorInsert = fCls.Insert(true);
            IFeatureBuffer newFeaBuffer = fCls.CreateFeatureBuffer();//创建新要素
            for (int i = 0; i < feature.Fields.FieldCount; i++)
            {
                if (feature.Fields.get_Field(i).Editable == false)
                    continue;
                newFeaBuffer.set_Value(i, feature.Value[i]);//.Fields.get_Field(i));//为新要素字段赋值
            }
            newFeaBuffer.Shape = polygonRes;//将合并后的多边形要素赋值给 新要素
            feaCursorInsert.InsertFeature(newFeaBuffer);
            feature.Delete();
            IFeature delFeature = null;
            while ((delFeature = featureCursor.NextFeature()) != null)
            {
                delFeature.Delete();
            }
            featureCursor.Flush();
            Marshal.FinalReleaseComObject(featureCursor);
        }
        //合并多边形  来自网络
        public IGeometry UnionOrRemoveRingFromPolygon(ArrayList pArrayPolygon)
        {
            IGeometry newGeom0 = null;
            IPolygon pPolygon_Each = null;
            IPolygon pPolygon_Result = null;
            IGeometryCollection pGeometryCollection = null;
            ISegmentCollection pSegmentCollection_Ring = null;
            object oMissing = Type.Missing;
            try
            {
                if (pArrayPolygon == null)
                    return null;
                if (pArrayPolygon.Count <= 1)
                    return null;
                pGeometryCollection = new PolygonClass();
                pPolygon_Result = pGeometryCollection as IPolygon;
                for (int i = 0; i < pArrayPolygon.Count; i++)
                {
                    pPolygon_Each = pArrayPolygon[i] as IPolygon;
                    IGeometryCollection pGeometryCollection_Temp = null;
                    pGeometryCollection_Temp = pPolygon_Each as IGeometryCollection;
                    for (int j = 0; j < pGeometryCollection_Temp.GeometryCount; j++)
                    {
                        pSegmentCollection_Ring = new RingClass();
                        pSegmentCollection_Ring.AddSegmentCollection(pGeometryCollection_Temp.get_Geometry(j) as ISegmentCollection);
                        pGeometryCollection.AddGeometry(pSegmentCollection_Ring as IGeometry, ref oMissing, ref oMissing);
                    }
                }
                pPolygon_Result.SimplifyPreserveFromTo();//执行toolbox中“消除”后，再用该工具条就出错
                //IRgbColor rgb_r = new RgbColorClass();
                //rgb_r.Red = 200;
                //IRgbColor rgb_r2 = new RgbColorClass();
                //MyArcEngineMethod.ArcMapDrawing.Draw_Polygon(activeView, pPolygon_Result as IPolygon, rgb_r2, rgb_r, 2);
                ///去多边形的空洞
                IGeometryCollection pGeocoll = pPolygon_Result as IGeometryCollection;
                int geomcount = pGeocoll.GeometryCount;
                if (geomcount > 1)
                {
                    IPolygon4 pPolygon4 = (IPolygon4)pPolygon_Result;
                    IGeometryBag bag = pPolygon4.ExteriorRingBag;     //获取多边形的所有外环
                    IEnumGeometry geo = bag as IEnumGeometry;
                    geo.Reset();
                    IRing exRing = geo.Next() as IRing;
                    newGeom0 = CreatePolygonfromRing(exRing);//将IRing转换为Ipolygon
                }
                else
                    newGeom0 = pPolygon_Result;
                //IRgbColor rgb = new RgbColorClass();
                //rgb.Blue = 200;
                //MyArcEngineMethod.ArcMapDrawing.Draw_Polygon(activeView, newGeom0 as IPolygon, rgb, rgb,2);
                return newGeom0;
            }
            catch (Exception ex)
            {
                //if (isShowError)
                MessageBox.Show("合并（去岛）多边形发生错误，错误信息为：->\r\n" + ex.Message,
                                    "错误信息", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return null;
            }

        }
        /// <summary>
        /// 将IRing转换为 IGeometry
        /// </summary>
        /// <param name="ExRing"></param>
        /// <returns></returns>
        public static IGeometry CreatePolygonfromRing(IRing ExRing)
        {
            ISegmentCollection SegCol = ExRing as ISegmentCollection;
            IPolygon PPolygon = new PolygonClass();
            ISegmentCollection newSegCol = PPolygon as ISegmentCollection;
            newSegCol.AddSegmentCollection(SegCol);
            return PPolygon as IGeometry;
        }


        public void SampleLabel(IFeatureClass fCls, string fldName, IEnvelope envelope, string value)
        {
            //空间查询
            ISpatialFilter sFilter = new SpatialFilterClass();
            sFilter.Geometry = envelope;
            sFilter.GeometryField = "SHAPE";//Shape也可以
            sFilter.SpatialRel = esriSpatialRelEnum.esriSpatialRelIntersects;//查询的空间关系 为Contains（包含）

            IFeatureCursor featureCursor = fCls.Search(sFilter, false);//执行空间查询
            IFeature feature = null;
            while ((feature = featureCursor.NextFeature()) != null)//遍历查询到的要素
            {
                IFields fields = feature.Fields;
                int attributeIndex = fields.FindField(fldName);// ("LabelCls");
                //给字段赋值
                feature.set_Value(attributeIndex, value);
                feature.Store();
            }
            featureCursor.Flush();
            Marshal.FinalReleaseComObject(featureCursor);
        }

    }
}
