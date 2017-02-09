using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using System.Drawing;

namespace IFCViewer
{
    /// <summary>
    /// Describe the color of IFCItem.
    /// </summary>
    class IFCItemColor
    {
        public float R = 0;
        public float G = 0;
        public float B = 0;
        public float A = 0;
    }

    /// <summary>
    /// Describes an item in the tree.
    /// </summary>
    class IFCTreeItem
    {
        /// <summary>
        /// Instance.
        /// </summary>
        public Int64 instance = -1;

        /// <summary>
        /// Node.
        /// </summary>
        public TreeNode treeNode = null;

        /// <summary>
        /// If it is not null the item can be selected.
        /// </summary>
        public IFCItem ifcItem = null;

        /// <summary>
        /// Color
        /// </summary>
        public IFCItemColor ifcColor = null;

        /// <summary>
        /// Getter
        /// </summary>
        public bool IsVisible
        {
            get
            {
                System.Diagnostics.Debug.Assert(treeNode != null, "Internal error.");

                if (treeNode.ImageIndex == CIFCTreeData.IMAGE_CHECKED)
                {
                    return true;
                }

                return false;
            }
        }
    }

    /// <summary>
    /// Generates entire IFC tree. 
    /// - Initiate control by retrieving data from IFC library and transmitting it to C# Tree control.
    ///    
    ///     - IFCProject Items
    ///         - Tree Item
    ///         - Check Box
    ///     - Not-referenced in structure 
    /// - Keeps bidirectional relationship IFCElementID <-> TreeItem
    ///     - OnSelect Tree Item -> Mark IFC element
    ///     - OnMark IFC Element -> Select Tree Item
    /// - Build Context Menu functionality
    /// </summary>
    class CIFCTreeData
    {
        /// <summary>
        /// Viewer
        /// </summary>
        IFCViewerWrapper _ifcViewer = null;

        /// <summary>
        /// Model
        /// </summary>
        Int64 _ifcModel = 0;

        /// <summary>
        /// Root of IFCItem-s
        /// </summary>
        IFCItem _ifcRoot = null;

        /// <summary>
        /// Tree control
        /// </summary>
        TreeView _treeControl = null;

        /// <summary>
        /// Contains info for the context menu.
        /// </summary>
        Dictionary<string, bool> _dicCheckedElements = new Dictionary<string, bool>();

        /// <summary>
        /// Zero-based indices of the images inside the image list.
        /// </summary>
        public const int IMAGE_CHECKED = 0;
        public const int IMAGE_UNCHECKED = 2;
        public const int IMAGE_PROPERTY_SET = 3;
        public const int IMAGE_PROPERTY = 4;
        public const int IMAGE_NOT_REFERENCED = 5;

        /// <summary>
        /// - Generates IFCProject-related items
        /// - Generates Not-referenced-in-structure items
        /// - Generates Header info
        /// - Generates check box per items
        /// </summary>
        public void BuildTree(IFCViewerWrapper ifcViewer, Int64 ifcModel, IFCItem ifcRoot, TreeView treeControl)
        {
            treeControl.Nodes.Clear();

            if (ifcViewer == null)
            {
                throw new ArgumentException("The viewer is null.");
            }

            if (ifcModel <= 0)
            {
                throw new ArgumentException("Invalid model.");
            }

            if (ifcRoot == null)
            {
                throw new ArgumentException("The root is null.");
            }

            if (treeControl == null)
            {
                throw new ArgumentException("The tree control is null.");
            }

            Cursor.Current = Cursors.WaitCursor;

            _ifcViewer = ifcViewer;
            _ifcModel = ifcModel;
            _ifcRoot = ifcRoot;
            _treeControl = treeControl;

            _dicCheckedElements.Clear();

            CreateHeaderTreeItems();
            CreateProjectTreeItems();
            CreateNotReferencedTreeItems();
        }

        /// <summary>
        /// Helper
        /// </summary>
        private void CreateHeaderTreeItems()
        {
            // Header info
            TreeNode tnHeaderInfo = _treeControl.Nodes.Add("Header Info");
            tnHeaderInfo.ImageIndex = tnHeaderInfo.SelectedImageIndex = IMAGE_PROPERTY_SET;

            // Descriptions
            TreeNode tnDescriptions = tnHeaderInfo.Nodes.Add("Descriptions");
            tnDescriptions.ImageIndex = tnDescriptions.SelectedImageIndex = IMAGE_PROPERTY;

            int i = 0;
            IntPtr description;
            while (IfcEngine.x64.GetSPFFHeaderItem(_ifcModel, 0, i++, IfcEngine.x64.sdaiUNICODE, out description) == 0)
            {
                TreeNode tnDescription = tnDescriptions.Nodes.Add(Marshal.PtrToStringAnsi(description));
                tnDescription.ImageIndex = tnDescription.SelectedImageIndex = IMAGE_PROPERTY;
            }

            // ImplementationLevel
            IntPtr implementationLevel;
            IfcEngine.x64.GetSPFFHeaderItem(_ifcModel, 1, 0, IfcEngine.x64.sdaiUNICODE, out implementationLevel);

            TreeNode tnImplementationLevel = tnHeaderInfo.Nodes.Add("ImplementationLevel = '" + Marshal.PtrToStringAnsi(implementationLevel) + "'");
            tnImplementationLevel.ImageIndex = tnImplementationLevel.SelectedImageIndex = IMAGE_PROPERTY;

            // Name
            IntPtr name;
            IfcEngine.x64.GetSPFFHeaderItem(_ifcModel, 2, 0, IfcEngine.x64.sdaiUNICODE, out name);

            TreeNode tnName = tnHeaderInfo.Nodes.Add("Name = '" + Marshal.PtrToStringAnsi(name) + "'");
            tnName.ImageIndex = tnName.SelectedImageIndex = IMAGE_PROPERTY;

            // TimeStamp
            IntPtr timeStamp;
            IfcEngine.x64.GetSPFFHeaderItem(_ifcModel, 3, 0, IfcEngine.x64.sdaiUNICODE, out timeStamp);

            TreeNode tnTimeStamp = tnHeaderInfo.Nodes.Add("TimeStamp = '" + Marshal.PtrToStringAnsi(timeStamp) + "'");
            tnTimeStamp.ImageIndex = tnTimeStamp.SelectedImageIndex = IMAGE_PROPERTY;

            // Authors
            TreeNode tnAuthors = tnHeaderInfo.Nodes.Add("Authors");
            tnAuthors.ImageIndex = tnAuthors.SelectedImageIndex = IMAGE_PROPERTY;

            i = 0;
            IntPtr author;
            while (IfcEngine.x64.GetSPFFHeaderItem(_ifcModel, 4, i++, IfcEngine.x64.sdaiUNICODE, out author) == 0)
            {
                TreeNode tnAuthor = tnAuthors.Nodes.Add(Marshal.PtrToStringAnsi(author));
                tnAuthor.ImageIndex = tnAuthor.SelectedImageIndex = IMAGE_PROPERTY;
            }

            // Organizations
            TreeNode tnOrganizations = tnHeaderInfo.Nodes.Add("Organizations");
            tnOrganizations.ImageIndex = tnOrganizations.SelectedImageIndex = IMAGE_PROPERTY;

            i = 0;
            IntPtr organization;
            while (IfcEngine.x64.GetSPFFHeaderItem(_ifcModel, 5, i++, IfcEngine.x64.sdaiUNICODE, out organization) == 0)
            {
                TreeNode tnOrganization = tnOrganizations.Nodes.Add(Marshal.PtrToStringAnsi(organization));
                tnOrganization.ImageIndex = tnOrganization.SelectedImageIndex = IMAGE_PROPERTY;
            }

            // PreprocessorVersion
            IntPtr preprocessorVersion;
            IfcEngine.x64.GetSPFFHeaderItem(_ifcModel, 6, 0, IfcEngine.x64.sdaiUNICODE, out preprocessorVersion);

            TreeNode tnPreprocessorVersion = tnHeaderInfo.Nodes.Add("PreprocessorVersion = '" + Marshal.PtrToStringAnsi(preprocessorVersion) + "'");
            tnPreprocessorVersion.ImageIndex = tnPreprocessorVersion.SelectedImageIndex = IMAGE_PROPERTY;

            // OriginatingSystem
            IntPtr originatingSystem;
            IfcEngine.x64.GetSPFFHeaderItem(_ifcModel, 7, 0, IfcEngine.x64.sdaiUNICODE, out originatingSystem);

            TreeNode tnOriginatingSystem = tnHeaderInfo.Nodes.Add("OriginatingSystem = '" + Marshal.PtrToStringAnsi(originatingSystem) + "'");
            tnOriginatingSystem.ImageIndex = tnOriginatingSystem.SelectedImageIndex = IMAGE_PROPERTY;

            // Authorization
            IntPtr authorization;
            IfcEngine.x64.GetSPFFHeaderItem(_ifcModel, 8, 0, IfcEngine.x64.sdaiUNICODE, out authorization);

            TreeNode tnAuthorization = tnHeaderInfo.Nodes.Add("Authorization = '" + Marshal.PtrToStringAnsi(authorization) + "'");
            tnAuthorization.ImageIndex = tnAuthorization.SelectedImageIndex = IMAGE_PROPERTY;

            // FileSchemas
            TreeNode tnFileSchemas = tnHeaderInfo.Nodes.Add("FileSchemas");
            tnFileSchemas.ImageIndex = tnFileSchemas.SelectedImageIndex = IMAGE_PROPERTY;

            i = 0;
            IntPtr fileSchema;
            while (IfcEngine.x64.GetSPFFHeaderItem(_ifcModel, 9, i++, IfcEngine.x64.sdaiUNICODE, out fileSchema) == 0)
            {
                TreeNode tnFileSchema = tnFileSchemas.Nodes.Add(Marshal.PtrToStringAnsi(fileSchema));
                tnFileSchema.ImageIndex = tnFileSchema.SelectedImageIndex = IMAGE_PROPERTY;
            }
        }

        /// <summary>
        /// Helper
        /// </summary>
        private void CreateProjectTreeItems()
        {
            // 엔티티 ID 얻어오기 
            Int64 iEntityID = IfcEngine.x64.sdaiGetEntityExtentBN(_ifcModel, "IfcProject");
            Int64 iEntitiesCount = IfcEngine.x64.sdaiGetMemberCount(iEntityID);

            for (Int64 iEntity = 0; iEntity < iEntitiesCount; iEntity++)
            {
                Int64 iInstance = 0;
                IfcEngine.x64.engiGetAggrElement(iEntityID, iEntity, IfcEngine.x64.sdaiINSTANCE, out iInstance);

                IFCTreeItem ifcTreeItem = new IFCTreeItem();
                ifcTreeItem.instance = iInstance;

                CreateTreeItem(null, ifcTreeItem);
                ifcTreeItem.treeNode.ImageIndex = ifcTreeItem.treeNode.SelectedImageIndex = IMAGE_CHECKED;

                AddChildrenTreeItems(ifcTreeItem, iInstance, "IfcSite");
            } // for (int iEntity = ...
        }

        /// <summary>
        /// Helper
        /// </summary>
        private void CreateNotReferencedTreeItems()
        {
            IFCTreeItem ifcTreeItem = new IFCTreeItem();
            ifcTreeItem.treeNode = _treeControl.Nodes.Add("Not Referenced");
            ifcTreeItem.treeNode.ForeColor = Color.Gray;
            ifcTreeItem.treeNode.ImageIndex = ifcTreeItem.treeNode.SelectedImageIndex = IMAGE_CHECKED;
            ifcTreeItem.treeNode.Tag = ifcTreeItem;

            FindNonReferencedIFCItems(_ifcRoot, ifcTreeItem.treeNode);

            if (ifcTreeItem.treeNode.Nodes.Count == 0)
            {
                // don't show empty Not Referenced item
                _treeControl.Nodes.Remove(ifcTreeItem.treeNode);
            }
        }

        /// <summary>
        /// Helper
        /// </summary>
        /// <param name="ifcParent"></param>
        /// <param name="iParentInstance"></param>
        /// <param name="strEntityName"></param>

        private void AddChildrenTreeItems(IFCTreeItem ifcParent, Int64 iParentInstance, string strEntityName)
        {
            // check for decomposition
            IntPtr decompositionInstance;
            IfcEngine.x64.sdaiGetAttrBN(iParentInstance, "IsDecomposedBy", IfcEngine.x64.sdaiAGGR, out decompositionInstance);

            if (decompositionInstance == IntPtr.Zero)
            {
                return;
            }

            Int64 iDecompositionsCount = IfcEngine.x64.sdaiGetMemberCount(decompositionInstance.ToInt32());
            for (Int64 iDecomposition = 0; iDecomposition < iDecompositionsCount; iDecomposition++)
            {
                Int64 iDecompositionInstance = 0;
                IfcEngine.x64.engiGetAggrElement(decompositionInstance.ToInt32(), iDecomposition, IfcEngine.x64.sdaiINSTANCE, out iDecompositionInstance);

                if (!IsInstanceOf(iDecompositionInstance, "IFCRELAGGREGATES"))
                {
                    continue;
                }

                IntPtr objectInstances;
                IfcEngine.x64.sdaiGetAttrBN(iDecompositionInstance, "RelatedObjects", IfcEngine.x64.sdaiAGGR, out objectInstances);

                Int64 iObjectsCount = IfcEngine.x64.sdaiGetMemberCount(objectInstances.ToInt32());
                for (Int64 iObject = 0; iObject < iObjectsCount; iObject++)
                {
                    Int64 iObjectInstance = 0;
                    IfcEngine.x64.engiGetAggrElement(objectInstances.ToInt32(), iObject, IfcEngine.x64.sdaiINSTANCE, out iObjectInstance);

                    if (!IsInstanceOf(iObjectInstance, strEntityName))
                    {
                        continue;
                    }

                    IFCTreeItem ifcTreeItem = new IFCTreeItem();
                    ifcTreeItem.instance = iObjectInstance;

                    CreateTreeItem(ifcParent, ifcTreeItem);
                    ifcTreeItem.treeNode.ImageIndex = ifcTreeItem.treeNode.SelectedImageIndex = IMAGE_CHECKED;

                    switch (strEntityName)
                    {
                        case "IfcSite":
                            {
                                AddChildrenTreeItems(ifcTreeItem, iObjectInstance, "IfcBuilding");

                            }
                            break;

                        case "IfcBuilding":
                            {
                                AddChildrenTreeItems(ifcTreeItem, iObjectInstance, "IfcBuildingStorey");
                            }
                            break;

                        case "IfcBuildingStorey":
                            {
                                AddElementTreeItems(ifcTreeItem, iObjectInstance);
                            }
                            break;

                        default:
                            break;
                    }
                } // for (int iObject = ...
            } // for (int iDecomposition = ...
        }



