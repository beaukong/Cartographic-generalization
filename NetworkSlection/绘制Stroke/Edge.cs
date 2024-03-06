using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using ESRI.ArcGIS.Geometry;
namespace testTool
{
    class Edge//:PolylineClass
    {
        public int index;//在varLineList中的下标
        public int whichSubStroke;//在哪个subStrokes中        
        public int loc;//在subStroke中的位置（0 首位，1 尾位，2 中间）
        public Node formerNode;//该弧段的前一结点
        public Node laterNode;
        public int formerEdge;//前一弧段
        public int laterEdge;//后一弧段

        public Edge() { }
        public Edge(int index)
        {
            this.index = index;
        }
        public Edge(int index, int whichSubStroke, int loc)
        {
            this.index = index;
            this.whichSubStroke = whichSubStroke;
            this.loc = loc;
        }
    }
}
