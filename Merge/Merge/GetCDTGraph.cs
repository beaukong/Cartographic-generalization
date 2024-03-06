using System;
using System.Drawing;
using System.Runtime.InteropServices;
using ESRI.ArcGIS.ADF.BaseClasses;
using ESRI.ArcGIS.ADF.CATIDs;
using ESRI.ArcGIS.Controls;
using ESRI.ArcGIS.Framework;
using ESRI.ArcGIS.Carto;
using ESRI.ArcGIS.Geodatabase;
using System.Windows.Forms;
using ESRI.ArcGIS.ArcMapUI;
using System.Collections.Generic;
using ESRI.ArcGIS.Geometry;
using ESRI.ArcGIS.Display;
using System.IO;

namespace InterchangeClassification
{
    /// <summary>
    /// Command that works in ArcMap/Map/PageLayout
    /// </summary>
    [Guid("5e3fb61f-9670-49a3-8907-16b268d652d4")]
    [ClassInterface(ClassInterfaceType.None)]
    [ProgId("InterchangeClassification.GetCDTGraph")]
    public sealed class GetCDTGraph : BaseCommand
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
        private IApplication m_application;
        private IFeatureLayer selectedLayer = null;
        private IFeatureClass selectedFeatureClass = null;
        private IMap mmMap = null;

        private int densifyThreshold = 150;
        private int SDMDensifyThreshold = 10;
        private int graphSize = 200;
        private int contextWith = 4;
        private int contextHeight = 6;
        private static int featureNum = 24;
        private double radisRatio = 0.1;
        private int strengthenNum = 6;
        private int rotateAngle = 60;