        private void AddElementTreeItems(IFCTreeItem ifcParent, Int64 iParentInstance)
        {
            IntPtr decompositionInstance;
            IfcEngine.x64.sdaiGetAttrBN(iParentInstance, "IsDecomposedBy", IfcEngine.x64.sdaiAGGR, out decompositionInstance);

            if (decompositionInstance == IntPtr.Zero)
            {
                return;
            }

            Int64 iDecompositionsCount = IfcEngine.x64.sdaiGetMemberCount(decompositionInstance.ToInt32());
            for (Int64 iDecomposition = 0; iDecomposition < iDecompositionsCount; iDecomposition++)
            {
                Int64 iDecompositionInstance = 0;
                IfcEngine.x64.engiGetAggrElement(decompositionInstance.ToInt32(), iDecomposition, IfcEngine.x64.sdaiINSTANCE, out iDecompositionInstance);

                if (!IsInstanceOf(iDecompositionInstance, "IFCRELAGGREGATES"))
                {
                    continue;
                }

                IntPtr objectInstances;
                IfcEngine.x64.sdaiGetAttrBN(iDecompositionInstance, "RelatedObjects", IfcEngine.x64.sdaiAGGR, out objectInstances);

                Int64 iObjectsCount = IfcEngine.x64.sdaiGetMemberCount(objectInstances.ToInt32());
                for (Int64 iObject = 0; iObject < iObjectsCount; iObject++)
                {
                    Int64 iObjectInstance = 0;
                    IfcEngine.x64.engiGetAggrElement(objectInstances.ToInt32(), iObject, IfcEngine.x64.sdaiINSTANCE, out iObjectInstance);

                    IFCTreeItem ifcTreeItem = new IFCTreeItem();
                    ifcTreeItem.instance = iObjectInstance;
                    ifcTreeItem.ifcItem = FindIFCItem(_ifcRoot, ifcTreeItem);

                    CreateTreeItem(ifcParent, ifcTreeItem);

                    _dicCheckedElements[GetItemType(iObjectInstance)] = true;

                    if (ifcTreeItem.ifcItem != null)
                    {
                        ifcTreeItem.ifcItem.ifcTreeItem = ifcTreeItem;
                        ifcTreeItem.treeNode.ImageIndex = ifcTreeItem.treeNode.SelectedImageIndex = IMAGE_CHECKED;
                    }
                    else
                    {
                        ifcTreeItem.treeNode.ImageIndex = ifcTreeItem.treeNode.SelectedImageIndex = IMAGE_NOT_REFERENCED;
                    }
                } // for (int iObject = ...
            } // for (int iDecomposition = ...

            // check for elements
            IntPtr elementsInstance;
            IfcEngine.x64.sdaiGetAttrBN(iParentInstance, "ContainsElements", IfcEngine.x64.sdaiAGGR, out elementsInstance);

            if (elementsInstance == IntPtr.Zero)
            {
                return;
            }

            Int64 iElementsCount = IfcEngine.x64.sdaiGetMemberCount(elementsInstance.ToInt32());
            for (Int64 iElement = 0; iElement < iElementsCount; iElement++)
            {
                Int64 iElementInstance = 0;
                IfcEngine.x64.engiGetAggrElement(elementsInstance.ToInt32(), iElement, IfcEngine.x64.sdaiINSTANCE, out iElementInstance);

                if (!IsInstanceOf(iElementInstance, "IFCRELCONTAINEDINSPATIALSTRUCTURE"))
                {
                    continue;
                }

                IntPtr objectInstances;
                IfcEngine.x64.sdaiGetAttrBN(iElementInstance, "RelatedElements", IfcEngine.x64.sdaiAGGR, out objectInstances);

                Int64 iObjectsCount = IfcEngine.x64.sdaiGetMemberCount(objectInstances.ToInt32());
                for (Int64 iObject = 0; iObject < iObjectsCount; iObject++)
                {
                    Int64 iObjectInstance = 0;
                    IfcEngine.x64.engiGetAggrElement(objectInstances.ToInt32(), iObject, IfcEngine.x64.sdaiINSTANCE, out iObjectInstance);

                    IFCTreeItem ifcTreeItem = new IFCTreeItem();
                    ifcTreeItem.instance = iObjectInstance;
                    ifcTreeItem.ifcItem = FindIFCItem(_ifcRoot, ifcTreeItem);

                    CreateTreeItem(ifcParent, ifcTreeItem);

                    _dicCheckedElements[GetItemType(iObjectInstance)] = true;

                    if (ifcTreeItem.ifcItem != null)
                    {
                        ifcTreeItem.ifcItem.ifcTreeItem = ifcTreeItem;
                        ifcTreeItem.treeNode.ImageIndex = ifcTreeItem.treeNode.SelectedImageIndex = IMAGE_CHECKED;

                        GetColor(ifcTreeItem);
                    }
                    else
                    {
                        ifcTreeItem.treeNode.ImageIndex = ifcTreeItem.treeNode.SelectedImageIndex = IMAGE_NOT_REFERENCED;
                    }

                    IntPtr definedByInstances;
                    IfcEngine.x64.sdaiGetAttrBN(iObjectInstance, "IsDefinedBy", IfcEngine.x64.sdaiAGGR, out definedByInstances);

                    if (definedByInstances == IntPtr.Zero)
                    {
                        continue;
                    }

                    Int64 iDefinedByCount = IfcEngine.x64.sdaiGetMemberCount(definedByInstances.ToInt32());
                    for (Int64 iDefinedBy = 0; iDefinedBy < iDefinedByCount; iDefinedBy++)
                    {
                        Int64 iDefinedByInstance = 0;
                        IfcEngine.x64.engiGetAggrElement(definedByInstances.ToInt32(), iDefinedBy, IfcEngine.x64.sdaiINSTANCE, out iDefinedByInstance);

                        if (IsInstanceOf(iDefinedByInstance, "IFCRELDEFINESBYPROPERTIES"))
                        {
                            AddPropertyTreeItems(ifcTreeItem, iDefinedByInstance);
                        }
                        else
                        {
                            if (IsInstanceOf(iDefinedByInstance, "IFCRELDEFINESBYTYPE"))
                            {
                                // NA
                            }
                        }
                    }
                } // for (int iObject = ...
            } // for (int iDecomposition = ...
        }

        /// <summary>
        /// Helper. 
        /// </summary>
        /// <param name="ifcTreeItem"></param>
        void GetColor(IFCTreeItem ifcTreeItem)
        {
            if (ifcTreeItem == null)
            {
                throw new ArgumentException("The item is null.");
            }

            // C++ => getRGB_object()
            IntPtr representationInstance;
            IfcEngine.x64.sdaiGetAttrBN(ifcTreeItem.instance, "Representation", IfcEngine.x64.sdaiINSTANCE, out representationInstance);
            if (representationInstance == IntPtr.Zero)
            {
                return;
            }

            // C++ => getRGB_productDefinitionShape()
            IntPtr representationsInstance;
            IfcEngine.x64.sdaiGetAttrBN(representationInstance.ToInt32(), "Representations", IfcEngine.x64.sdaiAGGR, out representationsInstance);

            Int64 iRepresentationsCount = IfcEngine.x64.sdaiGetMemberCount(representationsInstance.ToInt32());
            for (Int64 iRepresentation = 0; iRepresentation < iRepresentationsCount; iRepresentation++)
            {
                Int64 iShapeInstance = 0;
                IfcEngine.x64.engiGetAggrElement(representationsInstance.ToInt32(), iRepresentation, IfcEngine.x64.sdaiINSTANCE, out iShapeInstance);

                if (iShapeInstance == 0)
                {
                    continue;
                }

                // C++ => getRGB_shapeRepresentation()
                IntPtr representationIdentifier;
                IfcEngine.x64.sdaiGetAttrBN(iShapeInstance, "RepresentationIdentifier", IfcEngine.x64.sdaiUNICODE, out representationIdentifier);

                if (Marshal.PtrToStringAnsi(representationIdentifier) == "Body")
                {
                    IntPtr itemsInstance;
                    IfcEngine.x64.sdaiGetAttrBN(iShapeInstance, "Items", IfcEngine.x64.sdaiAGGR, out itemsInstance);

                    Int64 iItemsCount = IfcEngine.x64.sdaiGetMemberCount(itemsInstance.ToInt32());
                    for (int iItem = 0; iItem < iItemsCount; iItem++)
                    {
                        Int64 iItemInstance = 0;
                        IfcEngine.x64.engiGetAggrElement(itemsInstance.ToInt32(), iItem, IfcEngine.x64.sdaiINSTANCE, out iItemInstance);

                        IntPtr styledByItem;
                        IfcEngine.x64.sdaiGetAttrBN(iItemInstance, "StyledByItem", IfcEngine.x64.sdaiINSTANCE, out styledByItem);

                        if (styledByItem != IntPtr.Zero)
                        {
                            getRGB_styledItem(ifcTreeItem, styledByItem.ToInt32());
                        }
                        else
                        {
                            searchDeeper(ifcTreeItem, iItemInstance);
                        } // else if (iItemInstance != 0)

                        if (ifcTreeItem.ifcColor != null)
                        {
                            return;
                        }
                    } // for (int iItem = ...
                }
            } // for (int iRepresentation = ...
        }



        void searchDeeper(IFCTreeItem ifcTreeItem, Int64 iParentInstance)
        {
            IntPtr styledByItem;
            IfcEngine.x64.sdaiGetAttrBN(iParentInstance, "StyledByItem", IfcEngine.x64.sdaiINSTANCE, out styledByItem);

            if (styledByItem != IntPtr.Zero)
            {
                getRGB_styledItem(ifcTreeItem, styledByItem.ToInt32());
                if (ifcTreeItem.ifcColor != null)
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
                    searchDeeper(ifcTreeItem, firstOperand.ToInt32());
                }
            } // if (IsInstanceOf(iParentInstance, "IFCBOOLEANCLIPPINGRESULT"))
            else
            {
                if (IsInstanceOf(iParentInstance, "IFCMAPPEDITEM"))
                {
                    IntPtr mappingSource;
                    IfcEngine.x64.sdaiGetAttrBN(iParentInstance, "MappingSource", IfcEngine.x64.sdaiINSTANCE, out mappingSource);

                    IntPtr mappedRepresentation;
                    IfcEngine.x64.sdaiGetAttrBN(mappingSource.ToInt32(), "MappedRepresentation", IfcEngine.x64.sdaiINSTANCE, out mappedRepresentation);

                    if (mappedRepresentation != IntPtr.Zero)
                    {
                        IntPtr representationIdentifier;
                        IfcEngine.x64.sdaiGetAttrBN(mappedRepresentation.ToInt32(), "RepresentationIdentifier", IfcEngine.x64.sdaiUNICODE, out representationIdentifier);

                        if (Marshal.PtrToStringAnsi(representationIdentifier) == "Body")
                        {
                            IntPtr itemsInstance;
                            IfcEngine.x64.sdaiGetAttrBN(mappedRepresentation.ToInt32(), "Items", IfcEngine.x64.sdaiAGGR, out itemsInstance);

                            Int64 iItemsCount = IfcEngine.x64.sdaiGetMemberCount(itemsInstance.ToInt32());
                            for (int iItem = 0; iItem < iItemsCount; iItem++)
                            {
                                Int64 iItemInstance = 0;
                                IfcEngine.x64.engiGetAggrElement(itemsInstance.ToInt32(), iItem, IfcEngine.x64.sdaiINSTANCE, out iItemInstance);

                                styledByItem = IntPtr.Zero;
                                IfcEngine.x64.sdaiGetAttrBN(iItemInstance, "StyledByItem", IfcEngine.x64.sdaiINSTANCE, out styledByItem);

                                if (styledByItem != IntPtr.Zero)
                                {
                                    getRGB_styledItem(ifcTreeItem, styledByItem.ToInt32());
                                }
                                else
                                {
                                    searchDeeper(ifcTreeItem, iItemInstance);
                                } // else if (iItemInstance != 0)

                                if (ifcTreeItem.ifcColor != null)
                                {
                                    return;
                                }
                            } // for (int iItem = ...
                        } // if (Marshal.PtrToStringAnsi(representationIdentifier) == "Body")
                    } // if (mappedRepresentation != IntPtr.Zero)
                } // if (IsInstanceOf(iParentInstance, "IFCMAPPEDITEM"))
            } // else if (IsInstanceOf(iParentInstance, "IFCBOOLEANCLIPPINGRESULT"))
        }

