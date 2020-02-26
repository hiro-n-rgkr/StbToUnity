﻿using System.Collections.Generic;
using System.Xml.Linq;
using System;

using UnityEngine;

public partial class STBReader:MonoBehaviour {
    /// <summary>
    /// Make Slab GameObjects
    /// </summary>
    static void MakeSlabObjs(XDocument xDoc) {
        int[] nodeIndex = new int[4];
        string slabName;
        int slabNum = 0;
        var xSlabs = xDoc.Root.Descendants("StbSlab");
        GameObject slabs = new GameObject("StbSlabs");

        foreach (var xSlab in xSlabs) {
            List<int> xSlabNodeIds = new List<int>();
            Mesh meshObj = new Mesh();
            int countNode = 0;

            var xNodeIds = xSlab.Element("StbNodeid_List").Elements("StbNodeid");
            foreach (var xNodeId in xNodeIds) {
                xSlabNodeIds.Add((int)xNodeId.Attribute("id"));
                countNode++;
            }
            int i = 0;
            while (i < 4) {
                if (countNode == 4)
                    nodeIndex[i] = m_vertexIDs.IndexOf(xSlabNodeIds[i]);
                else if (i == 3) // triangle slab
                    break;
                i++;
            }
            meshObj = CreateMesh.Slab(m_stbNodes, nodeIndex);

            slabName = string.Format("Slab{0}", slabNum);
            GameObject slab = new GameObject(slabName);
            slab.AddComponent<MeshFilter>().mesh = meshObj;
            slab.AddComponent<MeshRenderer>().material = new Material(Shader.Find("Custom/CulloffSurfaceShader")) {
                color = GetMemberColor("RC", "Slab")
            };
            slab.transform.parent = slabs.transform;

            slabNum++;
            xSlabNodeIds.Clear(); // foreachごとでListにAddし続けてるのでここで値をClear
        }
    }

    static void MakeElementMesh(XDocument xDoc, string xDateTag, string structType) {
        Vector3 nodeStart, nodeEnd;
        float hight = 0;
        float width = 0;
        int elemNum = 0;
        int stbSecIndex = 0;
        int nodeIndexStart, nodeIndexEnd, xNodeStart, xNodeEnd, xElementIdSection, idSection;
        var xElements = xDoc.Root.Descendants(xDateTag);
        string shape, xKind;
        string shapeType = "";

        GameObject elements = new GameObject(xDateTag + "s");
        GameObject barObj = new GameObject(xDateTag + "Bar");
        foreach (var xElement in xElements) {
            switch (structType) {
                case "Girder":
                case "Beam":
                case "Brace":
                    xNodeStart = (int)xElement.Attribute("idNode_start");
                    xNodeEnd = (int)xElement.Attribute("idNode_end");
                    break;
                case "Column":
                case "Post":
                    xNodeStart = (int)xElement.Attribute("idNode_bottom");
                    xNodeEnd = (int)xElement.Attribute("idNode_top");
                    break;
                default:
                    xNodeStart = 0;
                    xNodeEnd = 0;
                    break;
            }
            xElementIdSection = (int)xElement.Attribute("id_section");
            xKind = (string)xElement.Attribute("kind_structure");

            // 始点と終点の座標取得
            nodeIndexStart = m_vertexIDs.IndexOf(xNodeStart);
            nodeIndexEnd = m_vertexIDs.IndexOf(xNodeEnd);
            nodeStart = m_stbNodes[nodeIndexStart];
            nodeEnd = m_stbNodes[nodeIndexEnd];

            if (xKind == "RC") {
                switch (structType) {
                    case "Girder":
                    case "Beam":
                        stbSecIndex = m_xRcBeamId.IndexOf(xElementIdSection);
                        hight = m_xRcBeamDepth[stbSecIndex] / 1000f;
                        width = m_xRcBeamWidth[stbSecIndex] / 1000f;
                        break;
                    case "Column":
                    case "Post":
                        stbSecIndex = m_xRcColumnId.IndexOf(xElementIdSection);
                        hight = m_xRcColumnDepth[stbSecIndex] / 1000f;
                        width = m_xRcColumnWidth[stbSecIndex] / 1000f;
                        break;
                    default:
                        break;
                }
                if (width == 0)
                    shapeType = "Pipe";
                else
                    shapeType = "BOX";
            }
            else if (xKind == "S") {
                switch (structType) {
                    case "Girder":
                    case "Beam":
                        idSection = m_xStBeamId.IndexOf(xElementIdSection);
                        shape = m_xStBeamShape[idSection];
                        break;
                    case "Column":
                    case "Post":
                        idSection = m_xStColumnId.IndexOf(xElementIdSection);
                        shape = m_xStColumnShape[idSection];
                        break;
                    case "Brace":
                        idSection = m_xStBraceId.IndexOf(xElementIdSection);
                        shape = m_xStBraceShape[idSection];
                        break;
                    default:
                        shape = "";
                        break;
                }
                stbSecIndex = m_xStName.IndexOf(shape);
                hight = m_xStParamA[stbSecIndex] / 1000f;
                width = m_xStParamB[stbSecIndex] / 1000f;
                shapeType = m_xStType[stbSecIndex];
            }
            m_shapeMesh = MakeElementsMeshFromVertex(nodeStart, nodeEnd, hight, width, shapeType, structType, elemNum, elements, xKind);
            // 配筋の作成
            if (xKind == "RC") {
                if (shapeType == "BOX") {
                    switch (structType) {
                        case "Column":
                        case "Post":
                            CreateBar.Column(stbSecIndex, nodeStart, nodeEnd, width, hight, barObj, elemNum);
                            break;
                        case "Girder":
                        case "Beam":
                            CreateBar.Beam(stbSecIndex, nodeStart, nodeEnd, width, hight, barObj, elemNum);
                            break;
                        default:
                            break;
                    }
                }
            }
            elemNum++;
        }
        m_shapeMesh.Clear();
    }

