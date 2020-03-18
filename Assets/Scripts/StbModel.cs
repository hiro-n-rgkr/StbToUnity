﻿using System;
using System.Collections.Generic;
using System.Xml.Linq;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Stevia.STB.Model {
    /// <summary>
    /// 位置・断面情報（節点・部材・階・軸）
    /// </summary>
    public class StbModel {
        // TODO 一括でStbModelに属するものを読み込めるようにする
    }

    /// <summary>
    /// 節点（複数） 各節点を管理
    /// </summary>
    public class StbNodes {
        public List<int> Id { get; } = new List<int>();
        public List<double> X { get; } = new List<double>();
        public List<double> Y { get; } = new List<double>();
        public List<double> Z { get; } = new List<double>();
        public List<NodeKind> Kind { get; } = new List<NodeKind>();
        public List<int> IdMember { get; } = new List<int>();
        public List<Vector3> Vertex { get; } = new List<Vector3>();

        public void LoadData(XDocument stbDoc) {
            int index = 0;
            var stbNodes = stbDoc.Root.Descendants("StbNode");
            foreach (var stbNode in stbNodes) {
                // 必須コード
                Id.Add((int)stbNode.Attribute("id"));
                X.Add((double)stbNode.Attribute("x") / 1000d);
                Y.Add((double)stbNode.Attribute("z") / 1000d); // Y-Up対応
                Z.Add((double)stbNode.Attribute("y") / 1000d);
                
                // 必須ではないコード
                if (stbNode.Attribute("id_member") != null) {
                    IdMember.Add((int)stbNode.Attribute("id_member"));
                }
                else {
                    IdMember.Add(-1);
                }
                switch ((string)stbNode.Attribute("kind")) {
                    case "ON_BEAM": Kind.Add(NodeKind.ON_BEAM); break;
                    case "ON_COLUMN": Kind.Add(NodeKind.ON_COLUMN); break;
                    case "ON_GRID": Kind.Add(NodeKind.ON_GRID); break;
                    case "ON_CANTI": Kind.Add(NodeKind.ON_CANTI); break;
                    case "ON_SLAB": Kind.Add(NodeKind.ON_SLAB); break;
                    case "OTHER": Kind.Add(NodeKind.OTHER); break;
                    default: break;
                }

                // StbNodeにはない追加した属性
                Vertex.Add(new Vector3((float)X[index], (float)Y[index], (float)Z[index]));
                index++;
            }
        }

        public enum NodeKind {
            ON_BEAM,
            ON_COLUMN,
            ON_GRID,
            ON_CANTI,
            ON_SLAB,
            OTHER
        }
    }

    /// <summary>
    /// 軸情報
    /// </summary>
    public class StbAxes {
    }

    /// <summary>
    /// 階情報（複数）
    /// </summary>
    public class StbStorys {
        public List<int> Id { get; } = new List<int>();
        public List<string> Name { get; } = new List<string>();
        public List<double> Height { get; } = new List<double>();
        public List<StoryKind> Kind { get; } = new List<StoryKind>();
        public List<int> IdDependens { get; } = new List<int>();
        public List<string> StrengthConcrete { get; } = new List<string>();
        public List<List<int>> NodeIdList { get; } = new List<List<int>>();

        public void LoadData(XDocument stbData) {
            var stbStorys = stbData.Root.Descendants("StbStory");
            foreach (var stbStory in stbStorys) {
                // 必須コード
                Id.Add((int)stbStory.Attribute("id"));
                Height.Add((double)stbStory.Attribute("height") / 1000d);
                switch ((string)stbStory.Attribute("kind")) {
                    case "GENERAL": Kind.Add(StoryKind.GENERAL); break;
                    case "BASEMENT": Kind.Add(StoryKind.BASEMENT); break;
                    case "ROOF": Kind.Add(StoryKind.ROOF); break;
                    case "PENTHOUSE": Kind.Add(StoryKind.PENTHOUSE); break;
                    case "ISOLATION": Kind.Add(StoryKind.ISOLATION); break;
                    default: break;
                }
                
                // 必須ではないコード
                // リストの長さが合うように、空の場合はstring.Enpty
                if (stbStory.Attribute("name") != null) {
                    Name.Add((string)stbStory.Attribute("name"));
                }
                else {
                    Name.Add(string.Empty);
                }
                if (stbStory.Attribute("concrete_strength") != null) {
                    StrengthConcrete.Add((string)stbStory.Attribute("concrete_strength"));
                }
                else {
                    StrengthConcrete.Add(string.Empty);
                }

                // TODO
                // 所属節点の読み込み　List<List<int>> NodeIdList　の Set 部分の作成
            }
        }

        public enum StoryKind { 
            GENERAL,
            BASEMENT,
            ROOF,
            PENTHOUSE,
            ISOLATION
        }
    }

    /// <summary>
    /// 柱・梁・スラブ・壁などの部材情報
    /// </summary>
    public class StbMembers {
    }

    /// <summary>
    /// 断面情報
    /// </summary>
    public class StbSections {
    }

    /// <summary>
    /// 継手情報
    /// </summary>
    public class StbJoints {
    }

    /// <summary>
    /// 床組（複数）
    /// </summary>
    public class StbSlabFrames {
    }
}