        /// <summary>
        /// Helper
        /// </summary>
        /// <param name="iStyledByItemInstance"></param>
        void getRGB_styledItem(IFCTreeItem ifcTreeItem, int iStyledByItemInstance)
        {
            IntPtr stylesInstance;
            IfcEngine.x64.sdaiGetAttrBN(iStyledByItemInstance, "Styles", IfcEngine.x64.sdaiAGGR, out stylesInstance);

            Int64 iStylesCount = IfcEngine.x64.sdaiGetMemberCount(stylesInstance.ToInt32());
            for (Int64 iStyle = 0; iStyle < iStylesCount; iStyle++)
            {
                Int64 iStyleInstance = 0;
                IfcEngine.x64.engiGetAggrElement(stylesInstance.ToInt32(), iStyle, IfcEngine.x64.sdaiINSTANCE, out iStyleInstance);

                if (iStyleInstance == 0)
                {
                    continue;
                }

                getRGB_presentationStyleAssignment(ifcTreeItem, iStyleInstance);
            } // for (int iStyle = ...
        }



        void getRGB_presentationStyleAssignment(IFCTreeItem ifcTreeItem, Int64 iParentInstance)
        {
            IntPtr stylesInstance;
            IfcEngine.x64.sdaiGetAttrBN(iParentInstance, "Styles", IfcEngine.x64.sdaiAGGR, out stylesInstance);

            Int64 iStylesCount = IfcEngine.x64.sdaiGetMemberCount(stylesInstance.ToInt32());
            for (Int64 iStyle = 0; iStyle < iStylesCount; iStyle++)
            {
                Int64 iStyleInstance = 0;
                IfcEngine.x64.engiGetAggrElement(stylesInstance.ToInt32(), iStyle, IfcEngine.x64.sdaiINSTANCE, out iStyleInstance);

                if (iStyleInstance == 0)
                {
                    continue;
                }

                getRGB_surfaceStyle(ifcTreeItem, iStyleInstance);
            } // for (int iStyle = ...
        }



        unsafe void getRGB_surfaceStyle(IFCTreeItem ifcTreeItem, Int64 iParentInstance)
        {
            IntPtr stylesInstance;
            IfcEngine.x64.sdaiGetAttrBN(iParentInstance, "Styles", IfcEngine.x64.sdaiAGGR, out stylesInstance);

            Int64 iStylesCount = IfcEngine.x64.sdaiGetMemberCount(stylesInstance.ToInt32());
            for (Int64 iStyle = 0; iStyle < iStylesCount; iStyle++)
            {
                Int64 iStyleInstance = 0;
                IfcEngine.x64.engiGetAggrElement(stylesInstance.ToInt32(), iStyle, IfcEngine.x64.sdaiINSTANCE, out iStyleInstance);

                if (iStyleInstance == 0)
                {
                    continue;
                }

                IntPtr surfaceColour;
                IfcEngine.x64.sdaiGetAttrBN(iStyleInstance, "SurfaceColour", IfcEngine.x64.sdaiINSTANCE, out surfaceColour);

                if (surfaceColour == IntPtr.Zero)
                {
                    continue;
                }

                double R = 0;
                IfcEngine.x64.sdaiGetAttrBN(surfaceColour.ToInt32(), "Red", IfcEngine.x64.sdaiREAL, out *(IntPtr*)&R);

                double G = 0;
                IfcEngine.x64.sdaiGetAttrBN(surfaceColour.ToInt32(), "Green", IfcEngine.x64.sdaiREAL, out *(IntPtr*)&G);

                double B = 0;
                IfcEngine.x64.sdaiGetAttrBN(surfaceColour.ToInt32(), "Blue", IfcEngine.x64.sdaiREAL, out *(IntPtr*)&B);

                ifcTreeItem.ifcColor = new IFCItemColor();
                ifcTreeItem.ifcColor.R = (float)R;
                ifcTreeItem.ifcColor.G = (float)G;
                ifcTreeItem.ifcColor.B = (float)B;

                return;
            } // for (int iStyle = ...
        }

        /// <summary>
        /// Helper
        /// </summary>
        /// <param name="ifcParent"></param>
        /// <param name="iParentInstance"></param>     



        private void AddPropertyTreeItems(IFCTreeItem ifcParent, Int64 iParentInstance)
        {
            IntPtr propertyInstances;
            IfcEngine.x64.sdaiGetAttrBN(iParentInstance, "RelatingPropertyDefinition", IfcEngine.x64.sdaiINSTANCE, out propertyInstances);

            if (IsInstanceOf(propertyInstances.ToInt32(), "IFCELEMENTQUANTITY"))
            {
                IFCTreeItem ifcPropertySetTreeItem = new IFCTreeItem();
                ifcPropertySetTreeItem.instance = propertyInstances.ToInt32();

                CreateTreeItem(ifcParent, ifcPropertySetTreeItem);
                ifcPropertySetTreeItem.treeNode.ImageIndex = ifcPropertySetTreeItem.treeNode.SelectedImageIndex = IMAGE_PROPERTY_SET;

                // check for quantity
                IntPtr quantitiesInstance;
                IfcEngine.x64.sdaiGetAttrBN(propertyInstances.ToInt32(), "Quantities", IfcEngine.x64.sdaiAGGR, out quantitiesInstance);

                if (quantitiesInstance == IntPtr.Zero)
                {
                    return;
                }

                Int64 iQuantitiesCount = IfcEngine.x64.sdaiGetMemberCount(quantitiesInstance.ToInt32());
                for (int iQuantity = 0; iQuantity < iQuantitiesCount; iQuantity++)
                {
                    Int64 iQuantityInstance = 0;
                    IfcEngine.x64.engiGetAggrElement(quantitiesInstance.ToInt32(), iQuantity, IfcEngine.x64.sdaiINSTANCE, out iQuantityInstance);

                    IFCTreeItem ifcQuantityTreeItem = new IFCTreeItem();
                    ifcQuantityTreeItem.instance = iQuantityInstance;

                    if (IsInstanceOf(iQuantityInstance, "IFCQUANTITYLENGTH"))
                        CreatePropertyTreeItem(ifcPropertySetTreeItem, ifcQuantityTreeItem, "IFCQUANTITYLENGTH");
                    else
                        if (IsInstanceOf(iQuantityInstance, "IFCQUANTITYAREA"))
                            CreatePropertyTreeItem(ifcPropertySetTreeItem, ifcQuantityTreeItem, "IFCQUANTITYAREA");
                        else
                            if (IsInstanceOf(iQuantityInstance, "IFCQUANTITYVOLUME"))
                                CreatePropertyTreeItem(ifcPropertySetTreeItem, ifcQuantityTreeItem, "IFCQUANTITYVOLUME");
                            else
                                if (IsInstanceOf(iQuantityInstance, "IFCQUANTITYCOUNT"))
                                    CreatePropertyTreeItem(ifcPropertySetTreeItem, ifcQuantityTreeItem, "IFCQUANTITYCOUNT");
                                else
                                    if (IsInstanceOf(iQuantityInstance, "IFCQUANTITYWEIGTH"))
                                        CreatePropertyTreeItem(ifcPropertySetTreeItem, ifcQuantityTreeItem, "IFCQUANTITYWEIGTH");
                                    else
                                        if (IsInstanceOf(iQuantityInstance, "IFCQUANTITYTIME"))
                                            CreatePropertyTreeItem(ifcPropertySetTreeItem, ifcQuantityTreeItem, "IFCQUANTITYTIME");
                } // for (int iQuantity = ...
            }
            else
            {
                if (IsInstanceOf(propertyInstances.ToInt32(), "IFCPROPERTYSET"))
                {
                    IFCTreeItem ifcPropertySetTreeItem = new IFCTreeItem();
                    ifcPropertySetTreeItem.instance = propertyInstances.ToInt32();

                    CreateTreeItem(ifcParent, ifcPropertySetTreeItem);
                    ifcPropertySetTreeItem.treeNode.ImageIndex = ifcPropertySetTreeItem.treeNode.SelectedImageIndex = IMAGE_PROPERTY_SET;

                    // check for quantity
                    IntPtr propertiesInstance;
                    IfcEngine.x64.sdaiGetAttrBN(propertyInstances.ToInt32(), "HasProperties", IfcEngine.x64.sdaiAGGR, out propertiesInstance);

                    if (propertiesInstance == IntPtr.Zero)
                    {
                        return;
                    }

                    Int64 iPropertiesCount = IfcEngine.x64.sdaiGetMemberCount(propertiesInstance.ToInt32());
                    for (int iProperty = 0; iProperty < iPropertiesCount; iProperty++)
                    {
                        Int64 iPropertyInstance = 0;
                        IfcEngine.x64.engiGetAggrElement(propertiesInstance.ToInt32(), iProperty, IfcEngine.x64.sdaiINSTANCE, out iPropertyInstance);

                        if (!IsInstanceOf(iPropertyInstance, "IFCPROPERTYSINGLEVALUE"))
                            continue;

                        IFCTreeItem ifcPropertyTreeItem = new IFCTreeItem();
                        ifcPropertyTreeItem.instance = iPropertyInstance;

                        CreatePropertyTreeItem(ifcPropertySetTreeItem, ifcPropertyTreeItem, "IFCPROPERTYSINGLEVALUE");
                    } // for (int iProperty = ...
                }
            }
        }


        /// <summary>
        /// Helper
        /// </summary>
        /// <param name="ifcParent"></param>
        /// <param name="ifcItem"></param>
        private void CreateTreeItem(IFCTreeItem ifcParent, IFCTreeItem ifcItem)
        {
            //            IntPtr ifcType = IfcEngine.x64.engiGetInstanceClassInfo(ifcItem.instance);
            //            string strIfcType = Marshal.PtrToStringAnsi(ifcType);

            Int64 entity = IfcEngine.x64.sdaiGetInstanceType(ifcItem.instance);
            IntPtr entityNamePtr = IntPtr.Zero;
            IfcEngine.x64.engiGetEntityName(entity, IfcEngine.x64.sdaiUNICODE, out entityNamePtr);
            string strIfcType = Marshal.PtrToStringAnsi(entityNamePtr);

            IntPtr name;
            IfcEngine.x64.sdaiGetAttrBN(ifcItem.instance, "Name", IfcEngine.x64.sdaiUNICODE, out name);

            string strName = Marshal.PtrToStringAnsi(name);

            IntPtr description;
            IfcEngine.x64.sdaiGetAttrBN(ifcItem.instance, "Description", IfcEngine.x64.sdaiUNICODE, out description);

            string strDescription = Marshal.PtrToStringAnsi(description);

            string strItemText = "'" + (string.IsNullOrEmpty(strName) ? "<name>" : strName) +
                    "', '" + (string.IsNullOrEmpty(strDescription) ? "<description>" : strDescription) +
                    "' (" + strIfcType + ")";

            if ((ifcParent != null) && (ifcParent.treeNode != null))
            {
                ifcItem.treeNode = ifcParent.treeNode.Nodes.Add(strItemText);
            }
            else
            {
                ifcItem.treeNode = _treeControl.Nodes.Add(strItemText);
            }

            if (ifcItem.ifcItem == null)
            {
                // item without visual representation
                ifcItem.treeNode.ForeColor = Color.Gray;
            }

            ifcItem.treeNode.Tag = ifcItem;
        }

        /// <summary>
        /// Helper
        /// </summary>
        /// <param name="ifcParent"></param>
        /// <param name="ifcItem"></param>
        private void CreatePropertyTreeItem(IFCTreeItem ifcParent, IFCTreeItem ifcItem, string strProperty)
        {
            //IntPtr ifcType = IfcEngine.x64.engiGetInstanceClassInfo(ifcItem.instance);
            Int64 entity = IfcEngine.x64.sdaiGetInstanceType(ifcItem.instance);
            IntPtr entityNamePtr = IntPtr.Zero;
            IfcEngine.x64.engiGetEntityName(entity, IfcEngine.x64.sdaiUNICODE, out entityNamePtr);
            //string strIfcType = Marshal.PtrToStringAnsi(ifcType);
            string strIfcType = Marshal.PtrToStringAnsi(entityNamePtr);

            IntPtr name;
            IfcEngine.x64.sdaiGetAttrBN(ifcItem.instance, "Name", IfcEngine.x64.sdaiUNICODE, out name);

            string strName = Marshal.PtrToStringAnsi(name);

            string strValue = string.Empty;
            switch (strProperty)
            {
                case "IFCQUANTITYLENGTH":
                    {
                        IntPtr value;
                        IfcEngine.x64.sdaiGetAttrBN(ifcItem.instance, "LengthValue", IfcEngine.x64.sdaiUNICODE, out value);

                        strValue = Marshal.PtrToStringAnsi(value);
                    }
                    break;

                case "IFCQUANTITYAREA":
                    {
                        IntPtr value;
                        IfcEngine.x64.sdaiGetAttrBN(ifcItem.instance, "AreaValue", IfcEngine.x64.sdaiUNICODE, out value);

                        strValue = Marshal.PtrToStringAnsi(value);
                    }
                    break;

                case "IFCQUANTITYVOLUME":
                    {
                        IntPtr value;
                        IfcEngine.x64.sdaiGetAttrBN(ifcItem.instance, "VolumeValue", IfcEngine.x64.sdaiUNICODE, out value);

                        strValue = Marshal.PtrToStringAnsi(value);
                    }
                    break;

                case "IFCQUANTITYCOUNT":
                    {
                        IntPtr value;
                        IfcEngine.x64.sdaiGetAttrBN(ifcItem.instance, "CountValue", IfcEngine.x64.sdaiUNICODE, out value);

                        strValue = Marshal.PtrToStringAnsi(value);
                    }
                    break;

                case "IFCQUANTITYWEIGTH":
                    {
                        IntPtr value;
                        IfcEngine.x64.sdaiGetAttrBN(ifcItem.instance, "WeigthValue", IfcEngine.x64.sdaiUNICODE, out value);

                        strValue = Marshal.PtrToStringAnsi(value);
                    }
                    break;

                case "IFCQUANTITYTIME":
                    {
                        IntPtr value;
                        IfcEngine.x64.sdaiGetAttrBN(ifcItem.instance, "TimeValue", IfcEngine.x64.sdaiUNICODE, out value);

                        strValue = Marshal.PtrToStringAnsi(value);
                    }
                    break;

                case "IFCPROPERTYSINGLEVALUE":
                    {
                        IntPtr value;
                        IfcEngine.x64.sdaiGetAttrBN(ifcItem.instance, "NominalValue", IfcEngine.x64.sdaiUNICODE, out value);

                        strValue = Marshal.PtrToStringAnsi(value);
                    }
                    break;

                default:
                    throw new Exception("Unknown property.");
            } // switch (strProperty)    

            string strItemText = "'" + (string.IsNullOrEmpty(strName) ? "<name>" : strName) +
                    "' = '" + (string.IsNullOrEmpty(strValue) ? "<value>" : strValue) +
                    "' (" + strIfcType + ")";

            if ((ifcParent != null) && (ifcParent.treeNode != null))
            {
                ifcItem.treeNode = ifcParent.treeNode.Nodes.Add(strItemText);
            }
            else
            {
                ifcItem.treeNode = _treeControl.Nodes.Add(strItemText);
            }

            if (ifcItem.ifcItem == null)
            {
                // item without visual representation
                ifcItem.treeNode.ForeColor = Color.Gray;
            }

            ifcItem.treeNode.ImageIndex = ifcItem.treeNode.SelectedImageIndex = IMAGE_PROPERTY;
        }

