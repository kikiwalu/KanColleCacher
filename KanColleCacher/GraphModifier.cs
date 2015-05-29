﻿using d_f_32.KanColleCacher;
using d_f_32.KanColleCacher.Configuration;
using Fiddler;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

namespace Gizeta.KanColleCacher
{
    public class GraphModifier
    {
        private string jsonData = "";

        public GraphModifier()
        {
            this.Initialize();
        }

        public void Initialize()
        {
            FiddlerApplication.BeforeRequest += FiddlerApplication_BeforeRequest;
            FiddlerApplication.BeforeResponse += FiddlerApplication_BeforeResponse;

            var folder = new DirectoryInfo(Path.Combine(Settings.Current.CacheFolder, "kcs", "resources", "swf", "ships"));
            if (folder.Exists)
            {
                foreach (var file in folder.GetFiles())
                {
                    if (file.FullName.EndsWith(".config.ini"))
                    {
                        ModifyData.Items.Add(new ModifyData(file.FullName));
                    }
                }
            }
        }

        public void Dispose()
        {
            FiddlerApplication.BeforeRequest -= FiddlerApplication_BeforeRequest;
            FiddlerApplication.BeforeResponse -= FiddlerApplication_BeforeResponse;

            ModifyData.Items.Clear();
            jsonData = "";
        }

        private void setModifiedData(ModifyData data)
        {
            string graphStr = Regex.Match(jsonData, @"\{([^{]+?)" + data.FileName + @"([^}]+?)\}").Groups[0].Value;
            string sortNo = Regex.Match(graphStr, @"api_sortno"":(\d+)").Groups[1].Value;
            string infoStr = Regex.Match(jsonData, @"\{([^{]+?)api_sortno"":" + sortNo + @"([^}]+?)\}").Groups[0].Value;

            var graphReplaceStr = graphStr;
            var infoReplaceStr = infoStr;

            var temp = data.Data["ship_name"];
            if (temp != null)
            {
                infoReplaceStr = Regex.Replace(infoReplaceStr, @"api_name"":""(.+?)""", @"api_name"":""" + temp + @"""");
            }

            var modList = new string[] { "boko_n", "boko_d",
                                         "kaisyu_n", "kaisyu_d",
                                         "kaizo_n", "kaizo_d",
                                         "map_n", "map_d",
                                         "ensyuf_n", "ensyuf_d",
                                         "ensyue_n",
                                         "battle_n", "battle_d" };
            foreach (var mod in modList)
            {
                if(!data.Data.ContainsKey("boko_n_left")) break;

                temp = data.Data[mod + "_left"];
                if (temp != null)
                {
                    graphReplaceStr = Regex.Replace(graphReplaceStr, mod + @""":\[([\d-]+),([\d-]+)\]", mod + @""":[" + temp + @",$2]");
                }

                temp = data.Data[mod + "_top"];
                if (temp != null)
                {
                    graphReplaceStr = Regex.Replace(graphReplaceStr, mod + @""":\[([\d-]+),([\d-]+)\]", mod + @""":[$1," + temp + @"]");
                }
            }

            jsonData = jsonData.Replace(graphStr, graphReplaceStr);
            jsonData = jsonData.Replace(infoStr, infoReplaceStr);
        }

        private void FiddlerApplication_BeforeRequest(Session oSession)
        {
            if (oSession.PathAndQuery.StartsWith("/kcsapi/api_start2"))
            {
                oSession.bBufferResponse = true;
            }
        }

        private void FiddlerApplication_BeforeResponse(Session oSession)
        {
            if (oSession.PathAndQuery.StartsWith("/kcsapi/api_start2"))
            {
                jsonData = oSession.GetResponseBodyAsString();
                ModifyData.Items.ForEach(x => setModifiedData(x));
                oSession.utilSetResponseBody(jsonData);
            }
        }
    }

    internal class ModifyData
    {
        public static List<ModifyData> Items = new List<ModifyData>();

        internal ModifyData(string path)
        {
            var st = path.LastIndexOf('\\') + 1;
            var ed = path.LastIndexOf(".config.ini");
            if (st > 0 && ed > st)
            {
                this.FileName = path.Substring(st, ed - st);
            }
            else
            {
                this.FileName = "Unknown";
            }

            this.Data = new Dictionary<string,string>();
            var parser = ConfigParser.ReadIniFile(path);
            if (parser["info"] != null)
            {
                this.Data.Add("ship_name", parser["info"]["ship_name"]);
            }
            if (parser["graph"] != null)
            {
                var modList = new string[] { "boko_n", "boko_d",
                                             "kaisyu_n", "kaisyu_d",
                                             "kaizo_n", "kaizo_d",
                                             "map_n", "map_d",
                                             "ensyuf_n", "ensyuf_d",
                                             "ensyue_n",
                                             "battle_n", "battle_d" };
                foreach (var mod in modList)
                {
                    this.Data.Add(mod + "_left", parser["graph"][mod + "_left"]);
                    this.Data.Add(mod + "_top", parser["graph"][mod + "_top"]);
                }
            }
        }

        public string FileName { get; set; }
        public Dictionary<string, string> Data { get; set; }
    }
}
