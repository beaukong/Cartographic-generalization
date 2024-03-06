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
    /// 将线构建stroke后，将不同stroke按不同颜色绘出.
    /// </summary>
    [Guid("e79f007a-8cc7-47b7-aa1f-7af49af7b8ae")]
    [ClassInterface(ClassInterfaceType.None)]
    [ProgId("testTool.DrawStroke")]
    public sealed class DrawStroke : BaseTool
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
        private IActiveView activew = null;
        private IMap map = null;
        private ILayer layer = null;
        private IFeatureClass fClass = null;
        private IFeatureClass desFClass = null;
        private INewEnvelopeFeedback newEnvelopeFeedback = null;

        private List<IPolyline> varLineList = null;//存储每条弧段
        private Dictionary<long, long> varLineIDandOrderDic = null; //varLine 的ID与在varLineList中的下标对应Dic(实际上ID并不是连续的)
        //private List<IFeature> featureList = null;
        private List<Node> nodes = null;//存储每条弧段的端点，空间上允许重叠
        private List<Node> uniqueNodes = null;//河网网各弧段的端点，即结点集（不存在空间上重合的点)

        //起始点相关的其他线下标
        private List<List<int>> fromLinesList = null;
        private List<List<int>> toLinesList = null;
        //标识与该线起始点相连接的线，接点是起始点(0)还是终止点(1)
        private List<List<int>> fromPtLinesList = null;
        private List<List<int>> toPtLinesList = null;

        private List<JoinEdgePair> joinEdgePairs = null;//各结点对应的连接弧段(两个弧段一对)
        private int curPairID = 0;//向joinEdgePairs添加元素的同时，记录其ID
        private const double thresholdForStroke = 30;//构建stroke时相连弧段夹角阈值

        private List<List<int>> mainStrokes = null;//存储stroke(组成弧段数目均大于1)
        private List<int> mainStrokesArcsIndex = null;//mainStrokes中包含的弧段在varLineList下标

        //strokes_sel与strokes_assist无交集
        private List<List<int>> strokes_sel = null;//参与选取的stroke
        private List<List<int>> strokes_assist = null;//辅助选取的stroke(一般较重要而保留)

        //存储mainStrokes的组成弧段总数目
        private int mainStrokeArcsSum = 0;
        //存储mainStrokes的组成弧段总长度
        private Double mainStrokeArcsLength = 0.0;
        private double HydroNetSelectRatio = 0.5;
        public DrawStroke()
        {
            //
            // TODO: Define values for the public properties
            //
            base.m_category = "绘制Stroke"; //localizable text 
            base.m_caption = "绘制Stroke";  //localizable text 
            base.m_message = "绘制Strokel";  //localizable text
            base.m_toolTip = "绘制Stroke";  //localizable text
            base.m_name = "绘制Stroke";   //unique id, non-localizable (e.g. "MyCategory_MyTool")
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
            // TODO: Add DrawStroke.OnClick implementation
            map = (application.Document as IMxDocument).ActiveView.FocusMap;
            activew = map as IActiveView;
            layer = DApplication.App.currentLayer;
            if (layer==null)
            {
                MessageBox.Show("地图文档中无数据！");
                return;
            }
            fClass = (layer as IFeatureLayer).FeatureClass;
        }

        public override void OnMouseDown(int Button, int Shift, int X, int Y)
        {
            // TODO:  Add DrawStroke.OnMouseDown implementation
            if (activew == null)
            {
                activew = m_hookHelper.FocusMap as IActiveView;
            }
            else
            {
                newEnvelopeFeedback = null;
                IPoint point = null;
                point = activew.ScreenDisplay.DisplayTransformation.ToMapPoint(X, Y);
                newEnvelopeFeedback = new NewEnvelopeFeedbackClass();
                newEnvelopeFeedback.Display = activew.ScreenDisplay;
                newEnvelopeFeedback.Start(point);
            }

        }

        public override void OnMouseMove(int Button, int Shift, int X, int Y)
        {
            // TODO:  Add DrawStroke.OnMouseMove implementation
            if (newEnvelopeFeedback!=null)
            {
                if (activew==null)
                {
                    activew = m_hookHelper.FocusMap as IActiveView;
                }                
                IPoint point = null;
                point = activew.ScreenDisplay.DisplayTransformation.ToMapPoint(X, Y);
                newEnvelopeFeedback.MoveTo(point);
            }
        }

        public override void OnMouseUp(int Button, int Shift, int X, int Y)
        {
                IEnvelope envelop = newEnvelopeFeedback.Stop();
                try
                {
                    DrawIntersectLine(envelop);
                    varLineList.Clear();
                    varLineIDandOrderDic.Clear();
                    //featureList.Clear();
                    nodes.Clear();
                    uniqueNodes.Clear();
                    fromLinesList.Clear();
                    fromPtLinesList.Clear();
                    toLinesList.Clear();
                    toPtLinesList.Clear();
                    joinEdgePairs.Clear();
                    mainStrokes.Clear();
                    mainStrokesArcsIndex.Clear();
                    strokes_assist.Clear();
                    strokes_sel.Clear();
                    MessageBox.Show("Stroke绘制完成！");
                }
                catch (System.Runtime.InteropServices.COMException e)
                {
                    varLineList.Clear();
                    varLineIDandOrderDic.Clear();
                    //featureList.Clear();
                    nodes.Clear();
                    uniqueNodes.Clear();
                    fromLinesList.Clear();
                    fromPtLinesList.Clear();
                    toLinesList.Clear();
                    toPtLinesList.Clear();
                    joinEdgePairs.Clear();
                    mainStrokes.Clear();
                    mainStrokesArcsIndex.Clear();
                    strokes_assist.Clear();
                    strokes_sel.Clear();
                    MessageBox.Show(e.ToString());
                }         
        }
        public override void OnDblClick()
        {
            newEnvelopeFeedback = null;
            activew.Refresh();
            updateMap();
        }
        #endregion

        private void DrawIntersectLine(IEnvelope envelop)
        {
            #region  框选给线绘颜色
            ////空间查询
            ////得到框选的线，对每条线调用DrawLine()
            //ISpatialFilter filter = new SpatialFilterClass();
            //filter.Geometry = envelop;
            //filter.SpatialRel = esriSpatialRelEnum.esriSpatialRelIntersects;

            //IFeatureCursor cursor = fClass.Search(filter, false);
            //IFeature feature = null;
            //IRgbColor color = null;
            //color = new RgbColorClass();
            //color.Red = new Random().Next(0, 255);
            //color.Green = new Random().Next(0, 255);
            //color.Blue = new Random().Next(0, 255);
            //while ((feature = cursor.NextFeature()) != null)
            //{
            //    IGeometry geometry = feature.ShapeCopy;
            //    IPolyline polyLine = geometry as IPolyline;
            //    MyArcEngineMethod.ArcMapDrawing.Draw_Polyline(activew, polyLine, color, 2);
            //}
            ////try
            ////{//???怎么打开地图
            ////    application.OpenDocument("D:\\ArcGis10.2.2\\data\\wh_didi\\1_5shp\\Wuhan20151124_1\\Wuhan20151124_1.shp");
            ////    application.OpenDocument(@"D:\ArcGis10.2.2\data\wh_didi\1_5shp\Wuhan20151124_1\Wuhan20151124_1.shp");
            ////}
            ////catch (Exception e)
            ////{
            ////    MessageBox.Show(e.Message);
            ////}
            
            #endregion
            ///copy原始图层 作为目标图层
            //目标文件夹D:\ArcGis10.2.2\data\projectData\HyStroke  名字是原始图层名字+Stroke+随机数
            try
            {
                //获取源文件路径
                IDataLayer2 dataLayer = layer as IDataLayer2;
                IDatasetName dsName = dataLayer.DataSourceName as IDatasetName;
                 IWorkspaceName wSName = dsName.WorkspaceName;
                 string sorPath = wSName.PathName+"\\"+layer.Name;//sorPath = "C:\\Users\\Administrator\\Documents\\ArcGIS\\Default.gdb"
                //获取目标文件所在文件夹及名字
                 string desLayerDir = "D:\\ArcGis10.2.2\\data\\projectData\\HyStroke";
                 string desLayerName = layer.Name + "Stroke" + new Random().Next(1, 100).ToString();
                //开始copy
                 bool isCopy = MyArcEngineMethod.OtherMethod.CopySameNameFiles(sorPath, desLayerDir, desLayerName);//第三个参数是不带后缀的目标文件名。方法内部会添加后缀
                string desShpPath = desLayerDir + "\\" + desLayerName + ".shp";//目标文件路径
                 //如果目标文件不存在，return
                 if (!File.Exists(desShpPath))
                 {
                     MessageBox.Show("程序已退出，shp路径有误！", "警告", MessageBoxButtons.OK, MessageBoxIcon.Error);
                     return;
                 } 
                 desFClass = MyArcEngineMethod.FeatureClassOperation.OpenFeatureClass(desShpPath);
                 MyArcEngineMethod.FieldOperation.CreateFieldsByName(desFClass, esriFieldType.esriFieldTypeInteger, "Stroke");//创建Stroke字段
            }
            catch (Exception e)
            {

                MessageBox.Show(e.Message);//"创建要素类失败，请检查！");
                return;
            }
            if (desFClass == null)
            {
                MessageBox.Show("创建要素类失败，请检查！");
                return;
            }
            if (desFClass != null)
            {
                varLineList = new List<IPolyline>();
                varLineIDandOrderDic = new Dictionary<long, long>();
                //featureList = new List<IFeature>();
                nodes = new List<Node>();
                uniqueNodes = new List<Node>();
                fromLinesList = new List<List<int>>();
                toLinesList = new List<List<int>>();
                fromPtLinesList = new List<List<int>>();
                toPtLinesList = new List<List<int>>();
                joinEdgePairs = new List<JoinEdgePair>();
                mainStrokes = new List<List<int>>();
                mainStrokesArcsIndex = new List<int>();
                strokes_assist = new List<List<int>>();
                strokes_sel = new List<List<int>>();
                ///空间查询
                //遍历 将每条线要素放入List<IPolyline>varLineList中
                IFeatureCursor desCursor = desFClass.Update(null, false);
                varLineList = new List<IPolyline>();
                IFeature feature = null;
                long order = 0;
                while ((feature = desCursor.NextFeature()) != null)
                {
                    long id = (long)feature.OID;
                    IGeometry geometry = feature.ShapeCopy;
                    IPolyline polyLine = geometry as IPolyline;
                    varLineList.Add(polyLine);//存储每条弧段
                    varLineIDandOrderDic.Add(id, order++);//varLine 的ID与在varLineList中的下标对应Dic(实际上ID并不是连续的)
                    //featureList.Add(feature);
                }
                ///提取 fromLinesList，toLinesList, fromPtLinesList, toPtLinesList
                ExtractLineAndPtData();
                ///构建stroke 
                ConstructStroke();
                ///获取字段索引，不同stroke赋不同的值
                //MyArcEngineMethod.FieldOperation.CreateFieldsByName(desFClass, esriFieldType.esriFieldTypeInteger, "Stroke");
                int index_Stroke = desFClass.Fields.FindField("Stroke");//获取字段Stroke的在属性表中索引
                int int_stroke = 0;//stroke的序号
                for (int count = 0; count < mainStrokes.Count; count++)
                {
                    List<int> stroke = mainStrokes[count];
                    int_stroke++;
                    foreach (var lineIndex in stroke)
                    {
                        IFeature fea = desFClass.GetFeature(lineIndex);//lineIndex正是OID的顺序。
                        fea.set_Value(index_Stroke, int_stroke);
                        fea.Store();
                    }
                }
                //application.OpenDocument(desLayerDir + desLayerName);
            }
        }

        /// <summary>
        /// 提取 fromLinesList，toLinesList, fromPtLinesList, toPtLinesList
        /// </summary>
        private void ExtractLineAndPtData()
        {
            for (int i = 0; i < varLineList.Count; i++)
            {
                IPolyline polyLinei = varLineList[i];
                int dotNum = (polyLinei as IPointCollection).PointCount;//获得每条线点的个数
                Node startNode = new Node(polyLinei.FromPoint.X, polyLinei.FromPoint.Y);//该线的起点结点
                Node endNode=new Node(polyLinei.ToPoint.X,polyLinei.ToPoint.Y);
                startNode.nextNode = endNode;
                endNode.lastNode = startNode;
                nodes.Add(startNode);//将结点node加入到List<Node>nodes中，允许重复
                nodes.Add(endNode);
                //将河网线端点（空间上不重合）添加到结点集中
                AddNode(ref uniqueNodes, startNode);
                AddNode(ref uniqueNodes, endNode);

                ////将当前弧段添加到edges
                //Edge currEdge = new Edge(i);
                //currEdge.formerNode = startNode;
                //currEdge.laterNode = endNode;
                //edges.Add(new Edge(i));

                //对一个点获取其邻接线的下标,并标注接点是起始点(0)还是终止点(1)
                Dictionary<int, int> startDotNearLines = GetLinesNearDot(varLineList, i, startNode);//<一个起点邻接的线下标，对应接点状态：该接点是该邻接线的起点还是终点>
                List<int> s_nearLinesIndexes = new List<int>();//一个起点邻接线下标
                List<int> s_joinState = new List<int>();//一个起点各个邻接线邻接点的状态
                foreach (KeyValuePair<int, int> kvp in startDotNearLines)
                {
                    s_nearLinesIndexes.Add(kvp.Key);
                    s_joinState.Add(kvp.Value);
                }
                fromLinesList.Add(s_nearLinesIndexes);//每个起点的邻接线下标，
                fromPtLinesList.Add(s_joinState);

                List<int> e_nearLinesIndexes = new List<int>();//终点邻接线下标
                List<int> e_joinState = new List<int>();//终点各个邻接线邻接点的状态                      
                Dictionary<int, int> endDotNearLines = GetLinesNearDot(varLineList, i, endNode);//<终点邻接的线下标，对应接点状态>
                foreach (KeyValuePair<int, int> kvp in endDotNearLines)
                {
                    e_nearLinesIndexes.Add(kvp.Key);
                    e_joinState.Add(kvp.Value);
                }
                toLinesList.Add(e_nearLinesIndexes);
                toPtLinesList.Add(e_joinState);
            }
        }

        //由河网线的端点不重复地添加至结点集
        internal void AddNode(ref List<Node> nodes, Node node)
        {
            bool added = false;
            foreach (IPoint nodei in nodes)
            {
                //}
                //foreach (Dot dot in nodes)
                //{
                //此处两个不重合结点的距离不应小于GetLinesNearDot方法中矩形查询的半个对角线长
                if (CalculateDistance(nodei, node) < 10.0)
                {
                    added = true;
                    break;
                }
            }
            if (added == false)
                nodes.Add(node);
        }

        //计算两点之间的欧氏距离
        internal static double CalculateDistance(IPoint pt1, IPoint pt2)
        {
            return Math.Sqrt(Math.Pow(pt1.X - pt2.X, 2) + Math.Pow(pt1.Y - pt2.Y, 2));
        }

        //对一个点获取其邻接线的下标,并标注接点是起始点(0)还是终止点(1)
        internal Dictionary<int, int> GetLinesNearDot(List<IPolyline> lineList, int lineIndex, Node node)
        {
            Dictionary<int, int> nearLines = new Dictionary<int, int>();//<邻接线在varLineList中的下标，接点状态（0 or 1）>
            //空间查询当前点相邻的线
            IEnvelope envelope = new EnvelopeClass();
            envelope.PutCoords(node.X - 1, node.Y - 1, node.X + 1, node.Y + 1);
            ISpatialFilter filter=new SpatialFilterClass();
            filter.Geometry=envelope;
            filter.SpatialRel= esriSpatialRelEnum.esriSpatialRelIntersects;
            IFeatureCursor cursor = desFClass.Search(filter,false);
            //遍历查询到的线，确定<其在vaLineList中下标，起点0 or终点1>
            IFeature feature = null;
            while ((feature = cursor.NextFeature()) != null)
            {
                long id = (long)feature.OID;//当前要素的OID
                if (varLineIDandOrderDic.Count>0&&varLineIDandOrderDic.ContainsKey(id))
                {
                    int lineorder = (int)varLineIDandOrderDic[id];//根据OID从varLineIDandOrderDic获取线序号（在varLineList中的序号）
                    node.joinEdges.Add(lineorder);
                    if (lineorder != lineIndex)//从邻接线中排除当前线本身
                    {
                        IPolyline curPlyline = lineList[lineorder];
                        IPoint startPoint = curPlyline.FromPoint;//获取当前这条线的起点
                        IPoint endPoint = curPlyline.ToPoint;
                        double startDis = Math.Sqrt(Math.Pow(startPoint.X - node.X, 2)) + Math.Sqrt(Math.Pow(startPoint.Y - node.Y, 2));//计算结点与当前线起点的距离
                        double endDis = Math.Sqrt(Math.Pow(endPoint.X - node.X, 2)) + Math.Sqrt(Math.Pow(endPoint.Y - node.Y, 2));//计算结点与当前线终点的距离
                        if (startDis <= endDis)nearLines.Add(lineorder, 0);
                        else nearLines.Add(lineorder, 1);
                    }
                }
            }
            return nearLines;
        }

        // 构建stroke
        internal void ConstructStroke()
        {
            #region  获取两个弧段一组的stroke连接pair
            List<int> edgesInjoinEdgePairs = new List<int>();//记录在joinEdgePairs出现的弧段下标（每个弧段最多出现2次）
            for (int k = 0; k< uniqueNodes.Count;k++)
            {
                Queue<int> joinEdgesIndexes = new Queue<int>();
                foreach (int edgeIndex in uniqueNodes[k].joinEdges)
                {
                    joinEdgesIndexes.Enqueue(edgeIndex);
                }
                if (joinEdgesIndexes.Count>1)
                {
                    //任意两条相连弧段一组,建立stroke连接候选集R
                    List<JoinEdgePair> R = new List<JoinEdgePair>();
                    while (joinEdgesIndexes.Count > 0)
                    {
                        int[] edgeIndexArr = joinEdgesIndexes.ToArray();
                        int firstEdge = joinEdgesIndexes.Dequeue(); //前一弧段在varLineList中的下标                            

                        for (int c = 1; c < edgeIndexArr.Length; c++)
                        {
                            JoinEdgePair pair = new JoinEdgePair();
                            if (firstEdge < edgeIndexArr[c])//确保前一弧段下标小于后一弧段下标
                            {
                                pair.firstEdge = firstEdge;
                                pair.secondEdge = edgeIndexArr[c];
                            }
                            else
                            {
                                pair.firstEdge = edgeIndexArr[c];
                                pair.secondEdge = firstEdge;
                            }
                            pair.midNode = uniqueNodes[k];//添加前后弧段相连结点                              
                            R.Add(pair);
                        }
                    }

                    List<int> selectedIndexes = new List<int>();//记录该结点已在pair中的邻接弧段（下标）
                    //遍历sroke连接候选集R，选出可构成stroke连接的弧段对pair 
                    foreach (JoinEdgePair edgePair   in R)
                    {
                        int firstEdge = edgePair.firstEdge;
                        int secondEdge = edgePair.secondEdge;
                        //判断是否已建立包含 firstEdge或 secondEdge 的连接组合
                        if (selectedIndexes.Contains(firstEdge) || selectedIndexes.Contains(secondEdge))
                            continue;
                        IPolyline formerArc = varLineList[firstEdge];//前一弧段
                        IPolyline laterArc = varLineList[secondEdge];//后一弧段

                        #region  1-根据属性信息确定R中一组相连弧段是否构成stroke连接
                        //找出前后edge的NAME字段值
                        //找出NAME字段的index  desFClass.Fields.FindFields("NAME")
                        int index_NAME = desFClass.Fields.FindField("NAME");
                        //int index_GID = desFClass.Fields.FindField("GID");
                        //根据索引找到字段值  festure.Value[index]
                        string formerArcRiverName = Convert.ToString(desFClass.GetFeature(firstEdge).Value[index_NAME]);//前edge的NAME字段值  
                        string laterArcRiverName = Convert.ToString(desFClass.GetFeature(secondEdge).Value[index_NAME]);
                        //string formerArcRiverName = Convert.ToString(featureList[firstEdge].Value[index_NAME]);//前edge的NAME字段值
                        //string laterArcRiverName = Convert.ToString(featureList[secondEdge].Value[index_NAME]);
                        //前后弧段所属河网名称相同，则构成stroke连接
                        if (formerArcRiverName.Length > 0 && laterArcRiverName.Length > 0)
                        {
                            if (formerArcRiverName != null && laterArcRiverName!=null&&formerArcRiverName == laterArcRiverName)
                            {
                                edgePair.ID = curPairID;//赋ID
                                curPairID++;
                                joinEdgePairs.Add(edgePair);
                                selectedIndexes.Add(firstEdge);
                                selectedIndexes.Add(secondEdge);
                                continue;
                            }
                        }
                        #endregion
                        #region  2-计算R中一组相连弧段的夹角并依次确定是否构成stroke连接
                        IPoint fPoint1 = formerArc.FromPoint;//前一弧段起点
                        IPoint fPoint2 = formerArc.ToPoint;    //前一弧段终点
                        IPoint fFuthestPoint = null;                 //前一弧段中距离起始点直线最远的点，作为计算弧段夹角的点
                        double disMax = 0;
                        IPointCollection fPointCollection = formerArc as IPointCollection;
                        for (int i2 = 1; i2 < fPointCollection.PointCount - 2; i2++)
                        {
                            double dis = CalDisPt2Line(fPointCollection.get_Point(i2), fPoint1, fPoint2);
                            if (disMax < dis)
                            {
                                disMax = dis;
                                fFuthestPoint = fPointCollection.get_Point(i2);
                            }
                        }
                        IPoint lPoint1 = laterArc.FromPoint;//前一弧段起点
                        IPoint lPoint2 = laterArc.ToPoint;    //前一弧段终点
                        IPoint lFuthestPoint = null;                 //前一弧段中距离起始点直线最远的点，作为计算弧段夹角的点
                        disMax = 0;
                        IPointCollection lPointCollection = laterArc as IPointCollection;
                        for (int i2 = 1; i2 < lPointCollection.PointCount - 2; i2++)
                        {
                            double dis = CalDisPt2Line(lPointCollection.get_Point(i2), lPoint1, lPoint2);
                            if (disMax < dis)
                            {
                                disMax = dis;
                                lFuthestPoint = lPointCollection.get_Point(i2);//这里尽管有多个点还是会为null？？？？？？？？？？？？？

                            }
                        }
                        if (fFuthestPoint==null)
                        {
                            int oid_fst = desFClass.GetFeature(firstEdge).OID;
                            MessageBox.Show("有问题线的oid是" + oid_fst);
                        }
                        if (lFuthestPoint==null)
                        {
                            int oid_snd = desFClass.GetFeature(secondEdge).OID;
                            MessageBox.Show("有问题线的oid是" + oid_snd);
                        }
                        double intersecAngle = CalLinesAngle(fFuthestPoint, uniqueNodes[k], lFuthestPoint);//弧段夹角
                        //相连两弧段夹角大于阈值
                        if (intersecAngle >= thresholdForStroke)
                        {
                            edgePair.ID = curPairID;//赋ID
                            curPairID++;
                            joinEdgePairs.Add(edgePair);
                            selectedIndexes.Add(firstEdge);
                            selectedIndexes.Add(secondEdge);
                        }
                        #endregion
                    }
                }
            }
            #endregion

            #region  根据弧段间的stroke连接关系将varLineList中的弧段分组--------------------------------------------
            //注意：若一条线(A)全程和另一条线(B)（全程或部分）完全贴合，则在joinEdgePairs会出现(A,B) (A,B)                      
            #region  按各pair的首尾弧段joinEdgePairs排序，筛选出异常pair(其某一弧段出现3次)，标记其skip为true。 将joinEdgePairs中的正常数据添加到 List<List<int>> pairs
            //(一)按各pair的首弧段joinEdgePairs排序，筛选出异常pair，标记其skip为true
            PairsSortByEdgeIndex(joinEdgePairs, true);//按各pair的首弧段joinEdgePairs排序
            for (int i2 = 0; i2 < joinEdgePairs.Count; i2++)
            {
                JoinEdgePair pair = joinEdgePairs[i2];//当前pair
                if (pair.skip == true)
                    continue;

                if (i2 > 0 && i2 < joinEdgePairs.Count - 1)
                {
                    //当前pair与前一个pair的首弧段相同时
                    if (pair.firstEdge == joinEdgePairs[i2 - 1].firstEdge)
                    {
                        //1.出现2个相邻pair的首尾弧段相同的情况,则跳过第2个重复的pair
                        if (pair.secondEdge == joinEdgePairs[i2 - 1].secondEdge)
                        {
                            pair.skip = true;
                            continue;
                        }
                        //2.出现3个相邻pair的首弧段相同的情况,则跳过第3个pair
                        else if (pair.firstEdge == joinEdgePairs[i2 + 1].firstEdge)
                        {
                            joinEdgePairs[i2 + 1].skip = true;
                        }
                        //3.检查在当前pair之前的pairs尾弧段，看pair.firstEdge是否已经出现2次
                        else
                        {
                            for (int j = 0; j < i2 - 1; j++)
                            {
                                if (joinEdgePairs[j].secondEdge == pair.firstEdge)
                                {
                                    pair.skip = true;
                                    break;
                                }
                            }
                        }
                    }
                }               
            }
            //（二）按各pair的尾弧段joinEdgePairs排序，筛选出异常pair，标记其skip为true
            PairsSortByEdgeIndex(joinEdgePairs, false);
            for (int i2 = 0; i2 < joinEdgePairs.Count; i2++)
            {
                JoinEdgePair pair = joinEdgePairs[i2];//当前pair
                if (pair.skip == true)
                    continue;

                if (i2 > 0 && i2 < joinEdgePairs.Count - 1)
                {
                    //当前pair与前一个pair的尾弧段相同时
                    if (pair.secondEdge == joinEdgePairs[i2 - 1].secondEdge)
                    {
                        //1.出现3个相邻pair的尾弧段相同的情况,则跳过第3个pair
                        if (pair.secondEdge == joinEdgePairs[i2 + 1].secondEdge)
                        {
                            joinEdgePairs[i2 + 1].skip = true;
                        }
                        //2.检查在当前pair之后的pairs首弧段，出现第3个pair.secondEdge下标时，则将对应pair的skip设为true
                        else
                        {
                            for (int j = i2 + 1; j < joinEdgePairs.Count; j++)
                            {
                                if (joinEdgePairs[j].firstEdge == pair.secondEdge)
                                {
                                    joinEdgePairs[j].skip = true;
                                    break;
                                }
                            }
                        }
                    }
                }
            }

            //将joinEdgePairs中的正常数据添加到pairs进行后续操作
            List<List<int>> pairs = new List<List<int>>();//[ [0,5],[1,3],...,[7,9] ]                                               
            //  String test="";
            foreach (var pair in joinEdgePairs)
            {
                if (pair.skip == false)
                {
                    List<int> onePair = new List<int>();
                    onePair.Add(pair.firstEdge);
                    onePair.Add(pair.secondEdge);
                    pairs.Add(onePair);
                    //  test += "[" + pair.firstEdge + "," + pair.secondEdge + "]" + ",";
                }
            }
            #endregion

            #region  根据pairs构建stroke 存在mainStrokes，strokes_sel
            //dic每个元素为：一个弧段，与该弧段有联系的弧段集
            Dictionary<int, List<int>> dic = new Dictionary<int, List<int>>();
            foreach (var pair in pairs)
            {
                //pair[0]是第一个弧段索引，pair[1]是第二个弧段索引
                if (!dic.ContainsKey(pair[0]))
                {
                    List<int> temp = new List<int>();
                    temp.Add(pair[1]);
                    dic[pair[0]] = temp;
                }
                else
                {
                    dic[pair[0]].Add(pair[1]);
                }

                if (!dic.ContainsKey(pair[1]))//？？这样把pair[0],pair[1]分别作为key添加，会重复添加吧
                {
                    List<int> tmp = new List<int>();
                    tmp.Add(pair[0]);
                    dic[pair[1]] = tmp;
                }
                else
                {
                    dic[pair[1]].Add(pair[0]);
                }
            }

            List<int> delKey = new List<int>();  //存储已添加的key                    

            foreach (KeyValuePair<int, List<int>> kvp in dic)
            {
                int key = kvp.Key;
                List<int> value = kvp.Value;
                if (delKey.Contains(key))
                    continue;
                if (value.Count > 1)//？？？value是key弧段的后续连接弧段串吧？大部分的count>1呀
                    continue;

                int temp = key;//0
                int maxIterateCount = 20;
                while (maxIterateCount > 0)//？？迭代次数一够，直接终止循环，也不再建立onestroke，不再添加mainstroke
                {
                    int lastEleInValue = value[value.Count - 1];
                    if ((dic[lastEleInValue]).Count != 1)
                    {
                        foreach (var x in dic[lastEleInValue])
                        {
                            if (x != temp)
                            {
                                dic[key].Add(x);//此处value增加元素
                                break;
                            }
                        }

                        //因为上一步加了一个数据，所以要-2
                        int lastEleInNewValue = value[value.Count - 2];
                        temp = lastEleInNewValue;

                        delKey.Add(lastEleInNewValue);
                        maxIterateCount--;
                    }
                    else
                    {
                        List<int> oneStroke = new List<int>();
                        oneStroke.Add(key);
                        delKey.Add(key);

                        mainStrokesArcsIndex.Add(key);//
                        mainStrokeArcsLength += varLineList[key].Length;

                        delKey.Add(lastEleInValue);

                        foreach (int v in value)
                        {
                            oneStroke.Add(v);
                            mainStrokesArcsIndex.Add(v);//
                            mainStrokeArcsLength += varLineList[v].Length;
                        }

                        mainStrokes.Add(oneStroke);
                        strokes_sel.Add(oneStroke);
                        mainStrokeArcsSum += oneStroke.Count;//
                        break;
                    }
                }
            }
            delKey.Clear();
            #endregion

            #endregion

            #region  从mainStrokes中选出需要保留的stroke添加到strokes_assist，选取比例越小，strokes_assist中元素越少
            int aveMainStrokeArcsNum = (int)(mainStrokeArcsSum / mainStrokes.Count);//mainStroke平均包含的弧段数目
            double aveMainStrokeLength = mainStrokeArcsLength / mainStrokes.Count;//mainStroke平均长度
            for (var i = 0; i < mainStrokes.Count; i++)
            {
                int connectivityMore = 0;
                float lengthMore = 1;
                int containArcNumsMore = 1;
                if (Convert.ToDouble(HydroNetSelectRatio) > 0.6)
                {
                    connectivityMore = 2;
                    lengthMore = 1.5F;
                    containArcNumsMore = 1;
                }
                else if (Convert.ToDouble(HydroNetSelectRatio) > 0.4)
                {
                    connectivityMore = 3;
                    lengthMore = 1.8F;
                    containArcNumsMore = 2;
                }
                else
                {
                    connectivityMore = 4;
                    lengthMore = 3F;
                    containArcNumsMore = 3;
                }

                List<int> ms = mainStrokes[i];
                //1.优先保留连通性较好的mainStroke(河网交叉口数量)，这里取平均一个结点所连河网>=connectivityMore
                int joinRiversSum = 0;
                for (int j = 0; j < ms.Count - 1; j++)
                {
                    if (fromLinesList[ms[j]] != null)
                        joinRiversSum += fromLinesList[ms[j]].Count;
                }
                if (joinRiversSum / ms.Count - connectivityMore >= 0.0001)
                {
                    strokes_assist.Add(ms);
                    continue;
                }

                //2.保留长度大于平均水平lengthMore倍的mainStroke
                double msLength = 0.0;
                foreach (int arc in ms)
                {
                    msLength += varLineList[arc].Length;
                }
                if (msLength >= (lengthMore * aveMainStrokeLength))
                {
                    strokes_assist.Add(ms);
                    continue;
                }

                //3.保留包含弧段数目多于平均水平至少containArcNumsMore个的mainStroke
                if (ms.Count > aveMainStrokeArcsNum + containArcNumsMore)
                {
                    strokes_assist.Add(ms);
                    continue;
                }
            }

            //从strokes_sel中去掉strokes_assist中的元素
            foreach (var assist in strokes_assist)
            {
                strokes_sel.Remove(assist);
            }

            //将单个弧段添加到 strokes_sel
            for (int j = 0; j < varLineList.Count; j++)
            {
                if (mainStrokesArcsIndex.Contains(j))
                    continue;
                List<int> singleArc = new List<int>();//单个弧段，该list长度只有1
                singleArc.Add(j);
                strokes_sel.Add(singleArc);
                mainStrokes.Add(singleArc);
            }

            #endregion
        }
        //计算一个点到一条直线段的欧氏距离
        internal static double CalDisPt2Line(IPoint pt, IPoint linePt1, IPoint LinePt2)
        {
            double x1 = linePt1.X;
            double y1 = linePt1.Y;
            double x2 = LinePt2.X;
            double y2 = LinePt2.Y;
            double k = 0;
            double dis = 0;

            if (x1 != x2)
            {
                k = (y2 - y1) / (x2 - x1);
                dis = (Math.Abs(pt.Y - k * pt.X + k * x1 - y1)) / (Math.Sqrt(1 + k * k));
            }
            else
            {
                dis = Math.Abs(pt.X - x1);
            }
            return dis;
        }

        //计算两直线段之间的夹角
        internal static double CalLinesAngle(IPoint dot1, IPoint dot, IPoint dot2)
        {
            double a; double b; double c;
            a = CalculateDistance(dot1, dot);
            b = CalculateDistance(dot2, dot);
            c = CalculateDistance(dot1, dot2);
            if (a > 0 && b > 0)
            {
                double degreeValue = 0.0;
                double cosvalue = (a * a + b * b - c * c) / (2 * a * b);
                if ((cosvalue + 1) <= 0)
                    degreeValue = Math.PI;
                else
                    degreeValue = Math.Acos(cosvalue);
                degreeValue = degreeValue * 180 / Math.PI;
                degreeValue = degreeValue > 0 ? degreeValue : 180 + degreeValue;

                return degreeValue;
            }
            else // 表示存在重复点情况
            {
                return 0;
            }
        }

        //对joinEdgePairs按第一个(或第二个)弧段下标排序（升序）
        internal static void PairsSortByEdgeIndex(List<JoinEdgePair> pairs, bool firstEdge)
        {
            if (firstEdge == true)
            {
                for (int i = 1; i <= pairs.Count - 1; i++)
                {
                    for (int j = 0; j < pairs.Count - i; j++)
                    {
                        if (pairs[j + 1].firstEdge < pairs[j].firstEdge)
                        {
                            JoinEdgePair temp = pairs[j];
                            pairs[j] = pairs[j + 1];
                            pairs[j + 1] = temp;
                        }
                    }
                }
            }
            else
            {
                for (int i = 1; i <= pairs.Count - 1; i++)
                {
                    for (int j = 0; j < pairs.Count - i; j++)
                    {
                        if (pairs[j + 1].secondEdge < pairs[j].secondEdge)
                        {
                            JoinEdgePair temp = pairs[j];
                            pairs[j] = pairs[j + 1];
                            pairs[j + 1] = temp;
                        }
                    }
                }
            }

        }

        //更新地图，清除选择的要素
        private void updateMap()
        {
            IFeatureSelection pFeatureSelection = layer as IFeatureSelection;
            pFeatureSelection.Clear();
        }
    }
}
