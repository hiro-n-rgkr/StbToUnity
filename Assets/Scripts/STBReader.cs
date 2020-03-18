﻿using System.Collections.Generic;
using System.Xml.Linq;
using SFB;
using UnityEngine;
using Stevia.STB.Model;
using Stevia.STB.Model.Member;
using Stevia.STB.Model.Section;


namespace Stevia {

    public partial class STBReader:MonoBehaviour {
        [SerializeField]
        Material _material;

        //List<int> _xRcColumnId = new List<int>();
        //List<int> _xRcColumnDepth = new List<int>();
        //List<int> _xRcColumnWidth = new List<int>();
        //public static List<List<int>> _xRcColumnBar = new List<List<int>>();

        List<int> _xStColumnId = new List<int>();
        List<string> _xStColumnShape = new List<string>();

        List<int> _xRcBeamId = new List<int>();
        List<int> _xRcBeamDepth = new List<int>();
        List<int> _xRcBeamWidth = new List<int>();
        public static List<List<int>> _xRcBeamBar = new List<List<int>>();

        List<int> _xStBeamId = new List<int>();
        List<string> _xStBeamShape = new List<string>();

        List<int> _xStBraceId = new List<int>();
        List<string> _xStBraceShape = new List<string>();
        List<string> _xStName = new List<string>();
        List<float> _xStParamA = new List<float>();
        List<float> _xStParamB = new List<float>();
        List<string> _xStType = new List<string>();
        List<Mesh> _shapeMesh = new List<Mesh>();

        StbNodes _nodes = new StbNodes();
        StbStorys _storys = new StbStorys();
        public static StbSecColRC _stbSecColRC  = new StbSecColRC();

        void Start() {
            int i = 0;
            // stbデータの読み込み
            XDocument xDoc = GetStbFileData();
            // ノードの取得
            _nodes.LoadData(xDoc);
            // 階情報の取得
            _storys.LoadData(xDoc);
            // スラブの取得
            MakeSlabObjs(xDoc);
            // StbSecColumn_RC の取得
            _stbSecColRC.Load(xDoc);

            // StbSecColumn_S の取得
            var xStColumns = xDoc.Root.Descendants("StbSecColumn_S");
            foreach (var xSecSColumn in xStColumns) {
                _xStColumnId.Add((int)xSecSColumn.Attribute("id"));
                _xStColumnShape.Add((string)xSecSColumn.Element("StbSecSteelColumn").Attribute("shape"));
            }
            // StbSecBeam_RC の取得
            var xRcBeams = xDoc.Root.Descendants("StbSecBeam_RC");
            foreach (var xRcBeam in xRcBeams) {
                _xRcBeamId.Add((int)xRcBeam.Attribute("id"));
                var xFigure = xRcBeam.Element("StbSecFigure");
                var xBar = xRcBeam.Element("StbSecBar_Arrangement");

                // 子要素が StbSecHaunch か StbSecStraight を判定
                if (xFigure.Element("StbSecHaunch") != null) {
                    _xRcBeamDepth.Add((int)xFigure.Element("StbSecHaunch").Attribute("depth_center"));
                    _xRcBeamWidth.Add((int)xFigure.Element("StbSecHaunch").Attribute("width_center"));
                }
                else {
                    _xRcBeamDepth.Add((int)xFigure.Element("StbSecStraight").Attribute("depth"));
                    _xRcBeamWidth.Add((int)xFigure.Element("StbSecStraight").Attribute("width"));
                }
                _xRcBeamBar.Add(GetBeamBarInfo(xBar));
            }
            // StbSecBeam_S の取得
            var xStBeams = xDoc.Root.Descendants("StbSecBeam_S");
            foreach (var xStBeam in xStBeams) {
                _xStBeamId.Add((int)xStBeam.Attribute("id"));
                _xStBeamShape.Add((string)xStBeam.Element("StbSecSteelBeam").Attribute("shape"));
            }
            // StbSecBrace_S の取得
            var xStBraces = xDoc.Root.Descendants("StbSecBrace_S");
            foreach (var xStBrace in xStBraces) {
                _xStBraceId.Add((int)xStBrace.Attribute("id"));
                _xStBraceShape.Add((string)xStBrace.Element("StbSecSteelBrace").Attribute("shape"));
            }
            // S断面形状の取得
            i = 0;
            string[,] SteelSecName = GetSteelSecNameArray();
            while (i < SteelSecName.GetLength(0)) {
                GetStbSteelSection(xDoc, SteelSecName[i, 0], SteelSecName[i, 1]);
                i++;
            }
            // meshの生成
            i = 0;
            string[,] memberName = GetMemberNameArray();
            while (i < memberName.GetLength(0)) {
                MakeElementMesh(xDoc, memberName[i, 0], memberName[i, 1]);
                i++;
            }
            // 配筋表示は最初はオフにする
            DisplySettings.BarOff();
        }

