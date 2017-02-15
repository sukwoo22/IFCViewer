using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;
using System.IO;
using System.Xml;
using System.Drawing;
using System.Diagnostics;
using System.Runtime.InteropServices;
using GlmNet;
using SharpGL;
using SharpGL.Shaders;
using SharpGL.VertexBuffers;



namespace IFCViewer
{
    class IFCItem
    {
        public void  CreateItem(IFCItem parent, int ifcID, string ifcType, string globalID, string name, string desc)
        {

            this.parent = parent;
            this.next = null;
            this.child = null;
            this.globalID = globalID;
            this.ifcIDx86 = ifcID;
            this.ifcType = ifcType;
            this.description = desc;
            this.name = name;

            if (parent != null)
            {
                if (parent.child == null)
                {
                    parent.child = this;
                }
                else
                {
                    IFCItem NextChild = parent;

                    while (true)
                    {
                        if (NextChild.next == null)
                        {
                            NextChild.next = this;
                            break;
                        }
                        else
                        {
                            NextChild = NextChild.next;
                        }

                    }

                }

            }
        }

        public void CreateItem(IFCItem parent, Int64 ifcID, string ifcType, string globalID, string name, string desc)
        {

            this.parent = parent;
            this.next = null;
            this.child = null;
            this.globalID = globalID;
            this.ifcID = ifcID;
            this.ifcType = ifcType;
            this.description = desc;
            this.name = name;

            if (parent != null)
            {
                if (parent.child == null)
                {
                    parent.child = this;
                }
                else
                {
                    IFCItem NextChild = parent;

                    while (true)
                    {
                        if (NextChild.next == null)
                        {
                            NextChild.next = this;
                            break;
                        }
                        else
                        {
                            NextChild = NextChild.next;
                        }

                    }

                }

            }
        }

        public int ifcIDx86 = 0;
        public int noVerticesForFacesx86;
        public int noPrimitivesForFacesx86;
        public int vertexOffsetForFacesx86;
        public int indexOffsetForFacesx86;
        public int noVerticesForWireFramex86;
        public int noPrimitivesForWireFramex86;
        public int vertexOffsetForWireFramex86;
        public int indexOffsetForWireFramex86;

        public Int64 ifcID = 0;
        public string globalID;
        public string ifcType;
        public string name;
        public string description;
        public IFCItem parent = null;
        public IFCItem next = null;
        public IFCItem child = null;
        public Int64 noVerticesForFaces;
        public Int64 noPrimitivesForFaces;
        public float[] verticesForFaces;
        public int[] indicesForFaces;
        public Int64 vertexOffsetForFaces;
        public Int64 indexOffsetForFaces;
        public Int64 noVerticesForWireFrame;
        public Int64 noPrimitivesForWireFrame;
        public float[] verticesForWireFrame;
        public int[] indicesForWireFrame;
        public int[] indicesForWireFrameLineParts;
        public Int64 vertexOffsetForWireFrame;
        public Int64 indexOffsetForWireFrame;

        public IFCTreeItem ifcTreeItem = null;
    }

    class IFCViewerWrapper
    {

        private IFCItem _rootIfcItem = null;
        private Control _destControl = null;
        private TreeView _treeControl = null;
        public List<IFCItem> ifcItemList = new List<IFCItem>();
        public VertexBufferArray vertexBufferArray = new VertexBufferArray();
        public VertexBuffer vertexBuffer = new VertexBuffer();
        public IndexBuffer indexBuffer = new IndexBuffer();

        private static IFCViewerWrapper instance = new IFCViewerWrapper();

        private IFCViewerWrapper() { }

        public static IFCViewerWrapper Instance
        {
            get
            {
                return instance;
            }
        }

        public Camera camera = Camera.Instance;