        /// <summary>
        /// Helper
        /// </summary>
        /// <param name="ifcTreeItem"></param>
        private IFCItem FindIFCItem(IFCItem ifcParent, IFCTreeItem ifcTreeItem)
        {
            if (ifcParent == null)
            {
                return null;
            }

            IFCItem ifcIterator = ifcParent;
            while (ifcIterator != null)
            {
                if (ifcIterator.ifcID == ifcTreeItem.instance)
                {
                    return ifcIterator;
                }

                IFCItem ifcItem = FindIFCItem(ifcIterator.child, ifcTreeItem);
                if (ifcItem != null)
                {
                    return ifcItem;
                }

                ifcIterator = ifcIterator.next;
            }

            return FindIFCItem(ifcParent.child, ifcTreeItem);
        }

        /// <summary>
        /// Helper
        /// </summary>
        /// <param name="ifcTreeItem"></param>
        private void FindNonReferencedIFCItems(IFCItem ifcParent, TreeNode tnNotReferenced)
        {
            if (ifcParent == null)
            {
                return;
            }

            IFCItem ifcIterator = ifcParent;
            while (ifcIterator != null)
            {
                if ((ifcIterator.ifcTreeItem == null) && (ifcIterator.ifcID != 0))
                {
                    string strItemText = "'" + (string.IsNullOrEmpty(ifcIterator.name) ? "<name>" : ifcIterator.name) +
                            "' = '" + (string.IsNullOrEmpty(ifcIterator.description) ? "<description>" : ifcIterator.description) +
                            "' (" + (string.IsNullOrEmpty(ifcIterator.ifcType) ? ifcIterator.globalID : ifcIterator.ifcType) + ")";

                    IFCTreeItem ifcTreeItem = new IFCTreeItem();
                    ifcTreeItem.instance = ifcIterator.ifcID;
                    ifcTreeItem.treeNode = tnNotReferenced.Nodes.Add(strItemText);
                    ifcTreeItem.ifcItem = FindIFCItem(_ifcRoot, ifcTreeItem);
                    ifcIterator.ifcTreeItem = ifcTreeItem;
                    ifcTreeItem.treeNode.Tag = ifcTreeItem;

                    if (ifcTreeItem.ifcItem != null)
                    {
                        ifcTreeItem.ifcItem.ifcTreeItem = ifcTreeItem;
                        ifcTreeItem.treeNode.ImageIndex = ifcTreeItem.treeNode.SelectedImageIndex = IMAGE_CHECKED;
                    }
                    else
                    {
                        ifcTreeItem.treeNode.ImageIndex = ifcTreeItem.treeNode.SelectedImageIndex = IMAGE_NOT_REFERENCED;
                    }
                }

                FindNonReferencedIFCItems(ifcIterator.child, tnNotReferenced);

                ifcIterator = ifcIterator.next;
            }

            FindNonReferencedIFCItems(ifcParent.child, tnNotReferenced);
        }

        /// <summary>
        /// Helper
        /// </summary>
        /// <param name="iInstance"></param>
        /// <returns></returns>


        private string GetItemType(Int64 iInstance)
        {
            //            IntPtr ifcType = IfcEngine.x64.engiGetInstanceClassInfo(iInstance);
            //            return Marshal.PtrToStringAnsi(ifcType);

            Int64 entity = IfcEngine.x64.sdaiGetInstanceType(iInstance);
            IntPtr entityNamePtr = IntPtr.Zero;
            IfcEngine.x64.engiGetEntityName(entity, IfcEngine.x64.sdaiUNICODE, out entityNamePtr);
            return Marshal.PtrToStringAnsi(entityNamePtr);
        }

        /// <summary>
        /// Helper
        /// </summary>
        /// <param name="iInstance"></param>
        /// <param name="strType"></param>
        /// <returns></returns>
        private bool IsInstanceOf(int iInstance, string strType)
        {
            if (IfcEngine.x64.sdaiGetInstanceType(iInstance) == IfcEngine.x64.sdaiGetEntity(_ifcModel, strType))
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// Helper
        /// </summary>
        /// <param name="iInstance"></param>
        /// <param name="strType"></param>
        /// <returns></returns>
        private bool IsInstanceOf(Int64 iInstance, string strType)
        {
            if (IfcEngine.x64.sdaiGetInstanceType(iInstance) == IfcEngine.x64.sdaiGetEntity(_ifcModel, strType))
            {
                return true;
            }

            return false;
        }
        /// <summary>
        /// Handler
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void OnNodeMouseClick(object sender, TreeNodeMouseClickEventArgs e)
        {
            Rectangle rcIcon = new Rectangle(e.Node.Bounds.Location - new Size(16, 0), new Size(16, 16));
            if (!rcIcon.Contains(e.Location))
            {
                return;
            }

            if (e.Node.Tag == null)
            {
                // skip properties
                return;
            }

            switch (e.Node.ImageIndex)
            {
                case IMAGE_CHECKED:
                    {
                        e.Node.ImageIndex = e.Node.SelectedImageIndex = IMAGE_UNCHECKED;

                        OnNodeMouseClick_UpdateChildrenTreeItems(e.Node);
                        UpdateParentTreeItems(e.Node);

                        _ifcViewer.Redraw();
                    }
                    break;

                case IMAGE_UNCHECKED:
                    {
                        e.Node.ImageIndex = e.Node.SelectedImageIndex = IMAGE_CHECKED;

                        OnNodeMouseClick_UpdateChildrenTreeItems(e.Node);
                        UpdateParentTreeItems(e.Node);

                        _ifcViewer.Redraw();
                    }
                    break;
            } // switch (e.Node.ImageIndex)
        }

        /// <summary>
        /// Helper
        /// </summary>
        /// <param name="tnParent"></param>
        private void OnNodeMouseClick_UpdateChildrenTreeItems(TreeNode tnParent)
        {
            foreach (TreeNode tnChild in tnParent.Nodes)
            {
                if ((tnChild.ImageIndex != IMAGE_CHECKED) && (tnChild.ImageIndex != IMAGE_UNCHECKED))
                {
                    // skip properties
                    continue;
                }

                switch (tnParent.ImageIndex)
                {
                    case IMAGE_CHECKED:
                        {
                            tnChild.ImageIndex = tnChild.SelectedImageIndex = IMAGE_CHECKED;
                        }
                        break;

                    case IMAGE_UNCHECKED:
                        {
                            tnChild.ImageIndex = tnChild.SelectedImageIndex = IMAGE_UNCHECKED;
                        }
                        break;
                } // switch (tnParent.ImageIndex)

                OnNodeMouseClick_UpdateChildrenTreeItems(tnChild);
            } // foreach (TreeNode tnChild in ...
        }

        /// <summary>
        /// Helper
        /// </summary>
        /// <param name="tnItem"></param>
        private void UpdateParentTreeItems(TreeNode tnItem)
        {
            if (tnItem.Parent == null)
            {
                return;
            }

            int iCheckedChildrenCount = 0;
            foreach (TreeNode tnChild in tnItem.Parent.Nodes)
            {
                if ((tnChild.ImageIndex != IMAGE_CHECKED) && (tnChild.ImageIndex != IMAGE_UNCHECKED))
                {
                    // skip properties
                    continue;
                }

                if (tnChild.ImageIndex == IMAGE_CHECKED)
                {
                    iCheckedChildrenCount++;
                }
            } // foreach (TreeNode tnChild in ...

            tnItem.Parent.ImageIndex = tnItem.Parent.SelectedImageIndex = iCheckedChildrenCount > 0 ? IMAGE_CHECKED : IMAGE_UNCHECKED;

            UpdateParentTreeItems(tnItem.Parent);
        }

        /// <summary>
        /// Handler
        /// </summary>
        public void OnAfterSelect(object sender, TreeViewEventArgs e)
        {
            if (e.Node.Tag == null)
            {
                // skip properties
                return;
            }

            if (e.Node.ImageIndex != IMAGE_CHECKED)
            {
                // skip unvisible & not referenced items
                return;
            }

            _ifcViewer.SelectItem((e.Node.Tag as IFCTreeItem).ifcItem);
        }

        /// <summary>
        /// Handler
        /// </summary>
        public void OnContextMenu_Opened(object sender, EventArgs e)
        {
            ContextMenuStrip contextMenu = sender as ContextMenuStrip;

            contextMenu.Items.Clear();
            foreach (var pair in this._dicCheckedElements)
            {
                ToolStripMenuItem menuItem = contextMenu.Items.Add(pair.Key) as ToolStripMenuItem;
                menuItem.CheckOnClick = true;
                menuItem.Checked = pair.Value;

                menuItem.Click += new EventHandler(delegate(object item, EventArgs args)
                {
                    _dicCheckedElements[((ToolStripMenuItem)item).Text] = ((ToolStripMenuItem)item).Checked;

                    foreach (TreeNode node in _treeControl.Nodes)
                    {
                        OnContextMenu_UpdateTreeElement(node);
                    }

                    _ifcViewer.Redraw();
                });
            }
        }

        /// <summary>
        /// Helper
        /// </summary>
        /// <param name="tnParent"></param>
        private void OnContextMenu_UpdateTreeElement(TreeNode tnParent)
        {
            if (tnParent.Tag != null)
            {
                OnContextMenu_UpdateTreeElement(tnParent.Tag as IFCTreeItem);
            }

            foreach (TreeNode tnChild in tnParent.Nodes)
            {
                if (tnChild.Tag != null)
                {
                    OnContextMenu_UpdateTreeElement(tnChild.Tag as IFCTreeItem);
                }

                OnContextMenu_UpdateTreeElement(tnChild);
            } // foreach (TreeNode tnChild in ...
        }

        /// <summary>
        /// Helper
        /// </summary>
        /// <param name="ifcTreeItem"></param>
        private void OnContextMenu_UpdateTreeElement(IFCTreeItem ifcTreeItem)
        {
            if (ifcTreeItem.ifcItem == null)
            {
                // skip not referenced items
                return;
            }

            if (string.IsNullOrEmpty(ifcTreeItem.ifcItem.ifcType))
            {
                // skip fake items
                return;
            }

            if (!_dicCheckedElements.ContainsKey(ifcTreeItem.ifcItem.ifcType))
            {
                // skip non-element items
                return;
            }

            ifcTreeItem.treeNode.ImageIndex = ifcTreeItem.treeNode.SelectedImageIndex =
                _dicCheckedElements[ifcTreeItem.ifcItem.ifcType] ? IMAGE_CHECKED : IMAGE_UNCHECKED;

            UpdateParentTreeItems(ifcTreeItem.treeNode);
        }

        /// <summary>
        /// Helper
        /// </summary>
        /// <param name="ifcItem"></param>
        public void OnSelectIFCElement(IFCItem ifcItem)
        {
            System.Diagnostics.Debug.Assert(ifcItem != null, "Internal error.");
            System.Diagnostics.Debug.Assert(ifcItem.ifcTreeItem != null, "Internal error.");
            System.Diagnostics.Debug.Assert(ifcItem.ifcTreeItem.treeNode != null, "Internal error.");

            _treeControl.SelectedNode = ifcItem.ifcTreeItem.treeNode;
        }
    }
}

//namespace IFCViewer
//{
//    /// <summary>
//    /// Describe the color of IFCItem.
//    /// </summary>
//    class IFCItemColor
//    {
//        public float R = 0;
//        public float G = 0;
//        public float B = 0;
//        public float A = 0;
//    }

//    /// <summary>
//    /// Describes an item in the tree.
//    /// </summary>
//    class IFCTreeItem
//    {
//        /// <summary>
//        /// Instance.
//        /// </summary>
//        public int instance = -1;

//        /// <summary>
//        /// Node.
//        /// </summary>
//        public TreeNode treeNode = null;

//        /// <summary>
//        /// If it is not null the item can be selected.
//        /// </summary>
//        public IFCItem ifcItem = null;

//        /// <summary>
//        /// Color
//        /// </summary>
//        public IFCItemColor ifcColor = null;

//        /// <summary>
//        /// Getter
//        /// </summary>
//        public bool IsVisible
//        {
//            get
//            {
//                System.Diagnostics.Debug.Assert(treeNode != null, "Internal error.");

//                if (treeNode.ImageIndex == CIFCTreeData.IMAGE_CHECKED)
//                {
//                    return true;
//                }

//                return false;
//            }
//        }
//    }

