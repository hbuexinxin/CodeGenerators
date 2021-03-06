﻿/*
This file is part of XXsd2Code <http://xxsd2code.sourceforge.net/>

XXsd2Code is free software: you can redistribute it and/or modify
it under the terms of the GNU General Public License as published by
the Free Software Foundation, either version 3 of the License, or
any later version.

XXsd2Code is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
GNU General Public License for more details.

You should have received a copy of the GNU General Public License
along with XXsd2Code.  If not, see <http://www.gnu.org/licenses/>.
*/


using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Diagnostics;
using System.Reflection;
using System.Xml.Schema;

namespace XXsd2Code.LanguageWriters
{
    public class JavaWriter: LanguageWriterBase
    {
        public JavaWriter(
            List<String> classNamesNoNestedTypes,
            List<String> classNames,
            List<String> includeFiles,
            List<String> includeFilesToSkip,
            List<String> externalNamespaces,
            Dictionary<int, string> xsdnNamespaces,
            string destinationFolder,
            string outerClassName,
            Dictionary<string, List<ClassElement>> externalClassesToGenerateMap,
            Dictionary<string, string> externalClassesnNamespaces,
            Dictionary<string, string> externalEnumsnNamespaces,
            Dictionary<string, List<EnumElement>> externalEnumsToGenerateMap,
            TargetLanguage targetLanguage
        ) 
        {
            _targetLanguage = targetLanguage;
            _classNamesNoNestedTypes = classNamesNoNestedTypes;
            _classNames = classNames;
            _includeFiles = includeFiles;
            _includeFilesToSkip = includeFilesToSkip;
            _externalNamespaces = externalNamespaces;
            _xsdnNamespaces = xsdnNamespaces;
            _destinationFolder = destinationFolder;
            _outerClassName = outerClassName;
            _externalClassesToGenerateMap = externalClassesToGenerateMap;
            _externalClassesnNamespaces = externalClassesnNamespaces;
            _externalEnumsnNamespaces = externalEnumsnNamespaces;
            _externalEnumsToGenerateMap = externalEnumsToGenerateMap;
        }

        public override void Write(StreamWriter sw, string packageName, Dictionary<string,
                                                               List<EnumElement>> enumsToGenerateMap,
                                                               Dictionary<string, List<ClassElement>> classesToGenerateMap)
        {
            IndentLevel = 0;

            sw.WriteLine("//Auto generated code");
            FileVersionInfo fv = FileVersionInfo.GetVersionInfo(Assembly.GetExecutingAssembly().Location);
            sw.WriteLine("//Code generated by XXsd2Code<http://xxsd2code.sourceforge.net/> {0} version {1}", fv.InternalName, fv.FileVersion, System.DateTime.Now.ToString());
            sw.WriteLine("//For any comments/suggestions contact code generator author at asheesh.goja@gmail.com");
            sw.WriteLine("//Auto generated code");
            sw.WriteLine(" ");

            sw.WriteLine(String.Format("package {0};", packageName));
            //sw.WriteLine(" ");
            //sw.WriteLine("Interfaces.ICloneable;");
           // sw.WriteLine(" ");

            #region Imports statements for elements

            foreach (KeyValuePair<String, List<ClassElement>> classe in classesToGenerateMap)
            {
                foreach (ClassElement var in classe.Value)
                {
                    if (var.Type == XmlTypeCode.Date)
                    {
                        sw.WriteLine("import java.util.Date;");
                        break;
                    }
                }
            }

            //if (enumsToGenerateMap.Count > 0)
            //    sw.WriteLine("import javax.xml.bind.annotation.XmlEnumValue;");

            sw.WriteLine(" ");

            #endregion

            //sw.WriteLine(" ");
            //sw.WriteLine(" ");

            //GenerateOuterClassBeginBlock(sw);
            //GenerateEnums(sw, enumsToGenerateMap);
            GenerateClasses(sw, classesToGenerateMap, enumsToGenerateMap);
            //GenerateOuterClassEndBlock(sw);

            sw.WriteLine("");
            sw.WriteLine("");

            sw.Close();

        }


