///////////////////////////////////////////////////////////////////////////
// Checkout.cs - supports extraction of package dependency graphs        //
// Ver 1.0                                                               //
// Jim Fawcett, CSE681-OnLine Software Modeling & Analysis, Summer 2017  //
///////////////////////////////////////////////////////////////////////////
/*
 * Package Operations:
 * -------------------
 * This package contains a class Checkout with public methods:
 * - extractCategory      : extract category folder name from a FileRef - see below
 * - extractFileName      : extract file name from a FileRef
 * - retrieveDependencies : visit and store names of all packages on a dependency (sub)tree
 * - doCheckout           : copy all the package metadata and referred files from a dependency
 *                          tree to the file Staging area
 * - testComponent()      : self-test
 * 
 * It also contains a TestCheckout class used to demonstrate that the Checkout class 
 * functions as expected.
 * 
 * Required Files:
 * ---------------
 * - Checkout.cs       - Builds, saves, loads, and queries package metadata information
 * - Checkin.cs        - manages file checkin to the repository
 * - MetaData.cs       - builds, saves, loads, and queries package metadata
 * - Ownership.cs      - authorizes actions on the Repository contents
 * - Storage.cs        - manages directories and their contents
 * - Version.cs        - builds, stores, and extracts version information
 * - TestUtilities.cs  - helper functions used mostly for testing
 * - IPluggable.cs     - Repository interfaces and shared data
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
using System.Text;
using System.Threading.Tasks;

namespace PluggableRepository
{
  // type aliases with semantic meaning
  using FileSpec = String;  // c:/.../category/filename
  using FileRef = String;   // category/filename
  using FileName = String;  // filename may have version number at end
  using FullPath = String;  // full path with no filename
  using DirName = String;   // directory name 
  using Dependencies = List<String>;
  using FileList = List<String>;

  ///////////////////////////////////////////////////////////////////
  // Checkout class - component for retrieving files from Repository

  public class Checkout : ICheckout
  {
    public string componentType { get; } = "Checkout";
    public Dependencies dependencies { get; set; } = new Dependencies();
    public FileList fileList { get; set; } = new FileList();

    public Checkout()
    {
      if (RepoEnvironment.checkout == null)
        RepoEnvironment.checkout = this;
    }
    public DirName extractCategory(FileRef fileRef)
    {
      int pos = fileRef.IndexOf('/');
      if (pos == -1)
        return "";
      return fileRef.Substring(0, pos);
    }

    public FileName extractFileName(FileRef fileRef)
    {
      int pos = fileRef.IndexOf('/');
      if (pos == -1)
        return "";
      return fileRef.Substring(pos + 1);
    }

    public bool retrieveDependencies(DirName category, FileName filename)
    {
      FileName found = RepoEnvironment.checkin.findStoredMetaData(category, filename);
      if (found == "")
        return false;

      FullPath pathToMetaData = System.IO.Path.Combine(RepoEnvironment.storagePath, category);
      IMetaData md = RepoEnvironment.checkin.loadMetaData(pathToMetaData, found);
      if (md == null)
        return false;

      if(ClientEnvironment.verbose)
      {
        TestUtilities.putLine(string.Format("contents of metadata \"{0}\" on dependency tree", found));
        md.show();
        TestUtilities.putLine();
      }
      // extract primary filename from metadata and add to fileList
      FileName srcFile = extractFileName(md.fileSpec);
      fileList.Add(srcFile);

      if (md.dependencies.Count == 0)
        return true;

      foreach(FileRef fileRef in md.dependencies)
      {
        FileName dfileName = extractFileName(fileRef);
        dependencies.Add(dfileName);
        retrieveDependencies(category, dfileName);
      }
      return true;
    }
    /*----< remove xml extension from versioned file name >--------*/

    string removeXmlExt(string fileName)
    {
      if (fileName.Contains(".xml"))
      {
        int pos = fileName.LastIndexOf(".xml");
        return fileName.Remove(pos, 4);
      }
      return fileName;
    }

    public bool doCheckout(DirName category, FileName fileName)
    {
      FileName found = RepoEnvironment.checkin.findStoredMetaData(category, fileName);
      if (found == "")
        return false;

      MetaData md = new MetaData();
      try
      {
        string path = System.IO.Path.Combine(RepoEnvironment.storagePath + "/", category + "/", found);
        md.load(path);
      }
      catch(Exception ex)
      {
        return false;
      }
      string refFile = md.fileSpec;
      fileList.Clear();
      string fileSpec = System.IO.Path.Combine(category + "/", fileName);
      fileList.Add(refFile);
      fileList.Add(fileSpec);
      Dependencies deps = RepoEnvironment.storage.descendents(fileSpec);
      foreach(string dep in deps)
      {
        fileList.Add(dep);
      }
      foreach(string file in fileList)
      {
        string cata = extractCategory(file);
        string name = extractFileName(file);
        RepoEnvironment.storage.retrieveFile(cata, name);
        string temp = removeXmlExt(name);
        RepoEnvironment.storage.retrieveFile(cata, temp);
      }
      //FileName found = RepoEnvironment.checkin.findStoredMetaData(category, fileName);
      //if (found == "")
      //  return false;
      //dependencies.Clear();
      //dependencies.Add(found);
      //bool result = retrieveDependencies(category, found);

      //foreach(FileName dfile in dependencies)
      //{
      //  RepoEnvironment.storage.retrieveFile(category, dfile);
      //}
      //foreach(FileName file in fileList)
      //{
      //  RepoEnvironment.storage.retrieveFile(category, file);
      //}
      //return result;
      return true;
    }
    public bool testComponent()
    {
      TestUtilities.vbtitle("testing checkout component", '=');
      TestCheckout test = new TestCheckout();
      return test.testCheckout();
    }
  }
  ///////////////////////////////////////////////////////////////////
  // TestCheckout class

  public class TestCheckout
  {
    //Checkout checkout = new Checkout();
    //Checkin checkin = new Checkin();

    public TestCheckout()
    {
    }

    public void testSetup()
    {
    }

    public bool testExtractFileRefInfo()
    {
      TestUtilities.vbtitle("testing FileRef handling");
      FileRef fileRef = "cat/file";
      DirName dtest = RepoEnvironment.checkout.extractCategory(fileRef);
      FileName ftest = RepoEnvironment.checkout.extractFileName(fileRef);
      bool result1 = (dtest == "cat") && (ftest == "file");
      TestUtilities.checkResult(result1, string.Format("valid FileRef \"{0}\" - ", fileRef));

      fileRef = "cat/";
      dtest = RepoEnvironment.checkout.extractCategory(fileRef);
      ftest = RepoEnvironment.checkout.extractFileName(fileRef);
      bool result2 = (dtest == "cat") && (ftest == "");
      TestUtilities.checkResult(result2, string.Format("empty file in FileRef \"{0}\" - ", fileRef));

      fileRef = "/file";
      dtest = RepoEnvironment.checkout.extractCategory(fileRef);
      ftest = RepoEnvironment.checkout.extractFileName(fileRef);
      bool result3 = (dtest == "") && (ftest == "file");
      TestUtilities.checkResult(result3, string.Format("empty category in FileRef \"{0}\" - ", fileRef));

      fileRef = "abcdefg";
      dtest = RepoEnvironment.checkout.extractCategory(fileRef);
      ftest = RepoEnvironment.checkout.extractFileName(fileRef);
      bool result4 = (dtest == "") && (ftest == "");
      TestUtilities.checkResult(result4, string.Format("invalid FileRef \"{0}\" - ", fileRef));

      return result1 && result2 && result3 && result4;
    }

    public bool testDoCheckout()
    {
      TestUtilities.vbtitle("testing doCheckout");

      // setup test by running checkin.testComponent()
      bool verbose = ClientEnvironment.verbose;
      ClientEnvironment.verbose = false;
      RepoEnvironment.checkin.testComponent();
      ClientEnvironment.verbose = verbose;

      // execute test
      DirName category = "test";
      FileName fileName = "testFile3.cs.xml";
      bool test = RepoEnvironment.checkout.doCheckout(category, fileName);
      return test;
    }

    public bool testCheckout()
    {
      testSetup();

      bool t1 = testExtractFileRefInfo();
      TestUtilities.checkResult(t1, "extractFileRefInfo");
      TestUtilities.putLine();

      bool t2 = testDoCheckout();
      TestUtilities.checkResult(t2, "doCheckout");
      TestUtilities.putLine();

      TestUtilities.checkResult(t1 && t2, "CheckOut component");
      return t1 && t2;
    }

    static void Main(string[] args)
    {
      if (RepoEnvironment.version == null)
        RepoEnvironment.version = new Version();
      if (RepoEnvironment.storage == null)
        RepoEnvironment.storage = new Storage();
      if (RepoEnvironment.checkin == null)
        RepoEnvironment.checkin = new Checkin();

      ClientEnvironment.verbose = true;
      Checkout checkout = new Checkout();
      checkout.testComponent();
      TestUtilities.putLine("\n");
    }
  }
}
