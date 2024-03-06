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
    /// ���߹���stroke�󣬽���ͬstroke����ͬ��ɫ���.
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

        private List<IPolyline> varLineList = null;//�洢ÿ������
        private Dictionary<long, long> varLineIDandOrderDic = null; //varLine ��ID����varLineList�е��±��ӦDic(ʵ����ID������������)
        //private List<IFeature> featureList = null;
        private List<Node> nodes = null;//�洢ÿ�����εĶ˵㣬�ռ��������ص�
        private List<Node> uniqueNodes = null;//�����������εĶ˵㣬����㼯�������ڿռ����غϵĵ�)

        //��ʼ����ص��������±�
        private List<List<int>> fromLinesList = null;
        private List<List<int>> toLinesList = null;
        //��ʶ�������ʼ�������ӵ��ߣ��ӵ�����ʼ��(0)������ֹ��(1)
        private List<List<int>> fromPtLinesList = null;
        private List<List<int>> toPtLinesList = null;

        private List<JoinEdgePair> joinEdgePairs = null;//������Ӧ�����ӻ���(��������һ��)
        private int curPairID = 0;//��joinEdgePairs���Ԫ�ص�ͬʱ����¼��ID
        private const double thresholdForStroke = 30;//����strokeʱ�������μн���ֵ

        private List<List<int>> mainStrokes = null;//�洢stroke(��ɻ�����Ŀ������1)
        private List<int> mainStrokesArcsIndex = null;//mainStrokes�а����Ļ�����varLineList�±�

        //strokes_sel��strokes_assist�޽���
        private List<List<int>> strokes_sel = null;//����ѡȡ��stroke
        private List<List<int>> strokes_assist = null;//����ѡȡ��stroke(һ�����Ҫ������)

        //�洢mainStrokes����ɻ�������Ŀ
        private int mainStrokeArcsSum = 0;
        //�洢mainStrokes����ɻ����ܳ���
        private Double mainStrokeArcsLength = 0.0;
        private double HydroNetSelectRatio = 0.5;
        public DrawStroke()
        {
            //
            // TODO: Define values for the public properties
            //
            base.m_category = "����Stroke"; //localizable text 
            base.m_caption = "����Stroke";  //localizable text 
            base.m_message = "����Strokel";  //localizable text
            base.m_toolTip = "����Stroke";  //localizable text
            base.m_name = "����Stroke";   //unique id, non-localizable (e.g. "MyCategory_MyTool")
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
                MessageBox.Show("��ͼ�ĵ��������ݣ�");
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
                    MessageBox.Show("Stroke������ɣ�");
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
            #region  ��ѡ���߻���ɫ
            ////�ռ��ѯ
            ////�õ���ѡ���ߣ���ÿ���ߵ���DrawLine()
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
            ////{//???��ô�򿪵�ͼ
            ////    application.OpenDocument("D:\\ArcGis10.2.2\\data\\wh_didi\\1_5shp\\Wuhan20151124_1\\Wuhan20151124_1.shp");
            ////    application.OpenDocument(@"D:\ArcGis10.2.2\data\wh_didi\1_5shp\Wuhan20151124_1\Wuhan20151124_1.shp");
            ////}
            ////catch (Exception e)
            ////{
            ////    MessageBox.Show(e.Message);
            ////}
            
            #endregion
            ///copyԭʼͼ�� ��ΪĿ��ͼ��
            //Ŀ���ļ���D:\ArcGis10.2.2\data\projectData\HyStroke  ������ԭʼͼ������+Stroke+�����
            try
            {
                //��ȡԴ�ļ�·��
                IDataLayer2 dataLayer = layer as IDataLayer2;
                IDatasetName dsName = dataLayer.DataSourceName as IDatasetName;
                 IWorkspaceName wSName = dsName.WorkspaceName;
                 string sorPath = wSName.PathName+"\\"+layer.Name;//sorPath = "C:\\Users\\Administrator\\Documents\\ArcGIS\\Default.gdb"
                //��ȡĿ���ļ������ļ��м�����
                 string desLayerDir = "D:\\ArcGis10.2.2\\data\\projectData\\HyStroke";
                 string desLayerName = layer.Name + "Stroke" + new Random().Next(1, 100).ToString();
                //��ʼcopy
                 bool isCopy = MyArcEngineMethod.OtherMethod.CopySameNameFiles(sorPath, desLayerDir, desLayerName);//�����������ǲ�����׺��Ŀ���ļ����������ڲ�����Ӻ�׺
                string desShpPath = desLayerDir + "\\" + desLayerName + ".shp";//Ŀ���ļ�·��
                 //���Ŀ���ļ������ڣ�return
                 if (!File.Exists(desShpPath))
                 {
                     MessageBox.Show("�������˳���shp·������", "����", MessageBoxButtons.OK, MessageBoxIcon.Error);
                     return;
                 } 
                 desFClass = MyArcEngineMethod.FeatureClassOperation.OpenFeatureClass(desShpPath);
                 MyArcEngineMethod.FieldOperation.CreateFieldsByName(desFClass, esriFieldType.esriFieldTypeInteger, "Stroke");//����Stroke�ֶ�
            }
            catch (Exception e)
            {

                MessageBox.Show(e.Message);//"����Ҫ����ʧ�ܣ����飡");
                return;
            }
            if (desFClass == null)
            {
                MessageBox.Show("����Ҫ����ʧ�ܣ����飡");
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
                ///�ռ��ѯ
                //���� ��ÿ����Ҫ�ط���List<IPolyline>varLineList��
                IFeatureCursor desCursor = desFClass.Update(null, false);
                varLineList = new List<IPolyline>();
                IFeature feature = null;
                long order = 0;
                while ((feature = desCursor.NextFeature()) != null)
                {
                    long id = (long)feature.OID;
                    IGeometry geometry = feature.ShapeCopy;
                    IPolyline polyLine = geometry as IPolyline;
                    varLineList.Add(polyLine);//�洢ÿ������
                    varLineIDandOrderDic.Add(id, order++);//varLine ��ID����varLineList�е��±��ӦDic(ʵ����ID������������)
                    //featureList.Add(feature);
                }
                ///��ȡ fromLinesList��toLinesList, fromPtLinesList, toPtLinesList
                ExtractLineAndPtData();
                ///����stroke 
                ConstructStroke();
                ///��ȡ�ֶ���������ͬstroke����ͬ��ֵ
                //MyArcEngineMethod.FieldOperation.CreateFieldsByName(desFClass, esriFieldType.esriFieldTypeInteger, "Stroke");
                int index_Stroke = desFClass.Fields.FindField("Stroke");//��ȡ�ֶ�Stroke�������Ա�������
                int int_stroke = 0;//stroke�����
                for (int count = 0; count < mainStrokes.Count; count++)
                {
                    List<int> stroke = mainStrokes[count];
                    int_stroke++;
                    foreach (var lineIndex in stroke)
                    {
                        IFeature fea = desFClass.GetFeature(lineIndex);//lineIndex����OID��˳��
                        fea.set_Value(index_Stroke, int_stroke);
                        fea.Store();
                    }
                }
                //application.OpenDocument(desLayerDir + desLayerName);
            }
        }

        /// <summary>
        /// ��ȡ fromLinesList��toLinesList, fromPtLinesList, toPtLinesList
        /// </summary>
        private void ExtractLineAndPtData()
        {
            for (int i = 0; i < varLineList.Count; i++)
            {
                IPolyline polyLinei = varLineList[i];
                int dotNum = (polyLinei as IPointCollection).PointCount;//���ÿ���ߵ�ĸ���
                Node startNode = new Node(polyLinei.FromPoint.X, polyLinei.FromPoint.Y);//���ߵ������
                Node endNode=new Node(polyLinei.ToPoint.X,polyLinei.ToPoint.Y);
                startNode.nextNode = endNode;
                endNode.lastNode = startNode;
                nodes.Add(startNode);//�����node���뵽List<Node>nodes�У������ظ�
                nodes.Add(endNode);
                //�������߶˵㣨�ռ��ϲ��غϣ���ӵ���㼯��
                AddNode(ref uniqueNodes, startNode);
                AddNode(ref uniqueNodes, endNode);

                ////����ǰ������ӵ�edges
                //Edge currEdge = new Edge(i);
                //currEdge.formerNode = startNode;
                //currEdge.laterNode = endNode;
                //edges.Add(new Edge(i));

                //��һ�����ȡ���ڽ��ߵ��±�,����ע�ӵ�����ʼ��(0)������ֹ��(1)
                Dictionary<int, int> startDotNearLines = GetLinesNearDot(varLineList, i, startNode);//<һ������ڽӵ����±꣬��Ӧ�ӵ�״̬���ýӵ��Ǹ��ڽ��ߵ���㻹���յ�>
                List<int> s_nearLinesIndexes = new List<int>();//һ������ڽ����±�
                List<int> s_joinState = new List<int>();//һ���������ڽ����ڽӵ��״̬
                foreach (KeyValuePair<int, int> kvp in startDotNearLines)
                {
                    s_nearLinesIndexes.Add(kvp.Key);
                    s_joinState.Add(kvp.Value);
                }
                fromLinesList.Add(s_nearLinesIndexes);//ÿ�������ڽ����±꣬
                fromPtLinesList.Add(s_joinState);

                List<int> e_nearLinesIndexes = new List<int>();//�յ��ڽ����±�
                List<int> e_joinState = new List<int>();//�յ�����ڽ����ڽӵ��״̬                      
                Dictionary<int, int> endDotNearLines = GetLinesNearDot(varLineList, i, endNode);//<�յ��ڽӵ����±꣬��Ӧ�ӵ�״̬>
                foreach (KeyValuePair<int, int> kvp in endDotNearLines)
                {
                    e_nearLinesIndexes.Add(kvp.Key);
                    e_joinState.Add(kvp.Value);
                }
                toLinesList.Add(e_nearLinesIndexes);
                toPtLinesList.Add(e_joinState);
            }
        }

        //�ɺ����ߵĶ˵㲻�ظ����������㼯
        internal void AddNode(ref List<Node> nodes, Node node)
        {
            bool added = false;
            foreach (IPoint nodei in nodes)
            {
                //}
                //foreach (Dot dot in nodes)
                //{
                //�˴��������غϽ��ľ��벻ӦС��GetLinesNearDot�����о��β�ѯ�İ���Խ��߳�
                if (CalculateDistance(nodei, node) < 10.0)
                {
                    added = true;
                    break;
                }
            }
            if (added == false)
                nodes.Add(node);
        }

        //��������֮���ŷ�Ͼ���
        internal static double CalculateDistance(IPoint pt1, IPoint pt2)
        {
            return Math.Sqrt(Math.Pow(pt1.X - pt2.X, 2) + Math.Pow(pt1.Y - pt2.Y, 2));
        }

        //��һ�����ȡ���ڽ��ߵ��±�,����ע�ӵ�����ʼ��(0)������ֹ��(1)
        internal Dictionary<int, int> GetLinesNearDot(List<IPolyline> lineList, int lineIndex, Node node)
        {
            Dictionary<int, int> nearLines = new Dictionary<int, int>();//<�ڽ�����varLineList�е��±꣬�ӵ�״̬��0 or 1��>
            //�ռ��ѯ��ǰ�����ڵ���
            IEnvelope envelope = new EnvelopeClass();
            envelope.PutCoords(node.X - 1, node.Y - 1, node.X + 1, node.Y + 1);
            ISpatialFilter filter=new SpatialFilterClass();
            filter.Geometry=envelope;
            filter.SpatialRel= esriSpatialRelEnum.esriSpatialRelIntersects;
            IFeatureCursor cursor = desFClass.Search(filter,false);
            //������ѯ�����ߣ�ȷ��<����vaLineList���±꣬���0 or�յ�1>
            IFeature feature = null;
            while ((feature = cursor.NextFeature()) != null)
            {
                long id = (long)feature.OID;//��ǰҪ�ص�OID
                if (varLineIDandOrderDic.Count>0&&varLineIDandOrderDic.ContainsKey(id))
                {
                    int lineorder = (int)varLineIDandOrderDic[id];//����OID��varLineIDandOrderDic��ȡ����ţ���varLineList�е���ţ�
                    node.joinEdges.Add(lineorder);
                    if (lineorder != lineIndex)//���ڽ������ų���ǰ�߱���
                    {
                        IPolyline curPlyline = lineList[lineorder];
                        IPoint startPoint = curPlyline.FromPoint;//��ȡ��ǰ�����ߵ����
                        IPoint endPoint = curPlyline.ToPoint;
                        double startDis = Math.Sqrt(Math.Pow(startPoint.X - node.X, 2)) + Math.Sqrt(Math.Pow(startPoint.Y - node.Y, 2));//�������뵱ǰ�����ľ���
                        double endDis = Math.Sqrt(Math.Pow(endPoint.X - node.X, 2)) + Math.Sqrt(Math.Pow(endPoint.Y - node.Y, 2));//�������뵱ǰ���յ�ľ���
                        if (startDis <= endDis)nearLines.Add(lineorder, 0);
                        else nearLines.Add(lineorder, 1);
                    }
                }
            }
            return nearLines;
        }

        // ����stroke
        internal void ConstructStroke()
        {
            #region  ��ȡ��������һ���stroke����pair
            List<int> edgesInjoinEdgePairs = new List<int>();//��¼��joinEdgePairs���ֵĻ����±꣨ÿ������������2�Σ�
            for (int k = 0; k< uniqueNodes.Count;k++)
            {
                Queue<int> joinEdgesIndexes = new Queue<int>();
                foreach (int edgeIndex in uniqueNodes[k].joinEdges)
                {
                    joinEdgesIndexes.Enqueue(edgeIndex);
                }
                if (joinEdgesIndexes.Count>1)
                {
                    //����������������һ��,����stroke���Ӻ�ѡ��R
                    List<JoinEdgePair> R = new List<JoinEdgePair>();
                    while (joinEdgesIndexes.Count > 0)
                    {
                        int[] edgeIndexArr = joinEdgesIndexes.ToArray();
                        int firstEdge = joinEdgesIndexes.Dequeue(); //ǰһ������varLineList�е��±�                            

                        for (int c = 1; c < edgeIndexArr.Length; c++)
                        {
                            JoinEdgePair pair = new JoinEdgePair();
                            if (firstEdge < edgeIndexArr[c])//ȷ��ǰһ�����±�С�ں�һ�����±�
                            {
                                pair.firstEdge = firstEdge;
                                pair.secondEdge = edgeIndexArr[c];
                            }
                            else
                            {
                                pair.firstEdge = edgeIndexArr[c];
                                pair.secondEdge = firstEdge;
                            }
                            pair.midNode = uniqueNodes[k];//���ǰ�󻡶��������                              
                            R.Add(pair);
                        }
                    }

                    List<int> selectedIndexes = new List<int>();//��¼�ý������pair�е��ڽӻ��Σ��±꣩
                    //����sroke���Ӻ�ѡ��R��ѡ���ɹ���stroke���ӵĻ��ζ�pair 
                    foreach (JoinEdgePair edgePair   in R)
                    {
                        int firstEdge = edgePair.firstEdge;
                        int secondEdge = edgePair.secondEdge;
                        //�ж��Ƿ��ѽ������� firstEdge�� secondEdge ���������
                        if (selectedIndexes.Contains(firstEdge) || selectedIndexes.Contains(secondEdge))
                            continue;
                        IPolyline formerArc = varLineList[firstEdge];//ǰһ����
                        IPolyline laterArc = varLineList[secondEdge];//��һ����

                        #region  1-����������Ϣȷ��R��һ�����������Ƿ񹹳�stroke����
                        //�ҳ�ǰ��edge��NAME�ֶ�ֵ
                        //�ҳ�NAME�ֶε�index  desFClass.Fields.FindFields("NAME")
                        int index_NAME = desFClass.Fields.FindField("NAME");
                        //int index_GID = desFClass.Fields.FindField("GID");
                        //���������ҵ��ֶ�ֵ  festure.Value[index]
                        string formerArcRiverName = Convert.ToString(desFClass.GetFeature(firstEdge).Value[index_NAME]);//ǰedge��NAME�ֶ�ֵ  
                        string laterArcRiverName = Convert.ToString(desFClass.GetFeature(secondEdge).Value[index_NAME]);
                        //string formerArcRiverName = Convert.ToString(featureList[firstEdge].Value[index_NAME]);//ǰedge��NAME�ֶ�ֵ
                        //string laterArcRiverName = Convert.ToString(featureList[secondEdge].Value[index_NAME]);
                        //ǰ�󻡶���������������ͬ���򹹳�stroke����
                        if (formerArcRiverName.Length > 0 && laterArcRiverName.Length > 0)
                        {
                            if (formerArcRiverName != null && laterArcRiverName!=null&&formerArcRiverName == laterArcRiverName)
                            {
                                edgePair.ID = curPairID;//��ID
                                curPairID++;
                                joinEdgePairs.Add(edgePair);
                                selectedIndexes.Add(firstEdge);
                                selectedIndexes.Add(secondEdge);
                                continue;
                            }
                        }
                        #endregion
                        #region  2-����R��һ���������εļнǲ�����ȷ���Ƿ񹹳�stroke����
                        IPoint fPoint1 = formerArc.FromPoint;//ǰһ�������
                        IPoint fPoint2 = formerArc.ToPoint;    //ǰһ�����յ�
                        IPoint fFuthestPoint = null;                 //ǰһ�����о�����ʼ��ֱ����Զ�ĵ㣬��Ϊ���㻡�μнǵĵ�
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
                        IPoint lPoint1 = laterArc.FromPoint;//ǰһ�������
                        IPoint lPoint2 = laterArc.ToPoint;    //ǰһ�����յ�
                        IPoint lFuthestPoint = null;                 //ǰһ�����о�����ʼ��ֱ����Զ�ĵ㣬��Ϊ���㻡�μнǵĵ�
                        disMax = 0;
                        IPointCollection lPointCollection = laterArc as IPointCollection;
                        for (int i2 = 1; i2 < lPointCollection.PointCount - 2; i2++)
                        {
                            double dis = CalDisPt2Line(lPointCollection.get_Point(i2), lPoint1, lPoint2);
                            if (disMax < dis)
                            {
                                disMax = dis;
                                lFuthestPoint = lPointCollection.get_Point(i2);//���ﾡ���ж���㻹�ǻ�Ϊnull��������������������������

                            }
                        }
                        if (fFuthestPoint==null)
                        {
                            int oid_fst = desFClass.GetFeature(firstEdge).OID;
                            MessageBox.Show("�������ߵ�oid��" + oid_fst);
                        }
                        if (lFuthestPoint==null)
                        {
                            int oid_snd = desFClass.GetFeature(secondEdge).OID;
                            MessageBox.Show("�������ߵ�oid��" + oid_snd);
                        }
                        double intersecAngle = CalLinesAngle(fFuthestPoint, uniqueNodes[k], lFuthestPoint);//���μн�
                        //���������μнǴ�����ֵ
                        if (intersecAngle >= thresholdForStroke)
                        {
                            edgePair.ID = curPairID;//��ID
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

            #region  ���ݻ��μ��stroke���ӹ�ϵ��varLineList�еĻ��η���--------------------------------------------
            //ע�⣺��һ����(A)ȫ�̺���һ����(B)��ȫ�̻򲿷֣���ȫ���ϣ�����joinEdgePairs�����(A,B) (A,B)                      
            #region  ����pair����β����joinEdgePairs����ɸѡ���쳣pair(��ĳһ���γ���3��)�������skipΪtrue�� ��joinEdgePairs�е�����������ӵ� List<List<int>> pairs
            //(һ)����pair���׻���joinEdgePairs����ɸѡ���쳣pair�������skipΪtrue
            PairsSortByEdgeIndex(joinEdgePairs, true);//����pair���׻���joinEdgePairs����
            for (int i2 = 0; i2 < joinEdgePairs.Count; i2++)
            {
                JoinEdgePair pair = joinEdgePairs[i2];//��ǰpair
                if (pair.skip == true)
                    continue;

                if (i2 > 0 && i2 < joinEdgePairs.Count - 1)
                {
                    //��ǰpair��ǰһ��pair���׻�����ͬʱ
                    if (pair.firstEdge == joinEdgePairs[i2 - 1].firstEdge)
                    {
                        //1.����2������pair����β������ͬ�����,��������2���ظ���pair
                        if (pair.secondEdge == joinEdgePairs[i2 - 1].secondEdge)
                        {
                            pair.skip = true;
                            continue;
                        }
                        //2.����3������pair���׻�����ͬ�����,��������3��pair
                        else if (pair.firstEdge == joinEdgePairs[i2 + 1].firstEdge)
                        {
                            joinEdgePairs[i2 + 1].skip = true;
                        }
                        //3.����ڵ�ǰpair֮ǰ��pairsβ���Σ���pair.firstEdge�Ƿ��Ѿ�����2��
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
            //����������pair��β����joinEdgePairs����ɸѡ���쳣pair�������skipΪtrue
            PairsSortByEdgeIndex(joinEdgePairs, false);
            for (int i2 = 0; i2 < joinEdgePairs.Count; i2++)
            {
                JoinEdgePair pair = joinEdgePairs[i2];//��ǰpair
                if (pair.skip == true)
                    continue;

                if (i2 > 0 && i2 < joinEdgePairs.Count - 1)
                {
                    //��ǰpair��ǰһ��pair��β������ͬʱ
                    if (pair.secondEdge == joinEdgePairs[i2 - 1].secondEdge)
                    {
                        //1.����3������pair��β������ͬ�����,��������3��pair
                        if (pair.secondEdge == joinEdgePairs[i2 + 1].secondEdge)
                        {
                            joinEdgePairs[i2 + 1].skip = true;
                        }
                        //2.����ڵ�ǰpair֮���pairs�׻��Σ����ֵ�3��pair.secondEdge�±�ʱ���򽫶�Ӧpair��skip��Ϊtrue
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

            //��joinEdgePairs�е�����������ӵ�pairs���к�������
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

            #region  ����pairs����stroke ����mainStrokes��strokes_sel
            //dicÿ��Ԫ��Ϊ��һ�����Σ���û�������ϵ�Ļ��μ�
            Dictionary<int, List<int>> dic = new Dictionary<int, List<int>>();
            foreach (var pair in pairs)
            {
                //pair[0]�ǵ�һ������������pair[1]�ǵڶ�����������
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

                if (!dic.ContainsKey(pair[1]))//����������pair[0],pair[1]�ֱ���Ϊkey��ӣ����ظ���Ӱ�
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

            List<int> delKey = new List<int>();  //�洢����ӵ�key                    

            foreach (KeyValuePair<int, List<int>> kvp in dic)
            {
                int key = kvp.Key;
                List<int> value = kvp.Value;
                if (delKey.Contains(key))
                    continue;
                if (value.Count > 1)//������value��key���εĺ������ӻ��δ��ɣ��󲿷ֵ�count>1ѽ
                    continue;

                int temp = key;//0
                int maxIterateCount = 20;
                while (maxIterateCount > 0)//������������һ����ֱ����ֹѭ����Ҳ���ٽ���onestroke���������mainstroke
                {
                    int lastEleInValue = value[value.Count - 1];
                    if ((dic[lastEleInValue]).Count != 1)
                    {
                        foreach (var x in dic[lastEleInValue])
                        {
                            if (x != temp)
                            {
                                dic[key].Add(x);//�˴�value����Ԫ��
                                break;
                            }
                        }

                        //��Ϊ��һ������һ�����ݣ�����Ҫ-2
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

            #region  ��mainStrokes��ѡ����Ҫ������stroke��ӵ�strokes_assist��ѡȡ����ԽС��strokes_assist��Ԫ��Խ��
            int aveMainStrokeArcsNum = (int)(mainStrokeArcsSum / mainStrokes.Count);//mainStrokeƽ�������Ļ�����Ŀ
            double aveMainStrokeLength = mainStrokeArcsLength / mainStrokes.Count;//mainStrokeƽ������
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
                //1.���ȱ�����ͨ�ԽϺõ�mainStroke(�������������)������ȡƽ��һ�������������>=connectivityMore
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

                //2.�������ȴ���ƽ��ˮƽlengthMore����mainStroke
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

                //3.��������������Ŀ����ƽ��ˮƽ����containArcNumsMore����mainStroke
                if (ms.Count > aveMainStrokeArcsNum + containArcNumsMore)
                {
                    strokes_assist.Add(ms);
                    continue;
                }
            }

            //��strokes_sel��ȥ��strokes_assist�е�Ԫ��
            foreach (var assist in strokes_assist)
            {
                strokes_sel.Remove(assist);
            }

            //������������ӵ� strokes_sel
            for (int j = 0; j < varLineList.Count; j++)
            {
                if (mainStrokesArcsIndex.Contains(j))
                    continue;
                List<int> singleArc = new List<int>();//�������Σ���list����ֻ��1
                singleArc.Add(j);
                strokes_sel.Add(singleArc);
                mainStrokes.Add(singleArc);
            }

            #endregion
        }
        //����һ���㵽һ��ֱ�߶ε�ŷ�Ͼ���
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

        //������ֱ�߶�֮��ļн�
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
            else // ��ʾ�����ظ������
            {
                return 0;
            }
        }

        //��joinEdgePairs����һ��(��ڶ���)�����±���������
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

        //���µ�ͼ�����ѡ���Ҫ��
        private void updateMap()
        {
            IFeatureSelection pFeatureSelection = layer as IFeatureSelection;
            pFeatureSelection.Clear();
        }
    }
}
