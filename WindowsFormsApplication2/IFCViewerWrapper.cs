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
    class Material
    {
        public Int64 materialID;
        public vec3 ambient;
        public vec3 diffuse;
        public vec3 specular;
        public vec3 emissive;
        public float transparency;
        public float power;
        public bool active;
        public long indexArrayOffset;
        public long indexArrayPrimitives;
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
            this.materialList = new List<Material>();
            this.itemInstances = new List<Int64>();
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
        public Int64 noIndicesForFaces;
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
        public Material material;
        public List<Material> materialList;
        public List<Int64> itemInstances;

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
                    ifcItem.noIndicesForFaces = noIndices;
                    ifcItem.verticesForFaces = new float[6 * noVertices];
                    ifcItem.indicesForFaces = new int[noIndices];

                    IfcEngine.x64.finalizeModelling(ifcModel, ifcItem.verticesForFaces, ifcItem.indicesForFaces, 0);

                    CreateMaterial(ifcItem);


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
                    getRGB_shapeRepresentation(iShapeInstance, item);
                } // for (int iRepresentation = ...

                // 재질이 없는 경우 -> 기본 재질을 사용
                if (item.materialList.Count == 0)
                {
                    Material material = new Material();
                    material.ambient = material.diffuse = material.specular = new vec3(0.8f, 0.8f, 0.8f);
                    material.transparency = 1.0f;

                    Int64 vertexBufferSize = 0, indexBufferSize = 0, transformationBufferSize = 0;
                    IfcEngine.x64.CalculateInstance(item.ifcID, out vertexBufferSize, out indexBufferSize, out transformationBufferSize);
                    material.indexArrayOffset = 0;
                    material.indexArrayPrimitives = indexBufferSize / 3;
                    item.materialList.Add(material);
                }

                // 재질이 하나만 있는 경우
                else if (item.materialList.Count == 1)
                {
                    Int64 vertexBufferSize = 0, indexBufferSize = 0, transformationBufferSize = 0;
                    IfcEngine.x64.CalculateInstance(item.ifcID, out vertexBufferSize, out indexBufferSize, out transformationBufferSize);
                    item.materialList[0].indexArrayOffset = 0;
                    item.materialList[0].indexArrayPrimitives = indexBufferSize / 3;
                }

                // 재질이 하나 이상일 경우
                else
                {
                    walkThroughGeometryTransformation(item);
                }

            }
        }

        private void getRGB_shapeRepresentation(Int64 ifcShapeRepresentationInstance, IFCItem item)
        {
            IntPtr representationIdentifier, representationType;
            IfcEngine.x64.sdaiGetAttrBN(ifcShapeRepresentationInstance, "RepresentationIdentifier", IfcEngine.x64.sdaiSTRING, out representationIdentifier);
            IfcEngine.x64.sdaiGetAttrBN(ifcShapeRepresentationInstance, "RepresentationType", IfcEngine.x64.sdaiSTRING, out representationType);

            string represenIdentifier = Marshal.PtrToStringAnsi(representationIdentifier);
            string represenType = Marshal.PtrToStringAnsi(representationType);

            if (represenIdentifier == "Body" || represenIdentifier == "Mesh" || represenType != "BoundingBox")
            {
                IntPtr itemsInstance;
                IfcEngine.x64.sdaiGetAttrBN(ifcShapeRepresentationInstance, "Items", IfcEngine.x64.sdaiAGGR, out itemsInstance);

                Int64 iItemsCount = IfcEngine.x64.sdaiGetMemberCount(itemsInstance.ToInt64());

                for (Int64 iItem = 0; iItem < iItemsCount; iItem++)
                {
                    Int64 iItemInstance = 0;
                    IfcEngine.x64.engiGetAggrElement(itemsInstance.ToInt64(), iItem, IfcEngine.x64.sdaiINSTANCE, out iItemInstance);

                    IntPtr styledByItem;
                    IfcEngine.x64.sdaiGetAttrBN(iItemInstance, "StyledByItem", IfcEngine.x64.sdaiINSTANCE, out styledByItem);

                    if (styledByItem != IntPtr.Zero)
                    {
                        getRGB_styledItem(item, styledByItem.ToInt64(), iItemInstance);
                    }
                    else
                    {
                        searchDeeper(item, iItemInstance);
                        item.itemInstances.Add(IfcEngine.x64.internalGetP21Line(iItemInstance));
                    } // else if (iItemInstance != 0)

                } // for (int iItem = ...
            }
        }

        private void searchDeeper(IFCItem item, Int64 iParentInstance)
        {
            IntPtr styledByItem;
            IfcEngine.x64.sdaiGetAttrBN(iParentInstance, "StyledByItem", IfcEngine.x64.sdaiINSTANCE, out styledByItem);

            if (styledByItem != IntPtr.Zero)
            {
                if (getRGB_styledItem(item, styledByItem.ToInt64(), iParentInstance))
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
                        getRGB_shapeRepresentation(mappedRepresentation.ToInt64(), item);
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


        private bool getRGB_styledItem(IFCItem item, Int64 iStyledByItemInstance, Int64 geometryInstance)
        {
            IntPtr stylesInstance;
            IfcEngine.x64.sdaiGetAttrBN(iStyledByItemInstance, "Styles", IfcEngine.x64.sdaiAGGR, out stylesInstance);

            Int64 iStylesCount = IfcEngine.x64.sdaiGetMemberCount(stylesInstance.ToInt64());

            Int64 prevCount = item.materialList.Count;

            bool isCreatingMaterial = false;

            for (Int64 iStyle = 0; iStyle < iStylesCount; iStyle++)
            {
                Int64 iStyleInstance = 0;
                IfcEngine.x64.engiGetAggrElement(stylesInstance.ToInt64(), iStyle, IfcEngine.x64.sdaiINSTANCE, out iStyleInstance);

                if (iStyleInstance == 0)
                {
                    continue;
                }

                getRGB_presentationStyleAssignment(item, iStyleInstance, geometryInstance);

            } // for (int iStyle = ...

            if (item.materialList.Count > prevCount)
            {
                isCreatingMaterial = true;
            }

            return isCreatingMaterial;
        }


        private void getRGB_presentationStyleAssignment(IFCItem item, Int64 iParentInstance, Int64 geometryInstance)
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

                getRGB_surfaceStyle(item, iStyleInstance, geometryInstance);
            } // for (int iStyle = ...

        }

        private unsafe void getRGB_surfaceStyle(IFCItem item, Int64 iParentInstance, Int64 geometryInstance)
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

                Material material = new Material();
                material.materialID = IfcEngine.x64.internalGetP21Line(geometryInstance);
                material.transparency = 1 - (float)transparency;
                material.ambient = material.diffuse = material.specular = new vec3((float)R, (float)G, (float)B);
                material.emissive = new vec3((float)R * 0.5f, (float)G * 0.5f, (float)B * 0.5f);

                item.materialList.Add(material);


            } // for (int iStyle = ...

        }

        private unsafe void walkThroughGeometryTransformation(IFCItem item)
        {

            Int64 rdfClassTransformation = IfcEngine.x64.GetClassByName(ifcModel, "Transformation");
            Int64 instanceClass = IfcEngine.x64.GetInstanceClass(item.ifcID);
            Int64 owlObjectTypePropertyObject = IfcEngine.x64.GetPropertyByName(ifcModel, "object");
            Int64 objectCard = 0;
            Int64* owlInstanceObject = null;

            IfcEngine.x64.GetObjectTypeProperty(item.ifcID, owlObjectTypePropertyObject, &owlInstanceObject, &objectCard);

            if (objectCard == 1)
            {
                walkThroughGeometryCollection(owlInstanceObject[0], item);
            }

        }

        private unsafe void walkThroughGeometryCollection(Int64 owlInstance, IFCItem item)
        {
            Int64 rdfClassCollection = IfcEngine.x64.GetClassByName(ifcModel, "Collection");
            Int64 owlObjectTypePropertyObjects = IfcEngine.x64.GetPropertyByName(ifcModel, "objects");

            if (IfcEngine.x64.GetInstanceClass(owlInstance) == rdfClassCollection)
            {
                Int64* owlInstanceObjects = null;
                Int64 objectsCard = 0;
                IfcEngine.x64.GetObjectTypeProperty(owlInstance, owlObjectTypePropertyObjects, &owlInstanceObjects, &objectsCard);
                for (Int64 i = 0; i < objectsCard; ++i)
                {
                    walkThroughGeometryObject(owlInstanceObjects[i], item);
                }
            }
            else
            {
                walkThroughGeometryObject(owlInstance, item);
            }
        }

        private unsafe void walkThroughGeometryObject(Int64 owlInstance, IFCItem item)
        {

            Int64* owlInstanceExpressID = null;
            Int64 expressIDCard = 0;

            Int64 owlDataTypePropertyExpressID = IfcEngine.x64.GetPropertyByName(ifcModel, "expressID");
            IfcEngine.x64.GetDataTypeProperty(owlInstance, owlDataTypePropertyExpressID, &owlInstanceExpressID, &expressIDCard);
            if (expressIDCard == 1)
            {
                Int64 expressID = owlInstanceExpressID[0];
                
                int i = 0;

                for (i = 0; i < item.itemInstances.Count; ++i)
                {
                    if( item.itemInstances[i] ==  expressID )
                    {
                        break;
                    }
                        
        
                }

                // 하위 객체가 있을 경우
                if( i < item.itemInstances.Count)
                {
                    findMaterialInstance(owlInstance, item);                   
                }
                // 자신이 하위 객체일 경우
                {
                    Int64 vertexBufferSize = 0, indexBufferSize = 0, transformationBufferSize, offset = 0;
                    IfcEngine.x64.CalculateInstance(owlInstance, out vertexBufferSize, out indexBufferSize, out transformationBufferSize);
                    
                    for(int j =0; j< item.materialList.Count; ++j)
                    {
                        if (item.materialList[j].active == true)
                        {
                            offset = item.materialList[j].indexArrayOffset + item.materialList[j].indexArrayPrimitives * 3;
                            continue;
                        }
                        item.materialList[j].active = true;
                        item.materialList[j].indexArrayOffset = offset;
                        item.materialList[j].indexArrayPrimitives = indexBufferSize / 3;
                        break;

                    }
                }
            }
            else
            {
                findMaterialInstance(owlInstance, item);
            }

        }


        private unsafe void findMaterialInstance(Int64 owlInstance, IFCItem item)
        {
            Int64 rdfClassTransformation = IfcEngine.x64.GetClassByName(ifcModel, "Transformation");
            Int64 rdfClassCollection = IfcEngine.x64.GetClassByName(ifcModel, "Collection");
            if (IfcEngine.x64.GetInstanceClass(owlInstance) == rdfClassTransformation)
            {
                Int64* owlInstanceObject = null;
                Int64 objectCard = 0;
                Int64 owlObjectTypePropertyObject = IfcEngine.x64.GetPropertyByName(ifcModel, "object");

                IfcEngine.x64.GetObjectTypeProperty(owlInstance, owlObjectTypePropertyObject, &owlInstanceObject, &objectCard);
                if (objectCard == 1)
                {
                    walkThroughGeometryObject(owlInstanceObject[0], item);
                }
                else
                {
                    Debug.Assert(objectCard != 1, "객체 개수가 한개 이상임");
                }
            }
            else if (IfcEngine.x64.GetInstanceClass(owlInstance) == rdfClassCollection)
            {
                Int64* owlInstanceObjects = null;
                Int64 objectsCard = 0;
                Int64 owlObjectTypePropertyObjects = IfcEngine.x64.GetPropertyByName(ifcModel, "objects");
                IfcEngine.x64.GetObjectTypeProperty(owlInstance, owlObjectTypePropertyObjects, &owlInstanceObjects, &objectsCard);
                for (Int64 j = 0; j < objectsCard; ++j)
                {
                    walkThroughGeometryObject(owlInstanceObjects[j], item);
                }
            }
        }

    }
}