        public bool ParseIfcFile(string sPath)
        {

            ClearMemory();

            if (true == File.Exists(sPath))
            {
                Int64 ifcModel = IfcEngine.x64.sdaiOpenModelBN(0, sPath, "IFC2X3_TC1.exp");

                string xmlSettings_IFC2x3 = @"IFC2X3-Settings.xml";
                string xmlSettings_IFC4 = @"IFC4-Settings.xml";

                if (ifcModel != 0)
                {
                   
                    IntPtr outputValue = IntPtr.Zero;

                    IfcEngine.x64.GetSPFFHeaderItem(ifcModel, 9, 0, IfcEngine.x64.sdaiUNICODE, out outputValue);

                    string s = Marshal.PtrToStringUni(outputValue);


                    XmlTextReader textReader = null;
                    if (s.Contains("IFC2") == true)
                    {
                        textReader = new XmlTextReader(xmlSettings_IFC2x3);
                    }
                    else
                    {
                        if (s.Contains("IFC4") == true)
                        {
                            IfcEngine.x64.sdaiCloseModel(ifcModel);
                            ifcModel = IfcEngine.x64.sdaiOpenModelBN(0, sPath, "IFC4.exp");

                            if (ifcModel != 0)
                                textReader = new XmlTextReader(xmlSettings_IFC4);
                        }
                    }

                    if (textReader == null)
                        return false;

                    // if node type us an attribute
                    while (textReader.Read())
                    {   
                        textReader.MoveToElement();

                        if (textReader.AttributeCount > 0)
                        {
                            if (textReader.LocalName == "object")
                            {
                                if (textReader.GetAttribute("name") != null)
                                {

                                    string Name = textReader.GetAttribute("name").ToString();

                                    RetrieveObjects(ifcModel, Name, Name);

                                }
                            }
                        }
                    }

                    int a = 0;
                    GenerateGeometry(ifcModel, _rootIfcItem, ref a);
                   

                    // -----------------------------------------------------------------
                    // Generate Tree Control
                    //_treeData.BuildTree(this, ifcModel, _rootIfcItem, _treeControl);


                    // -----------------------------------------------------------------

                    IfcEngine.x64.sdaiCloseModel(ifcModel);

                    return true;
                }
            }

            return false;
        }

#region 32비트 함수
        //private void RetrieveObjects(int ifcModel, string sObjectSPFFName, string ObjectDisplayName)
        //{
        //    int ifcObjectInstances = IfcEngine.x86.sdaiGetEntityExtentBN(ifcModel, ObjectDisplayName),
        //        noIfcObjectIntances = IfcEngine.x86.sdaiGetMemberCount(ifcObjectInstances);


        //    if (noIfcObjectIntances != 0)
        //    {

        //        for (int i = 0; i < noIfcObjectIntances; ++i)
        //        {
        //            int ifcObjectIns = 0;
        //            IfcEngine.x86.engiGetAggrElement(ifcObjectInstances, i, IfcEngine.x86.sdaiINSTANCE, out ifcObjectIns);

        //            IntPtr value = IntPtr.Zero;
        //            IfcEngine.x86.sdaiGetAttrBN(ifcObjectIns, "GlobalId", IfcEngine.x86.sdaiUNICODE, out value);

        //            string globalID = Marshal.PtrToStringUni((IntPtr)value);

        //            value = IntPtr.Zero;
        //            IfcEngine.x86.sdaiGetAttrBN(ifcObjectIns, "Name", IfcEngine.x86.sdaiUNICODE, out value);

        //            string name = Marshal.PtrToStringUni((IntPtr)value);

        //            value = IntPtr.Zero;
        //            IfcEngine.x86.sdaiGetAttrBN(ifcObjectIns, "Description", IfcEngine.x86.sdaiUNICODE, out value);

        //            string description = Marshal.PtrToStringUni((IntPtr)value);

        //            IFCItem ifcItem = new IFCItem();
        //            ifcItem.CreateItem(null, ifcObjectIns, ObjectDisplayName, globalID, name, description);

        //            ifcItemList.Add(ifcItem);

        //        }

        //    }
        //}

        //void GenerateGeometry(int ifcModel, IFCItem ifcItem, ref int a)
        //{
        //    for (var i = 0; i < ifcItemList.Count; ++i)
        //    {
        //        // -----------------------------------------------------------------
        //        // Generate WireFrames Geometry

        //        int setting = 0, mask = 0;
        //        mask += IfcEngine.x86.flagbit2;        //    PRECISION (32/64 bit)
        //        mask += IfcEngine.x86.flagbit3;        //	   INDEX ARRAY (32/64 bit)
        //        mask += IfcEngine.x86.flagbit5;        //    NORMALS
        //        mask += IfcEngine.x86.flagbit8;        //    TRIANGLES
        //        mask += IfcEngine.x86.flagbit12;       //    WIREFRAME
        //        setting += 0;		     //    DOUBLE PRECISION (double)

        //        setting += 0;            //    32 BIT INDEX ARRAY (Int32)

        //        setting += 0;            //    NORMALS OFF
        //        setting += 0;			 //    TRIANGLES OFF
        //        setting += IfcEngine.x86.flagbit12;    //    WIREFRAME ON


        //        IfcEngine.x86.setFormat(ifcModel, setting, mask);

        //        //GenerateWireFrameGeometry(ifcModel, ifcItemList[i]);
        //        // -----------------------------------------------------------------
        //        // Generate Faces Geometry

        //        setting = 0;
        //        setting += 0;		     //    SINGLE PRECISION (float)
        //        if (IntPtr.Size == 4) // indication for 32
        //        {
        //            setting += 0;            //    32 BIT INDEX ARRAY (Int32)
        //        }
        //        else
        //        {
        //            if (IntPtr.Size == 8)
        //            {
        //                setting += IfcEngine.x86.flagbit3;     //    64 BIT INDEX ARRAY (Int64)
        //            }
        //        }

