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
    struct Material
    {

        public vec3 ambient;
        public vec3 diffuse;
        public vec3 specular;
        public vec3 emissive;
        public float transparency;
        public float power;
               
    }

    class IFCItem
    {
       
        public void CreateItem(Int64 ifcID, string ifcType, string globalID, string name, string desc)
        {

            this.next = null;
            this.child = null;
            this.globalID = globalID;
            this.ifcID = ifcID;
            this.ifcType = ifcType;
            this.description = desc;
            this.name = name;
            this.isActiveMaterial = false;
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
        public bool isActiveMaterial;
        public Material material; 

        public IFCTreeItem ifcTreeItem = null;
    }

    class IFCViewerWrapper
    {

        private IFCItem _rootIfcItem = null;
        private Control _destControl = null;
        private TreeView _treeControl = null;
        private Int64 ifcModel = 0;
        private Int64 ifcItemCount = 0;
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
                ifcModel = IfcEngine.x64.sdaiOpenModelBN(0, sPath, "IFC2X3_TC1.exp");



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
                    GenerateGeometry(ifcModel, a);

                    // -----------------------------------------------------------------
                    // Generate Tree Control
                    //_treeData.BuildTree(this, ifcModel, _rootIfcItem, _treeControl);


                    // -----------------------------------------------------------------

                    IfcEngine.x64.sdaiCloseModel(ifcModel);
                    ifcModel = 0;

                    return true;
                }
            }

            return false;
        }

        public bool AppendFile(String sPath)
        {

            if (true == File.Exists(sPath))
            {
                int startIndex = ifcItemList.Count;


                ifcModel = IfcEngine.x64.sdaiOpenModelBN(0, sPath, "IFC2X3_TC1.exp");

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

                    int a = startIndex;
                    GenerateGeometry(ifcModel, a);

                    // -----------------------------------------------------------------
                    // Generate Tree Control
                    //_treeData.BuildTree(this, ifcModel, _rootIfcItem, _treeControl);


                    // -----------------------------------------------------------------

                    IfcEngine.x64.sdaiCloseModel(ifcModel);
                    ifcModel = 0;

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
                    ifcItem.CreateItem(ifcObjectIns, ObjectDisplayName, globalID, name, description);

                    ifcItemList.Add(ifcItem);

                }

            }
        }

       

        private void GenerateGeometry(long ifcModel, int startIndex)
        {
            for (var i = startIndex; i < ifcItemList.Count; ++i)
            {
                // -----------------------------------------------------------------
                // 기하 구조 생성
                Int64 setting = 0, mask = 0;
                mask += IfcEngine.x64.flagbit2;        //    PRECISION (32/64 bit)
                mask += IfcEngine.x64.flagbit3;        //	   INDEX ARRAY (32/64 bit)
                mask += IfcEngine.x64.flagbit5;        //    NORMALS
                mask += IfcEngine.x64.flagbit8;        //    TRIANGLES
                mask += IfcEngine.x64.flagbit12;       //    WIREFRAME
              
                setting += 0;		                   //    SINGLE PRECISION (float)
                setting += 0;                          //    32 BIT INDEX ARRAY (Int32)
                setting += IfcEngine.x64.flagbit5;     //    NORMALS ON
                setting += IfcEngine.x64.flagbit8;     //    TRIANGLES ON
                setting += 0;			               //    WIREFRAME OFF 
                IfcEngine.x64.setFormat(ifcModel, setting, mask);

                GenerateFacesGeometry(ifcModel, ifcItemList[i]);

                CreateMaterial(ifcItemList[i]);

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

        
        private void CreateMaterial(IFCItem item)
        {
            if (item.noVerticesForFaces != 0)
            {
                // C++ => getRGB_object()
                IntPtr representationInstance;
                IfcEngine.x64.sdaiGetAttrBN(item.ifcID, "Representation", IfcEngine.x64.sdaiINSTANCE, out representationInstance);
                if (representationInstance == IntPtr.Zero)
                {
                    return;
                }

                // C++ => getRGB_productDefinitionShape()
                IntPtr representationsInstance;
                IfcEngine.x64.sdaiGetAttrBN(representationInstance.ToInt64(), "Representations", IfcEngine.x64.sdaiAGGR, out representationsInstance);

                Int64 iRepresentationsCount = IfcEngine.x64.sdaiGetMemberCount(representationsInstance.ToInt64());
                for (Int64 iRepresentation = 0; iRepresentation < iRepresentationsCount; iRepresentation++)
                {
                    Int64 iShapeInstance = 0;
                    IfcEngine.x64.engiGetAggrElement(representationsInstance.ToInt64(), iRepresentation, IfcEngine.x64.sdaiINSTANCE, out iShapeInstance);

                    if (iShapeInstance == 0)
                    {
                        continue;
                    }

                    // C++ => getRGB_shapeRepresentation()
                    IntPtr representationIdentifier, representationType;
                    IfcEngine.x64.sdaiGetAttrBN(iShapeInstance, "RepresentationIdentifier", IfcEngine.x64.sdaiSTRING, out representationIdentifier);
                    IfcEngine.x64.sdaiGetAttrBN(iShapeInstance, "RepresentationType", IfcEngine.x64.sdaiSTRING, out representationType);

                    string represenIdentifier = Marshal.PtrToStringAnsi(representationIdentifier);
                    string represenType = Marshal.PtrToStringAnsi(representationType);

                    if (represenIdentifier == "Body" || represenIdentifier == "Mesh" || represenType != "BoundingBox") 
                    {
                        IntPtr itemsInstance;
                        IfcEngine.x64.sdaiGetAttrBN(iShapeInstance, "Items", IfcEngine.x64.sdaiAGGR, out itemsInstance);

                        long temp;
                        IfcEngine.x64.sdaiGetAttrBN(iShapeInstance, "Items", IfcEngine.x64.sdaiAGGR, out temp);


                        Int64 iItemsCount = IfcEngine.x64.sdaiGetMemberCount(itemsInstance.ToInt64());
                        long check = itemsInstance.ToInt64();
                        int check2 = itemsInstance.ToInt32();

                        long kc = IfcEngine.x64.sdaiGetMemberCount(temp);
                        for (Int64 iItem = 0; iItem < iItemsCount; iItem++)
                        {
                            Int64 iItemInstance = 0;
                            IfcEngine.x64.engiGetAggrElement(itemsInstance.ToInt64(), iItem, IfcEngine.x64.sdaiINSTANCE, out iItemInstance);

                            IntPtr styledByItem;
                            IfcEngine.x64.sdaiGetAttrBN(iItemInstance, "StyledByItem", IfcEngine.x64.sdaiINSTANCE, out styledByItem);

                            if (styledByItem != IntPtr.Zero)
                            {
                                getRGB_styledItem(item, styledByItem.ToInt64());
                            }
                            else
                            {
                                searchDeeper(item, iItemInstance);
                            } // else if (iItemInstance != 0)

                            if (item.isActiveMaterial)
                            {
                                return;
                            }
                        } // for (int iItem = ...
                    }
                } // for (int iRepresentation = ...

                if(item.isActiveMaterial == false)
                {
                    item.material = new Material();
                    item.material.ambient = item.material.diffuse = item.material.specular = new vec3(0.8f, 0.8f, 0.8f);
                    item.material.transparency = 1.0f;
                }
            }
        }


        private void searchDeeper(IFCItem item, Int64 iParentInstance)
        {
            IntPtr styledByItem;
            IfcEngine.x64.sdaiGetAttrBN(iParentInstance, "StyledByItem", IfcEngine.x64.sdaiINSTANCE, out styledByItem);

            if (styledByItem != IntPtr.Zero)
            {
                getRGB_styledItem(item, styledByItem.ToInt64());
                if (item.isActiveMaterial)
                {
                    return;
                }
            }

            if (IsInstanceOf(iParentInstance, "IFCBOOLEANCLIPPINGRESULT"))
            {
                IntPtr firstOperand;
                IfcEngine.x64.sdaiGetAttrBN(iParentInstance, "FirstOperand", IfcEngine.x64.sdaiINSTANCE, out firstOperand);

                if (firstOperand != IntPtr.Zero)
                {
                    searchDeeper(item, firstOperand.ToInt64());
                }
            } // if (IsInstanceOf(iParentInstance, "IFCBOOLEANCLIPPINGRESULT"))
            else
            {
                if (IsInstanceOf(iParentInstance, "IFCMAPPEDITEM"))
                {
                    IntPtr mappingSource;
                    IfcEngine.x64.sdaiGetAttrBN(iParentInstance, "MappingSource", IfcEngine.x64.sdaiINSTANCE, out mappingSource);

                    IntPtr mappedRepresentation;
                    IfcEngine.x64.sdaiGetAttrBN(mappingSource.ToInt64(), "MappedRepresentation", IfcEngine.x64.sdaiINSTANCE, out mappedRepresentation);

                    if (mappedRepresentation != IntPtr.Zero)
                    {
                        IntPtr representationIdentifier;
                        IfcEngine.x64.sdaiGetAttrBN(mappedRepresentation.ToInt64(), "RepresentationIdentifier", IfcEngine.x64.sdaiSTRING, out representationIdentifier);

                        if (Marshal.PtrToStringAnsi(representationIdentifier) == "Body")
                        {
                            IntPtr itemsInstance;
                            IfcEngine.x64.sdaiGetAttrBN(mappedRepresentation.ToInt64(), "Items", IfcEngine.x64.sdaiAGGR, out itemsInstance);

                            Int64 iItemsCount = IfcEngine.x64.sdaiGetMemberCount(itemsInstance.ToInt64());
                            for (Int64 iItem = 0; iItem < iItemsCount; iItem++)
                            {
                                Int64 iItemInstance = 0;
                                IfcEngine.x64.engiGetAggrElement(itemsInstance.ToInt64(), iItem, IfcEngine.x64.sdaiINSTANCE, out iItemInstance);

                                styledByItem = IntPtr.Zero;
                                IfcEngine.x64.sdaiGetAttrBN(iItemInstance, "StyledByItem", IfcEngine.x64.sdaiINSTANCE, out styledByItem);

                                if (styledByItem != IntPtr.Zero)
                                {
                                    getRGB_styledItem(item, styledByItem.ToInt64());
                                }
                                else
                                {
                                    searchDeeper(item, iItemInstance);
                                } // else if (iItemInstance != 0)

                                if (item.isActiveMaterial)
                                {
                                    return;
                                }
                            } // for (int iItem = ...
                        } // if (Marshal.PtrToStringAnsi(representationIdentifier) == "Body")
                    } // if (mappedRepresentation != IntPtr.Zero)
                } // if (IsInstanceOf(iParentInstance, "IFCMAPPEDITEM"))
            } // else if (IsInstanceOf(iParentInstance, "IFCBOOLEANCLIPPINGRESULT"))
        }

        private bool IsInstanceOf(Int64 iInstance, string strType)
        {
            if (IfcEngine.x64.sdaiGetInstanceType(iInstance) == IfcEngine.x64.sdaiGetEntity(ifcModel, strType))
            {
                return true;
            }

            return false;
        }        


        private void getRGB_styledItem(IFCItem item, Int64 iStyledByItemInstance)
        {
            IntPtr stylesInstance;
            IfcEngine.x64.sdaiGetAttrBN(iStyledByItemInstance, "Styles", IfcEngine.x64.sdaiAGGR, out stylesInstance);

            Int64 iStylesCount = IfcEngine.x64.sdaiGetMemberCount(stylesInstance.ToInt64());
            for (Int64 iStyle = 0; iStyle < iStylesCount; iStyle++)
            {
                Int64 iStyleInstance = 0;
                IfcEngine.x64.engiGetAggrElement(stylesInstance.ToInt64(), iStyle, IfcEngine.x64.sdaiINSTANCE, out iStyleInstance);

                if (iStyleInstance == 0)
                {
                    continue;
                }

                getRGB_presentationStyleAssignment(item, iStyleInstance);
            } // for (int iStyle = ...
        }


        private void getRGB_presentationStyleAssignment(IFCItem item, Int64 iParentInstance)
        {
            IntPtr stylesInstance;
            IfcEngine.x64.sdaiGetAttrBN(iParentInstance, "Styles", IfcEngine.x64.sdaiAGGR, out stylesInstance);

            Int64 iStylesCount = IfcEngine.x64.sdaiGetMemberCount(stylesInstance.ToInt64());
            for (Int64 iStyle = 0; iStyle < iStylesCount; iStyle++)
            {
                Int64 iStyleInstance = 0;
                IfcEngine.x64.engiGetAggrElement(stylesInstance.ToInt64(), iStyle, IfcEngine.x64.sdaiINSTANCE, out iStyleInstance);

                if (iStyleInstance == 0)
                {
                    continue;
                }

                getRGB_surfaceStyle(item, iStyleInstance);
            } // for (int iStyle = ...
        }

        private unsafe void getRGB_surfaceStyle(IFCItem item, Int64 iParentInstance)
        {
            IntPtr stylesInstance;
            IfcEngine.x64.sdaiGetAttrBN(iParentInstance, "Styles", IfcEngine.x64.sdaiAGGR, out stylesInstance);

            Int64 iStylesCount = IfcEngine.x64.sdaiGetMemberCount(stylesInstance.ToInt64());
            for (Int64 iStyle = 0; iStyle < iStylesCount; iStyle++)
            {
                Int64 iStyleInstance = 0;
                IfcEngine.x64.engiGetAggrElement(stylesInstance.ToInt64(), iStyle, IfcEngine.x64.sdaiINSTANCE, out iStyleInstance);

                if (iStyleInstance == 0)
                {
                    continue;
                }

                double transparency = 0;
                IfcEngine.x64.sdaiGetAttrBN(iStyleInstance, "Transparency", IfcEngine.x64.sdaiREAL, out transparency);
                
                IntPtr surfaceColour;
                IfcEngine.x64.sdaiGetAttrBN(iStyleInstance, "SurfaceColour", IfcEngine.x64.sdaiINSTANCE, out surfaceColour);

                if (surfaceColour == IntPtr.Zero)
                {
                    continue;
                }

                double R = 0;
                IfcEngine.x64.sdaiGetAttrBN(surfaceColour.ToInt64(), "Red", IfcEngine.x64.sdaiREAL, out *(IntPtr*)&R);

                double G = 0;
                IfcEngine.x64.sdaiGetAttrBN(surfaceColour.ToInt64(), "Green", IfcEngine.x64.sdaiREAL, out *(IntPtr*)&G);

                double B = 0;
                IfcEngine.x64.sdaiGetAttrBN(surfaceColour.ToInt64(), "Blue", IfcEngine.x64.sdaiREAL, out *(IntPtr*)&B);

                item.material = new Material();
                item.material.transparency = 1 - (float)transparency;
                item.isActiveMaterial = true;
                item.material.ambient = item.material.diffuse = item.material.specular = new vec3((float)R, (float)G, (float)B);
                item.material.emissive = new vec3((float)R * 0.5f, (float)G * 0.5f, (float)B * 0.5f);

                return;
            } // for (int iStyle = ...
        }
    }
}
