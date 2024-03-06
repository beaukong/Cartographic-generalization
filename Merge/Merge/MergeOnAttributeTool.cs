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
            base.m_category = "���������ϲ�"; //localizable text 
            base.m_caption = "���������ϲ�";  //localizable text 
            base.m_message = "��ѡ,�ϲ�������ĳ������ͬ�Ķ����";  //localizable text
            base.m_toolTip = "���������ϲ�";  //localizable text
            base.m_name = "���������ϲ�";   //unique id, non-localizable (e.g. "MyCategory_ArcMapTool")
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
                MessageBox.Show("��ͼ�ĵ��������ݣ�");
                return;
            }
        }

        public override void OnMouseDown(int Button, int Shift, int X, int Y)
        {
            // TODO:  Add BuildingBaseTool1.OnMouseDown implementation
            if (activeView == null)
            {
                activeView = m_hookHelper.FocusMap as IActiveView;//������ɶ��
            }
            else
            {
                newEnvelopeFb = null;//��ѡ�ľ���
                newEnvelopeFb = new NewEnvelopeFeedbackClass();
                IPoint point = activeView.ScreenDisplay.DisplayTransformation.ToMapPoint(X, Y);
                newEnvelopeFb.Display = activeView.ScreenDisplay;
                newEnvelopeFb.Start(point);//���ƾ��ε����
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
                newEnvelopeFb.MoveTo(point);//���ƾ����ƶ���·��
            }
        }

        public override void OnMouseUp(int Button, int Shift, int X, int Y)
        {
            // �½�һ���༭��
            IEditor editor = m_application.FindExtensionByName("ESRI Object Editor") as IEditor;
            esriEditState editorState = editor.EditState;
            // ��������ڱ༭״̬���������ʾ
            if (editorState != esriEditState.esriStateEditing)
            {
                MessageBox.Show("δ�򿪱༭����");
                newEnvelopeFb = null;
                return;
                //activeView.Refresh();// ˢ��һ����Ļ
            }
            else
            {
                editor.EnableUndoRedo(true);
                editor.StartOperation();
                if (newEnvelopeFb != null)
                {
                    IEnvelope envelope = newEnvelopeFb.Stop();//��û��Ƶľ���
                    if (envelope.IsEmpty != true)// != null)
                    {
                        try
                        {
                            //�ռ��ѯ������ͼ�㱻ѡ�е�Ҫ�أ���ִ�кϲ�
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
                            editor.StopOperation("����κϲ�");
                            return;
                        }
                    }
                }
                newEnvelopeFb = null;
                activeView.Refresh();
                editor.StopOperation("����κϲ�");

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
            //���ÿռ��ѯ�����뵱ǰ�������ڽ�
            ISpatialFilter sFilterTouch = new SpatialFilterClass();
            sFilterTouch.Geometry = feature.Shape;
            sFilterTouch.GeometryField = "SHAPE";//ShapeҲ����
            sFilterTouch.SpatialRel = esriSpatialRelEnum.esriSpatialRelTouches;// | esriSpatialRelEnum.esriSpatialRelIntersects | esriSpatialRelEnum.esriSpatialRelOverlaps;
            //��ʼ�ռ��ѯ �ڽӵĽ�����
            IFeatureCursor feaCursorTouch = fCls.Search(sFilterTouch, false);
            IFeature ifeatureTouch = null;
            //ArrayList pArrayPolygonTouch = new ArrayList();//�洢��ǰ������뵱ǰ������touch�Ľ�����
            //pArrayPolygonTouch.Add(feature.Shape as IPolygon);
            coordFeaValue = Convert.ToInt32(feature.get_Value(index_coordFea));
            //�����ռ��ѯ���ڽӵĽ�����
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
                ////ֻ���뵱ǰ������feature�ĺϲ��ο�����һ�µ��ڽӽ�������ӵ�
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
        //������ѡ�Ľ�����жϽ������Ƿ������������������ڽӣ�������ϲ�.j��ȥ�ϲ�ǰ��߽�����ĸ߶ȸ��ϲ�������
        public void MergePolygonsFroMaxHeight(IFeatureClass fCls, IEnvelope envelope, string coordinateFeature)
        {
            for (int j = 0; j < 10; j++)
            {
                int maxFeaOID = 0;// fCls.FeatureCount(null); 
                //1.�ռ��ѯ
                //ISpatialFilter sFiltermaxFeaOID = new SpatialFilterClass();
                //sFiltermaxFeaOID.Geometry = envelope;
                //sFiltermaxFeaOID.GeometryField = "SHAPE";//ShapeҲ����
                //sFiltermaxFeaOID.SpatialRel = esriSpatialRelEnum.esriSpatialRelIntersects;//��ѯ�Ŀռ��ϵ ΪContains��������

                IFeatureCursor featureCursormaxFeaOID = fCls.Search(null, false);//ִ�пռ��ѯ
                IFeature featuremaxFeaOID = null;
                while ((featuremaxFeaOID = featureCursormaxFeaOID.NextFeature()) != null)//������ѯ����Ҫ��
                {
                    if (featuremaxFeaOID.OID > maxFeaOID)
                        maxFeaOID = featuremaxFeaOID.OID;
                }

                int delNum = 0;
                //������ѡ��ÿ��������
                IGeometry polygonRes = null;//�ϲ���Ķ����
                //1.�ռ��ѯ
                ISpatialFilter sFilter = new SpatialFilterClass();
                sFilter.Geometry = envelope;
                sFilter.GeometryField = "SHAPE";//ShapeҲ����
                sFilter.SpatialRel = esriSpatialRelEnum.esriSpatialRelIntersects;//��ѯ�Ŀռ��ϵ ΪContains��������

                IFeatureCursor featureCursor = fCls.Search(sFilter, false);//ִ�пռ��ѯ
                //List<IPolygon> polygonLst = new List<IPolygon>();//ѡ�е� ������б� 
                //ArrayList pArrayPolygon = new ArrayList();
                List<int> idLst = new List<int>(); ;
                List<int> toDelIdLst =  new List<int>();
                //IPointArray pointArray = new PointArrayClass();
                int index_coordFea = fCls.FindField(coordinateFeature);
                IFeature feature = null;
                while ((feature = featureCursor.NextFeature()) != null)//������ѯ����Ҫ��
                {
                    ArrayList pArrayPolygonTouch = new ArrayList();//�洢��ǰ������뵱ǰ������touch�Ľ�����
                    int coordFea = 0;
                    QueryAdjacentBuildings(feature, fCls, index_coordFea, maxFeaOID,ref coordFea, ref pArrayPolygonTouch, ref toDelIdLst, ref idLst);


                    ////���ÿռ��ѯ�����뵱ǰ�������ڽ�
                    //ISpatialFilter sFilterTouch = new SpatialFilterClass();
                    //sFilterTouch.Geometry = feature.Shape;
                    //sFilterTouch.GeometryField = "SHAPE";//ShapeҲ����
                    //sFilterTouch.SpatialRel = esriSpatialRelEnum.esriSpatialRelTouches;
                    ////��ʼ�ռ��ѯ �ڽӵĽ�����
                    //IFeatureCursor feaCursorTouch = fCls.Search(sFilterTouch,false);
                    //IFeature ifeatureTouch=null;
                    //ArrayList pArrayPolygonTouch = new ArrayList();//�洢��ǰ������뵱ǰ������touch�Ľ�����
                    //pArrayPolygonTouch.Add(feature.Shape as IPolygon);
                    //int coordFeaValue = Convert.ToInt32(feature.get_Value(index_coordFea));
                    ////�����ռ��ѯ���ڽӵĽ�����
                    //while ((ifeatureTouch =feaCursorTouch.NextFeature())!=null)
                    //{
                    //    IPolygon polygonTouch = ifeatureTouch.Shape as IPolygon;
                    //    //PolygonClass plnClsTouch = ifeatureTouch.Shape as PolygonClass;
                    //    //pArrayPolygonTouch.Add(plnClsTouch);
                    //    int coorFeaValueTouch = Convert.ToInt32(ifeatureTouch.get_Value(index_coordFea));
                    //    //ֻ���뵱ǰ������feature�ĺϲ��ο�����һ�µ��ڽӽ�������ӵ�
                    //    if (coordFeaValue==coorFeaValueTouch)
                    //    {
                    //        pArrayPolygonTouch.Add(polygonTouch);                        
                    //    }
                    //    else
                    //        continue;
                    //}
                    //����ռ��ѯ���ڽ��Һϲ��ο�������ͬ�ĵĽ�������кϲ�
                    if (pArrayPolygonTouch.Count > 0)
                    {
                        delNum++;
                        //2 �ϲ�
                        polygonRes = UnionOrRemoveRingFromPolygon(pArrayPolygonTouch);//ִ�кϲ�
                        //feaCursorTouch.Flush();
                        //3 ����feature
                        //���ÿռ��ѯ�����뵱ǰ�������ڽ�
                        ISpatialFilter sFilterTouch = new SpatialFilterClass();
                        sFilterTouch.Geometry = feature.Shape;
                        sFilterTouch.GeometryField = "SHAPE";//ShapeҲ����
                        sFilterTouch.SpatialRel = esriSpatialRelEnum.esriSpatialRelTouches;// | esriSpatialRelEnum.esriSpatialRelIntersects | esriSpatialRelEnum.esriSpatialRelOverlaps;
                        IFeatureCursor feaCursorTouch = fCls.Search(sFilterTouch, false);//ִ�пռ��ѯ
                        IFeature ifeatureTouch = feaCursorTouch.NextFeature();//��ȡ�ռ��ѯ�ĵ�һ�������Ҫ��
                        IFeatureCursor feaCursorInsert = fCls.Insert(true);
                        IFeatureBuffer newFeaBuffer = fCls.CreateFeatureBuffer();//������Ҫ��
                        for (int i = 0; i < feature.Fields.FieldCount; i++)
                        {
                            if (feature.Fields.get_Field(i).Editable == false)
                                continue;
                            if (i == index_coordFea)
                                newFeaBuffer.set_Value(i, coordFea);
                            else
                                newFeaBuffer.set_Value(i, feature.Value[i]);//.Fields.get_Field(i));//Ϊ��Ҫ���ֶθ�ֵ
                        }
                        newFeaBuffer.Shape = polygonRes;//���ϲ���Ķ����Ҫ�ظ�ֵ�� ��Ҫ��
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
                featureCursor = fCls.Search(sFilter, false);//ִ�пռ��ѯ
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
            //    MessageBox.Show("��ѡ�Ķ�����������٣�");
            //    return;
            //}
            ////�ж�ÿ���������Ƿ����ڽӵĽ�����

            //////�ж����ڽӽ�����ĺϲ��ο������Ƿ���ͬ����ͬ��ϲ��������ϲ���Ҫ�ء�ɾ���ϲ�ǰҪ��
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
            //���ÿռ��ѯ�����뵱ǰ�������ڽ�
            ISpatialFilter sFilterTouch = new SpatialFilterClass();
            sFilterTouch.Geometry = feature.Shape;
            sFilterTouch.GeometryField = "SHAPE";//ShapeҲ����
            sFilterTouch.SpatialRel = esriSpatialRelEnum.esriSpatialRelTouches;// | esriSpatialRelEnum.esriSpatialRelIntersects | esriSpatialRelEnum.esriSpatialRelOverlaps;
            //��ʼ�ռ��ѯ �ڽӵĽ�����
            IFeatureCursor feaCursorTouch = fCls.Search(sFilterTouch, false);
            IFeature ifeatureTouch = null;
            //ArrayList pArrayPolygonTouch = new ArrayList();//�洢��ǰ������뵱ǰ������touch�Ľ�����
            //pArrayPolygonTouch.Add(feature.Shape as IPolygon);
            int coordFeaValue = Convert.ToInt32(feature.get_Value(index_coordFea));
            //�����ռ��ѯ���ڽӵĽ�����
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
                //ֻ���뵱ǰ������feature�ĺϲ��ο�����һ�µ��ڽӽ�������ӵ�
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
        //������ѡ�Ľ�����жϽ������Ƿ������������������ڽӲ��Ҹ߶�������ͬ��������ϲ�.����ֺϲ���������ʧ�������
        public void MergePolygons(IFeatureClass fCls, IEnvelope envelope, string coordinateFeature)
        {
            for (int j = 0; j < 10; j++)
            {
                int maxFeaOID = 0;// fCls.FeatureCount(null); 
                //1.�ռ��ѯ
                //ISpatialFilter sFiltermaxFeaOID = new SpatialFilterClass();
                //sFiltermaxFeaOID.Geometry = envelope;
                //sFiltermaxFeaOID.GeometryField = "SHAPE";//ShapeҲ����
                //sFiltermaxFeaOID.SpatialRel = esriSpatialRelEnum.esriSpatialRelIntersects;//��ѯ�Ŀռ��ϵ ΪContains��������

                IFeatureCursor featureCursormaxFeaOID = fCls.Search(null, false);//ִ�пռ��ѯ
                IFeature featuremaxFeaOID = null;
                while ((featuremaxFeaOID = featureCursormaxFeaOID.NextFeature()) != null)//������ѯ����Ҫ��
                {
                    if (featuremaxFeaOID.OID > maxFeaOID)
                        maxFeaOID = featuremaxFeaOID.OID;
                }

                int delNum = 0;
                //������ѡ��ÿ��������
                IGeometry polygonRes = null;//�ϲ���Ķ����
                //1.�ռ��ѯ
                ISpatialFilter sFilter = new SpatialFilterClass();
                sFilter.Geometry = envelope;
                sFilter.GeometryField = "SHAPE";//ShapeҲ����
                sFilter.SpatialRel = esriSpatialRelEnum.esriSpatialRelIntersects;//��ѯ�Ŀռ��ϵ ΪContains��������

                IFeatureCursor featureCursor = fCls.Search(sFilter, false);//ִ�пռ��ѯ
                //List<IPolygon> polygonLst = new List<IPolygon>();//ѡ�е� ������б� 
                //ArrayList pArrayPolygon = new ArrayList();
                List<int> idLst = new List<int>(); ;
                List<int> toDelIdLst = new List<int>();
                //IPointArray pointArray = new PointArrayClass();
                int index_coordFea = fCls.FindField(coordinateFeature);
                IFeature feature = null;
                while ((feature = featureCursor.NextFeature()) != null)//������ѯ����Ҫ��
                {
                    ArrayList pArrayPolygonTouch = new ArrayList();//�洢��ǰ������뵱ǰ������touch�Ľ�����
                    QueryAdjacentBuildingsWithSameAttri(feature, fCls, index_coordFea, maxFeaOID, ref pArrayPolygonTouch, ref toDelIdLst, ref idLst);


                    ////���ÿռ��ѯ�����뵱ǰ�������ڽ�
                    //ISpatialFilter sFilterTouch = new SpatialFilterClass();
                    //sFilterTouch.Geometry = feature.Shape;
                    //sFilterTouch.GeometryField = "SHAPE";//ShapeҲ����
                    //sFilterTouch.SpatialRel = esriSpatialRelEnum.esriSpatialRelTouches;
                    ////��ʼ�ռ��ѯ �ڽӵĽ�����
                    //IFeatureCursor feaCursorTouch = fCls.Search(sFilterTouch,false);
                    //IFeature ifeatureTouch=null;
                    //ArrayList pArrayPolygonTouch = new ArrayList();//�洢��ǰ������뵱ǰ������touch�Ľ�����
                    //pArrayPolygonTouch.Add(feature.Shape as IPolygon);
                    //int coordFeaValue = Convert.ToInt32(feature.get_Value(index_coordFea));
                    ////�����ռ��ѯ���ڽӵĽ�����
                    //while ((ifeatureTouch =feaCursorTouch.NextFeature())!=null)
                    //{
                    //    IPolygon polygonTouch = ifeatureTouch.Shape as IPolygon;
                    //    //PolygonClass plnClsTouch = ifeatureTouch.Shape as PolygonClass;
                    //    //pArrayPolygonTouch.Add(plnClsTouch);
                    //    int coorFeaValueTouch = Convert.ToInt32(ifeatureTouch.get_Value(index_coordFea));
                    //    //ֻ���뵱ǰ������feature�ĺϲ��ο�����һ�µ��ڽӽ�������ӵ�
                    //    if (coordFeaValue==coorFeaValueTouch)
                    //    {
                    //        pArrayPolygonTouch.Add(polygonTouch);                        
                    //    }
                    //    else
                    //        continue;
                    //}
                    //����ռ��ѯ���ڽ��Һϲ��ο�������ͬ�ĵĽ�������кϲ�
                    if (pArrayPolygonTouch.Count > 0)
                    {
                        delNum++;
                        //2 �ϲ�
                        polygonRes = UnionOrRemoveRingFromPolygon(pArrayPolygonTouch);//ִ�кϲ�
                        //feaCursorTouch.Flush();
                        //3 ����feature
                        //���ÿռ��ѯ�����뵱ǰ�������ڽ�
                        ISpatialFilter sFilterTouch = new SpatialFilterClass();
                        sFilterTouch.Geometry = feature.Shape;
                        sFilterTouch.GeometryField = "SHAPE";//ShapeҲ����
                        sFilterTouch.SpatialRel = esriSpatialRelEnum.esriSpatialRelTouches;// | esriSpatialRelEnum.esriSpatialRelIntersects | esriSpatialRelEnum.esriSpatialRelOverlaps;
                        IFeatureCursor feaCursorTouch = fCls.Search(sFilterTouch, false);//ִ�пռ��ѯ
                        IFeature ifeatureTouch = feaCursorTouch.NextFeature();//��ȡ�ռ��ѯ�ĵ�һ�������Ҫ��
                        IFeatureCursor feaCursorInsert = fCls.Insert(true);
                        IFeatureBuffer newFeaBuffer = fCls.CreateFeatureBuffer();//������Ҫ��
                        for (int i = 0; i < feature.Fields.FieldCount; i++)
                        {
                            if (feature.Fields.get_Field(i).Editable == false)
                                continue;
                            newFeaBuffer.set_Value(i, feature.Value[i]);//.Fields.get_Field(i));//Ϊ��Ҫ���ֶθ�ֵ
                        }
                        newFeaBuffer.Shape = polygonRes;//���ϲ���Ķ����Ҫ�ظ�ֵ�� ��Ҫ��
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
                featureCursor = fCls.Search(sFilter, false);//ִ�пռ��ѯ
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
            //    MessageBox.Show("��ѡ�Ķ�����������٣�");
            //    return;
            //}
            ////�ж�ÿ���������Ƿ����ڽӵĽ�����

            //////�ж����ڽӽ�����ĺϲ��ο������Ƿ���ͬ����ͬ��ϲ��������ϲ���Ҫ�ء�ɾ���ϲ�ǰҪ��
        }

        //�ϲ�����
        //�����ǣ�1Ҫ���ࣻ2��ѡ����
        //�����壺1 �ռ��ѯѡ�еĶ���Σ�2���ж��Ƿ�Ϊ��ͨ����Σ��ϲ��� 3 ����Ҫ�� 
        public void MergePolygons(IFeatureClass fCls, IEnvelope envelope)
        {
            IGeometry polygonRes = null;//�ϲ���Ķ����
            //1.�ռ��ѯ
            ISpatialFilter sFilter = new SpatialFilterClass();
            sFilter.Geometry = envelope;
            sFilter.GeometryField = "SHAPE";//ShapeҲ����
            sFilter.SpatialRel = esriSpatialRelEnum.esriSpatialRelIntersects;//��ѯ�Ŀռ��ϵ ΪContains��������
            fCls.Search(null, false);
            IFeatureCursor featureCursor = fCls.Search(sFilter, false);//ִ�пռ��ѯ
            List<IPolygon> polygonLst = new List<IPolygon>();//ѡ�е� ������б� 
            ArrayList pArrayPolygon = new ArrayList();
            IPointArray pointArray = new PointArrayClass();
            IFeature feature = null;
            while ((feature = featureCursor.NextFeature()) != null)//������ѯ����Ҫ��
            {
                IPolygon polygon = feature.Shape as IPolygon;
                PolygonClass plnCls = feature.Shape as PolygonClass;
                pArrayPolygon.Add(feature.Shape);

            }
            if (pArrayPolygon.Count <= 1)
            {
                MessageBox.Show("��ѡ�Ķ�����������٣�");
                return;
            }
            //2 �ϲ�
            polygonRes = UnionOrRemoveRingFromPolygon(pArrayPolygon);//ִ�кϲ�

            featureCursor.Flush();
            //3 ����feature
            featureCursor = fCls.Search(sFilter, false);//ִ�пռ��ѯ
            feature = featureCursor.NextFeature();//��ȡ�ռ��ѯ�ĵ�һ�������Ҫ��
            IFeatureCursor feaCursorInsert = fCls.Insert(true);
            IFeatureBuffer newFeaBuffer = fCls.CreateFeatureBuffer();//������Ҫ��
            for (int i = 0; i < feature.Fields.FieldCount; i++)
            {
                if (feature.Fields.get_Field(i).Editable == false)
                    continue;
                newFeaBuffer.set_Value(i, feature.Value[i]);//.Fields.get_Field(i));//Ϊ��Ҫ���ֶθ�ֵ
            }
            newFeaBuffer.Shape = polygonRes;//���ϲ���Ķ����Ҫ�ظ�ֵ�� ��Ҫ��
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
        //�ϲ������  ��������
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
                pPolygon_Result.SimplifyPreserveFromTo();//ִ��toolbox�С������������øù������ͳ���
                //IRgbColor rgb_r = new RgbColorClass();
                //rgb_r.Red = 200;
                //IRgbColor rgb_r2 = new RgbColorClass();
                //MyArcEngineMethod.ArcMapDrawing.Draw_Polygon(activeView, pPolygon_Result as IPolygon, rgb_r2, rgb_r, 2);
                ///ȥ����εĿն�
                IGeometryCollection pGeocoll = pPolygon_Result as IGeometryCollection;
                int geomcount = pGeocoll.GeometryCount;
                if (geomcount > 1)
                {
                    IPolygon4 pPolygon4 = (IPolygon4)pPolygon_Result;
                    IGeometryBag bag = pPolygon4.ExteriorRingBag;     //��ȡ����ε������⻷
                    IEnumGeometry geo = bag as IEnumGeometry;
                    geo.Reset();
                    IRing exRing = geo.Next() as IRing;
                    newGeom0 = CreatePolygonfromRing(exRing);//��IRingת��ΪIpolygon
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
                MessageBox.Show("�ϲ���ȥ��������η������󣬴�����ϢΪ��->\r\n" + ex.Message,
                                    "������Ϣ", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return null;
            }

        }
        /// <summary>
        /// ��IRingת��Ϊ IGeometry
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
            //�ռ��ѯ
            ISpatialFilter sFilter = new SpatialFilterClass();
            sFilter.Geometry = envelope;
            sFilter.GeometryField = "SHAPE";//ShapeҲ����
            sFilter.SpatialRel = esriSpatialRelEnum.esriSpatialRelIntersects;//��ѯ�Ŀռ��ϵ ΪContains��������

            IFeatureCursor featureCursor = fCls.Search(sFilter, false);//ִ�пռ��ѯ
            IFeature feature = null;
            while ((feature = featureCursor.NextFeature()) != null)//������ѯ����Ҫ��
            {
                IFields fields = feature.Fields;
                int attributeIndex = fields.FindField(fldName);// ("LabelCls");
                //���ֶθ�ֵ
                feature.set_Value(attributeIndex, value);
                feature.Store();
            }
            featureCursor.Flush();
            Marshal.FinalReleaseComObject(featureCursor);
        }

    }
}