        //        setting += IfcEngine.x86.flagbit5;     //    NORMALS ON
        //        setting += IfcEngine.x86.flagbit8;     //    TRIANGLES ON
        //        setting += 0;			 //    WIREFRAME OFF 
        //        IfcEngine.x86.setFormat(ifcModel, setting, mask);

        //        //GenerateFacesGeometry(ifcModel, ifcItem);

        //        IfcEngine.x86.cleanMemory(ifcModel, 0);

        //        //GenerateGeometry(ifcModel, ifcItem.child, ref a);
        //    }
        //}
#endregion

        private void RetrieveObjects(Int64 ifcModel, string sObjectSPFFName, string ObjectDisplayName)
        {
            Int64 ifcObjectInstances = IfcEngine.x64.sdaiGetEntityExtentBN(ifcModel, ObjectDisplayName),
                noIfcObjectIntances = IfcEngine.x64.sdaiGetMemberCount(ifcObjectInstances);


            if (noIfcObjectIntances != 0)
            {

                for (Int64 i = 0; i < noIfcObjectIntances; ++i)
                {
                    Int64 ifcObjectIns = 0;
                    IfcEngine.x64.engiGetAggrElement(ifcObjectInstances, i, IfcEngine.x64.sdaiINSTANCE, out ifcObjectIns);

                    IntPtr value = IntPtr.Zero;
                    IfcEngine.x64.sdaiGetAttrBN(ifcObjectIns, "GlobalId", IfcEngine.x64.sdaiUNICODE, out value);

                    string globalID = Marshal.PtrToStringUni((IntPtr)value);

                    value = IntPtr.Zero;
                    IfcEngine.x64.sdaiGetAttrBN(ifcObjectIns, "Name", IfcEngine.x64.sdaiUNICODE, out value);

                    string name = Marshal.PtrToStringUni((IntPtr)value);

                    value = IntPtr.Zero;
                    IfcEngine.x64.sdaiGetAttrBN(ifcObjectIns, "Description", IfcEngine.x64.sdaiUNICODE, out value);

                    string description = Marshal.PtrToStringUni((IntPtr)value);

                    IFCItem ifcItem = new IFCItem();
                    ifcItem.CreateItem(null, ifcObjectIns, ObjectDisplayName, globalID, name, description);

                    ifcItemList.Add(ifcItem);

                }

            }
        }

       

        private void GenerateGeometry(long ifcModel, IFCItem _rootIfcItem, ref int a)
        {
            for (var i = 0; i < ifcItemList.Count; ++i)
            {
                // -----------------------------------------------------------------
                // 기하 구조 생성
                Int64 setting = 0, mask = 0;
                mask += IfcEngine.x64.flagbit2;        //    PRECISION (32/64 bit)
                mask += IfcEngine.x64.flagbit3;        //	   INDEX ARRAY (32/64 bit)
                mask += IfcEngine.x64.flagbit5;        //    NORMALS
                mask += IfcEngine.x64.flagbit8;        //    TRIANGLES
                mask += IfcEngine.x64.flagbit12;       //    WIREFRAME
              
                setting += 0;		     //    SINGLE PRECISION (float)
                setting += IfcEngine.x64.flagbit5;     //    NORMALS ON
                setting += IfcEngine.x64.flagbit8;     //    TRIANGLES ON
                setting += 0;			 //    WIREFRAME OFF 
               IfcEngine.x64.setFormat(ifcModel, setting, mask);

                GenerateFacesGeometry(ifcModel, ifcItemList[i]);

                IfcEngine.x64.cleanMemory(ifcModel, 0);

            }
        }
       
        private void GenerateFacesGeometry(Int64 ifcModel, IFCItem ifcItem)
        {
            if (ifcItem.ifcID != 0)
            {
                Int64 noVertices = 0, noIndices = 0;
                IfcEngine.x64.initializeModellingInstance(ifcModel, ref noVertices, ref noIndices, 0, ifcItem.ifcID);

                if (noVertices != 0 && noIndices != 0)
                {

                    ifcItem.noVerticesForFaces = noVertices;
                    ifcItem.noPrimitivesForFaces = noIndices / 3;
                    ifcItem.verticesForFaces = new float[6 * noVertices];
                    ifcItem.indicesForFaces = new int[noIndices];


                    IfcEngine.x64.finalizeModelling(ifcModel, ifcItem.verticesForFaces, ifcItem.indicesForFaces, 0);
                   
                }
            }
        }

        public void ClearMemory()
        {
            ifcItemList.Clear();
            GC.Collect();
        }


        public void InitParser(Control destControl, TreeView destTreeControl)
        {
            _destControl = destControl;
            _treeControl = destTreeControl;
        }


        public void Redraw()
        {
           
        }

        public void SelectItem(IFCItem ifcItem)
        {

        }
    }
}
