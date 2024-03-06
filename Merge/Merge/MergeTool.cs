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
            base.m_category = "合并多边形"; //localizable text 
            base.m_caption = "合并多边形";  //localizable text 
            base.m_message = "点击后，框选邻近多边形进行合并";  //localizable text
            base.m_toolTip = "合并多边形";  //localizable text
            base.m_name = "合并多边形";   //unique id, non-localizable (e.g. "MyCategory_ArcMapTool")
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
                            MergePolygons((selectLayer as IFeatureLayer).FeatureClass, envelope);
                            ////过去建筑物在道路上的街景采样点
                            //GetSamplePointStreetveiw();
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

        #region//合并方法
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
        #endregion
        
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

        #region//[无用，参照qgis c++代码]  获取每个建筑物 对应道路上的街景采样点坐标、采样方向、采样俯仰角
        public void GetSamplePointStreetveiw()
        {
            ///合并道路网为一个要素？（学习陈欢给李莹的POI匹配）
            ///遍历建筑物
            ///获取距离建筑物最近的道路点
            ///根据建筑物质心、最近道路点确定采样方向和采样俯仰角。
            String poiPath = @"D:\VS2019\project\QT\QT\data\Wuhan20151124_2\轨迹数据.shp";//@"F:\ArcGIS10_2_2\data\FuTian\1\point.shp";
            String roadPath = @"D:\VS2019\project\QT\QT\data\Wuhan20151124_2\道路数据.shp";
            // string path = AppDomain.CurrentDomain.BaseDirectory + "help.txt";
            // if(args.Length == 1)
            // {
            //     MyMethods.OtherMethods.PrintHelp(args[0], path);
            //     return;
            // }

            // if (args.Length != 2)
            // {
            //     throw new ArgumentException("请依次输入轨迹点要素路径，路网要素类路径!");
            // }
            //// var log = MyArcEngineMethod.MyLogger.MyLoggerFactory.GetInstance(new StackTrace(new StackFrame(true)), true);
            //    poiPath = args[0];//第一个输入参数
            //    roadPath = args[1];//第二个输入参数       
            if ((!File.Exists(poiPath)) ||
                (!File.Exists(roadPath)))
            {
                Console.WriteLine("错误：输入参数路径不存在！请检查！");
                return;
            }
            try
            {
                //匹配点
                MatchedPoint(poiPath, roadPath, null);
                //Console.WriteLine("处理完成!\n按下任意键退出！");
                //log.Message("路网匹配处理完成！已输出CSV文件路径为："+csvPath);
            }
            catch (Exception e)
            {
                //log.Message("路网匹配处理失败！请检查！！！");
                //log.stackTrace = new StackTrace(e);
                //log.Error(1);
                Console.WriteLine(e.StackTrace);
            }
        }
        //匹配点
        //1.先将所有的道路融合
        //2.遍历所有的点
        //3.计算位置误差，创建DisError字段进行记录
        //输入：轨迹点路径，路网要要素路径
        public void MatchedPoint(string poiPath, string roadPath, string csvPath)
        {
            //日志记录，记得调用close方法
            using (MyArcEngineMethod.MyLogger.MyLogger logger = MyArcEngineMethod.MyLogger.MyLoggerFactory.GetInstance(new StackTrace(new StackFrame(true))))
            {
                IFeatureClass poiClass = MyArcEngineMethod.FeatureClassOperation.OpenFeatureClass(poiPath);
                IFeatureClass roadClass = MyArcEngineMethod.FeatureClassOperation.OpenFeatureClass(roadPath);

                //异常检测
                if (poiClass == null || roadClass == null) throw new ArgumentNullException("错误：输入参数为空！");

                //融合所有的道路网
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
                Console.WriteLine("路网已融合为一个要素！");
                int consoleCount = 0; int total = poiClass.FeatureCount(null);
#endif
                //融合完成,创建要素类，然后生成要素类文件
                string generatedPointShpPath = System.IO.Path.GetDirectoryName(poiPath) + "/matchedPoi.shp";
                IFeatureClass generatedPointClass = MyArcEngineMethod.FeatureClassOperation.
        CreateNewFeatureClass(generatedPointShpPath, esriGeometryType.esriGeometryPoint, poiClass, true);
                int errorIndex = MyArcEngineMethod.FieldOperation.CreateOneFieldAndGetIndex(poiClass,
                  esriFieldType.esriFieldTypeDouble, "DisError");
                //复制要素类字段
                MyArcEngineMethod.FieldOperation.CopyFeatureClassFields(poiClass, generatedPointClass);
                int poiFidindex = MyArcEngineMethod.FieldOperation.CreateOneFieldAndGetIndex(generatedPointClass,
                    esriFieldType.esriFieldTypeInteger, "PoiFid");
                logger.Message("generatedPointShp created successfully!");

                //计算位置精度，导出为csv文件
#if ACCURACY
                double misSum = 0; int curLineID = -1; int pCount = 0;
                int idIndex = poiClass.FindField("LineID");
                StreamWriter sw = new StreamWriter(csvPath, false, System.Text.Encoding.UTF8);
                sw.WriteLine("LineID,PoisitionAccuracy,PointNumber");
#endif
                //创建字段 DisError
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

                    //插入元素
                    IFeatureBuffer fb = generatedPointClass.CreateFeatureBuffer();
                    fb.Shape = p;
                    fb.Value[poiFidindex] = mFea.OID;//写入原点要素的OID
                    //复制字段
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
                logger.Message("已成功导出位置精度CSV！");
#endif
            }
        }

        //计算两直线段之间的夹角  pt2X,pt2Y是顶角的坐标，
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
            else // 表示存在重复点情况
            {
                return 0;
            }
        }

        //计算两点之间的欧氏距离
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
     //* 获取AB连线与正北方向的角度
     //* @param A  A点的经纬度
     //* @param B  B点的经纬度
     //* @return  AB连线与正北方向的角度（0~360）
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