        protected override void WriteClass(StreamWriter sw, string className, List<ClassElement> classMetadata,
                    Dictionary<String, List<ClassElement>> classesToGenerateMap,
                    Dictionary<String, List<EnumElement>> enumsToGenerateMap)
        {

            if (IsExternalType(className))
            {
                classesToGenerateMap.Remove(className);
                return;
            }

            List<string> dep = GetClassDependencies(className, classesToGenerateMap);

            foreach (string s in dep)
            {
                if (s == className) continue;

                if (classesToGenerateMap.ContainsKey(s))
                {
                    WriteClass(sw, s, classesToGenerateMap[s], classesToGenerateMap, enumsToGenerateMap);
                    classesToGenerateMap.Remove(s);
                }
            }
            List<ClassElement> val = classMetadata;

            string contextClassName = "";
            contextClassName = String.Format("{0}", className);

            List<string> vars = new List<string>();

            sw.WriteLine(" ");

            sw.WriteLine("{0}public\tclass\t{1} implements Cloneable", GetTab(), contextClassName);
            sw.WriteLine("{0}{1}", GetTab(), "{");


            #region Declarations

            IndentLevel++;
            String collectionType = String.Empty;
            foreach (ClassElement var in val)
            {
                if (var.IsCollection)
                {
                    collectionType = "java.util.List<" + XSDToJavaType(var) + ">";

                    sw.WriteLine("");
                    sw.WriteLine("{0}public\t{1}\t{2};{3}", GetTab(), collectionType, var.Name, var.Comment);
                }
                else
                {
                    String typeName = XSDToJavaType(var);
                    //if (String.Equals(typeName, "boolean", StringComparison.Ordinal))
                    //{
                    //    //.Net XML serializer writes bool as "true" and "false", but native serializer
                    //    //only handles 1 / 0. Generate a property to override the default XML serialization
                    //    //behavior such that serialized bool's are 1 / 0
                    //    String newVar = String.Format("xml_{0}", var.Name);
                    //    sw.WriteLine();
                    //    sw.Write(GetTab());
                    //    sw.Write("@XmlTransient ");
                    //    sw.WriteLine("protected {0} {1}; {2}", typeName, var.Name, var.Comment);
                    //    sw.Write(GetTab());
                    //    sw.Write("@XmlElement(name = \"{0}\") ", var.Name);
                    //    sw.WriteLine("private int {0}; ", newVar);
                    //    sw.Write(GetTab());                      
                    //    sw.WriteLine("protected int getXml_{0}() {{ return {0} ? 1 : 0; }}", var.Name);
                    //    sw.Write(GetTab());
                    //    sw.WriteLine("protected void setXml_{0}(int {1}) {{ this.{1} = {1}; }}", var.Name, newVar);
                    //    sw.WriteLine();

                    //}
                    if (var.IsEnum == true)
                    {
                        string nSpace = string.Empty;
                        if (_externalEnumsToGenerateMap.ContainsKey(var.CustomType))
                        {
                            nSpace = _externalEnumsToGenerateMap[var.CustomType][0].NameSpace + ".";
                        }
                        sw.WriteLine("{0}public\t{4}{1}{0}{2};{3}", GetTab(), typeName, var.Name, var.Comment, nSpace);
                    }
                    else
                    {
                        sw.WriteLine("{0}public\t{1}{0}{2};{3}", GetTab(), typeName, var.Name, var.Comment);
                    }
                }
            }

            #endregion


            #region Default constructor
            sw.WriteLine("			");
            sw.WriteLine(GetTab() + "//default constructor");
            String defaultCtor = String.Format("{0}public\t{1}()", GetTab(), contextClassName);
            sw.WriteLine(defaultCtor);
            sw.WriteLine(GetTab() + "{");
            IndentLevel++;
            foreach (ClassElement var in val)
            {

                string defVal = GetDefaultsString(var, enumsToGenerateMap);
                if (defVal != "")
                {
                    String defaultVal = String.Format("{0}{1} = {2} ;", GetTab(), var.Name, defVal);
                    sw.WriteLine(defaultVal);
                }
                if ((var.CustomType != null) && (var.IsEnum == false))
                {
                    if (var.IsCollection == false)
                    {
                        string nSpace = string.Empty;
                        sw.WriteLine("{0}{1} = new {3}{2}() ;", GetTab(), var.Name, XSDToJavaType(var), nSpace);
                    }
                    else
                    {
                        collectionType = "java.util.ArrayList<" + XSDToJavaType(var) + ">";
                        sw.WriteLine("{0}{1} = new {2}() ;", GetTab(), var.Name, collectionType);
                    }
                }
                if (var.Type == XmlTypeCode.String)
                {
                    if (var.IsCollection == false)
                    {
                        sw.WriteLine("{0}{1} = \"\";", GetTab(), var.Name);
                    }
                    else
                    {
                        collectionType = "java.util.ArrayList<" + XSDToJavaType(var) + ">";
                        sw.WriteLine("{0}{1} = new {2}() ;", GetTab(), var.Name, collectionType);
                    }
                }
                //}
            }
            IndentLevel--;
            sw.WriteLine(GetTab() + "}");
            #endregion


            #region Destructor
            #endregion


            #region Copy constuctor

            #endregion


            #region Clonable Override
            sw.WriteLine("			");
            sw.WriteLine(GetTab() + "//Clonable Override");
            String equalToOperator = String.Format("{0}@Override  public\t{1} clone()\tthrows CloneNotSupportedException", GetTab(), contextClassName);
            sw.WriteLine(equalToOperator);
            sw.WriteLine(GetTab() + "{");
            IndentLevel++;

            String body = String.Format("{0}{1}\t instance = new {1}() ;", GetTab(), contextClassName);
            sw.WriteLine(body);

            foreach (ClassElement var in val)
            {
                //if (var.IsCollection && var.CustomType != null)
                if (var.IsCollection)
                {
                    String equalStatement = String.Format("{0}instance.{1}.addAll({1}) ;", GetTab(), var.Name);
                    sw.WriteLine(equalStatement);
                }
                else if (var.CustomType != null && var.IsEnum == false)
                {
                    String equalStatement = String.Format("{0}instance.{1} = ({2}){1}.clone() ;", GetTab(), var.Name, var.CustomType);
                    sw.WriteLine(equalStatement);
                }
                else
                {
                    String equalStatement = String.Format("{0}instance.{1} = {1} ;", GetTab(), var.Name);
                    sw.WriteLine(equalStatement);
                }
            }


            sw.WriteLine(GetTab() + "return instance;");
            IndentLevel--;
            sw.WriteLine(GetTab() + "}");
            #endregion


            #region DeepCopy
            sw.WriteLine("			");
            sw.WriteLine(GetTab() + "//DeepCopy");
            String copyFrom = String.Format("{0}public\tvoid DeepCopy({1} rhs)", GetTab(), contextClassName);
            sw.WriteLine(copyFrom);
            sw.WriteLine(GetTab() + "{");
            IndentLevel++;

            foreach (ClassElement var in val)
            {
                if (var.IsCollection)
                {
                    String equalStatement = String.Format("{0}{1}.addAll(rhs.{1}) ;", GetTab(), var.Name);
                    sw.WriteLine(equalStatement);
                }
                else if (var.CustomType != null && var.IsEnum == false)
                {
                    String equalStatement = String.Format("{0}{1}.DeepCopy(rhs.{1}) ;", GetTab(), var.Name);
                    sw.WriteLine(equalStatement);
                }
                else
                {
                    String equalStatement = String.Format("{0}{1} = rhs.{1} ;", GetTab(), var.Name);
                    sw.WriteLine(equalStatement);
                }
            }

            IndentLevel--;
            sw.WriteLine(GetTab() + "}");
            #endregion

            IndentLevel--;

            sw.WriteLine("			");
            sw.WriteLine(GetTab() + "}");

            classesToGenerateMap.Remove(className);
        }

        string XSDToJavaType(ClassElement element)
        {
            switch (element.Type)
            {
                case XmlTypeCode.String:
                    return "String";
                case XmlTypeCode.Long:
                    return "long";
                case XmlTypeCode.Float:
                case XmlTypeCode.Decimal:
                case XmlTypeCode.Double:
                    return "double";
                case XmlTypeCode.Short:
                case XmlTypeCode.Integer:
                case XmlTypeCode.PositiveInteger:
                case XmlTypeCode.NegativeInteger:
                case XmlTypeCode.Int:
                    return "int";
                case XmlTypeCode.Boolean:
                    return "boolean";
                case XmlTypeCode.Element:
                    return element.CustomType;
                case XmlTypeCode.DateTime:
                    return "Date";
                case XmlTypeCode.Date:
                    return "Date";
                default:
                    return "Object";
            }
        }

    }
}