//    /// <summary>
//    /// Generates entire IFC tree. 
//    /// - Initiate control by retrieving data from IFC library and transmitting it to C# Tree control.
//    ///    
//    ///     - IFCProject Items
//    ///         - Tree Item
//    ///         - Check Box
//    ///     - Not-referenced in structure 
//    /// - Keeps bidirectional relationship IFCElementID <-> TreeItem
//    ///     - OnSelect Tree Item -> Mark IFC element
//    ///     - OnMark IFC Element -> Select Tree Item
//    /// - Build Context Menu functionality
//    /// </summary>
//    class CIFCTreeData
//    {
//        /// <summary>
//        /// Viewer
//        /// </summary>
//        IFCViewerWrapper _ifcViewer = null;

//        /// <summary>
//        /// Model
//        /// </summary>
//        int _ifcModel = 0;

//        /// <summary>
//        /// Root of IFCItem-s
//        /// </summary>
//        IFCItem _ifcRoot = null;

//        /// <summary>
//        /// Tree control
//        /// </summary>
//        TreeView _treeControl = null;

//        /// <summary>
//        /// Contains info for the context menu.
//        /// </summary>
//        Dictionary<string, bool> _dicCheckedElements = new Dictionary<string, bool>();

//        /// <summary>
//        /// Zero-based indices of the images inside the image list.
//        /// </summary>
//        public const int IMAGE_CHECKED = 0;
//        public const int IMAGE_UNCHECKED = 2;
//        public const int IMAGE_PROPERTY_SET = 3;
//        public const int IMAGE_PROPERTY = 4;
//        public const int IMAGE_NOT_REFERENCED = 5;

//        /// <summary>
//        /// - Generates IFCProject-related items
//        /// - Generates Not-referenced-in-structure items
//        /// - Generates Header info
//        /// - Generates check box per items
//        /// </summary>
//        public void BuildTree(IFCViewerWrapper ifcViewer, int ifcModel, IFCItem ifcRoot, TreeView treeControl)
//        {
//            treeControl.Nodes.Clear();

//            if (ifcViewer == null)
//            {
//                throw new ArgumentException("The viewer is null.");
//            }

//            if (ifcModel <= 0)
//            {
//                throw new ArgumentException("Invalid model.");
//            }

//            //if (ifcRoot == null)
//            //{
//            //    throw new ArgumentException("The root is null.");
//            //}

//            if (treeControl == null)
//            {
//                throw new ArgumentException("The tree control is null.");
//            }

//            Cursor.Current = Cursors.WaitCursor;

//            _ifcViewer = ifcViewer;
//            _ifcModel = ifcModel;
//            _ifcRoot = ifcRoot;
//            _treeControl = treeControl;

//            _dicCheckedElements.Clear();

//            CreateHeaderTreeItems();
//            CreateProjectTreeItems();
//            CreateNotReferencedTreeItems();
//        }

//        /// <summary>
//        /// Helper
//        /// </summary>
//        private void CreateHeaderTreeItems()
//        {
//            // Header info
//            TreeNode tnHeaderInfo = _treeControl.Nodes.Add("Header Info");
//            tnHeaderInfo.ImageIndex = tnHeaderInfo.SelectedImageIndex = IMAGE_PROPERTY_SET;

//            // Descriptions
//            TreeNode tnDescriptions = tnHeaderInfo.Nodes.Add("Descriptions");
//            tnDescriptions.ImageIndex = tnDescriptions.SelectedImageIndex = IMAGE_PROPERTY;

//            int i = 0;
//            IntPtr description;
//            while (IfcEngine.x86.GetSPFFHeaderItem(_ifcModel, 0, i++, IfcEngine.x86.sdaiUNICODE, out description) == 0)
//            {
//                TreeNode tnDescription = tnDescriptions.Nodes.Add(Marshal.PtrToStringUni(description));
//                tnDescription.ImageIndex = tnDescription.SelectedImageIndex = IMAGE_PROPERTY;
//            }

//            // ImplementationLevel
//            IntPtr implementationLevel;
//            IfcEngine.x86.GetSPFFHeaderItem(_ifcModel, 1, 0, IfcEngine.x86.sdaiUNICODE, out implementationLevel);

//            TreeNode tnImplementationLevel = tnHeaderInfo.Nodes.Add("ImplementationLevel = '" + Marshal.PtrToStringUni(implementationLevel) + "'");
//            tnImplementationLevel.ImageIndex = tnImplementationLevel.SelectedImageIndex = IMAGE_PROPERTY;

//            // Name
//            IntPtr name;
//            IfcEngine.x86.GetSPFFHeaderItem(_ifcModel, 2, 0, IfcEngine.x86.sdaiUNICODE, out name);

//            TreeNode tnName = tnHeaderInfo.Nodes.Add("Name = '" + Marshal.PtrToStringUni(name) + "'");
//            tnName.ImageIndex = tnName.SelectedImageIndex = IMAGE_PROPERTY;

//            // TimeStamp
//            IntPtr timeStamp;
//            IfcEngine.x86.GetSPFFHeaderItem(_ifcModel, 3, 0, IfcEngine.x86.sdaiUNICODE, out timeStamp);

//            TreeNode tnTimeStamp = tnHeaderInfo.Nodes.Add("TimeStamp = '" + Marshal.PtrToStringUni(timeStamp) + "'");
//            tnTimeStamp.ImageIndex = tnTimeStamp.SelectedImageIndex = IMAGE_PROPERTY;

//            // Authors
//            TreeNode tnAuthors = tnHeaderInfo.Nodes.Add("Authors");
//            tnAuthors.ImageIndex = tnAuthors.SelectedImageIndex = IMAGE_PROPERTY;

//            i = 0;
//            IntPtr author;
//            while (IfcEngine.x86.GetSPFFHeaderItem(_ifcModel, 4, i++, IfcEngine.x86.sdaiUNICODE, out author) == 0)
//            {
//                TreeNode tnAuthor = tnAuthors.Nodes.Add(Marshal.PtrToStringUni(author));
//                tnAuthor.ImageIndex = tnAuthor.SelectedImageIndex = IMAGE_PROPERTY;
//            }

//            // Organizations
//            TreeNode tnOrganizations = tnHeaderInfo.Nodes.Add("Organizations");
//            tnOrganizations.ImageIndex = tnOrganizations.SelectedImageIndex = IMAGE_PROPERTY;

//            i = 0;
//            IntPtr organization;
//            while (IfcEngine.x86.GetSPFFHeaderItem(_ifcModel, 5, i++, IfcEngine.x86.sdaiUNICODE, out organization) == 0)
//            {
//                TreeNode tnOrganization = tnOrganizations.Nodes.Add(Marshal.PtrToStringUni(organization));
//                tnOrganization.ImageIndex = tnOrganization.SelectedImageIndex = IMAGE_PROPERTY;
//            }

//            // PreprocessorVersion
//            IntPtr preprocessorVersion;
//            IfcEngine.x86.GetSPFFHeaderItem(_ifcModel, 6, 0, IfcEngine.x86.sdaiUNICODE, out preprocessorVersion);

//            TreeNode tnPreprocessorVersion = tnHeaderInfo.Nodes.Add("PreprocessorVersion = '" + Marshal.PtrToStringUni(preprocessorVersion) + "'");
//            tnPreprocessorVersion.ImageIndex = tnPreprocessorVersion.SelectedImageIndex = IMAGE_PROPERTY;

//            // OriginatingSystem
//            IntPtr originatingSystem;
//            IfcEngine.x86.GetSPFFHeaderItem(_ifcModel, 7, 0, IfcEngine.x86.sdaiUNICODE, out originatingSystem);

//            TreeNode tnOriginatingSystem = tnHeaderInfo.Nodes.Add("OriginatingSystem = '" + Marshal.PtrToStringUni(originatingSystem) + "'");
//            tnOriginatingSystem.ImageIndex = tnOriginatingSystem.SelectedImageIndex = IMAGE_PROPERTY;

//            // Authorization
//            IntPtr authorization;
//            IfcEngine.x86.GetSPFFHeaderItem(_ifcModel, 8, 0, IfcEngine.x86.sdaiUNICODE, out authorization);

//            TreeNode tnAuthorization = tnHeaderInfo.Nodes.Add("Authorization = '" + Marshal.PtrToStringUni(authorization) + "'");
//            tnAuthorization.ImageIndex = tnAuthorization.SelectedImageIndex = IMAGE_PROPERTY;

//            // FileSchemas
//            TreeNode tnFileSchemas = tnHeaderInfo.Nodes.Add("FileSchemas");
//            tnFileSchemas.ImageIndex = tnFileSchemas.SelectedImageIndex = IMAGE_PROPERTY;

//            i = 0;
//            IntPtr fileSchema;
//            while (IfcEngine.x86.GetSPFFHeaderItem(_ifcModel, 9, i++, IfcEngine.x86.sdaiUNICODE, out fileSchema) == 0)
//            {
//                TreeNode tnFileSchema = tnFileSchemas.Nodes.Add(Marshal.PtrToStringUni(fileSchema));
//                tnFileSchema.ImageIndex = tnFileSchema.SelectedImageIndex = IMAGE_PROPERTY;
//            }
//        }

//        /// <summary>
//        /// Helper
//        /// </summary>
//        private void CreateProjectTreeItems()
//        {
//            int iEntityID = IfcEngine.x86.sdaiGetEntityExtentBN(_ifcModel, "IfcProject");
//            int iEntitiesCount = IfcEngine.x86.sdaiGetMemberCount(iEntityID);

//            for (int iEntity = 0; iEntity < iEntitiesCount; iEntity++)
//            {
//                int iInstance = 0;
//                IfcEngine.x86.engiGetAggrElement(iEntityID, iEntity, IfcEngine.x86.sdaiINSTANCE, out iInstance);

//                IFCTreeItem ifcTreeItem = new IFCTreeItem();
//                ifcTreeItem.instance = iInstance;

//                CreateTreeItem(null, ifcTreeItem);
//                ifcTreeItem.treeNode.ImageIndex = ifcTreeItem.treeNode.SelectedImageIndex = IMAGE_CHECKED;

//                AddChildrenTreeItems(ifcTreeItem, iInstance, "IfcSite");
//            } // for (int iEntity = ...
//        }

//        /// <summary>
//        /// Helper
//        /// </summary>
//        private void CreateNotReferencedTreeItems()
//        {
//            IFCTreeItem ifcTreeItem = new IFCTreeItem();
//            ifcTreeItem.treeNode = _treeControl.Nodes.Add("Not Referenced");
//            ifcTreeItem.treeNode.ForeColor = Color.Gray;
//            ifcTreeItem.treeNode.ImageIndex = ifcTreeItem.treeNode.SelectedImageIndex = IMAGE_CHECKED;
//            ifcTreeItem.treeNode.Tag = ifcTreeItem;

//            FindNonReferencedIFCItems(_ifcRoot, ifcTreeItem.treeNode);

//            if (ifcTreeItem.treeNode.Nodes.Count == 0)
//            {
//                // don't show empty Not Referenced item
//                _treeControl.Nodes.Remove(ifcTreeItem.treeNode);
//            }
//        }

//        /// <summary>
//        /// Helper
//        /// </summary>
//        /// <param name="ifcParent"></param>
//        /// <param name="iParentInstance"></param>
//        /// <param name="strEntityName"></param>

//        private void AddChildrenTreeItems(IFCTreeItem ifcParent, int iParentInstance, string strEntityName)
//        {
//            // check for decomposition
//            IntPtr decompositionInstance;
//            IfcEngine.x86.sdaiGetAttrBN(iParentInstance, "IsDecomposedBy", IfcEngine.x86.sdaiAGGR, out decompositionInstance);

//            if (decompositionInstance == IntPtr.Zero)
//            {
//                return;
//            }

//            int iDecompositionsCount = IfcEngine.x86.sdaiGetMemberCount(decompositionInstance.ToInt32());
//            for (int iDecomposition = 0; iDecomposition < iDecompositionsCount; iDecomposition++)
//            {
//                int iDecompositionInstance = 0;
//                IfcEngine.x86.engiGetAggrElement(decompositionInstance.ToInt32(), iDecomposition, IfcEngine.x86.sdaiINSTANCE, out iDecompositionInstance);

//                if (!IsInstanceOf(iDecompositionInstance, "IFCRELAGGREGATES"))
//                {
//                    continue;
//                }

//                IntPtr objectInstances;
//                IfcEngine.x86.sdaiGetAttrBN(iDecompositionInstance, "RelatedObjects", IfcEngine.x86.sdaiAGGR, out objectInstances);

//                int iObjectsCount = IfcEngine.x86.sdaiGetMemberCount(objectInstances.ToInt32());
//                for (int iObject = 0; iObject < iObjectsCount; iObject++)
//                {
//                    int iObjectInstance = 0;
//                    IfcEngine.x86.engiGetAggrElement(objectInstances.ToInt32(), iObject, IfcEngine.x86.sdaiINSTANCE, out iObjectInstance);

//                    if (!IsInstanceOf(iObjectInstance, strEntityName))
//                    {
//                        continue;
//                    }

//                    IFCTreeItem ifcTreeItem = new IFCTreeItem();
//                    ifcTreeItem.instance = iObjectInstance;

//                    CreateTreeItem(ifcParent, ifcTreeItem);
//                    ifcTreeItem.treeNode.ImageIndex = ifcTreeItem.treeNode.SelectedImageIndex = IMAGE_CHECKED;

//                    switch (strEntityName)
//                    {
//                        case "IfcSite":
//                            {
//                                AddChildrenTreeItems(ifcTreeItem, iObjectInstance, "IfcBuilding");
//                            }
//                            break;

//                        case "IfcBuilding":
//                            {
//                                AddChildrenTreeItems(ifcTreeItem, iObjectInstance, "IfcBuildingStorey");
//                            }
//                            break;

//                        case "IfcBuildingStorey":
//                            {
//                                AddElementTreeItems(ifcTreeItem, iObjectInstance);
//                            }
//                            break;

