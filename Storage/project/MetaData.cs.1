///////////////////////////////////////////////////////////////////////////
// MetaData.cs - in-memory representation of Repository MetaData file    //
// Ver 1.0                                                               //
// Jim Fawcett, CSE681-OnLine Software Modeling & Analysis, Summer 2017  //
///////////////////////////////////////////////////////////////////////////
/*
 * Package Operations:
 * -------------------
 * This package contains a class MetaData with properties:
 * - name           : package name string
 * - fileSpec       : fully-qualified name of referenced file, e.g., the package
 * - dateTime       : DateTime instance
 * - description    : description string
 * - dependencies   : List<string> containing dependency package names
 * and with methods:
 * - Metadata(...)  : constructor with optional name argument
 * - show()         : displays the MetaData instance on the console
 * - save(path)     : saves instance as an XML file
 * - load(path)     : loads an existing instance with properties from an XML file
 * - testMetaData() : self-test
 * 
 * It also contains a TestMetaData class with a single main method, used to
 * demonstrate that the MetaData class functions as expected.
 * 
 * Required Files:
 * ---------------
 * - MetaData.cs       - Builds, saves, loads, and queries package metadata information
 * - IPluggable.cs     - Repository interfaces and shared data
 * - Version.cs        - builds, stores, and extracts version information
 * - TestUtilities.cs  - helper functions used mostly for testing
 * 
 * Maintenance History:
 * --------------------
 * Ver 1.0 : 31 May 2017
 * - first release
 * 
 */
