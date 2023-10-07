using Microsoft.CSharp;
using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Resources;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using static System.Net.Mime.MediaTypeNames;


namespace ssrs_onkey_no_login
{
    internal class Program
    {
        [DllImport("kernel32")]
        private static extern int GetPrivateProfileString(string section, string key, string def, StringBuilder retVal, int size, string filePath);

        public static string ReadValueFromIniFile(string Section, string Key, string def, string filePath)
        {
            StringBuilder temp = new StringBuilder(255);
            int i = GetPrivateProfileString(Section, Key, def, temp, 255, filePath);
            return temp.ToString();
        }

        static void Main(string[] args)
        {
            string iniFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "config.ini");
            string basePath = ReadValueFromIniFile("config", "path", @"C:\Program Files\Microsoft SQL Server\MSRS13.MSSQLSERVER\Reporting Services\ReportServer", iniFilePath);

            Console.WriteLine("SSRS免登录设置工具V1.1");
            Console.WriteLine("");


            Console.WriteLine("Reporting Services 目录为：");
            Console.WriteLine(basePath);
            Console.WriteLine("按任意键继续...");
            Console.WriteLine("");
            Console.ReadKey();

            string bakPath = @"c:\SSRS_noLogin_bak\" + DateTime.Now.ToString("yyyyMMddHHmmss");
            Directory.CreateDirectory(bakPath);
            File.Copy(basePath + "\\rsreportserver.config", bakPath + "\\rsreportserver.config");
            File.Copy(basePath + "\\web.config", bakPath + "\\web.config");
            File.Copy(basePath + "\\rssrvpolicy.config", bakPath + "\\rssrvpolicy.config");

            Console.WriteLine(@"原文件已备份到以下目录：");
            Console.WriteLine(bakPath);

            Console.WriteLine("");

            Console.WriteLine("第1步开始:");
            Step1(basePath);
            Console.WriteLine("第1步结束.");

            Console.WriteLine("第2步开始:");
            Step2(basePath);
            Console.WriteLine("第2步结束.");

            Console.WriteLine("第3步开始:");
            Step3(basePath);
            Console.WriteLine("第3步结束.");

            Console.WriteLine("第4步开始:");
            Step4(basePath);
            Console.WriteLine("第4步结束.");

            Console.WriteLine("第5步开始:");
            Step5(basePath);
            Console.WriteLine("第5步结束.");

            Console.WriteLine("完成");
            Console.WriteLine("按任意键退出...");
            Console.ReadKey();
        }

        static void Step1(string basePath)
        {
            string file = basePath + "\\rsreportserver.config";
            XmlDocument doc = new XmlDocument();
            doc.Load(file);                  //Configuration
            XmlNode xn = doc.SelectSingleNode("Configuration/Authentication/AuthenticationTypes");
            XmlNode oldXn = xn.SelectSingleNode("RSWindowsNTLM");
            if (oldXn == null)
            {
                Console.WriteLine("未找到RSWindowsNTLM,或已更改");
            }
            else
            {
                XmlNode newXn = doc.CreateElement("Custom");
                xn.ReplaceChild(newXn, oldXn);
            }
            doc.Save(file);

        }
        static void Step2(string basePath)
        {
            XmlDocument doc = new XmlDocument();
            string file = basePath + "\\web.config";
            doc.Load(file);
            XmlNode xn2 = doc.SelectSingleNode("configuration/system.web/authentication");
            XmlElement xm2 = (XmlElement)xn2;
            xm2.SetAttribute("mode", "None");

            XmlNode xn3 = doc.SelectSingleNode("configuration/system.web/identity");
            XmlElement xm3 = (XmlElement)xn3;
            xm3.SetAttribute("impersonate", "false");

            doc.Save(file);
        }
        static void Step3(string basePath)
        {
            CSharpCodeProvider objCSharpCodePrivoder = new CSharpCodeProvider();
            CompilerParameters objCompilerParameters = new CompilerParameters();
            objCompilerParameters.ReferencedAssemblies.Add("System.dll");
            objCompilerParameters.ReferencedAssemblies.Add(basePath + @"\bin\Microsoft.ReportingServices.Interfaces.dll");
            string class1 = Resources.Class1;
            string strSourceCode = class1;
            objCompilerParameters.GenerateInMemory = false;
            objCompilerParameters.OutputAssembly = "Microsoft.Samples.ReportingServices.AnonymousSecurity.dll";
            CompilerResults cr = objCSharpCodePrivoder.CompileAssemblyFromSource(objCompilerParameters, strSourceCode);
            if (cr.Errors.HasErrors)
            {
                string strErrorMsg = cr.Errors.Count.ToString() + " Errors:";
                for (int x = 0; x < cr.Errors.Count; x++)
                {
                    strErrorMsg = strErrorMsg + "/r/nLine: " +
                                 cr.Errors[x].Line.ToString() + " - " +
                                 cr.Errors[x].ErrorText;
                }
                Console.WriteLine(strErrorMsg);
                return;
            }
            File.Copy("Microsoft.Samples.ReportingServices.AnonymousSecurity.dll", basePath + @"\bin\Microsoft.Samples.ReportingServices.AnonymousSecurity.dll", true);
            File.Delete("Microsoft.Samples.ReportingServices.AnonymousSecurity.dll");
        }
        static void Step4(string basePath)
        {
            XmlDocument doc = new XmlDocument();
            string file = basePath + "\\rsreportserver.config";
            doc.Load(file);
            XmlNode xn2 = doc.SelectSingleNode("Configuration/Extensions/Security/Extension");
            XmlElement xm2 = (XmlElement)xn2;
            xm2.SetAttribute("Name", "None");
            xm2.SetAttribute("Type", "Microsoft.Samples.ReportingServices.AnonymousSecurity.Authorization, Microsoft.Samples.ReportingServices.AnonymousSecurity");

            XmlNode xn3 = doc.SelectSingleNode("Configuration/Extensions/Authentication/Extension");
            XmlElement xm3 = (XmlElement)xn3;
            xm3.SetAttribute("Name", "None");
            xm3.SetAttribute("Type", "Microsoft.Samples.ReportingServices.AnonymousSecurity.AuthenticationExtension, Microsoft.Samples.ReportingServices.AnonymousSecurity");

            doc.Save(file);
        }

        static void Step5(string basePath)
        {
            XmlDocument doc = new XmlDocument();
            string file = basePath + "\\rssrvpolicy.config";
            doc.Load(file);
            XmlNode xn1 = doc.SelectSingleNode("configuration/mscorlib/security/PolicyLevel/CodeGroup[@class=UnionCodeGroup]");
            if (xn1 != null)
            {
                //已添加
            }
            else
            {
                XmlNode xn = doc.SelectSingleNode("configuration/mscorlib/security/policy/PolicyLevel");
                XmlElement xe = doc.CreateElement("CodeGroup");
                xe.SetAttribute("class", "UnionCodeGroup");
                xe.SetAttribute("version", "");
                xe.SetAttribute("PermissionSetName", "FullTrust");
                xe.SetAttribute("Name", "Private_assembly");
                xe.SetAttribute("Description", "This code group grants custom code full trust.");

                XmlElement xe2 = doc.CreateElement("IMembershipCondition");
                xe2.SetAttribute("class", "UrlMembershipCondition");
                xe2.SetAttribute("version", "");
                xe2.SetAttribute("Url", basePath + @"\bin\Microsoft.Samples.ReportingServices.AnonymousSecurity.dll");

                xe.AppendChild(xe2);
                xn.AppendChild(xe);
            }
            doc.Save(file);
        }

    }
}