//                        default:
//                            break;
//                    }
//                } // for (int iObject = ...
//            } // for (int iDecomposition = ...
//        }

//        /// <summary>
//        /// Helper
//        /// </summary>
//        /// <param name="ifcParent"></param>
//        /// <param name="iParentInstance"></param>     
//        private void AddElementTreeItems(IFCTreeItem ifcParent, int iParentInstance)
//        {
//            IntPtr decompositionInstance;
//            IfcEngine.x86.sdaiGetAttrBN(iParentInstance, "IsDecomposedBy", IfcEngine.x86.sdaiAGGR, out decompositionInstance);

//            if (decompositionInstance == IntPtr.Zero)
//            {
//                return;
//            }

//            int iDecompositionsCount = IfcEngine.x86.sdaiGetMemberCount(decompositionInstance.ToInt32());
//            for (int iDecomposition = 0; iDecomposition < iDecompositionsCount; iDecomposition++)
//            {
//                int iDecompositionInstance = 0;
//                IfcEngine.x86.engiGetAggrElement(decompositionInstance.ToInt32(), iDecomposition, IfcEngine.x86.sdaiINSTANCE, out iDecompositionInstance);

//                if (!IsInstanceOf(iDecompositionInstance, "IFCRELAGGREGATES"))
//                {
//                    continue;
//                }

//                IntPtr objectInstances;
//                IfcEngine.x86.sdaiGetAttrBN(iDecompositionInstance, "RelatedObjects", IfcEngine.x86.sdaiAGGR, out objectInstances);

//                int iObjectsCount = IfcEngine.x86.sdaiGetMemberCount(objectInstances.ToInt32());
//                for (int iObject = 0; iObject < iObjectsCount; iObject++)
//                {
//                    int iObjectInstance = 0;
//                    IfcEngine.x86.engiGetAggrElement(objectInstances.ToInt32(), iObject, IfcEngine.x86.sdaiINSTANCE, out iObjectInstance);

//                    IFCTreeItem ifcTreeItem = new IFCTreeItem();
//                    ifcTreeItem.instance = iObjectInstance;
//                    ifcTreeItem.ifcItem = FindIFCItem(_ifcRoot, ifcTreeItem);

//                    CreateTreeItem(ifcParent, ifcTreeItem);

//                    _dicCheckedElements[GetItemType(iObjectInstance)] = true;

//                    if (ifcTreeItem.ifcItem != null)
//                    {
//                        ifcTreeItem.ifcItem.ifcTreeItem = ifcTreeItem;
//                        ifcTreeItem.treeNode.ImageIndex = ifcTreeItem.treeNode.SelectedImageIndex = IMAGE_CHECKED;
//                    }
//                    else
//                    {
//                        ifcTreeItem.treeNode.ImageIndex = ifcTreeItem.treeNode.SelectedImageIndex = IMAGE_NOT_REFERENCED;
//                    }
//                } // for (int iObject = ...
//            } // for (int iDecomposition = ...

//            // check for elements
//            IntPtr elementsInstance;
//            IfcEngine.x86.sdaiGetAttrBN(iParentInstance, "ContainsElements", IfcEngine.x86.sdaiAGGR, out elementsInstance);

//            if (elementsInstance == IntPtr.Zero)
//            {
//                return;
//            }

//            int iElementsCount = IfcEngine.x86.sdaiGetMemberCount(elementsInstance.ToInt32());
//            for (int iElement = 0; iElement < iElementsCount; iElement++)
//            {
//                int iElementInstance = 0;
//                IfcEngine.x86.engiGetAggrElement(elementsInstance.ToInt32(), iElement, IfcEngine.x86.sdaiINSTANCE, out iElementInstance);

//                if (!IsInstanceOf(iElementInstance, "IFCRELCONTAINEDINSPATIALSTRUCTURE"))
//                {
//                    continue;
//                }

//                IntPtr objectInstances;
//                IfcEngine.x86.sdaiGetAttrBN(iElementInstance, "RelatedElements", IfcEngine.x86.sdaiAGGR, out objectInstances);

//                int iObjectsCount = IfcEngine.x86.sdaiGetMemberCount(objectInstances.ToInt32());
//                for (int iObject = 0; iObject < iObjectsCount; iObject++)
//                {
//                    int iObjectInstance = 0;
//                    IfcEngine.x86.engiGetAggrElement(objectInstances.ToInt32(), iObject, IfcEngine.x86.sdaiINSTANCE, out iObjectInstance);

//                    IFCTreeItem ifcTreeItem = new IFCTreeItem();
//                    ifcTreeItem.instance = iObjectInstance;
//                    ifcTreeItem.ifcItem = FindIFCItem(_ifcRoot, ifcTreeItem);

//                    CreateTreeItem(ifcParent, ifcTreeItem);

//                    _dicCheckedElements[GetItemType(iObjectInstance)] = true;

//                    if (ifcTreeItem.ifcItem != null)
//                    {
//                        ifcTreeItem.ifcItem.ifcTreeItem = ifcTreeItem;
//                        ifcTreeItem.treeNode.ImageIndex = ifcTreeItem.treeNode.SelectedImageIndex = IMAGE_CHECKED;

//                        GetColor(ifcTreeItem);
//                    }
//                    else
//                    {
//                        ifcTreeItem.treeNode.ImageIndex = ifcTreeItem.treeNode.SelectedImageIndex = IMAGE_NOT_REFERENCED;
//                    }

//                    IntPtr definedByInstances;
//                    IfcEngine.x86.sdaiGetAttrBN(iObjectInstance, "IsDefinedBy", IfcEngine.x86.sdaiAGGR, out definedByInstances);

//                    if (definedByInstances == IntPtr.Zero)
//                    {
//                        continue;
//                    }

//                    int iDefinedByCount = IfcEngine.x86.sdaiGetMemberCount(definedByInstances.ToInt32());
//                    for (int iDefinedBy = 0; iDefinedBy < iDefinedByCount; iDefinedBy++)
//                    {
//                        int iDefinedByInstance = 0;
//                        IfcEngine.x86.engiGetAggrElement(definedByInstances.ToInt32(), iDefinedBy, IfcEngine.x86.sdaiINSTANCE, out iDefinedByInstance);

//                        if (IsInstanceOf(iDefinedByInstance, "IFCRELDEFINESBYPROPERTIES"))
//                        {
//                            AddPropertyTreeItems(ifcTreeItem, iDefinedByInstance);
//                        }
//                        else
//                        {
//                            if (IsInstanceOf(iDefinedByInstance, "IFCRELDEFINESBYTYPE"))
//                            {
//                                // NA
//                            }
//                        }
//                    }
//                } // for (int iObject = ...
//            } // for (int iDecomposition = ...
//        }

//        /// <summary>
//        /// Helper. 
//        /// </summary>
//        /// <param name="ifcTreeItem"></param>
//        void GetColor(IFCTreeItem ifcTreeItem)
//        {
//            if (ifcTreeItem == null)
//            {
//                throw new ArgumentException("The item is null.");
//            }

//            // C++ => getRGB_object()
//            IntPtr representationInstance;
//            IfcEngine.x86.sdaiGetAttrBN(ifcTreeItem.instance, "Representation", IfcEngine.x86.sdaiINSTANCE, out representationInstance);
//            if (representationInstance == IntPtr.Zero)
//            {
//                return;
//            }

//            // C++ => getRGB_productDefinitionShape()
//            IntPtr representationsInstance;
//            IfcEngine.x86.sdaiGetAttrBN(representationInstance.ToInt32(), "Representations", IfcEngine.x86.sdaiAGGR, out representationsInstance);

//            int iRepresentationsCount = IfcEngine.x86.sdaiGetMemberCount(representationsInstance.ToInt32());
//            for (int iRepresentation = 0; iRepresentation < iRepresentationsCount; iRepresentation++)
//            {
//                int iShapeInstance = 0;
//                IfcEngine.x86.engiGetAggrElement(representationsInstance.ToInt32(), iRepresentation, IfcEngine.x86.sdaiINSTANCE, out iShapeInstance);

//                if (iShapeInstance == 0)
//                {
//                    continue;
//                }

//                // C++ => getRGB_shapeRepresentation()
//                IntPtr representationIdentifier;
//                IfcEngine.x86.sdaiGetAttrBN(iShapeInstance, "RepresentationIdentifier", IfcEngine.x86.sdaiUNICODE, out representationIdentifier);

//                if (Marshal.PtrToStringUni(representationIdentifier) == "Body")
//                {
//                    IntPtr itemsInstance;
//                    IfcEngine.x86.sdaiGetAttrBN(iShapeInstance, "Items", IfcEngine.x86.sdaiAGGR, out itemsInstance);

//                    int iItemsCount = IfcEngine.x86.sdaiGetMemberCount(itemsInstance.ToInt32());
//                    for (int iItem = 0; iItem < iItemsCount; iItem++)
//                    {
//                        int iItemInstance = 0;
//                        IfcEngine.x86.engiGetAggrElement(itemsInstance.ToInt32(), iItem, IfcEngine.x86.sdaiINSTANCE, out iItemInstance);

//                        IntPtr styledByItem;
//                        IfcEngine.x86.sdaiGetAttrBN(iItemInstance, "StyledByItem", IfcEngine.x86.sdaiINSTANCE, out styledByItem);

//                        if (styledByItem != IntPtr.Zero)
//                        {
//                            getRGB_styledItem(ifcTreeItem, styledByItem.ToInt32());
//                        }
//                        else
//                        {
//                            searchDeeper(ifcTreeItem, iItemInstance);
//                        } // else if (iItemInstance != 0)

//                        if (ifcTreeItem.ifcColor != null)
//                        {
//                            return;
//                        }
//                    } // for (int iItem = ...
//                }
//            } // for (int iRepresentation = ...
//        }

//        /// <summary>
//        /// Helper
//        /// </summary>
//        /// <param name="ifcTreeItem"></param>
//        /// <param name="iParentInstance"></param>
//        void searchDeeper(IFCTreeItem ifcTreeItem, int iParentInstance)
//        {
//            IntPtr styledByItem;
//            IfcEngine.x86.sdaiGetAttrBN(iParentInstance, "StyledByItem", IfcEngine.x86.sdaiINSTANCE, out styledByItem);

//            if (styledByItem != IntPtr.Zero)
//            {
//                getRGB_styledItem(ifcTreeItem, styledByItem.ToInt32());
//                if (ifcTreeItem.ifcColor != null)
//                {
//                    return;
//                }
//            }

//            if (IsInstanceOf(iParentInstance, "IFCBOOLEANCLIPPINGRESULT"))
//            {
//                IntPtr firstOperand;
//                IfcEngine.x86.sdaiGetAttrBN(iParentInstance, "FirstOperand", IfcEngine.x86.sdaiINSTANCE, out firstOperand);

//                if (firstOperand != IntPtr.Zero)
//                {
//                    searchDeeper(ifcTreeItem, firstOperand.ToInt32());
//                }
//            } // if (IsInstanceOf(iParentInstance, "IFCBOOLEANCLIPPINGRESULT"))
//            else
//            {
//                if (IsInstanceOf(iParentInstance, "IFCMAPPEDITEM"))
//                {
//                    IntPtr mappingSource;
//                    IfcEngine.x86.sdaiGetAttrBN(iParentInstance, "MappingSource", IfcEngine.x86.sdaiINSTANCE, out mappingSource);

//                    IntPtr mappedRepresentation;
//                    IfcEngine.x86.sdaiGetAttrBN(mappingSource.ToInt32(), "MappedRepresentation", IfcEngine.x86.sdaiINSTANCE, out mappedRepresentation);

//                    if (mappedRepresentation != IntPtr.Zero)
//                    {
//                        IntPtr representationIdentifier;
//                        IfcEngine.x86.sdaiGetAttrBN(mappedRepresentation.ToInt32(), "RepresentationIdentifier", IfcEngine.x86.sdaiUNICODE, out representationIdentifier);

//                        if (Marshal.PtrToStringUni(representationIdentifier) == "Body")
//                        {
//                            IntPtr itemsInstance;
//                            IfcEngine.x86.sdaiGetAttrBN(mappedRepresentation.ToInt32(), "Items", IfcEngine.x86.sdaiAGGR, out itemsInstance);

//                            int iItemsCount = IfcEngine.x86.sdaiGetMemberCount(itemsInstance.ToInt32());
//                            for (int iItem = 0; iItem < iItemsCount; iItem++)
//                            {
//                                int iItemInstance = 0;
//                                IfcEngine.x86.engiGetAggrElement(itemsInstance.ToInt32(), iItem, IfcEngine.x86.sdaiINSTANCE, out iItemInstance);

//                                styledByItem = IntPtr.Zero;
//                                IfcEngine.x86.sdaiGetAttrBN(iItemInstance, "StyledByItem", IfcEngine.x86.sdaiINSTANCE, out styledByItem);

//                                if (styledByItem != IntPtr.Zero)
//                                {
//                                    getRGB_styledItem(ifcTreeItem, styledByItem.ToInt32());
//                                }
//                                else
//                                {
//                                    searchDeeper(ifcTreeItem, iItemInstance);
//                                } // else if (iItemInstance != 0)

//                                if (ifcTreeItem.ifcColor != null)
//                                {
//                                    return;
//                                }
//                            } // for (int iItem = ...
//                        } // if (Marshal.PtrToStringAnsi(representationIdentifier) == "Body")
//                    } // if (mappedRepresentation != IntPtr.Zero)
//                } // if (IsInstanceOf(iParentInstance, "IFCMAPPEDITEM"))
//            } // else if (IsInstanceOf(iParentInstance, "IFCBOOLEANCLIPPINGRESULT"))
//        }

