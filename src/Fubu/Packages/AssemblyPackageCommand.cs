using System;
using System.ComponentModel;
using System.IO;
using System.Xml;
using FubuCore;
using FubuCore.CommandLine;
using FubuMVC.Core.Packaging;

namespace Fubu.Packages
{
    public class AssemblyPackageInput
    {
        [Description("The root folder for the project if different from the project file's folder")]
        public string RootFolder { get; set; }
        
        [Description("Name of the csproj file.  If set, this command attempts to add the zip files as embedded resources")]
        public string ProjFileFlag { get; set; }


    }

    // TODO -- make this mess with the csproj files
    // TODO -- do something that tests this
    [CommandDescription("Bundle up the content and data files for a self contained assembly package", Name = "assembly-pak")]
    public class AssemblyPackageCommand : FubuCommand<AssemblyPackageInput>
    {
        public override bool Execute(AssemblyPackageInput input)
        {
            input.RootFolder = AliasCommand.AliasFolder(input.RootFolder);

            var zipService = new ZipFileService();


            createZipFile(input, "content", zipService);
            createZipFile(input, "data", zipService);


            return true;
        }

        private void createZipFile(AssemblyPackageInput input, string childFolderName, ZipFileService zipService)
        {
            var contentDirectory = FileSystem.Combine(input.RootFolder, childFolderName);
            if (!Directory.Exists(contentDirectory)) return;

            
            var zipFileName = "pak-{0}.zip".ToFormat(childFolderName);
            


            var contentFile = FileSystem.Combine(input.RootFolder, zipFileName);
            Console.WriteLine("Creating zip file " + contentFile);

            if (File.Exists(contentFile))
            {
                File.Delete(contentFile);
            }
            
            
            zipService.CreateZipFile(contentFile, file =>
            {
                
                file.AddFiles(new ZipFolderRequest(){
                    RootDirectory = contentDirectory,
                    ZipDirectory = string.Empty
                });
            });

            if (input.ProjFileFlag.IsNotEmpty())
            {
                var document = new XmlDocument();
                var projectFileName = FileSystem.Combine(input.RootFolder, input.ProjFileFlag);
                document.Load(projectFileName);

                //var search = "//ItemGroup/EmbeddedResource[@Include='{0}']".ToFormat(zipFileName);
                //if (document.DocumentElement.SelectSingleNode(search, new XmlNamespaceManager(document.NameTable)) == null)
                if (!document.DocumentElement.OuterXml.Contains(zipFileName))
                {
                    Console.WriteLine("Adding the ItemGroup / Embedded Resource for {0} to {1}".ToFormat(zipFileName, projectFileName));
                    var node = document.CreateNode(XmlNodeType.Element,"ItemGroup", document.DocumentElement.NamespaceURI);
                    var element = document.CreateNode(XmlNodeType.Element, "EmbeddedResource", document.DocumentElement.NamespaceURI);
                    var attribute = document.CreateAttribute("Include");
                    attribute.Value = zipFileName;
                    element.Attributes.Append(attribute);
                    node.AppendChild(element);
                    document.DocumentElement.AppendChild(node);

                    document.Save(projectFileName);
                }
                else
                {
                    Console.WriteLine("The file {0} is already embedded in project {1}".ToFormat(zipFileName, projectFileName));
                }
            }
        }
    }
}