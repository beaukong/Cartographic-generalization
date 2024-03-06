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
using System.IO;
using System.Diagnostics;
using MyArcEngineMethod;
using MyArcEngineMethod.MyExtensionMethod;
namespace Merge
{
    /// <summary>
    /// Summary description for MergeTool.
    /// </summary>
    [Guid("1c459180-b9d4-46ca-85e3-4abd356c735b")]
    [ClassInterface(ClassInterfaceType.None)]
    [ProgId("Merge.MergeTool")]
    public sealed class MergeTool : BaseTool
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
        public MergeTool()
        {
            //
            // TODO: Define values for the public properties
            //
            base.m_category = "�ϲ������"; //localizable text 
            base.m_caption = "�ϲ������";  //localizable text 
            base.m_message = "����󣬿�ѡ�ڽ�����ν��кϲ�";  //localizable text
            base.m_toolTip = "�ϲ������";  //localizable text
            base.m_name = "�ϲ������";   //unique id, non-localizable (e.g. "MyCategory_ArcMapTool")
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
            // TODO: Add MergeTool.OnClick implementation
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
                            MergePolygons((selectLayer as IFeatureLayer).FeatureClass, envelope);
                            ////��ȥ�������ڵ�·�ϵĽ־�������
                            //GetSamplePointStreetveiw();
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

        #region//�ϲ�����
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
        #endregion
        
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

        #region//[���ã�����qgis c++����]  ��ȡÿ�������� ��Ӧ��·�ϵĽ־����������ꡢ�������򡢲���������
        public void GetSamplePointStreetveiw()
        {
            ///�ϲ���·��Ϊһ��Ҫ�أ���ѧϰ�»�����Ө��POIƥ�䣩
            ///����������
            ///��ȡ���뽨��������ĵ�·��
            ///���ݽ��������ġ������·��ȷ����������Ͳ��������ǡ�
            String poiPath = @"D:\VS2019\project\QT\QT\data\Wuhan20151124_2\�켣����.shp";//@"F:\ArcGIS10_2_2\data\FuTian\1\point.shp";
            String roadPath = @"D:\VS2019\project\QT\QT\data\Wuhan20151124_2\��·����.shp";
            // string path = AppDomain.CurrentDomain.BaseDirectory + "help.txt";
            // if(args.Length == 1)
            // {
            //     MyMethods.OtherMethods.PrintHelp(args[0], path);
            //     return;
            // }

            // if (args.Length != 2)
            // {
            //     throw new ArgumentException("����������켣��Ҫ��·����·��Ҫ����·��!");
            // }
            //// var log = MyArcEngineMethod.MyLogger.MyLoggerFactory.GetInstance(new StackTrace(new StackFrame(true)), true);
            //    poiPath = args[0];//��һ���������
            //    roadPath = args[1];//�ڶ����������       
            if ((!File.Exists(poiPath)) ||
                (!File.Exists(roadPath)))
            {
                Console.WriteLine("�����������·�������ڣ����飡");
                return;
            }
            try
            {
                //ƥ���
                MatchedPoint(poiPath, roadPath, null);
                //Console.WriteLine("�������!\n����������˳���");
                //log.Message("·��ƥ�䴦����ɣ������CSV�ļ�·��Ϊ��"+csvPath);
            }
            catch (Exception e)
            {
                //log.Message("·��ƥ�䴦��ʧ�ܣ����飡����");
                //log.stackTrace = new StackTrace(e);
                //log.Error(1);
                Console.WriteLine(e.StackTrace);
            }
        }
        //ƥ���
        //1.�Ƚ����еĵ�·�ں�
        //2.�������еĵ�
        //3.����λ��������DisError�ֶν��м�¼
        //���룺�켣��·����·��ҪҪ��·��
        public void MatchedPoint(string poiPath, string roadPath, string csvPath)
        {
            //��־��¼���ǵõ���close����
            using (MyArcEngineMethod.MyLogger.MyLogger logger = MyArcEngineMethod.MyLogger.MyLoggerFactory.GetInstance(new StackTrace(new StackFrame(true))))
            {
                IFeatureClass poiClass = MyArcEngineMethod.FeatureClassOperation.OpenFeatureClass(poiPath);
                IFeatureClass roadClass = MyArcEngineMethod.FeatureClassOperation.OpenFeatureClass(roadPath);

                //�쳣���
                if (poiClass == null || roadClass == null) throw new ArgumentNullException("�����������Ϊ�գ�");

                //�ں����еĵ�·��
                IFeatureCursor union = roadClass.Search(null, false);
                IGeometry mergedLines = null;
                IFeature mFea = null;
                while ((mFea = union.NextFeature()) != null)
                {
                    if (mergedLines == null)
                    {
                        mergedLines = mFea.ShapeCopy;
                        continue;
                    }
                    else
                    {
                        mergedLines = (mergedLines as ITopologicalOperator).Union(mFea.ShapeCopy);
                    }
                }
                Marshal.FinalReleaseComObject(union);
#if CONSOLE
                Console.WriteLine("·�����ں�Ϊһ��Ҫ�أ�");
                int consoleCount = 0; int total = poiClass.FeatureCount(null);
#endif
                //�ں����,����Ҫ���࣬Ȼ������Ҫ�����ļ�
                string generatedPointShpPath = System.IO.Path.GetDirectoryName(poiPath) + "/matchedPoi.shp";
                IFeatureClass generatedPointClass = MyArcEngineMethod.FeatureClassOperation.
        CreateNewFeatureClass(generatedPointShpPath, esriGeometryType.esriGeometryPoint, poiClass, true);
                int errorIndex = MyArcEngineMethod.FieldOperation.CreateOneFieldAndGetIndex(poiClass,
                  esriFieldType.esriFieldTypeDouble, "DisError");
                //����Ҫ�����ֶ�
                MyArcEngineMethod.FieldOperation.CopyFeatureClassFields(poiClass, generatedPointClass);
                int poiFidindex = MyArcEngineMethod.FieldOperation.CreateOneFieldAndGetIndex(generatedPointClass,
                    esriFieldType.esriFieldTypeInteger, "PoiFid");
                logger.Message("generatedPointShp created successfully!");

                //����λ�þ��ȣ�����Ϊcsv�ļ�
#if ACCURACY
                double misSum = 0; int curLineID = -1; int pCount = 0;
                int idIndex = poiClass.FindField("LineID");
                StreamWriter sw = new StreamWriter(csvPath, false, System.Text.Encoding.UTF8);
                sw.WriteLine("LineID,PoisitionAccuracy,PointNumber");
#endif
                //�����ֶ� DisError
                IFeatureCursor poiUpdate = poiClass.Update(null, false);
                IFeatureCursor insert = generatedPointClass.Insert(true);
                double dis = 0;
                while ((mFea = poiUpdate.NextFeature()) != null)
                {
                    IPoint p = (mergedLines as IProximityOperator).ReturnNearestPoint((mFea.ShapeCopy as IPoint),
                        esriSegmentExtension.esriNoExtension);
                    if (p == null)
                    {
                        continue;
                    }
                    dis = p.GetDistance(mFea.ShapeCopy as IPoint);
                    mFea.Value[errorIndex] = dis;
                    poiUpdate.UpdateFeature(mFea);

                    //����Ԫ��
                    IFeatureBuffer fb = generatedPointClass.CreateFeatureBuffer();
                    fb.Shape = p;
                    fb.Value[poiFidindex] = mFea.OID;//д��ԭ��Ҫ�ص�OID
                    //�����ֶ�
                    for (int i = 0; i < mFea.Fields.FieldCount; i++)
                    {
                        IField field = mFea.Fields.get_Field(i);
                        if (field.Editable && field.Type != esriFieldType.esriFieldTypeGeometry)
                        {
                            int index1 = fb.Fields.FindField(field.Name);
                            if (index1 != -1)
                            {
                                fb.Value[index1] = mFea.get_Value(i);
                            }
                        }
                    }
                    insert.InsertFeature(fb);

#if CONSOLE
                    Console.WriteLine("Progress:{0}/ {1}", ++consoleCount, total);
#endif

#if ACCURACY
                    int lineID = int.Parse(mFea.get_Value(idIndex).ToString());
                    if (lineID != curLineID)
                    {
                        if (pCount > 1)
                        {
                            sw.WriteLine(curLineID.ToString()+","+(misSum / pCount).Round(4)+","+ pCount);                                                 
                        }
                        misSum = p.GetDistance(mFea.Shape as IPoint);    
                        pCount = 1;
                        curLineID = lineID;
                    }
                    else 
                    {
                        //
                        pCount++;
                        misSum += p.GetDistance(mFea.Shape as IPoint);                    
                    }
#endif
                }
                poiUpdate.Flush();
                Marshal.FinalReleaseComObject(poiUpdate);
                insert.Flush();
                Marshal.FinalReleaseComObject(insert);
                logger.Message("Matched finished!");
#if ACCURACY
                sw.Flush();
                sw.Close();
                logger.Message("�ѳɹ�����λ�þ���CSV��");
#endif
            }
        }

        //������ֱ�߶�֮��ļн�  pt2X,pt2Y�Ƕ��ǵ����꣬
        internal static double CalLinesAngle(double pt1X, double pt1Y, double pt2X, double pt2Y, double pt3X, double pt3Y)
        {
            if (pt1X == pt2X && pt1Y >= pt2Y)
                return 0;
            if(pt1X == pt2X && pt1Y < pt2Y)
                return 180;
            if (pt1Y == pt2Y && pt1X > pt2X)
                return 90;
            if (pt1Y == pt2Y && pt1X < pt2X)
                return 270;
            double a; double b; double c;
            a = CalculateDistance(pt1X,  pt1Y,  pt2X,  pt2Y);
            b = CalculateDistance(pt3X,  pt3Y,  pt2X,  pt2Y);
            c = CalculateDistance(pt1X, pt1Y, pt3X, pt3Y);
            if (a > 0 && b > 0)
            {
                double degreeValue = 0.0;
                double cosvalue = (a * a + b * b - c * c) / (2 * a * b);
                if ((cosvalue + 1) <= 0)
                    degreeValue = Math.PI;
                else
                    degreeValue = Math.Acos(cosvalue);
                degreeValue = degreeValue * 180 / Math.PI;
                degreeValue = (pt2X < pt1X)? degreeValue : 360- degreeValue;
                
                //degreeValue = pt2X < pt1X ? degreeValue : 180 + degreeValue;

                return degreeValue;
            }
            else // ��ʾ�����ظ������
            {
                return 0;
            }
        }

        //��������֮���ŷ�Ͼ���
        internal static double CalculateDistance(double pt1X, double pt1Y, double pt2X, double pt2Y)
        {
            return Math.Sqrt(Math.Pow(pt1X - pt2X, 2) + Math.Pow(pt1Y - pt2Y, 2));
        }
        #endregion
        //   public double CalculateAngle()
     //   {


     //       MyLatLng A = new MyLatLng(113.249648, 23.401553);
     //       MyLatLng B = new MyLatLng(113.246033, 23.403362);
     //       return getAngle(A, B);
     //   }

     //   /**
     //* ��ȡAB��������������ĽǶ�
     //* @param A  A��ľ�γ��
     //* @param B  B��ľ�γ��
     //* @return  AB��������������ĽǶȣ�0~360��
     //*/
     //   public double getAngle(MyLatLng A, MyLatLng B)
     //   {
     //       double dx = (B.m_RadLo - A.m_RadLo) * A.Ed;
     //       double dy = (B.m_RadLa - A.m_RadLa) * A.Ec;
     //       double angle = 0.0;
     //       angle = Math.Atan(Math.Abs(dx / dy)) * 180 / Math.PI;
     //       double dLo = B.m_Longitude - A.m_Longitude;
     //       double dLa = B.m_Latitude - A.m_Latitude;
     //       if (dLo > 0 && dLa <= 0)
     //       {
     //           angle = (90 - angle) + 90;
     //       }
     //       else if (dLo <= 0 && dLa < 0)
     //       {
     //           angle = angle + 180;
     //       }
     //       else if (dLo < 0 && dLa >= 0)
     //       {
     //           angle = (90 - angle) + 270;
     //       }
     //       return angle;
     //   }
    }
    public class MyLatLng
    {
        public double Rc = 6378137;
        public double Rj = 6356725;
        public double m_LoDeg, m_LoMin, m_LoSec;
        public double m_LaDeg, m_LaMin, m_LaSec;
        public double m_Longitude, m_Latitude;
        public double m_RadLo, m_RadLa;
        public double Ec;
        public double Ed;
        public MyLatLng(double longitude, double latitude)
        {
            m_LoDeg = (int)longitude;
            m_LoMin = (int)((longitude - m_LoDeg) * 60);
            m_LoSec = (longitude - m_LoDeg - m_LoMin / 60) * 3600;

            m_LaDeg = (int)latitude;
            m_LaMin = (int)((latitude - m_LaDeg) * 60);
            m_LaSec = (latitude - m_LaDeg - m_LaMin / 60) * 3600;

            m_Longitude = longitude;
            m_Latitude = latitude;
            m_RadLo = longitude * Math.PI / 180;
            m_RadLa = latitude * Math.PI / 180;
            Ec = Rj + (Rc - Rj) * (90 - m_Latitude) / 90;
            Ed = Ec * Math.Cos(m_RadLa);
        }
    }
}
