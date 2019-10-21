///////////////////////////////////////////////////////////////////////////
// Version.cs - Pluggable policy for storing packages                    //
// Ver 1.1                                                               //
// Jim Fawcett, CSE681-OnLine Software Modeling & Analysis, Spring 2017  //
///////////////////////////////////////////////////////////////////////////
/*
 * Package Operations:
 * -------------------
 * This package contains a single class Version with public functions:
 * - addVersion       : adds version text to filename
 * - removeVersion    : removes version text from filename
 * - hasversion       : filename has version number?
 * - getVersion       : returns version number of specified fileName
 * - getLatestVersion : returns highest version number of all files
 *                      with given category and name
 * - testVersion      : Version self test
 * 
 * Required Files:
 * ---------------
 * - Version.cs
 * - IPluggable     - Repository interfaces and shared data
 * - TestUtilities  - Helper class that is used mostly for testing
 * 
 * Maintenance History:
 * --------------------
 * Ver 1.1 : 15 Jul 2017
 * - added notes in comments
 * Ver 1.0 : 31 May 2017
 * - first release
 * 
 */
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace PluggableRepository
{
  // type aliases with semantic meaning
  using FileSpec = String;    // c:/.../category/filename
  using FileRef = String;     // category/filename
  using FileName = String;    // filename may have version number at end
  using FullPath = String;    // full path with no filename
  using DirName = String;     // directory name 
  using Key = String;         // Dictionary key with format category.fileName
  using VerNum = Int32;       // Dictionary value

  ///////////////////////////////////////////////////////////////////
  // Version class
  // - supports getting and setting version numbers for
  //   packages stored in the repository, e.g., metadata and source files

  public class Version : IVersion
  {
    public string componentType { get; } = "Version";
    private Dictionary<Key, VerNum> currentVersion_ = new Dictionary<Key, VerNum>();

    /*----< extract version number from specified fileName >-------*/
    /*
     *  Not necessarily the latest version number.
     *  Returns zero if file has no version number.
     */
    public int getVersion(FileName fileName)
    {
      VerNum ver = 0;
      int pos = fileName.LastIndexOf(".");
      string verStr = fileName.Substring(pos + 1);
      if (int.TryParse(verStr, out ver))
        return ver;
      return 0;
    }
    /*----< creates Dictionary of version numbers >----------------*/
    /*
     *  Reads filenames from each Storage category and builds
     *  Dictionary with category.filename keys and largest 
     *  version number values.
     *  
     *  ToDo: convert this processing to use VersionChain instance
     *        from Relationships package.
     */
    public void restoreVersionsFromFiles(string category="")
    {
      if (category == "")
        return;
      string catPath = Path.Combine(RepoEnvironment.storagePath, category);
      string[] files = Directory.GetFiles(catPath);
      foreach(string file in files)
      {
        string fileName = Path.GetFileName(file);
        string file_nv = removeVersion(fileName);
        string key = category + "." + file_nv;
        int versionNum = getVersion(fileName);
        if(currentVersion_.ContainsKey(key))
        {
          if (versionNum > currentVersion_[key])
            currentVersion_[key] = versionNum;
        }
        else
        {
          currentVersion_[key] = versionNum;
        }
      }
    }
    /*----< refreshes version number cache from storage files >----*/

    public void restoreVersionsFromCategories()
    {
      string[] dirs = System.IO.Directory.GetDirectories(RepoEnvironment.storagePath);
      foreach(string dir in dirs)
      {
        restoreVersionsFromFiles(dir);
      }
    }
    /*----< constructor >------------------------------------------*/
    /*
     * Builds latest version number dictionary - see above.
     */
    public Version()
    {
      string[] cats = Directory.GetDirectories(RepoEnvironment.storagePath);
      foreach(string cat in cats)
      {
        DirectoryInfo di = new DirectoryInfo(cat);
        string dir = di.Name;
        
        restoreVersionsFromFiles(dir);
      }
    }
    /*----< does this fileName have a version number? >------------*/

    public bool hasVersion(string fileName)
    {
      int version;
      int pos = fileName.LastIndexOf('.');
      string end = fileName.Substring(pos + 1);
      if (int.TryParse(end, out version))
      {
        return true;
      }
      return false;
    }
    /*----< strips version number from filename >------------------*/
    /*
     * Used when retrieving file from storage.
     */
    public string removeVersion(string filename)
    {
      int version;
      int pos = filename.LastIndexOf('.');
      string end = filename.Substring(pos + 1);
      if (int.TryParse(end, out version))
      {
        return filename.Substring(0, pos);
      }
      return filename;
    }
    /*----< adds version number to filename >----------------------*/
    /*
     * Adds version number equal to 1 + latestVersionNumber.
     * Used before storing file in Storage.
     */
    public string addVersion(string category, string fileName)
    {
      string key = category + "." + removeVersion(fileName);
      int versionNumber = 0;
      if (currentVersion_.ContainsKey(key))
        versionNumber = currentVersion_[key];
      currentVersion_[key] = ++versionNumber;
      return fileName + "." + versionNumber.ToString();
    }
    /*----< returns the current version number for filename >------*/

    public int getLatestVersion(string category, string filename)
    {
      int temp = 0;
      filename = removeVersion(filename);
      string key = category + "." + filename;
      if (currentVersion_.ContainsKey(key))
        temp = currentVersion_[key];
      return temp;
    }
    /*----< Version component self test >--------------------------*/

    public bool testComponent()
    {
      TestUtilities.vbtitle("Testing Version Component", '=');

      string test = "foobar.h";
      string vertest = addVersion("test", "foobar.h");
      TestUtilities.putLine(string.Format("versioned string = \"{0}\"", vertest));
      vertest = addVersion("test", "foobar.h");
      TestUtilities.putLine(string.Format("versioned string = \"{0}\"", vertest));
      string dvertest = removeVersion(vertest);
      TestUtilities.putLine(string.Format("de-versioned string = \"{0}\"", dvertest));
      return test == dvertest;
    }
  }
  /*----< demonstration >------------------------------------------*/

#if (TEST_VERSION)
  class TestVersion
  {
    static void Main(string[] args)
    {
      ClientEnvironment.verbose = true;

      Version version = new Version();
      TestUtilities.checkResult(version.testComponent(), "\n  Version Component");

      Console.Write("\n\n");
    }
  }
#endif
}