//        /// <summary>
//        /// Helper
//        /// </summary>
//        /// <param name="iStyledByItemInstance"></param>
//        void getRGB_styledItem(IFCTreeItem ifcTreeItem, int iStyledByItemInstance)
//        {
//            IntPtr stylesInstance;
//            IfcEngine.x86.sdaiGetAttrBN(iStyledByItemInstance, "Styles", IfcEngine.x86.sdaiAGGR, out stylesInstance);

//            int iStylesCount = IfcEngine.x86.sdaiGetMemberCount(stylesInstance.ToInt32());
//            for (int iStyle = 0; iStyle < iStylesCount; iStyle++)
//            {
//                int iStyleInstance = 0;
//                IfcEngine.x86.engiGetAggrElement(stylesInstance.ToInt32(), iStyle, IfcEngine.x86.sdaiINSTANCE, out iStyleInstance);

//                if (iStyleInstance == 0)
//                {
//                    continue;
//                }

//                getRGB_presentationStyleAssignment(ifcTreeItem, iStyleInstance);
//            } // for (int iStyle = ...
//        }

//        /// <summary>
//        /// Helper
//        /// </summary>
//        /// <param name="iParentInstance"></param>
//        void getRGB_presentationStyleAssignment(IFCTreeItem ifcTreeItem, int iParentInstance)
//        {
//            IntPtr stylesInstance;
//            IfcEngine.x86.sdaiGetAttrBN(iParentInstance, "Styles", IfcEngine.x86.sdaiAGGR, out stylesInstance);

//            int iStylesCount = IfcEngine.x86.sdaiGetMemberCount(stylesInstance.ToInt32());
//            for (int iStyle = 0; iStyle < iStylesCount; iStyle++)
//            {
//                int iStyleInstance = 0;
//                IfcEngine.x86.engiGetAggrElement(stylesInstance.ToInt32(), iStyle, IfcEngine.x86.sdaiINSTANCE, out iStyleInstance);

//                if (iStyleInstance == 0)
//                {
//                    continue;
//                }

//                getRGB_surfaceStyle(ifcTreeItem, iStyleInstance);
//            } // for (int iStyle = ...
//        }

//        /// <summary>
//        /// Helper
//        /// </summary>
//        /// <param name="iParentInstance"></param>
//        unsafe void getRGB_surfaceStyle(IFCTreeItem ifcTreeItem, int iParentInstance)
//        {
//            IntPtr stylesInstance;
//            IfcEngine.x86.sdaiGetAttrBN(iParentInstance, "Styles", IfcEngine.x86.sdaiAGGR, out stylesInstance);

//            int iStylesCount = IfcEngine.x86.sdaiGetMemberCount(stylesInstance.ToInt32());
//            for (int iStyle = 0; iStyle < iStylesCount; iStyle++)
//            {
//                int iStyleInstance = 0;
//                IfcEngine.x86.engiGetAggrElement(stylesInstance.ToInt32(), iStyle, IfcEngine.x86.sdaiINSTANCE, out iStyleInstance);

//                if (iStyleInstance == 0)
//                {
//                    continue;
//                }

//                IntPtr surfaceColour;
//                IfcEngine.x86.sdaiGetAttrBN(iStyleInstance, "SurfaceColour", IfcEngine.x86.sdaiINSTANCE, out surfaceColour);

//                if (surfaceColour == IntPtr.Zero)
//                {
//                    continue;
//                }

//                double R = 0;
//                IfcEngine.x86.sdaiGetAttrBN(surfaceColour.ToInt32(), "Red", IfcEngine.x86.sdaiREAL, out *(IntPtr*)&R);

//                double G = 0;
//                IfcEngine.x86.sdaiGetAttrBN(surfaceColour.ToInt32(), "Green", IfcEngine.x86.sdaiREAL, out *(IntPtr*)&G);

//                double B = 0;
//                IfcEngine.x86.sdaiGetAttrBN(surfaceColour.ToInt32(), "Blue", IfcEngine.x86.sdaiREAL, out *(IntPtr*)&B);

//                ifcTreeItem.ifcColor = new IFCItemColor();
//                ifcTreeItem.ifcColor.R = (float)R;
//                ifcTreeItem.ifcColor.G = (float)G;
//                ifcTreeItem.ifcColor.B = (float)B;

//                return;
//            } // for (int iStyle = ...
//        }

//        /// <summary>
//        /// Helper
//        /// </summary>
//        /// <param name="ifcParent"></param>
//        /// <param name="iParentInstance"></param>     
//        private void AddPropertyTreeItems(IFCTreeItem ifcParent, int iParentInstance)
//        {
//            IntPtr propertyInstances;
//            IfcEngine.x86.sdaiGetAttrBN(iParentInstance, "RelatingPropertyDefinition", IfcEngine.x86.sdaiINSTANCE, out propertyInstances);

//            if (IsInstanceOf(propertyInstances.ToInt32(), "IFCELEMENTQUANTITY"))
//            {
//                IFCTreeItem ifcPropertySetTreeItem = new IFCTreeItem();
//                ifcPropertySetTreeItem.instance = propertyInstances.ToInt32();

//                CreateTreeItem(ifcParent, ifcPropertySetTreeItem);
//                ifcPropertySetTreeItem.treeNode.ImageIndex = ifcPropertySetTreeItem.treeNode.SelectedImageIndex = IMAGE_PROPERTY_SET;

//                // check for quantity
//                IntPtr quantitiesInstance;
//                IfcEngine.x86.sdaiGetAttrBN(propertyInstances.ToInt32(), "Quantities", IfcEngine.x86.sdaiAGGR, out quantitiesInstance);

//                if (quantitiesInstance == IntPtr.Zero)
//                {
//                    return;
//                }

//                int iQuantitiesCount = IfcEngine.x86.sdaiGetMemberCount(quantitiesInstance.ToInt32());
//                for (int iQuantity = 0; iQuantity < iQuantitiesCount; iQuantity++)
//                {
//                    int iQuantityInstance = 0;
//                    IfcEngine.x86.engiGetAggrElement(quantitiesInstance.ToInt32(), iQuantity, IfcEngine.x86.sdaiINSTANCE, out iQuantityInstance);

//                    IFCTreeItem ifcQuantityTreeItem = new IFCTreeItem();
//                    ifcQuantityTreeItem.instance = iQuantityInstance;

//                    if (IsInstanceOf(iQuantityInstance, "IFCQUANTITYLENGTH"))
//                        CreatePropertyTreeItem(ifcPropertySetTreeItem, ifcQuantityTreeItem, "IFCQUANTITYLENGTH");
//                    else
//                        if (IsInstanceOf(iQuantityInstance, "IFCQUANTITYAREA"))
//                            CreatePropertyTreeItem(ifcPropertySetTreeItem, ifcQuantityTreeItem, "IFCQUANTITYAREA");
//                        else
//                            if (IsInstanceOf(iQuantityInstance, "IFCQUANTITYVOLUME"))
//                                CreatePropertyTreeItem(ifcPropertySetTreeItem, ifcQuantityTreeItem, "IFCQUANTITYVOLUME");
//                            else
//                                if (IsInstanceOf(iQuantityInstance, "IFCQUANTITYCOUNT"))
//                                    CreatePropertyTreeItem(ifcPropertySetTreeItem, ifcQuantityTreeItem, "IFCQUANTITYCOUNT");
//                                else
//                                    if (IsInstanceOf(iQuantityInstance, "IFCQUANTITYWEIGTH"))
//                                        CreatePropertyTreeItem(ifcPropertySetTreeItem, ifcQuantityTreeItem, "IFCQUANTITYWEIGTH");
//                                    else
//                                        if (IsInstanceOf(iQuantityInstance, "IFCQUANTITYTIME"))
//                                            CreatePropertyTreeItem(ifcPropertySetTreeItem, ifcQuantityTreeItem, "IFCQUANTITYTIME");
//                } // for (int iQuantity = ...
//            }
//            else
//            {
//                if (IsInstanceOf(propertyInstances.ToInt32(), "IFCPROPERTYSET"))
//                {
//                    IFCTreeItem ifcPropertySetTreeItem = new IFCTreeItem();
//                    ifcPropertySetTreeItem.instance = propertyInstances.ToInt32();

//                    CreateTreeItem(ifcParent, ifcPropertySetTreeItem);
//                    ifcPropertySetTreeItem.treeNode.ImageIndex = ifcPropertySetTreeItem.treeNode.SelectedImageIndex = IMAGE_PROPERTY_SET;

//                    // check for quantity
//                    IntPtr propertiesInstance;
//                    IfcEngine.x86.sdaiGetAttrBN(propertyInstances.ToInt32(), "HasProperties", IfcEngine.x86.sdaiAGGR, out propertiesInstance);

//                    if (propertiesInstance == IntPtr.Zero)
//                    {
//                        return;
//                    }

//                    int iPropertiesCount = IfcEngine.x86.sdaiGetMemberCount(propertiesInstance.ToInt32());
//                    for (int iProperty = 0; iProperty < iPropertiesCount; iProperty++)
//                    {
//                        int iPropertyInstance = 0;
//                        IfcEngine.x86.engiGetAggrElement(propertiesInstance.ToInt32(), iProperty, IfcEngine.x86.sdaiINSTANCE, out iPropertyInstance);

//                        if (!IsInstanceOf(iPropertyInstance, "IFCPROPERTYSINGLEVALUE"))
//                            continue;

//                        IFCTreeItem ifcPropertyTreeItem = new IFCTreeItem();
//                        ifcPropertyTreeItem.instance = iPropertyInstance;

//                        CreatePropertyTreeItem(ifcPropertySetTreeItem, ifcPropertyTreeItem, "IFCPROPERTYSINGLEVALUE");
//                    } // for (int iProperty = ...
//                }
//            }
//        }

//        /// <summary>
//        /// Helper
//        /// </summary>
//        /// <param name="ifcParent"></param>
//        /// <param name="ifcItem"></param>
//        private void CreateTreeItem(IFCTreeItem ifcParent, IFCTreeItem ifcItem)
//        {
//            //            IntPtr ifcType = IfcEngine.x86.engiGetInstanceClassInfo(ifcItem.instance);
//            //            string strIfcType = Marshal.PtrToStringAnsi(ifcType);

//            int entity = IfcEngine.x86.sdaiGetInstanceType(ifcItem.instance);
//            IntPtr entityNamePtr = IntPtr.Zero;
//            IfcEngine.x86.engiGetEntityName(entity, IfcEngine.x86.sdaiUNICODE, out entityNamePtr);
//            string strIfcType = Marshal.PtrToStringUni(entityNamePtr);

//            IntPtr name;
//            IfcEngine.x86.sdaiGetAttrBN(ifcItem.instance, "Name", IfcEngine.x86.sdaiUNICODE, out name);

//            string strName = Marshal.PtrToStringUni(name);

//            IntPtr description;
//            IfcEngine.x86.sdaiGetAttrBN(ifcItem.instance, "Description", IfcEngine.x86.sdaiUNICODE, out description);

//            string strDescription = Marshal.PtrToStringUni(description);

//            string strItemText = "'" + (string.IsNullOrEmpty(strName) ? "<name>" : strName) +
//                    "', '" + (string.IsNullOrEmpty(strDescription) ? "<description>" : strDescription) +
//                    "' (" + strIfcType + ")";

//            if ((ifcParent != null) && (ifcParent.treeNode != null))
//            {
//                ifcItem.treeNode = ifcParent.treeNode.Nodes.Add(strItemText);
//            }
//            else
//            {
//                ifcItem.treeNode = _treeControl.Nodes.Add(strItemText);
//            }

//            if (ifcItem.ifcItem == null)
//            {
//                // item without visual representation
//                ifcItem.treeNode.ForeColor = Color.Gray;
//            }

//            ifcItem.treeNode.Tag = ifcItem;
//        }

//        /// <summary>
//        /// Helper
//        /// </summary>
//        /// <param name="ifcParent"></param>
//        /// <param name="ifcItem"></param>
//        private void CreatePropertyTreeItem(IFCTreeItem ifcParent, IFCTreeItem ifcItem, string strProperty)
//        {
//            //IntPtr ifcType = IfcEngine.x86.engiGetInstanceClassInfo(ifcItem.instance);
//            int entity = IfcEngine.x86.sdaiGetInstanceType(ifcItem.instance);
//            IntPtr entityNamePtr = IntPtr.Zero;
//            IfcEngine.x86.engiGetEntityName(entity, IfcEngine.x86.sdaiUNICODE, out entityNamePtr);
//            //string strIfcType = Marshal.PtrToStringAnsi(ifcType);
//            string strIfcType = Marshal.PtrToStringUni(entityNamePtr);

//            IntPtr name;
//            IfcEngine.x86.sdaiGetAttrBN(ifcItem.instance, "Name", IfcEngine.x86.sdaiUNICODE, out name);

//            string strName = Marshal.PtrToStringUni(name);

//            string strValue = string.Empty;
//            switch (strProperty)
//            {
//                case "IFCQUANTITYLENGTH":
//                    {
//                        IntPtr value;
//                        IfcEngine.x86.sdaiGetAttrBN(ifcItem.instance, "LengthValue", IfcEngine.x86.sdaiUNICODE, out value);

//                        strValue = Marshal.PtrToStringUni(value);
//                    }
//                    break;

//                case "IFCQUANTITYAREA":
//                    {
//                        IntPtr value;
//                        IfcEngine.x86.sdaiGetAttrBN(ifcItem.instance, "AreaValue", IfcEngine.x86.sdaiUNICODE, out value);

//                        strValue = Marshal.PtrToStringUni(value);
//                    }
//                    break;

//                case "IFCQUANTITYVOLUME":
//                    {
//                        IntPtr value;
//                        IfcEngine.x86.sdaiGetAttrBN(ifcItem.instance, "VolumeValue", IfcEngine.x86.sdaiUNICODE, out value);

//                        strValue = Marshal.PtrToStringUni(value);
//                    }
//                    break;

