using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace testTool
{
    class JoinEdgePair
    {
        public int ID;//unique ID
        public int firstEdge;//第一个弧段下标
        public int secondEdge;//第二个弧段下标
        public Node midNode;//第一弧段与第二弧段相连结点

        public bool skip;//对JoinEdgePair进行stroke连接前，筛选异常pair是，标记该pair是否跳过

        public bool merged;//标记该pair是否由合并的pairs首位弧段构成  

        public int subStrokeID;//merged为true时,其所属的subStrokeID

        public JoinEdgePair()
        {
            merged = false;
            skip = false;
        }

        public JoinEdgePair(int first, int second)
        {
            firstEdge = first;
            secondEdge = second;
            skip = false;
            merged = false;
            midNode = new Node();
        }
    }
}