        XDocument GetStbFileData() {
            var extensions = new[] {
            new ExtensionFilter("ST-Bridge Files", "stb", "STB" ),
            new ExtensionFilter("All Files", "*" ),
        };
            string paths = StandaloneFileBrowser.OpenFilePanel("Open File", "", extensions, true)[0];
            XDocument xDoc = XDocument.Load(paths);
            return (xDoc);
        }

        string[,] GetSteelSecNameArray() {
            string[,] steelSecNameArray = new string[7, 2] {
            {"StbSecRoll-H", "H"},
            {"StbSecBuild-H", "H"},
            {"StbSecRoll-BOX", "BOX"},
            {"StbSecBuild-BOX", "BOX"},
            {"StbSecPipe", "Pipe"},
            {"StbSecRoll-L", "L"},
            {"StbSecRoll-Bar", "Bar"}
        };
            return (steelSecNameArray);
        }

        string[,] GetMemberNameArray() {
            string[,] memberNameArray = new string[5, 2] {
            {"StbColumn", "Column"},
            {"StbGirder", "Girder"},
            {"StbPost", "Post"},
            {"StbBeam", "Beam"},
            {"StbBrace", "Brace"}
        };
            return (memberNameArray);
        }

        //List<int> GetColumnBarInfo(XElement xBar) {
        //    List<int> barList = new List<int>();
        //    string elementName = "StbSecRect_Column_Same";

        //    // Main 1
        //    barList.Add((int)xBar.Element(elementName).Attribute("count_main_X_1st"));
        //    barList.Add((int)xBar.Element(elementName).Attribute("count_main_Y_1st"));
        //    // Main2
        //    if (xBar.Element(elementName).Attribute("count_main_X_2nd") != null)
        //        barList.Add((int)xBar.Element(elementName).Attribute("count_main_X_2nd"));
        //    else
        //        barList.Add(0);
        //    if (xBar.Element(elementName).Attribute("count_main_Y_2nd") != null)
        //        barList.Add((int)xBar.Element(elementName).Attribute("count_main_Y_2nd"));
        //    else
        //        barList.Add(0);
        //    // Main total
        //    barList.Add((int)xBar.Element(elementName).Attribute("count_main_total"));
        //    // Band
        //    barList.Add((int)xBar.Element(elementName).Attribute("pitch_band"));
        //    barList.Add((int)xBar.Element(elementName).Attribute("count_band_dir_X"));
        //    barList.Add((int)xBar.Element(elementName).Attribute("count_band_dir_Y"));
        //    return (barList);
        //}

        List<int> GetBeamBarInfo(XElement xBar) {
            List<int> barList = new List<int>();
            string elementName;

            if (xBar.Element("StbSecBeam_Start_Center_End_Section") != null)
                elementName = "StbSecBeam_Start_Center_End_Section";
            else if (xBar.Element("StbSecBeam_Start_End_Section") != null)
                elementName = "StbSecBeam_Start_End_Section";
            else if (xBar.Element("StbSecBeam_Same_Section") != null)
                elementName = "StbSecBeam_Same_Section";
            else
                return (new List<int> { 2, 2, 0, 0, 0, 0, 200, 2 });

            // Main 1
            barList.Add((int)xBar.Element(elementName).Attribute("count_main_top_1st"));
            barList.Add((int)xBar.Element(elementName).Attribute("count_main_bottom_1st"));
            // Main2
            if (xBar.Element(elementName).Attribute("count_main_top_2nd") != null)
                barList.Add((int)xBar.Element(elementName).Attribute("count_main_top_2nd"));
            else
                barList.Add(0);
            if (xBar.Element(elementName).Attribute("count_main_bottom_2nd") != null)
                barList.Add((int)xBar.Element(elementName).Attribute("count_main_bottom_2nd"));
            else
                barList.Add(0);
            // Main3
            if (xBar.Element(elementName).Attribute("count_main_top_3rd") != null)
                barList.Add((int)xBar.Element(elementName).Attribute("count_main_top_3rd"));
            else
                barList.Add(0);
            if (xBar.Element(elementName).Attribute("count_main_bottom_3rd") != null)
                barList.Add((int)xBar.Element(elementName).Attribute("count_main_bottom_3rd"));
            else
                barList.Add(0);
            // Band
            barList.Add((int)xBar.Element(elementName).Attribute("pitch_stirrup"));
            barList.Add((int)xBar.Element(elementName).Attribute("count_stirrup"));
            return (barList);
        }
    }
}
