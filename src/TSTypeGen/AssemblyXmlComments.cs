using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;

namespace TSTypeGen
{
    public class AssemblyXmlComments
    {
        private XmlDocument _document;

        private const string TypeScriptCommentNodeName = "typeScriptComment";
        private const string SummaryCommentNodeName = "summary";

        public AssemblyXmlComments(string xmlCommentsFilePath)
        {
            _document = new XmlDocument();
            _document.Load(xmlCommentsFilePath);
        }

        public List<string> GetTypeScriptComment(MemberInfo memberInfo)
        {
            var element = GetDocumentation(memberInfo);
            var typeScriptCommentElement = element?.SelectSingleNode(TypeScriptCommentNodeName) ?? element?.SelectSingleNode(SummaryCommentNodeName);
            if (string.IsNullOrEmpty(typeScriptCommentElement?.InnerText.Trim()))
                return null;

            return GetTypeScriptComment(typeScriptCommentElement.InnerText);
        }

        public List<string> GetTypeScriptComment(Type classOrInterface)
        {
            var element = GetDocumentation(classOrInterface);
            var typeScriptCommentElement = element?.SelectSingleNode(TypeScriptCommentNodeName) ?? element?.SelectSingleNode(SummaryCommentNodeName);
            if (string.IsNullOrEmpty(typeScriptCommentElement?.InnerText.Trim()))
            {
                return null;
            }

            return GetTypeScriptComment(typeScriptCommentElement.InnerText);
        }

        private List<string> GetTypeScriptComment(string comment)
        {
            comment = Regex.Replace(comment, Environment.NewLine + "$", "");
            comment = Regex.Replace(comment, "^" + Environment.NewLine, "");

            var result = new List<string>();

            var maxIndentationSize = int.MaxValue;

            using var sr = new StringReader(comment);
            string line;
            while ((line = sr.ReadLine()) != null)
            {
                if (line.Trim() != string.Empty)
                {
                    var indentationSize = 0;
                    foreach (var chr in line)
                    {
                        if (chr == ' ')
                            indentationSize++;
                        else
                            break;
                    }

                    if (indentationSize < maxIndentationSize)
                        maxIndentationSize = indentationSize;

                    result.Add(line);
                }
                else
                {
                    result.Add("");
                }
            }

            if (result.Last() == string.Empty)
            {
                result.RemoveAt(result.Count - 1);
            }

            return result.Select(line => Regex.Replace(line, "^" + new string(' ', maxIndentationSize), "")).ToList();
        }

        private XmlElement GetDocumentation(Type type)
        {
            return XmlFromName(type, 'T', "");
        }

        private XmlElement GetDocumentation(MemberInfo memberInfo)
        {
            return XmlFromName(memberInfo.DeclaringType, memberInfo.MemberType.ToString()[0], memberInfo.Name);
        }

        private XmlElement XmlFromName(Type type, char prefix, string name)
        {
            string fullName;

            if (string.IsNullOrEmpty(name))
                fullName = prefix + ":" + type.FullName;
            else
                fullName = prefix + ":" + type.FullName + "." + name;

            var matchedElement = _document["doc"]["members"].SelectSingleNode("member[@name='" + fullName + "']") as XmlElement;

            return matchedElement;
        }
    }
}