//                case "IFCQUANTITYCOUNT":
//                    {
//                        IntPtr value;
//                        IfcEngine.x86.sdaiGetAttrBN(ifcItem.instance, "CountValue", IfcEngine.x86.sdaiUNICODE, out value);

//                        strValue = Marshal.PtrToStringUni(value);
//                    }
//                    break;

//                case "IFCQUANTITYWEIGTH":
//                    {
//                        IntPtr value;
//                        IfcEngine.x86.sdaiGetAttrBN(ifcItem.instance, "WeigthValue", IfcEngine.x86.sdaiUNICODE, out value);

//                        strValue = Marshal.PtrToStringUni(value);
//                    }
//                    break;

//                case "IFCQUANTITYTIME":
//                    {
//                        IntPtr value;
//                        IfcEngine.x86.sdaiGetAttrBN(ifcItem.instance, "TimeValue", IfcEngine.x86.sdaiUNICODE, out value);

//                        strValue = Marshal.PtrToStringUni(value);
//                    }
//                    break;

//                case "IFCPROPERTYSINGLEVALUE":
//                    {
//                        IntPtr value;
//                        IfcEngine.x86.sdaiGetAttrBN(ifcItem.instance, "NominalValue", IfcEngine.x86.sdaiUNICODE, out value);

//                        strValue = Marshal.PtrToStringUni(value);
//                    }
//                    break;

//                default:
//                    throw new Exception("Unknown property.");
//            } // switch (strProperty)    

//            string strItemText = "'" + (string.IsNullOrEmpty(strName) ? "<name>" : strName) +
//                    "' = '" + (string.IsNullOrEmpty(strValue) ? "<value>" : strValue) +
//                    "' (" + strIfcType + ")";

//            if ((ifcParent != null) && (ifcParent.treeNode != null))
//            {
//                ifcItem.treeNode = ifcParent.treeNode.Nodes.Add(strItemText);
//            }
//            else
//            {
//                ifcItem.treeNode = _treeControl.Nodes.Add(strItemText);
//            }

//            if (ifcItem.ifcItem == null)
//            {
//                // item without visual representation
//                ifcItem.treeNode.ForeColor = Color.Gray;
//            }

//            ifcItem.treeNode.ImageIndex = ifcItem.treeNode.SelectedImageIndex = IMAGE_PROPERTY;
//        }

//        /// <summary>
//        /// Helper
//        /// </summary>
//        /// <param name="ifcTreeItem"></param>
//        private IFCItem FindIFCItem(IFCItem ifcParent, IFCTreeItem ifcTreeItem)
//        {
//            if (ifcParent == null)
//            {
//                return null;
//            }

//            IFCItem ifcIterator = ifcParent;
//            while (ifcIterator != null)
//            {
//                if (ifcIterator.ifcID == ifcTreeItem.instance)
//                {
//                    return ifcIterator;
//                }

//                IFCItem ifcItem = FindIFCItem(ifcIterator.child, ifcTreeItem);
//                if (ifcItem != null)
//                {
//                    return ifcItem;
//                }

//                ifcIterator = ifcIterator.next;
//            }

//            return FindIFCItem(ifcParent.child, ifcTreeItem);
//        }

//        /// <summary>
//        /// Helper
//        /// </summary>
//        /// <param name="ifcTreeItem"></param>
//        private void FindNonReferencedIFCItems(IFCItem ifcParent, TreeNode tnNotReferenced)
//        {
//            if (ifcParent == null)
//            {
//                return;
//            }

//            IFCItem ifcIterator = ifcParent;
//            while (ifcIterator != null)
//            {
//                if ((ifcIterator.ifcTreeItem == null) && (ifcIterator.ifcID != 0))
//                {
//                    string strItemText = "'" + (string.IsNullOrEmpty(ifcIterator.name) ? "<name>" : ifcIterator.name) +
//                            "' = '" + (string.IsNullOrEmpty(ifcIterator.description) ? "<description>" : ifcIterator.description) +
//                            "' (" + (string.IsNullOrEmpty(ifcIterator.ifcType) ? ifcIterator.globalID : ifcIterator.ifcType) + ")";

//                    IFCTreeItem ifcTreeItem = new IFCTreeItem();
//                    ifcTreeItem.instance = ifcIterator.ifcID;
//                    ifcTreeItem.treeNode = tnNotReferenced.Nodes.Add(strItemText);
//                    ifcTreeItem.ifcItem = FindIFCItem(_ifcRoot, ifcTreeItem);
//                    ifcIterator.ifcTreeItem = ifcTreeItem;
//                    ifcTreeItem.treeNode.Tag = ifcTreeItem;

//                    if (ifcTreeItem.ifcItem != null)
//                    {
//                        ifcTreeItem.ifcItem.ifcTreeItem = ifcTreeItem;
//                        ifcTreeItem.treeNode.ImageIndex = ifcTreeItem.treeNode.SelectedImageIndex = IMAGE_CHECKED;
//                    }
//                    else
//                    {
//                        ifcTreeItem.treeNode.ImageIndex = ifcTreeItem.treeNode.SelectedImageIndex = IMAGE_NOT_REFERENCED;
//                    }
//                }

//                FindNonReferencedIFCItems(ifcIterator.child, tnNotReferenced);

//                ifcIterator = ifcIterator.next;
//            }

//            FindNonReferencedIFCItems(ifcParent.child, tnNotReferenced);
//        }

//        /// <summary>
//        /// Helper
//        /// </summary>
//        /// <param name="iInstance"></param>
//        /// <returns></returns>
//        private string GetItemType(int iInstance)
//        {
//            //            IntPtr ifcType = IfcEngine.x86.engiGetInstanceClassInfo(iInstance);
//            //            return Marshal.PtrToStringAnsi(ifcType);

//            int entity = IfcEngine.x86.sdaiGetInstanceType(iInstance);
//            IntPtr entityNamePtr = IntPtr.Zero;
//            IfcEngine.x86.engiGetEntityName(entity, IfcEngine.x86.sdaiUNICODE, out entityNamePtr);
//            return Marshal.PtrToStringUni(entityNamePtr);
//        }

//        /// <summary>
//        /// Helper
//        /// </summary>
//        /// <param name="iInstance"></param>
//        /// <param name="strType"></param>
//        /// <returns></returns>
//        private bool IsInstanceOf(int iInstance, string strType)
//        {
//            if (IfcEngine.x86.sdaiGetInstanceType(iInstance) == IfcEngine.x86.sdaiGetEntity(_ifcModel, strType))
//            {
//                return true;
//            }

//            return false;
//        }

//        /// <summary>
//        /// Handler
//        /// </summary>
//        /// <param name="sender"></param>
//        /// <param name="e"></param>
//        public void OnNodeMouseClick(object sender, TreeNodeMouseClickEventArgs e)
//        {
//            Rectangle rcIcon = new Rectangle(e.Node.Bounds.Location - new Size(16, 0), new Size(16, 16));
//            if (!rcIcon.Contains(e.Location))
//            {
//                return;
//            }

//            if (e.Node.Tag == null)
//            {
//                // skip properties
//                return;
//            }

//            switch (e.Node.ImageIndex)
//            {
//                case IMAGE_CHECKED:
//                    {
//                        e.Node.ImageIndex = e.Node.SelectedImageIndex = IMAGE_UNCHECKED;

//                        OnNodeMouseClick_UpdateChildrenTreeItems(e.Node);
//                        UpdateParentTreeItems(e.Node);

//                        _ifcViewer.Redraw();
//                    }
//                    break;

//                case IMAGE_UNCHECKED:
//                    {
//                        e.Node.ImageIndex = e.Node.SelectedImageIndex = IMAGE_CHECKED;

//                        OnNodeMouseClick_UpdateChildrenTreeItems(e.Node);
//                        UpdateParentTreeItems(e.Node);

//                        _ifcViewer.Redraw();
//                    }
//                    break;
//            } // switch (e.Node.ImageIndex)
//        }

//        /// <summary>
//        /// Helper
//        /// </summary>
//        /// <param name="tnParent"></param>
//        private void OnNodeMouseClick_UpdateChildrenTreeItems(TreeNode tnParent)
//        {
//            foreach (TreeNode tnChild in tnParent.Nodes)
//            {
//                if ((tnChild.ImageIndex != IMAGE_CHECKED) && (tnChild.ImageIndex != IMAGE_UNCHECKED))
//                {
//                    // skip properties
//                    continue;
//                }

//                switch (tnParent.ImageIndex)
//                {
//                    case IMAGE_CHECKED:
//                        {
//                            tnChild.ImageIndex = tnChild.SelectedImageIndex = IMAGE_CHECKED;
//                        }
//                        break;

//                    case IMAGE_UNCHECKED:
//                        {
//                            tnChild.ImageIndex = tnChild.SelectedImageIndex = IMAGE_UNCHECKED;
//                        }
//                        break;
//                } // switch (tnParent.ImageIndex)

//                OnNodeMouseClick_UpdateChildrenTreeItems(tnChild);
//            } // foreach (TreeNode tnChild in ...
//        }

//        /// <summary>
//        /// Helper
//        /// </summary>
//        /// <param name="tnItem"></param>
//        private void UpdateParentTreeItems(TreeNode tnItem)
//        {
//            if (tnItem.Parent == null)
//            {
//                return;
//            }

//            int iCheckedChildrenCount = 0;
//            foreach (TreeNode tnChild in tnItem.Parent.Nodes)
//            {
//                if ((tnChild.ImageIndex != IMAGE_CHECKED) && (tnChild.ImageIndex != IMAGE_UNCHECKED))
//                {
//                    // skip properties
//                    continue;
//                }

//                if (tnChild.ImageIndex == IMAGE_CHECKED)
//                {
//                    iCheckedChildrenCount++;
//                }
//            } // foreach (TreeNode tnChild in ...

//            tnItem.Parent.ImageIndex = tnItem.Parent.SelectedImageIndex = iCheckedChildrenCount > 0 ? IMAGE_CHECKED : IMAGE_UNCHECKED;

//            UpdateParentTreeItems(tnItem.Parent);
//        }

//        /// <summary>
//        /// Handler
//        /// </summary>
//        public void OnAfterSelect(object sender, TreeViewEventArgs e)
//        {
//            if (e.Node.Tag == null)
//            {
//                // skip properties
//                return;
//            }

//            if (e.Node.ImageIndex != IMAGE_CHECKED)
//            {
//                // skip unvisible & not referenced items
//                return;
//            }

//            _ifcViewer.SelectItem((e.Node.Tag as IFCTreeItem).ifcItem);
//        }

//        /// <summary>
//        /// Handler
//        /// </summary>
//        public void OnContextMenu_Opened(object sender, EventArgs e)
//        {
//            ContextMenuStrip contextMenu = sender as ContextMenuStrip;

//            contextMenu.Items.Clear();
//            foreach (var pair in this._dicCheckedElements)
//            {
//                ToolStripMenuItem menuItem = contextMenu.Items.Add(pair.Key) as ToolStripMenuItem;
//                menuItem.CheckOnClick = true;
//                menuItem.Checked = pair.Value;

//                menuItem.Click += new EventHandler(delegate(object item, EventArgs args)
//                {
//                    _dicCheckedElements[((ToolStripMenuItem)item).Text] = ((ToolStripMenuItem)item).Checked;

//                    foreach (TreeNode node in _treeControl.Nodes)
//                    {
//                        OnContextMenu_UpdateTreeElement(node);
//                    }

//                    _ifcViewer.Redraw();
//                });
//            }
//        }

//        /// <summary>
//        /// Helper
//        /// </summary>
//        /// <param name="tnParent"></param>
//        private void OnContextMenu_UpdateTreeElement(TreeNode tnParent)
//        {
//            if (tnParent.Tag != null)
//            {
//                OnContextMenu_UpdateTreeElement(tnParent.Tag as IFCTreeItem);
//            }

//            foreach (TreeNode tnChild in tnParent.Nodes)
//            {
//                if (tnChild.Tag != null)
//                {
//                    OnContextMenu_UpdateTreeElement(tnChild.Tag as IFCTreeItem);
//                }

//                OnContextMenu_UpdateTreeElement(tnChild);
//            } // foreach (TreeNode tnChild in ...
//        }

//        /// <summary>
//        /// Helper
//        /// </summary>
//        /// <param name="ifcTreeItem"></param>
//        private void OnContextMenu_UpdateTreeElement(IFCTreeItem ifcTreeItem)
//        {
//            if (ifcTreeItem.ifcItem == null)
//            {
//                // skip not referenced items
//                return;
//            }

//            if (string.IsNullOrEmpty(ifcTreeItem.ifcItem.ifcType))
//            {
//                // skip fake items
//                return;
//            }

//            if (!_dicCheckedElements.ContainsKey(ifcTreeItem.ifcItem.ifcType))
//            {
//                // skip non-element items
//                return;
//            }

//            ifcTreeItem.treeNode.ImageIndex = ifcTreeItem.treeNode.SelectedImageIndex =
//                _dicCheckedElements[ifcTreeItem.ifcItem.ifcType] ? IMAGE_CHECKED : IMAGE_UNCHECKED;

//            UpdateParentTreeItems(ifcTreeItem.treeNode);
//        }

//        /// <summary>
//        /// Helper
//        /// </summary>
//        /// <param name="ifcItem"></param>
//        public void OnSelectIFCElement(IFCItem ifcItem)
//        {
//            System.Diagnostics.Debug.Assert(ifcItem != null, "Internal error.");
//            System.Diagnostics.Debug.Assert(ifcItem.ifcTreeItem != null, "Internal error.");
//            System.Diagnostics.Debug.Assert(ifcItem.ifcTreeItem.treeNode != null, "Internal error.");

//            _treeControl.SelectedNode = ifcItem.ifcTreeItem.treeNode;
//        }
//    }
//}

