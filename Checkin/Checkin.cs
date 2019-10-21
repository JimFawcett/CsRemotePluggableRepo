///////////////////////////////////////////////////////////////////////////
// Checkin.cs - manages adding new packages to Repository                //
// Ver 1.1                                                               //
// Jim Fawcett, CSE681-OnLine Software Modeling & Analysis, Summer 2017  //
///////////////////////////////////////////////////////////////////////////
/*
 * Package Operations:
 * -------------------
 * This package contains a class Checkin with public properties:
 * - componentType          : component name
 * - isClosed               : is this checkin closed for modification?
 * and with public methods:
 * - Checkin                : initializes access to other components
 * - stagingPath            : returns path to staging area
 * - storagePath            : returns path to storage area
 * - storageFolderSpec      : returns category/fileName when applied to valid fileSpec
 * - extractCategory        : returns category name from fileSpec
 * - loadMetaData           : creates instance of MetaData given path to metadata file in storage
 * - doCheckin              : constructs metadata file for file in staging, saves file and metadata to storage
 * - findStoredMetaData     : returns path to versioned metadata file in storage
 * - showFoundStatus        : displays results of preceding find
 * - editMetadata           : enables editing of name, open status, and description
 * - addDependencies        : adds dependencies to stored metadata file
 * 
 * It also contains a TestCheckin class with public methods:
 * - TestCheckin            : constructor gets ready for testing by calling testSetup
 * - testSetup              : deletes all files in staging and storage and initializes version cache
 * - testPathHandling       : tests several path related functions
 * - testFindStoredMetadata : tests finding versioned metadata files in storage
 * - testDoCheckin          : tests checking in files from staging area
 * - testEditMetaData       : tests modifying selected properties of a stored metadata file
 * - testAddDependencies    : tests adding dependencies to stored metadata file
 * - testCheckin            : self-test for component by calling each of the functions, above
 * 
 * Required Files:
 * ---------------
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
 * ver 1.1 : 11 Jun 2017
 * - minor changes to class and test setup
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
using System.Threading;

namespace PluggableRepository
{
  // type aliases with semantic meaning
  using FileSpec = String;  // c:/.../category/filename
  using FileRef = String;   // category/filename
  using FileName = String;  // filename may have version number at end
  using FullPath = String;  // full path with no filename
  using DirName = String;   // directory name 
  using Dependencies = List<string>;

  ///////////////////////////////////////////////////////////////////
  // Checkin Class - implements Checkin Pluggin component
  //
  public class Checkin : ICheckin
  {
    public string componentType { get; } = "Checkin";
    public bool isClosed { get; set; } = false;
    private Dependencies dependencies = new Dependencies();

    /*----< constructor >------------------------------------------*/

    public Checkin()
    {
      if (RepoEnvironment.checkin == null)
        RepoEnvironment.checkin = this;
    }
    /*----< build staging path >-----------------------------------*/

    public FullPath stagingPath(string clientStagingDir)
    {
      return Path.Combine(RepoEnvironment.stagingPath, ClientEnvironment.stagingDir);
    }
    /*----< build storage path >-----------------------------------*/

    public FullPath storagePath(string category = "")
    {
      return Path.Combine(RepoEnvironment.storagePath, category);
    }
    /*----< storage folder spec >----------------------------------*/
    /*
     *  - returns FileRef, e.g., tail of path where tail starts after "Storage", i.e. "test/foobar.cs.3"
     *  - returns empty string if "Storage" is not contained in path
     */
    public FileRef storageFolderSpec(string path)
    {
      int pos = path.IndexOf("Storage");
      if (pos == path.Length || pos == -1)
        return "";
      if (pos + 8 > path.Length - 1)
        return "";
      string folderSpec = path.Substring(pos + 8);
      return folderSpec;
    }
    /*----< extract category from filespec >-----------------------*/

    public DirName extractCategory(FileSpec fileSpec)
    {
      string temp = storageFolderSpec(fileSpec);
      int pos = temp.IndexOf("/");
      if (pos == temp.Length || pos == -1)
        return "";
      return temp.Substring(0, pos);
    }
    /*----< extract fileName from filespec >-----------------------*/

    public FileName extractFileName(FileSpec fileSpec)
    {
      string temp = storageFolderSpec(fileSpec);
      int pos = temp.IndexOf("/");
      if (pos == temp.Length || pos == -1)
        return "";
      return temp.Substring(pos + 1);
    }
    /*----< create metadata from file >----------------------------*/
    /*
     * - create MetaData instance from mdFileName in path
     * - expects extension ".xml"
     */
    public IMetaData loadMetaData(FullPath path, FileName mdFileName)
    {
      MetaData md = new MetaData();
      bool result = TestUtilities.handleInvoke(
        () =>
        {
          string mdFileSpec = Path.Combine(path, mdFileName);
          if (md.load(mdFileSpec))
            return true;
          else
            return false;
        }
      );
      if (result)
        return md;
      else
        return null;
    }
    /*----< checkin file from staging area to storage area >-------*/
    /*
     * - CheckinInfo is defined in IPluggable.cs
     * - it contains properties found in a MetaData instance
     */
    public bool doCheckin(CheckinInfo info, bool prevIsOpen = false)
    {
      MetaData md = new MetaData(info.author, info.fileName);
      if (md == null)
        return false;

      md.name = info.name;
      md.author = info.author;
      md.isOpen = info.isOpen;
      md.description = info.description;
      foreach(string dep in info.deps)
      {
        md.dependencies.Add(dep);
      }

      // should we create new version or modify old version
      int fileVer = RepoEnvironment.version.getLatestVersion(info.category, info.fileName);
      bool doModify;
      if(fileVer == 0) // no previous checkin so make new version
      {
        doModify = false;
      }
      else if(!prevIsOpen) // last checkin was closed so make new version
      {
        doModify = false;
      }
      else // last checkin was open so just modify existing version
      {
        doModify = true;
      }

      // move or copy source file to versioned file in Storage
      bool t1;
      if (doModify)
        t1 = RepoEnvironment.storage.modifyFile(info.category, info.fileName);
      else
        t1 = RepoEnvironment.storage.addFile(info.category, info.fileName);

      // get version from version cache, possibly updated by addFile
      fileVer = RepoEnvironment.version.getLatestVersion(info.category, info.fileName);

      // build versioned fileSpec and add to MetaData instance
      md.fileSpec = info.category + "/" + info.fileName + "." + fileVer.ToString();
      string mdFileName = info.fileName + ".xml";

      // update dependencies cache
      FileSpec parent = info.category + "/" + info.fileName + ".xml." + fileVer.ToString();
      foreach (FileSpec dep in md.dependencies)
      {
        RepoEnvironment.storage.upDateDependencyCache(parent, dep);
      }

      md.save(RepoEnvironment.storage.stagingFilePath(mdFileName));

      // move or copy metadata file to versioned file in storage
      bool t2;
      int metaVer = RepoEnvironment.version.getLatestVersion(info.category, mdFileName);
      if (doModify)
        t2 = RepoEnvironment.storage.modifyFile(info.category, mdFileName);
      else
        t2 = RepoEnvironment.storage.addFile(info.category, mdFileName);

      List<string> stagedFiles = RepoEnvironment.storage.stagedFiles();
      RepoEnvironment.storage.deleteStagedFiles();
      return t1 && t2;
    }
    /*----< find metadata file in storage >------------------------*/
    /*
     * - returns versioned fileName of metadata in Storage
     * - if not found, returns empty string.
     */
    public FileName findStoredMetaData(DirName category, FileName fileName)
    {
      FileName tempFile = RepoEnvironment.version.removeVersion(fileName);
      if (Path.GetExtension(tempFile) != ".xml")
        return "";

      // if fileName has no version, check if storage/category has a
      // versioned fileName

      if (!RepoEnvironment.version.hasVersion(fileName))
      {
        int version = RepoEnvironment.version.getLatestVersion(category, fileName);
        if (version == 0)
          return "";
        fileName = fileName + "." + version.ToString();
      }

      // make sure there is a file with the cached version found above
      List<FileName> files = RepoEnvironment.storage.files(category);
      foreach (FileName file in files)
      {
        if (file == fileName)
          return file;
      }
      return "";
    }
    /*----< show found status >------------------------------------*/

    public void showFoundStatus(DirName category, FileName searchName, FileName foundName)
    {
      TestUtilities.putLine(string.Format("searching for file \"{0}/{1}\"", category, searchName));
      if (foundName.Length > 0)
        TestUtilities.putLine(string.Format("found file \"{0}/{1}\"", category, foundName));
      else
        TestUtilities.putLine(string.Format("didn't find file \"{0}/{1}\"", category, searchName));
    }
    /*----< edit stored metadata >---------------------------------*/
    /*
     * - set info.fileName to the name of package to be edited
     * - set info properties that are to be edited with new values
     * - only isOpen, name, and description can be changed
     * - once closed, metadata cannot be edited again
     */
    public bool editMetadata(CheckinInfo info)
    {
      string found = findStoredMetaData(info.category, info.fileName);
      if (found.Length == 0)
        return false;
      IMetaData md = loadMetaData(storagePath(info.category), found);
      if (md == null)
        return false;
      if(md.isOpen == false)
      {
        TestUtilities.putLine("can't edit closed metadata");
        return false;
      }
      if(ClientEnvironment.verbose)
      {
        Console.Write("\n  editing stored metadata file \"" + found + "\":");
        md.show();
      }
      if (info.isOpen == false)
        md.isOpen = false;
      if (info.name.Length > 0)
        md.name = info.name;
      if (info.description.Length > 0)
        md.description = info.description;

      bool test = TestUtilities.handleInvoke(
        () =>
        {
          return md.save(storagePath(info.category) + "/" + found);
        }
      );
      return test;
    }
    /*----< add dependencies to stored metadata >------------------*/
    /*
     * - dependencies are added in the form "category/filename"
     */
    public bool addDependencies(FileSpec fileSpec, Dependencies dependencies)
    {
      string category = extractCategory(fileSpec);
      if (category.Length == 0)
        return false;
      string fileName = extractFileName(fileSpec);
      if (fileName.Length == 0)
        return false;
      FileSpec found = findStoredMetaData(category, fileName);
      if (found.Length == 0)
        return false;

      IMetaData md = loadMetaData(storagePath(category), fileName);
      if (md == null)
        return false;

      if(ClientEnvironment.verbose)
      {
        Console.Write("\n  metadata before adding dependencies:");
        md.show();
      }
      foreach(string dep in dependencies)
      {
        md.dependencies.Add(dep);
      }
      if(!md.save(fileSpec))
        return false;

      return true;
    }
    /*----< checkin self-test >------------------------------------*/
    /*
     * - run by instance of TestCheckin class
     */
    public bool testComponent()
    {
      TestCheckin test = new TestCheckin();
      return test.testCheckin();
    }
  }
  ///////////////////////////////////////////////////////////////////
  // TestCheckin class - tests all major operations of checkin
  //
  class TestCheckin
  {
    private Checkin checkin = new Checkin();

    /*----< constructor >------------------------------------------*/

    public TestCheckin()
    {
      if (RepoEnvironment.version == null)
        RepoEnvironment.version = new Version();
      if (RepoEnvironment.storage == null)
        RepoEnvironment.storage = new Storage();
    }
    /*----< prepare file system for testing >---------------------*/

    void testSetup()
    {
      TestUtilities.putLine("purging storage files");
      Storage.deleteStaging();
      Storage.deleteStorage();

      // wait for storage and staging to clear
      Thread.Sleep(100);

      // clear version cache
      TestUtilities.putLine("resetting file version cache");
      RepoEnvironment.version.restoreVersionsFromFiles();
      TestUtilities.putLine();
    }
    /*----< test path handling >-----------------------------------*/
    /*
     * - test storagePath, extractCategory, and extractFilename
     */
    bool testPathHandling()
    {
      TestUtilities.vbtitle("testing path handling");

      DirName testCategory = "TestCategory";
      FileName testFileName = "foobar.cs.23";

      FullPath testPath1 = checkin.storagePath(testCategory + "/" + testFileName);
      TestUtilities.putLine(string.Format("test path = \"{0}\"", testPath1));

      DirName category = checkin.extractCategory(testPath1);
      if (category != testCategory)
        return false;
      TestUtilities.putLine(string.Format("category = \"{0}\"", category));

      FileName fileName = checkin.extractFileName(testPath1);
      if (fileName != testFileName)
        return false;
      TestUtilities.putLine(string.Format("fileName = \"{0}\"", fileName));

      testPath1 = "abc123";
      TestUtilities.putLine(string.Format("test path = \"{0}\"", testPath1));

      category = checkin.extractCategory(testPath1);
      if (category.Length != 0)
        return false;
      TestUtilities.putLine(string.Format("category = \"{0}\"", category));

      fileName = checkin.extractFileName(testFileName);
      if (fileName.Length !=0)
        return false;
      TestUtilities.putLine(string.Format("fileName = \"{0}\"", fileName));

      return true;
    }
    /*----< test findStoredMetadata >------------------------------*/

    bool testFindStoredMetaData()
    {
      TestUtilities.vbtitle("testing search for metadata files in storage");

      string category = "test";

      string testFile1 = "testFile1.cs";
      FileStream fs =File.Create(RepoEnvironment.storage.stagingFilePath(testFile1));
      fs.Close();
      string testFile2 = "testFile2.cs";
      fs = File.Create(RepoEnvironment.storage.stagingFilePath(testFile2));
      fs.Close();

      MetaData testMd1 = new MetaData("Jim Fawcett", "testFile1");
      testMd1.fileSpec = RepoEnvironment.version.addVersion(category, testFile1);
      string testMdFile1 = "testMd1.xml";
      string savePath = RepoEnvironment.storage.stagingFilePath(testMdFile1);
      testMd1.save(savePath);

      MetaData testMd2 = new MetaData("Jim Fawcett", "testFile2");
      testMd2.fileSpec = RepoEnvironment.version.addVersion(category, testFile2);
      string testMdFile2 = "testMd2.xml";
      savePath = RepoEnvironment.storage.stagingFilePath(testMdFile2);
      testMd2.save(savePath);

      RepoEnvironment.storage.addFile(category, testFile1, testFile1, false);  //copy
      RepoEnvironment.storage.addFile(category, testFile1, testFile1);         //move
      RepoEnvironment.storage.addFile(category, testFile2, testFile2);
      RepoEnvironment.storage.addFile(category, testMdFile1, testMdFile1, false);
      RepoEnvironment.storage.addFile(category, testMdFile1, testMdFile1);
      RepoEnvironment.storage.addFile(category, testMdFile2, testMdFile2);

      // shouldn't find this - it's not metadata
      MetaData md = new MetaData();
      string searchName1 = "testFile1.cs";  // shouldn't find, it's not a metadata file
      string fsm1 = checkin.findStoredMetaData(category, searchName1);
      checkin.showFoundStatus(category, searchName1, fsm1);

      // should find this - returns latest version fileSpec
      string searchName2 = "testMd1.xml";   // unversioned metadata file
      string fsm2 = checkin.findStoredMetaData(category, searchName2);
      checkin.showFoundStatus(category, searchName2, fsm2);

      // should find this - returns the versioned fileSpec
      string searchName3 = "testMd2.xml.1"; // versioned metadata file
      string fsm3 = checkin.findStoredMetaData(category, searchName3);
      checkin.showFoundStatus(category, searchName3, fsm3);

      return ((fsm1.Length == 0) && (fsm2.Length > 0) && (fsm3.Length > 0));
    }
    /*----< test checkin including dependencies >------------------*/
    /*
     * - places files testFile1.cs, testFile2.cs, and testFile3.cs in staging
     * - checks in first two files without dependencies
     * - checks in third file with dependencies on the first two
     */
    public bool testDoCheckin()
    {
      TestUtilities.vbtitle("testing doCheckin");
      testSetup();

      // checkin file testFile1.cs
      CheckinInfo info1 = new CheckinInfo();
      info1.category = "test";
      info1.name = "testFile1.cs";
      info1.isOpen = false;
      info1.fileName = "testFile1.cs";
      info1.author = "Jim Fawcett";
      info1.description = "first checkin test file";
      FileStream fs = File.Create(RepoEnvironment.storage.stagingFilePath(info1.fileName));
      fs.Close();
      bool t1 = checkin.doCheckin(info1);

      // show the resulting metadata file
      bool t2 = false;
      TestUtilities.putLine(string.Format("first checkin of file \"{0}\":", info1.fileName));
      FileName found = checkin.findStoredMetaData(info1.category, info1.name + ".xml");
      TestUtilities.putLine(string.Format("metadata \"{0}\" stored in category \"{1}\" has contents:", found, info1.category));
      IMetaData md = checkin.loadMetaData(checkin.storagePath(info1.category) + "/", found);
      if (md != null)
      {
        t2 = true;
        if (ClientEnvironment.verbose)
          md.show();
      }
      TestUtilities.putLine();

      // checkin file testFile1.cs again - should increase version number by 1
      info1.description = "checking in the first file again - increases version number";
      fs = File.Create(RepoEnvironment.storage.stagingFilePath(info1.fileName));
      fs.Close();
      bool t3 = checkin.doCheckin(info1);

      // show resulting metadata file
      bool t4 = false;
      TestUtilities.putLine(string.Format("second checkin of file \"{0}\":", info1.fileName));
      found = checkin.findStoredMetaData(info1.category, info1.name + ".xml");
      TestUtilities.putLine(string.Format("metadata \"{0}\" stored in category \"{1}\" has contents:", found, info1.category));
      md = checkin.loadMetaData(checkin.storagePath(info1.category) + "/", found);
      if (md != null)
      {
        t4 = true;
        if (ClientEnvironment.verbose)
          md.show();
      }
      TestUtilities.putLine();

      // checkin second file testFile2.cs
      CheckinInfo info2 = new CheckinInfo();
      info2.category = "test";
      info2.name = "testFile2.cs";
      info2.fileName = "testFile2.cs";
      info2.isOpen = false;
      info2.author = "Jim Fawcett";
      info2.description = "second checkin test file";
      fs = File.Create(RepoEnvironment.storage.stagingFilePath(info2.fileName));
      fs.Close();
      bool t5 = checkin.doCheckin(info2);

      // show resulting metadata
      bool t6 = false;
      found = checkin.findStoredMetaData(info2.category, info2.name + ".xml");
      TestUtilities.putLine(string.Format("metadata \"{0}\" stored in category \"{1}\" has contents:", found, info2.category));
      md = checkin.loadMetaData(checkin.storagePath(info2.category) + "/", found);
      if (md != null)
      {
        t6 = true;
        if (ClientEnvironment.verbose)
          md.show();
      }
      TestUtilities.putLine();

      // checkin third file testFile3.cs that depends on the first two files
      CheckinInfo info3 = new CheckinInfo();
      info3.category = "test";
      info3.name = "testFile3.cs";
      info3.fileName = "testFile3.cs";
      fs = File.Create(RepoEnvironment.storage.stagingFilePath(info3.fileName));
      fs.Close();
      info3.author = "Jim Fawcett";
      info3.description = "third checkin test file has dependencies on first two checkins";

      // add dependencies
      FileName found1 = checkin.findStoredMetaData(info1.category, info1.name + ".xml");
      found1 = "test/" + found1;
      info3.deps.Add(found1);

      FileName found2 = checkin.findStoredMetaData(info2.category, info2.name + ".xml");
      found2 = "test/" + found2;
      info3.deps.Add(found2);
      bool t7 = checkin.doCheckin(info3);

      // show resulting metadata file
      bool t8 = false;
      found = checkin.findStoredMetaData(info3.category, info3.name + ".xml");
      TestUtilities.putLine(string.Format("metadata \"{0}\" stored in category \"{1}\" has contents:", found, info3.category));
      md = checkin.loadMetaData(checkin.storagePath(info3.category) + "/", found);
      if(md != null)
      {
        t8 = true;
        if(ClientEnvironment.verbose)
          md.show();
      }

      return t1 && t2 && t3 && t4 && t5 && t6 && t7 && t8;
    }
    /*----< test editing of stored metadata >----------------------*/
    /*
     * - test closing (checkin), changing name and description
     * - no other properties can be changed.
     */
    public bool testEditMetadata()
    {
      if (ClientEnvironment.verbose)
        TestUtilities.title("testing editMetaData in Storage");

      string found = checkin.findStoredMetaData("test", "testMd2.xml");
      if (found.Length == 0)
        return false;
      CheckinInfo info = new CheckinInfo();
      info.fileName = "testMd2.xml";
      info.category = "test";
      info.name = "newFileName";
      info.description = "new description";
      info.isOpen = false;

      if(!checkin.editMetadata(info))
        return false;

      IMetaData md= null;
      bool test = TestUtilities.handleInvoke(
        () =>
        {
          md = checkin.loadMetaData(checkin.storagePath("test"), found);
          return (md != null);
        }
      );
      if (ClientEnvironment.verbose)
      {
        Console.Write("\n  revised metadata file \"{0}\" contains:", found);
        md.show();
      }
      return test;
    }
    /*----< test add dependencies to stored metadata >-------------*/
    /*
     * - dependencies must have form "category/filename"
     * - filename should be a versioned filename contained in Storage/category
     */
    bool testAddDependencies()
    {
      if (ClientEnvironment.verbose)
        TestUtilities.title("test adding dependencies to stored metadata");

      string category = "test";
      string fileName = "testFile1.cs.xml";
      string found = checkin.findStoredMetaData(category, fileName);
      if (found.Length == 0)
        return false;

      Dependencies deps = new Dependencies();
      deps.Add(category + "/foobar.cs.13");
      deps.Add(category + "/feebar.cs.23");

      bool t1 = checkin.addDependencies(checkin.storagePath(category) + "/" + found, deps);
      if (t1 == false)
        return false;

      if(ClientEnvironment.verbose)
      {
        IMetaData md = checkin.loadMetaData(checkin.storagePath(category), found);
        if (md == null)
          return false;

        Console.Write("\n  modified metadata in \"" + found + "\":");
        md.show();
      }
      return true;
    }
    /*----< Checkin Package self-test >----------------------------*/

    public bool testCheckin()
    {
      TestUtilities.vbtitle("Testing Checkin Component", '=');
      testSetup();

      bool t1 = testPathHandling();
      TestUtilities.checkResult(t1, "several path functions");
      TestUtilities.putLine();

      bool t2 = testFindStoredMetaData();
      TestUtilities.checkResult(t1, "findStoredMetaData");
      TestUtilities.putLine();

      bool t3 = testDoCheckin();
      TestUtilities.checkResult(t2, "doCheckin");
      TestUtilities.putLine();

      bool t4 = testEditMetadata();
      TestUtilities.checkResult(t3, "editMetadata");
      TestUtilities.putLine();

      bool t5 = testAddDependencies();
      TestUtilities.checkResult(t5, "addDependencies");

      return t1 && t2 && t3 && t4 && t5;
    }

    static void Main(string[] args)
    {
      if (RepoEnvironment.version == null)
        RepoEnvironment.version = new Version();
      if (RepoEnvironment.storage == null)
        RepoEnvironment.storage = new Storage();

      ClientEnvironment.verbose = true;

      TestCheckin test = new TestCheckin();
      test.testCheckin();
      TestUtilities.putLine("\n");
    }
  }
}