        private int clustGroupIndex = 0;
        private int typeIndex = 0;
        private int cityIndex = 0;
        private int typicalIndex = 0;
        public GetCDTGraph()
        {
            //
            // TODO: Define values for the public properties
            //
            base.m_category = "GetCDTGraph"; //localizable text
            base.m_caption = "GetCDTGraph";  //localizable text 
            base.m_message = "This should work in ArcMap/MapControl/PageLayoutControl";  //localizable text
            base.m_toolTip = "GetCDTGraph";  //localizable text
            base.m_name = "GetCDTGraph";   //unique id, non-localizable (e.g. "MyCategory_MyCommand")

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

        #region Overridden Class Methods

        /// <summary>
        /// Occurs when this command is created
        /// </summary>
        /// <param name="hook">Instance of the application</param>
        public override void OnCreate(object hook)
        {
            m_application = hook as IApplication;
            if (hook == null)
                return;

            try
            {
                m_hookHelper = new HookHelperClass();
                m_hookHelper.Hook = hook;
                if (m_hookHelper.ActiveView == null)
                    m_hookHelper = null;
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
        /// Occurs when this command is clicked
        /// </summary>
        public override void OnClick()
        {
            // TODO: Add GetCDTGraph.OnClick implementation
            selectedLayer = MyApplication.App.currentLayer as IFeatureLayer;
            mmMap = (m_application.Document as IMxDocument).ActiveView.FocusMap;
            if (selectedLayer == null)
            {
                MessageBox.Show("can't find the aim layer��");
                return;
            }
            selectedFeatureClass = selectedLayer.FeatureClass;
            (m_application.Document as IMxDocument).ActiveView.Refresh();
            DialogResult dr = MessageBox.Show("start to build CDTGraph? ", "tip", MessageBoxButtons.OKCancel);

            //��ĳЩ�ֶε�index���г�ʼ������������ȡ�ֶε�ֵ
            clustGroupIndex = selectedFeatureClass.FindField("structureI");
            typeIndex = selectedFeatureClass.FindField("type");
            cityIndex = selectedFeatureClass.FindField("city");
            typicalIndex = selectedFeatureClass.FindField("typical");
            int trainSamplIndex = selectedFeatureClass.FindField("trainSampl");

            //��ʼ��ÿ����ͼ���б����������ڽӾ�����������
            for (int i = 1; i <= returnMaxIndex(selectedFeatureClass); i++)
            {
                List<int> group = new List<int>();
                IQueryFilter filter = new QueryFilterClass();
                filter.WhereClause = "structureI=" + i;
                IFeatureCursor featureCursor = selectedFeatureClass.Search(filter, false);
                IFeature feature = null;
                while ((feature = featureCursor.NextFeature()) != null)
                {
                    //�жϵ�ǰ�����Ƿ�Ϊ��������������������
                    if (Convert.ToInt16(feature.get_Value(typicalIndex)) == 1 && Convert.ToInt16(feature.get_Value(trainSamplIndex)) == 1)
                    {
                        drawpolyline(feature.ShapeCopy as IPolyline, getColor(0, 0, 255), 2);
                        int oid = feature.OID;
                        group.Add(oid);
                    }
                }
                if (group.Count > 0)
                {
                    group.Sort();
                    for (int l = 0; l < strengthenNum; l++)
                    {                  
                        //������������ת����
                        IPolylineArray polylineArr = rotateSubGraph(group, l);
                        //��ȡ��ͼ��CDT
                        TinClass tin = buildCDT(polylineArr);
                        //��ȡÿ����ͼ���ڽӾ���
                        double[,] relSubGraphMatrix = returnSubGraphRelMatrix(tin);
                        //��ȡÿ����ͼ����������
                        double[,] subFeatureMatrix = returnSubFeatureMatrix(tin, polylineArr);
                        //��ȡÿ����ͼ�����
                        int subGraphType = returnSubGraphType(group);
                        //��ȡÿ����ͼ�Ļ��ε�OID
                        String[] subGraphOID = returnSubGraphOID(group);
                        //��ȡÿ����ͼ���ڵĳ���
                        String city = Convert.ToString(selectedFeatureClass.GetFeature(group[0]).get_Value(cityIndex));
                        //��ʼ��ÿ����ͼ���ڽӾ������������Լ���ǩд�뵽txt��
                        string sign = " ";
                        string filePath = "E:\\sampleForTrain\\" + city + "SubGraph" + i + "R" + l + ".txt";
                        StreamWriter sw = new StreamWriter(filePath, false);
                        //txt�ļ�0 - graphSize��Ϊ�ڽӾ���
                        for (int j = 0; j < graphSize; j++)
                        {
                            for (int k = 0; k < graphSize; k++)
                            {
                                sw.Write(relSubGraphMatrix[j, k].ToString() + sign);
                            }
                            sw.WriteLine();
                        }
                        //txt�ļ�graphSize - graphSize * 2��Ϊ��������
                        for (int j = graphSize; j < graphSize * 2; j++)
                        {
                            for (int k = 0; k < graphSize; k++)
                            {
                                if (k < featureNum)
                                {
                                    sw.Write(Convert.ToDouble(subFeatureMatrix[j - graphSize, k]).ToString() + sign);
                                }
                                else
                                {
                                    sw.Write(Convert.ToDouble(0).ToString() + sign);
                                }
                            }
                            sw.WriteLine();
                        }
                        //txt�ļ�graphSize * 2 - graphSize * 3��Ϊÿ����ͼ��ÿ�����ε�OID
                        for (int j = graphSize * 2; j < graphSize * 3; j++)
                        {
                            for (int k = 0; k < graphSize; k++)
                            {
                                if (k == 0 && subGraphOID[j - graphSize * 2] != null)
                                {
                                    sw.Write(Convert.ToString(subGraphOID[j - graphSize * 2]).ToString() + sign);
                                }
                                else
                                {
                                    sw.Write(Convert.ToDouble(0).ToString() + sign);
                                }
                            }
                            sw.WriteLine();
                        }
                        //txt�ļ�graphSize * 3 + 1��Ϊÿ����ͼ������ǩ
                        for (int k = 0; k < graphSize; k++)
                        {
                            if (k == 0)
                            {
                                sw.Write(Convert.ToDouble(subGraphType).ToString() + sign);
                            }
                            else
                            {
                                sw.Write(Convert.ToDouble(0).ToString() + sign);
                            }
                        }
                        sw.WriteLine();

                        sw.Flush();
                        sw.Close();
                        sw.Dispose();
                    }
                }
            }        
        }

        #endregion
        //���ظ�ͼ������ͼ�ĸ���
        public int returnMaxIndex(IFeatureClass selectedFeatureClass)
        {
            int maxIndex = 0;
            IFeatureCursor cur = selectedFeatureClass.Search(null, false);
            IFeature fc = null;
            while ((fc = cur.NextFeature()) != null)
            {
                int curNum = Convert.ToInt16(fc.get_Value(clustGroupIndex));
                if (maxIndex < curNum)
                    maxIndex = curNum;
            }
            return maxIndex;
        }
        //�����ڽӾ���
        public double[,] returnSubGraphRelMatrix(TinClass tin)
        {
            double[,] subGraphRelMatrix = new double[graphSize, graphSize];
            for (int i = 0; i < graphSize; i++)
            {
                if(i < tin.NodeCount)
                {
                    ITinNode tinNode = tin.GetNode(i + 1);               
                    if (tinNode.get_IsInsideExtent(tin.Extent))
                    {
                        if (tinNode.TagValue != i - 4) { MessageBox.Show("AdjencetMatrix error!!!"); }
                        ITinNodeArray tnArr = tinNode.GetAdjacentNodes();
                        for (int j = 0; j < tnArr.Count; j++)
                        {
                            ITinNode adjencetNode = tnArr.get_Element(j);
                            //������ϰ���
                            if (adjencetNode.get_IsInsideExtent(tin.Extent) && adjencetNode.TagValue > tinNode.TagValue)
                            {
                                double distance = pointDistance(ITinNodeToIPoint(tinNode), ITinNodeToIPoint(adjencetNode));
                                if (double.IsNaN(distance) || double.IsInfinity(distance))
                                {
                                    MessageBox.Show("1111");
                                }
                                subGraphRelMatrix[tinNode.TagValue, adjencetNode.TagValue] = 1.0 / distance;
                            }                    
                        }
                    }             
                }     
            }
            //�����°���
            for (int i = 0; i < graphSize; i++)
            {
                for (int j = 0; j < i; j++)
                {
                    subGraphRelMatrix[i, j] = subGraphRelMatrix[j, i];
                }
            }
            return subGraphRelMatrix;
        }
        //������������
        public double[,] returnSubFeatureMatrix(TinClass tin, IPolylineArray polylineArr)
        {
            double[,] groupFeature = new double[graphSize, featureNum];
            for (int i = 0; i < graphSize; i++)
            {
                if (i < tin.NodeCount)
                {
                    ITinNode tinNode = tin.GetNode(i + 1);
                    if (tinNode.get_IsInsideExtent(tin.Extent))
                    {
                        if (tinNode.TagValue != i - 4) { MessageBox.Show("FeatureMatrix error!!!"); }
                        double[] contextFeatureVector = returnContextFeature(tinNode, polylineArr);
                        for (int j = 0; j < featureNum; j++)
                        {
                            groupFeature[tinNode.TagValue, j] = contextFeatureVector[j];
                        }
                    }
                }
            }         
            return groupFeature;
        }
        
        //������״����������������㣩
        private double[] returnContextFeature(ITinNode tinNode, IPolylineArray polylineArr)
        {
            IEnvelope envelope = returnEnvelopeofPolylineArr(polylineArr);
            //��״�����Ӱ뾶����ǰ��ͼ��Ӿ��ε�б�ߵ�radisRatio
            double radis = (Math.Sqrt(envelope.Height) * (envelope.Height) + (envelope.Width) * (envelope.Width)) * radisRatio;
            //����һ��contextHeight * contextWith����״�����ӵľ���
            double[,] featureMatrix = new double[contextHeight, contextWith];
            //������������ת��Ϊ���ζ���
            IPoint tinPoint = ITinNodeToIPoint(tinNode);
            //����һ���㼯�������洢������״�����ӷ�Χ�ڵĵ�
            IPointCollection pc = new MultipointClass();
            //��ʼ����ͼÿһ�����ν��б���
            for (int i = 0; i < polylineArr.Count; i++)
            {
                IPolyline polyline = polylineArr.get_Element(i);
                //�����ǰ���ε�ǰ��״�����Ӱ뾶�ཻ�������
                bool near = ((tinPoint as ITopologicalOperator).Buffer(radis) as IPolygon as IRelationalOperator).Disjoint(polyline);
                if (!near)
                {
                    polyline.Densify(SDMDensifyThreshold, 0);//�Ի��ν��м��ܣ��õ�ĸ������������γ���
                    IPointCollection jointPolylineCollection = polyline as IPointCollection;
                    for (int j = 0; j < jointPolylineCollection.PointCount; j++)
                    {
                        //�����ǰ�����ϼ��ܺ�ĵ����ڵ�ǰ��״�������ڣ������
                        bool near1 = (((tinPoint as ITopologicalOperator).Buffer(radis) as IPolygon) as IRelationalOperator).Contains(jointPolylineCollection.get_Point(j));
                        if (near1) { pc.AddPoint(jointPolylineCollection.get_Point(j)); }
                    }
                }
            }

            //IPointCollection indeticalPc = new MultipointClass();
            ////��pc���غϵĵ��Ƴ�
            //indeticalPc = removeIdenticalPoint(pc);
            //��ʼ�ж�pc�ڵĵ������Ǹ���������
            for (int i = 0; i < pc.PointCount; i++)
            {
                IPoint point = pc.get_Point(i);
                double dis = pointDistance(point, tinPoint);
                double ang = calculateAngleX(point, tinPoint);
                List<int> gridCoordinates = judgeGrid(dis, ang, radis, contextHeight, contextWith);
                int row = gridCoordinates[0];
                int col = gridCoordinates[1];
                if (row < contextHeight && col < contextWith)
                {
                    featureMatrix[row, col] += 1;
                }
            }
            //�Ծ�����й�һ��
            for (int i = 0; i < contextHeight; i++)
            {
                for (int j = 0; j < contextWith; j++)
                {
                    featureMatrix[i, j] = featureMatrix[i, j] / (pc.PointCount);
                    if (double.IsNaN(featureMatrix[i, j]) || double.IsInfinity(featureMatrix[i, j])) { MessageBox.Show("feature error!"); }
                }
            }
            //��contextHeight * contextWith�ľ���ת����һά������
            double[] contextFeatureVector = new double[contextHeight * contextWith];
            int index = 0;
            for (int i = 0; i < contextHeight; i++)
            {
                for (int j = 0; j < contextWith; j++)
                {
                    contextFeatureVector[index++] = featureMatrix[i, j];
                }
            }
            return contextFeatureVector;
        }

        //������x��нǵĽǶ� 0��360��
        public double calculateAngleX(IPoint pt, IPoint ptZ)
        {
            IPoint vec1 = new PointClass();
            vec1.X = pt.X - ptZ.X;
            vec1.Y = pt.Y - ptZ.Y;

            double angle = Math.Atan2(vec1.Y, vec1.X); //[-pi,pi]
            double theta = angle * 180 / Math.PI;
            if (theta < 0)
                theta += 360;
            return theta;
        }
        //�ж������ĸ�����
        public List<int> judgeGrid(double dis ,double ang,double R,int n1,int n2 )
        {
            List<int> outIJ =new List<int>();
            int i;//��
            int j;//��
            j = (int)(dis*n2/R);//����ȡ�� 
            i = (int)(ang*n1/360);
            outIJ.Add(i);
            outIJ.Add(j);
            return outIJ;
        }
        //����Լ��������
        private TinClass buildCDT(IPolylineArray polylineArr)
        {
            IPointCollection pc = new MultipointClass();
            IPointCollection identicalPc = new MultipointClass();
            for (int i = 0; i < polylineArr.Count; i++)
            {
                IPolyline polyline = polylineArr.get_Element(i);
                polyline.Densify(densifyThreshold, 0);
                pc.AddPointCollection(polyline as IPointCollection);
            }
            identicalPc = removeIdenticalPoint(pc);
            //��ʼtin���г�ʼ��
            TinClass tin = new TinClass();
            EnvelopeClass tinExtent = new EnvelopeClass();
            tinExtent.SpatialReference = (identicalPc as IGeometry).SpatialReference;
            tinExtent.Union((identicalPc as IGeometry).Envelope);
            tin.InitNew(tinExtent);
            tin.StartInMemoryEditing();
            tin.SetToConstrainedDelaunay();
            int tagValue = 0;
            //��ʼ�ӵ�����Լ��������
            for (int j = 0; j < identicalPc.PointCount; j++)
            {
                IPoint p1 = identicalPc.get_Point(j);
                p1.Z = 0.0;
                tin.AddPointZ(p1, tagValue++);
            }
            return tin;
        }

        //�Ƴ��㼯����ͬ�ĵ�
        private IPointCollection removeIdenticalPoint(IPointCollection pc)
        {
            Dictionary<int, bool> dic = new Dictionary<int, bool>();
            for (int i = 0; i < pc.PointCount; i++)
            {
                for (int j = i + 1; j < pc.PointCount; j++)
                {
                    bool near = PointEqual(pc.get_Point(i), pc.get_Point(j));
                    if (near && !dic.ContainsKey(j))
                    {
                        dic[j] = true;
                    }
                }
            }
            IPointCollection newPc = new MultipointClass();
            for (int i = 0; i < pc.PointCount; i++)
            {
                if (!dic.ContainsKey(i))
                {
                    newPc.AddPoint(pc.get_Point(i));
                }
            }
            return newPc;
        }

        /// �鿴�������Ƿ��ص�
        private Boolean PointEqual(IPoint p1, IPoint p2)
        {
            if ((p1.X - p2.X) * (p1.X - p2.X) + (p1.Y - p2.Y) * (p1.Y - p2.Y) < 0.001) return true;
            else return false;
        }

        private IEnvelope returnEnvelopeofPolylineArr(IPolylineArray polylineArr)
        {
            IPointCollection pc = new MultipointClass();
            for(int i = 0; i < polylineArr.Count; i++)
            {
                pc.AddPointCollection(polylineArr.get_Element(i) as IPointCollection);
            }
            return (pc as IGeometry).Envelope; 
        }

        public int returnSubGraphType(List<int> oneGroup)
        {
            Dictionary<int, int> dic = new Dictionary<int, int>();
            int res = 0;
            int count = 0;
            for (int i = 0; i < oneGroup.Count; i++)
            {
                IFeature propertyFea = selectedFeatureClass.GetFeature(oneGroup[i]);
                int type = Convert.ToInt16(propertyFea.get_Value(typeIndex));
                if (dic.ContainsKey(type))
                {
                    dic[type] = dic[type] + 1;
                }
                if (!dic.ContainsKey(type))
                {
                    dic[type] = 1;
                }
                if (dic[type] > count)
                {
                    res = type;
                    count = dic[type];
                }
            }
            return res;
        }

        private IPoint ITinNodeToIPoint(ITinNode tinNode)
        {
            IPoint point = new PointClass();
            point.X = tinNode.X;
            point.Y = tinNode.Y;
            return point;
        }

        public String[] returnSubGraphOID(List<int> oneGroup)
        {
            String[] OID = new String[graphSize];
            oneGroup.Sort();
            for (int i = 0; i < graphSize; i++)
            {
                if (i < oneGroup.Count)
                {
                    IFeature propertyFea = selectedFeatureClass.GetFeature(oneGroup[i]);
                    OID[i] = Convert.ToString(propertyFea.OID);
                }
            }
            return OID;
        }

        public double pointDistance(IPoint point1, IPoint point2)
        {
            double vector_x = point1.X - point2.X;
            double vector_y = point1.Y - point2.Y;
            double vec_len = Math.Sqrt(vector_x * vector_x + vector_y * vector_y);
            return vec_len;
        }

        // ��ת�任
        private IGeometry Rotate(IGeometry pGeometry, IPoint pPoint, double angle)
        {
            IPointCollection pPointCollection = pGeometry as IPointCollection;
            ITransform2D pTransform2D = pGeometry as ITransform2D;
            pTransform2D.Rotate(pPoint, (angle / 180) * Math.PI);
            return pTransform2D as IGeometry;
        }

        //��һ����ͼ������ת
        private IPolylineArray rotateSubGraph(List<int> group, int rotateNum)
        {
            if (group.Count == 0) { MessageBox.Show("null polyline! "); }
            IPolylineArray polylineArr =  new PolylineArrayClass();
            for (int i = 0; i < group.Count; i++)
            {
                IFeature fc = selectedFeatureClass.GetFeature(group[i]);
                IPolyline polyline = fc.ShapeCopy as IPolyline;
                polylineArr.Add(polyline);
            }
            IEnvelope envelope = returnEnvelopeofPolylineArr(polylineArr);
            //��ǰ�߼���Ӿ��ε����ĵ�
            IPoint centerPt = new PointClass();
            centerPt.X = (envelope.LowerLeft.X + envelope.UpperRight.X) / 2;
            centerPt.Y = (envelope.LowerLeft.Y + envelope.UpperRight.Y) / 2;
            //�õ���ת֮����߼���
            IPolylineArray rotatePolylineArray = new PolylineArrayClass();
            for (int i = 0; i < polylineArr.Count; i++)
            {
                IPolyline polyline = polylineArr.get_Element(i);
                ITransform2D pTransform2D = (polyline as IGeometry) as ITransform2D;
                pTransform2D.Rotate(centerPt, ((rotateAngle * rotateNum) * Math.PI) / 180);
                rotatePolylineArray.Add(pTransform2D as IPolyline);

            }
            return rotatePolylineArray;
        }

        public void drawpolyline(IPolyline polyline, IRgbColor color, double lineWidth)
        {
            IPolyline m_polyline = polyline;
            IScreenDisplay screenDisplay = (m_application.Document as IMxDocument).ActiveView.ScreenDisplay;
            ISimpleLineSymbol lineSymbol = new SimpleLineSymbolClass();
            IRgbColor rgbColor = new RgbColorClass();
            rgbColor = color;
            lineSymbol.Color = rgbColor;
            lineSymbol.Width = lineWidth;
            screenDisplay.StartDrawing(screenDisplay.hDC, (short)esriScreenCache.esriNoScreenCache);
            screenDisplay.SetSymbol((ISymbol)lineSymbol);
            screenDisplay.DrawPolyline(m_polyline);
            screenDisplay.FinishDrawing();
        }

        private IRgbColor getColor(int R, int G, int B)
        {
            IRgbColor color = new RgbColorClass();
            color.Red = R;
            color.Green = G;
            color.Blue = B;
            return color;
        }
    }
}