using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace PluggableRepository
{
  // type aliases with semantic meaning
  using FileSpec = String;  // c:/.../category/filename
  using FileRef = String;   // category/filename
  using FileName = String;  // filename may have version number at end
  using FullPath = String;  // full path with no filename
  using DirName = String;   // directory name 

  ///////////////////////////////////////////////////////////////////
  // MetaData class - represents package in repository

  public class MetaData : IMetaData
  {
    public string componentType { get; } = "MetaData";
    public string name { get; set; } = "";
    public string author { get; set; } = "";
    public bool isOpen { get; set; } = true;
    public string fileSpec { get; set; } = "";
    public DateTime dateTime { get; set; }
    public string description { get; set; } = "";
    public List<string> dependencies { get; set; } = new List<string>();

    /*----< default constructor >----------------------------------*/

    public MetaData()
    {
      this.author = "undefined";
      this.name = "undefined";
      dateTime = DateTime.Now;
      if (RepoEnvironment.version == null)
        RepoEnvironment.version = new Version();
    }
    /*----< constructor >------------------------------------------*/

    public MetaData(string author, string name)
    {
      this.author = author;
      this.name = name;
      dateTime = DateTime.Now;
      if (RepoEnvironment.version == null)
        RepoEnvironment.version = new Version();
    }
    /*----< display >----------------------------------------------*/

    public void show()
    {
      Console.Write("\n  Repository Package MetaData:");
      Console.Write("\n    name:               {0}", name);
      Console.Write("\n    author:             {0}", author);
      Console.Write("\n    isOpen:             {0}", isOpen.ToString());
      Console.Write("\n    Package represents: {0}", fileSpec);
      Console.Write("\n    DateTime:           {0}", dateTime.ToString());
      Console.Write("\n    Description:        {0}", description);
      Console.Write("\n    Package Dependencies:");
      foreach (string dep in dependencies)
      {
        Console.Write("\n      {0}", dep);
      }
    }
    /*----< display >----------------------------------------------*/

    public string showStr()
    {
      string showstr = "\n  Repository Package MetaData:";
      showstr += string.Format("\n    name:               {0}", name);
      showstr += string.Format("\n    author:             {0}", author);
      showstr += string.Format("\n    isOpen:             {0}", isOpen.ToString());
      showstr += string.Format("\n    Package represents: {0}", fileSpec);
      showstr += string.Format("\n    DateTime:           {0}", dateTime.ToString());
      showstr += string.Format("\n    Description:        {0}", description);
      showstr += string.Format("\n    Package Dependencies:");
      foreach (string dep in dependencies)
      {
        showstr += string.Format("\n      {0}", dep);
      }
      return showstr;
    }
    /*----< save to XML file >---------------------------------------*/

    public bool save(FullPath path)
    {
      try
      {
        XDocument doc = new XDocument();
        XElement root = new XElement("packageMetaData");
        doc.Add(root);

        XElement temp = new XElement("packageName");
        XText text = new XText(name);
        temp.Add(text);
        root.Add(temp);

        temp = new XElement("author");
        text = new XText(author);
        temp.Add(text);
        root.Add(temp);

        temp = new XElement("isOpen");
        text = new XText(isOpen.ToString());
        temp.Add(text);
        root.Add(temp);

        temp = new XElement("fileSpec");
        text = new XText(fileSpec);
        temp.Add(text);
        root.Add(temp);

        temp = new XElement("dateTime");
        text = new XText(dateTime.ToString());
        temp.Add(text);
        root.Add(temp);

        temp = new XElement("description");
        text = new XText(description);
        temp.Add(text);
        root.Add(temp);

        XElement deps = new XElement("dependencies");
        foreach (string dep in dependencies)
        {
          temp = new XElement("dependency");
          text = new XText(dep);
          temp.Add(text);
          deps.Add(temp);
        }
        root.Add(deps);
        doc.Save(path);
        return true;
      }
      catch(Exception ex)
      {
        Console.Write("\n  {0}", ex.Message);
        return false;
      }
    }
    /*----< load package metadata >----------------------------------*/

    public bool load(FullPath packageSpec)
    {
      try
      {
        XDocument doc = XDocument.Load(packageSpec);
        name = doc.Descendants("packageName").First().Value;
        author = doc.Descendants("author").First().Value;
        string isOpenString = doc.Descendants("isOpen").First().Value;
        isOpen = Convert.ToBoolean(isOpenString);
        fileSpec = doc.Descendants("fileSpec").First().Value;
        string dateTimeString = doc.Descendants("dateTime").First().Value;
        dateTime = DateTime.Parse(dateTimeString);
        description = doc.Descendants("description").First().Value;
        IEnumerable<XElement> nodes = doc.Descendants("dependency");
        foreach(XElement node in nodes)
        {
          dependencies.Add(node.Value);
        }
      }
      catch(Exception ex)
      {
        Console.Write("\n  {0}", ex.Message);
        return false;
      }
      return true;
    }
    /*----< metadata self test >-----------------------------------*/

    public static bool testMetaData()
    {
      TestUtilities.vbtitle("Testing MetaData Generation");

      bool test = true;

      MetaData md1 = new MetaData("Jim Fawcett", "SomeFile");
      md1.fileSpec = "SomeFile.cs";
      md1.dependencies.Add("AnotherFile");
      md1.dependencies.Add("StillAnotherFile");
      md1.description = "First test metadata instance";

      if (ClientEnvironment.verbose)
        md1.show();

      string category = "test";
      string fileName = md1.fileSpec + ".xml";
      fileName = RepoEnvironment.version.addVersion(category, fileName);
      string savePath = Path.Combine(RepoEnvironment.storagePath, category, fileName);
      savePath = Path.GetFullPath(savePath);

      TestUtilities.putLine();

      if (!md1.save(savePath))
      {
        Console.Write("\n  couldn't save \"{0}\"\n", fileName);
        test = false;
      }
      else
      {
        TestUtilities.putLine("saved \"" + savePath + "\"\n");
      }

      MetaData md2 = new MetaData();
      if (md2.load(savePath))
      {
        if (ClientEnvironment.verbose)
        {
          Console.Write("\n  loaded metadata from xml file\"{0}\"\n", savePath);
          md2.show();
          Console.Write("\n");
        }
      }
      else
      {
        Console.Write("\n  failed to load metadata from xml file \"{0}\"\n", savePath);
        test = false;
      }
      return test;
    }
    public bool testComponent()
    {
      if (ClientEnvironment.verbose)
        TestUtilities.title("testing MetaData component", '=');
      
      return testMetaData();
    }
  }
  ///////////////////////////////////////////////////////////////////
  // TestMetaData class

  public class TestMetaData
  {
#if (TEST_METADATA)
    static void Main(string[] args)
    {
      ClientEnvironment.verbose = true;
      MetaData metadata = new MetaData();
      TestUtilities.checkResult(metadata.testComponent(), "MetaData");
      TestUtilities.putLine("\n\n");
    }
#endif
  }
}