    public static List<Mesh> MakeElementsMeshFromVertex(Vector3 nodeStart, Vector3 nodeEnd, float hight, float width, string shapeType, string structType, int elementNum, GameObject elements, string kind) {
        Vector3[] vertexS = new Vector3[6];
        Vector3[] vertexE = new Vector3[6];
        Mesh meshObj = new Mesh();

        float dx = nodeEnd.x - nodeStart.x;
        float dy = nodeEnd.y - nodeStart.y;
        float dz = nodeEnd.z - nodeStart.z;
        float angleY = -1f * Mathf.Atan2(dy, dx);
        float angleZ = -1f * Mathf.Atan2(dz, dx);

        // 梁は部材天端の中心が起点に対して、柱・ブレースは部材芯が起点なので場合分け
        switch (structType) {
            case "Girder":
            case "Beam":
                vertexS = GetGirderVertex(nodeStart, width, hight, angleZ);
                vertexE = GetGirderVertex(nodeEnd, width, hight, angleZ);
                break;
            case "Column":
            case "Post":
                vertexS = GetColumnVertex(nodeStart, width, hight, angleY);
                vertexE = GetColumnVertex(nodeEnd, width, hight, angleY);
                break;
            case "Brace":
                vertexS = GetBraceVertex(nodeStart, width, hight, angleZ);
                vertexE = GetBraceVertex(nodeEnd, width, hight, angleZ);
                break;
            default: break;
        }
        switch (shapeType) {
            case "H":
                meshObj = CreateMesh.H(vertexS, vertexE); break;
            case "BOX":
                meshObj = CreateMesh.BOX(vertexS, vertexE); break;
            case "Pipe":
                meshObj = CreateMesh.Pipe(nodeStart, nodeEnd, hight / 2); break;
            case "L":
                meshObj = CreateMesh.L(vertexS, vertexE); break;
            default: break;
        }

        string name = string.Format(structType + "{0}", elementNum);
        GameObject element = new GameObject(name);
        element.AddComponent<MeshFilter>().mesh = meshObj;
        element.AddComponent<MeshRenderer>().material = new Material(Shader.Find("Custom/CulloffSurfaceShader")) {
                color = GetMemberColor(kind, structType)
        };
        element.transform.parent = elements.transform;

        return m_shapeMesh;
    }

