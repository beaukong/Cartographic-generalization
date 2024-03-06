using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using ESRI.ArcGIS.Geometry;
namespace testTool
{
    class Node:IPoint
    {
        public List<int> joinEdges;//结点连接的边在varLineList中的下标
        public Node lastNode;//该结点的上一个结点
        public Node nextNode;//该结点的下一个结点
        //public IPoint point { get; set; }
        private double x;
        private double y;
        public Node(){ }
        public Node(double x_,double y_)
        {            
            this.X = x_;
            this.Y = y_;
            //this.X = x;
            //this.Y = y;
            joinEdges = new List<int>();
        }

        public int Compare(IPoint otherPoint)
        {
            throw new NotImplementedException();
        }

        public void ConstrainAngle(double constraintAngle, IPoint anchor, bool allowOpposite)
        {
            throw new NotImplementedException();
        }

        public void ConstrainDistance(double constraintRadius, IPoint anchor)
        {
            throw new NotImplementedException();
        }

        public esriGeometryDimension Dimension
        {
            get { throw new NotImplementedException(); }
        }

        public IEnvelope Envelope
        {
            get { throw new NotImplementedException(); }
        }

        public void GeoNormalize()
        {
            throw new NotImplementedException();
        }

        public void GeoNormalizeFromLongitude(double Longitude)
        {
            throw new NotImplementedException();
        }

        public esriGeometryType GeometryType
        {
            get { throw new NotImplementedException(); }
        }

        public int ID
        {
            get
            {
                throw new NotImplementedException();
            }
            set
            {
                throw new NotImplementedException();
            }
        }

        public bool IsEmpty
        {
            get { throw new NotImplementedException(); }
        }

        public double M
        {
            get
            {
                throw new NotImplementedException();
            }
            set
            {
                throw new NotImplementedException();
            }
        }

        public void Project(ISpatialReference newReferenceSystem)
        {
            throw new NotImplementedException();
        }

        public void PutCoords(double X, double Y)
        {
            throw new NotImplementedException();
        }

        public void QueryCoords(out double X, out double Y)
        {
            throw new NotImplementedException();
        }

        public void QueryEnvelope(IEnvelope outEnvelope)
        {
            throw new NotImplementedException();
        }

        public void SetEmpty()
        {
            throw new NotImplementedException();
        }

        public void SnapToSpatialReference()
        {
            throw new NotImplementedException();
        }

        public ISpatialReference SpatialReference
        {
            get
            {
                throw new NotImplementedException();
            }
            set
            {
                throw new NotImplementedException();
            }
        }

        public double X
        {
            get
            {
                 return this.x;
            }
            set
            {
                this.x = value;
            }
        }

        public double Y
        {
            get
            {
                return this.y;
            }
            set
            {
                this.y = value;
            }
        }

        public double Z
        {
            get
            {
                throw new NotImplementedException();
            }
            set
            {
                throw new NotImplementedException();
            }
        }

        public double get_VertexAttribute(esriGeometryAttributes attributeType)
        {
            throw new NotImplementedException();
        }

        public void set_VertexAttribute(esriGeometryAttributes attributeType, double attributeValue)
        {
            throw new NotImplementedException();
        }
    }
}
