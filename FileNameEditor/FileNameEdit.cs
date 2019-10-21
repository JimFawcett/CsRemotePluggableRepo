///////////////////////////////////////////////////////////////////////////
// FileNameEdit.cs - supports construction, inserts, and removals        //
//                   of text in file names                               //
// Ver 1.0                                                               //
// Jim Fawcett, CSE681-OnLine Software Modeling & Analysis, Summer 2017  //
///////////////////////////////////////////////////////////////////////////
/*
 * Note:
 * -----
 * In the repository, file names have the three forms:
 * - myFile.cs         // file before being checked in
 * - myFile.cs.3       // versioned file in storage
 * - myFile.cs.xml.3   // versioned metadata file describing myFile.cs.3
 * 
 * Package Operations:
 * -------------------
 * This package contains a class FileNameEditor with public methods:
 * - addXmlExt            : adds .xml after file extension, before version number
 * - removeXmlExt         : removes .xml substring
 * - pathCombine          : manages combining parts of paths, treating seperators appropriately
 * - stagingPath          : defines path to staging storage, the launch directory for checkin and checkout
 * - storagePath          : defines root path to Repository storage
 * - fileSpec             : builds fully qualified file name
 * - storageFolderRef     : builds categoryName/fileName
 * - extractCategory      : returns categoryName from storageFolderRef
 * - extractFileName      : returns fileName from storageFolderRef
 * - showResult           : test reporting, replaces repeated lines of code with a single function
 * - testComponent()      : self-test
 * 
 * It also contains a TestFileSync class used to demonstrate that the FileSync class 
 * functions as expected.
 * 
 * Required Files:
 * ---------------
 * - MetaData.cs       - builds, saves, loads, and queries package metadata
 * - TestUtilities.cs  - helper functions used mostly for testing
 * - IPluggable.cs     - Repository interfaces and shared data
 * 
 * Maintenance History:
 * --------------------
 * Ver 1.0 : 24 Jul 2017
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
  using FullDir = String;  // full path with no filename
  using DirName = String;   // directory name 

  ///////////////////////////////////////////////////////////////////
  // FileNameEditor class
  // - provides helper functions for manageing file and path names

  public class FileNameEditor
  {
    /*----< add xml extension before version number >--------------*/

    public static FileName addXmlExt(string fileName)
    {
      if (fileName.Contains(".xml"))
        return fileName;
      int pos = fileName.LastIndexOf(".");
      if (pos > 0)
      {
        return fileName.Insert(pos, ".xml");
      }
      return "";
    }
    /*----< remove xml extension from versioned file name >--------*/

    public static FileName removeXmlExt(string fileName)
    {
      if (fileName.Contains(".xml"))
      {
        int pos = fileName.LastIndexOf(".xml");
        return fileName.Remove(pos, 4);
      }
      return fileName;
    }
    /*----< combine paths with seperator handling >----------------*/

    public static FileSpec pathCombine(string leftPath, string rightPath)
    {
      char separator = leftPath[leftPath.Length - 1];
      if(separator == '\\' || separator == '/')
      {
        return System.IO.Path.Combine(leftPath, rightPath);
      }
      else
      {
        return System.IO.Path.Combine(leftPath + '/', rightPath);
      }
    }
    /*----< build staging path >-----------------------------------*/

    public static FullDir stagingPath(string clientStagingDir)
    {
      return pathCombine(RepoEnvironment.stagingPath, clientStagingDir);
    }
    /*----< build storage path >-----------------------------------*/

    public static FullDir storagePath(string category = "")
    {
      return pathCombine(RepoEnvironment.storagePath, category);
    }
    /*----< return fully qualified file name >---------------------*/

    public static FileSpec fileSpec(FullDir path, FileName fileName)
    {
      return pathCombine(path, fileName);
    }
    /*----< storage folder spec >----------------------------------*/
    /*
     *  - returns FileRef, e.g., tail of path where tail starts after "Storage", i.e. "test/foobar.cs.3"
     *  - returns empty string if "Storage" is not contained in path
     */
    public static FileRef storageFolderRef(FileSpec fileSpec)
    {
      int pos = fileSpec.IndexOf("Storage");
      if (pos == fileSpec.Length || pos == -1)
        return "";
      if (pos + 8 > fileSpec.Length - 1)
        return "";
      string folderSpec = fileSpec.Substring(pos + 8);
      return folderSpec;
    }
    /*----< extract category from filespec >-----------------------*/

    public static DirName extractCategory(FileSpec fileSpec)
    {
      //string temp = storageFolderRef(fileSpec);
      string temp = fileSpec;
      int pos = temp.IndexOf("/");
      if (pos == temp.Length || pos == -1)
        return "";
      return temp.Substring(0, pos);
    }
    /*----< extract fileName from filespec >-----------------------*/

    public static FileName extractFileName(FileSpec fileSpec)
    {
      //string temp = storageFolderRef(fileSpec);
      string temp = fileSpec;
      int pos = temp.IndexOf("/");
      if (pos == temp.Length || pos == -1)
        return "";
      return temp.Substring(pos + 1);
    }
    ///*----< extract version number from specified fileName >-------*/
    ///*
    // *  Not necessarily the latest version number.
    // *  Returns zero if file has no version number.
    // */
    //public int getVersion(FileName fileName)
    //{
    //  VerNum ver = 0;
    //  int pos = fileName.LastIndexOf(".");
    //  string verStr = fileName.Substring(pos + 1);
    //  if (int.TryParse(verStr, out ver))
    //    return ver;
    //  return 0;
    //}
    /*----< self test >--------------------------------------------*/

    public static void showResult(bool result, string resultText, string testName)
    {
      Console.Write("\n  {0}", resultText);
      TestUtilities.checkResult(result, testName);
    }

    public bool testComponent()
    {
      TestUtilities.title("Testing FileNameEditor", '=');
      TestUtilities.putLine();

      TestUtilities.title("Testing extension edits");
      FileName fileName = "SomeFile.cs.2";
      FileName test1Name = addXmlExt(fileName);
      bool t1 = test1Name.Contains(".xml");
      showResult(t1, test1Name, "addXmlExt");

      FileName test2Name = removeXmlExt(test1Name);
      bool t2 = test2Name == fileName;
      showResult(t2, test2Name, "removeXmlExt");

      FileName test3Name = removeXmlExt(test2Name);
      bool t3 = test3Name == fileName;
      showResult(t3, test3Name, "removeXmlExt");
      TestUtilities.putLine();

      TestUtilities.title("Testing path construction");
      DirName stagingdir = "Fawcett";
      FullDir stagingpath = stagingPath(stagingdir);
      bool t4 = (stagingpath.Contains("C:/") || stagingpath.Contains("../")) && stagingpath.Contains(stagingdir);
      showResult(t4, stagingpath, "stagingPath");

      DirName category = "SomeCategory";
      FullDir storagepath = storagePath(category);
      bool t5 = (storagepath.Contains("C:/") || storagepath.Contains("../")) && storagepath.Contains(category);
      showResult(t5, storagepath, "storagePath");

      FileName someFileName = "someFileName";
      FileSpec filespec = fileSpec(storagepath, someFileName);
      bool t6 = filespec.Contains("/someFileName");
      showResult(t6, filespec, "fileSpec");

      FileRef fileref = storageFolderRef(filespec);
      bool t7 = fileref.IndexOf('/') == fileref.LastIndexOf('/');
      showResult(t7, fileref, "storageFolderRef");

      DirName cat = extractCategory(fileref);
      bool t8 = cat == category;
      showResult(t8, cat, "extractCategory");

      FileName file = extractFileName(fileref);
      bool t9 = file == someFileName;
      showResult(t8, file, "extractFileName");

      return t1 && t2 && t3 && t4 && t5 && t6 && t7 && t8 && t9;
    }
  }
#if (TEST_FILENAMEEDITOR)
  class TestFileNameEditing
  {
    static void Main(string[] args)
    {
      ClientEnvironment.verbose = true;

      FileNameEditor ed = new FileNameEditor();
      bool t = ed.testComponent();

      TestUtilities.putLine();
      TestUtilities.checkResult(t, "testComponent");

      Console.Write("\n\n");
    }
  }
#endif
}