    static Color GetMemberColor(string kind, string structType) {
        Color unexpected = new Color(1, 0, 1, 1);

        if (kind == "RC") {
            switch (structType) {
                case "Column": return ColorInput.m_memberColor[0];
                case "Post": return ColorInput.m_memberColor[1];
                case "Girder": return ColorInput.m_memberColor[2];
                case "Beam": return ColorInput.m_memberColor[3];
                case "Brace": return ColorInput.m_memberColor[4];
                case "Slab": return ColorInput.m_memberColor[5];
                default: return unexpected;
            }
        }
        else if (kind == "S") {
            switch (structType) {
                case "Column": return ColorInput.m_memberColor[6];
                case "Post": return ColorInput.m_memberColor[7];
                case "Girder": return ColorInput.m_memberColor[8];
                case "Beam": return ColorInput.m_memberColor[9];
                case "Brace": return ColorInput.m_memberColor[10];
                default: return unexpected;
            }
        }
        else return unexpected;
    }

    static Vector3[] GetGirderVertex(Vector3 node, float width, float hight, float angle) {
        //  Y        3 - 4 - 5 
        //  ^        |   |   |  
        //  o >  X   0 - 1 - 2
        Vector3[] vertex = new Vector3[6];

        vertex[0] = new Vector3(node.x + width / 2 * (float)Math.Sin(angle),
                                node.y - hight,
                                node.z + width / 2 * (float)Math.Cos(angle)
                                );
        vertex[1] = new Vector3(node.x,
                                node.y - hight,
                                node.z
                                );
        vertex[2] = new Vector3(node.x - width / 2 * (float)Math.Sin(angle),
                                node.y - hight,
                                node.z - width / 2 * (float)Math.Cos(angle)
                                );
        vertex[3] = new Vector3(node.x + width / 2 * (float)Math.Sin(angle),
                                node.y,
                                node.z + width / 2 * (float)Math.Cos(angle)
                                );
        vertex[4] = node;
        vertex[5] = new Vector3(node.x - width / 2 * (float)Math.Sin(angle),
                                node.y,
                                node.z - width / 2 * (float)Math.Cos(angle)
                                );
        return (vertex);
    }

    static Vector3[] GetColumnVertex(Vector3 node, float width, float hight, float angle) {
        //  Y        3 - 4 - 5 
        //  ^        |   |   |  
        //  o >  X   0 - 1 - 2
        Vector3[] vertex = new Vector3[6];

        vertex[0] = new Vector3(node.x - width / 2 * (float)Math.Sin(angle),
                                node.y - width / 2 * (float)Math.Cos(angle),
                                node.z - hight / 2
                                );
        vertex[1] = new Vector3(node.x,
                                node.y,
                                node.z + hight / 2
                                );
        vertex[2] = new Vector3(node.x + width / 2 * (float)Math.Sin(angle),
                                node.y + width / 2 * (float)Math.Cos(angle),
                                node.z - hight / 2
                                );
        vertex[3] = new Vector3(node.x - width / 2 * (float)Math.Sin(angle),
                                node.y - width / 2 * (float)Math.Cos(angle),
                                node.z + hight / 2
                                );
        vertex[4] = new Vector3(node.x,
                                node.y,
                                node.z - hight / 2
                                );
        vertex[5] = new Vector3(node.x + width / 2 * (float)Math.Sin(angle),
                                node.y + width / 2 * (float)Math.Cos(angle),
                                node.z + hight / 2
                                );
        return (vertex);
    }

    static Vector3[] GetBraceVertex(Vector3 node, float width, float hight, float angle) {
        //  Y        3 - 4 - 5 
        //  ^        |   |   |  
        //  o >  X   0 - 1 - 2
        Vector3[] vertex = new Vector3[6];

        vertex[0] = new Vector3(node.x + width / 2 * (float)Math.Sin(angle),
                                node.y - width / 2,
                                node.z + width / 2 * (float)Math.Cos(angle)
                                );
        vertex[1] = new Vector3(node.x,
                                node.y - width / 2,
                                node.z
                                );
        vertex[2] = new Vector3(node.x - width / 2 * (float)Math.Sin(angle),
                                node.y - width / 2,
                                node.z - width / 2 * (float)Math.Cos(angle)
                                );
        vertex[3] = new Vector3(node.x + width / 2 * (float)Math.Sin(angle),
                                node.y + width / 2,
                                node.z + width / 2 * (float)Math.Cos(angle)
                                );
        vertex[4] = new Vector3(node.x,
                                node.y + width / 2,
                                node.z
                                );
        vertex[5] = new Vector3(node.x - width / 2 * (float)Math.Sin(angle),
                                node.y + width / 2,
                                node.z - width / 2 * (float)Math.Cos(angle)
                                );
        return (vertex);
    }
}