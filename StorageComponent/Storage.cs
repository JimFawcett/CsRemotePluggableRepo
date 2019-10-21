///////////////////////////////////////////////////////////////////////////
// Storage.cs - Pluggable policy for storing packages                    //
//                                                                       //
// Jim Fawcett, CSE681-OnLine Software Modeling & Analysis, Spring 2017  //
///////////////////////////////////////////////////////////////////////////
/*
 * Package Operations:
 * -------------------
 * This package contains a class Storage with public functions:
 * - createStorage     : creates specified folder on storage path
 * - stagingPath       : string staging path including client stagingDir
 * - stagingFilePath   : returns fully qualified fileName in staging path
 * - storagePath       : string storage path including category
 * - storageFilePath   : returns fully qualified fileName in storage path
 * - addFile           : moves or copies file from staging to storage after versioning
 * - retrieveFile      : copies file from storage to staging and removes version
 * - findFile          : returns versioned name of file found in category
 * - findVersions      : returns list of all versions of specified file
 * - deleteFile        : deletes versioned file in category
 * - loadCategories    : loads list of default storage folders from XML file
 * - saveCategories    : saves list of default storage folders to XML file
 * - addCategory       : adds a category to the categories list
 * - files             : returns list of files in category
 * - categories        : return list of all folders in storage path
 * - deleteStorage     : deletes all files and folders in storage area
 * - deleteStaging     : deletes all files and folders in staging area
 * 
 * This package also contains a class TestStorage with public functions:
 * - testCreateStorage : tests creation of storage with specified categories
 * - testDeleteStaging : tests deleting all files and folders in staging path
 * - testDeleteStorage : tests deleting all files and folders in storage path
 * - testAddFile       : tests versioning and copying file to specified category
 * - testRetrieveFile  : tests retrieving specified file from storage to staging and removing versioning
 * - testFindFile      : tests finding files and finding all versions of a file
 * - testViews         : demos files and categories functions
 * - testStorage       : Storage self test
 * 
 * Required Files:
 * ---------------
 * - Storage.cs     - manages directories and their contents
 * - Version.cs     - builds, tracks, and extracts version numbers
 * - IPluggable.cs  - Repository interfaces and shared data
 * - TestUtilities  - Helper class that is used mostly for testing
 * 
 * Maintenance History:
 * --------------------
 * 31 May 2017
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
using System.Threading;

namespace PluggableRepository
{
  // type aliases with semantic meaning
  using FileSpec = String;  // c:/.../category/filename
  using FileRef = String;   // category/filename
  using FileName = String;  // filename may have version number at end
  using PathName = String;  // full path with no filename
  using DirName = String;   // directory name 

  ///////////////////////////////////////////////////////////////////
  // Storage class
  // - manages Repository storage

  public class Storage : IStorage
  {
    public string componentType { get; } = "Storage";
    public List<string> categories_ { get; set; } = new List<string>();
    private Dependency dependency { get; set; } = new Dependency();
    private VersionChain versionChain { get; set; } = new VersionChain();

    /*----< constructor >------------------------------------------*/

    public Storage()
    {
      loadCategories();
      foreach(DirName cat in categories_)
      {
        createStorage(cat);
      }
      if (RepoEnvironment.storage == null)
        RepoEnvironment.storage = this;
    }
    /*----< finalizer >--------------------------------------------*/

    ~Storage()
    {
      saveCategories();
    }
    /*----< create directory for storing packages >----------------*/

    public bool createStorage(DirName category = "")
    {
      PathName path = Path.Combine(RepoEnvironment.storagePath, category);
      DirectoryInfo di = Directory.CreateDirectory(path);
      if (di.Exists)
      {
        return true;
      }
      return false;
    }
    /*----< populate dependency and version info from storage >----*/

    public void analyzeDependencies()
    {
      IVersion version = RepoEnvironment.version;
      if (version == null)
        version = new Version();

      List<DirName> cats = categories();
      foreach (DirName cat in cats)
      {
        List<FileSpec> fileNames = files(cat);
        foreach (FileName fileName in fileNames)
        {
          //FileSpec fileSpec = System.IO.Path.Combine(cat + "/", fileName);
          FileSpec fileSpec = makeFileRef(cat, fileName);
          if (fileSpec.Contains(".xml"))
          {
            string path = System.IO.Path.Combine(RepoEnvironment.storagePath, fileSpec);
            MetaData md = new MetaData();
            md.load(path);
            foreach (string pkg in md.dependencies)
            {
              dependency.addChild(fileSpec, pkg);
              dependency.addParent(pkg, fileSpec);
            }
          }
          int ver = RepoEnvironment.version.getVersion(fileSpec);
          string key = RepoEnvironment.version.removeVersion(fileSpec);
          versionChain.addVersion(key, ver, false);
        }
        foreach (FileName fileName in fileNames)
        {
          FileSpec fileSpec = System.IO.Path.Combine(cat, fileName);
          string key = RepoEnvironment.version.removeVersion(fileSpec);
          versionChain.sort(key);
        }
      }
    }
    /*----< adds child to dependency cache >-----------------------*/

    public void upDateDependencyCache(FileSpec parent, FileSpec child)
    {
      dependency.addChild(parent, child);
      dependency.addParent(child, parent);
    }
    /*----< returns children of package >--------------------------*/

    public List<FileSpec> children(FileSpec package)
    {
      return dependency.getChildren(package);
    }
    /*----< returns parents of package >---------------------------*/

    public List<FileSpec> parents(FileSpec package)
    {
      return dependency.getParents(package);
    }
    /*----< returns decendents of package >-----------------------*/

    public List<FileRef> descendents(FileSpec package)
    {
      return dependency.descendents(package);
    }
    /*----< make FileRef, e.g., category/filename >---------------*/

    public FileRef makeFileRef(DirName category, FileName fileName)
    {
      return System.IO.Path.Combine(category + "/", fileName);
    }
    /*----< returns path to client's staging path >---------------*/

    public PathName stagingPath(DirName subFolder = "")
    {
      return Path.Combine(RepoEnvironment.stagingPath, ClientEnvironment.stagingDir, subFolder);
    }
    /*----< add fileName to end of stagingPath >------------------*/

    public FileSpec stagingFilePath(FileName fileName)
    {
      return Path.Combine(stagingPath(), fileName);
    }
    /*----< returns path to category in storage area >-------------*/

    public PathName storagePath(DirName category = "")
    {
      return Path.Combine(RepoEnvironment.storagePath, category);
    }
    /*----< add fileName to end of storagePath >-------------------*/

    public FileSpec storageFilePath(DirName category, FileName fileName)
    {
      return Path.Combine(storagePath(category), fileName);
    }
    /*----< move or copy file from staging area to storage >-------*/
    /* 
     *  srcFile is the name of file in stagingPath
     *  dstFile is the name of the file to be moved to storagePath
     *  category is subdirectory of storagePath where dstFile will be moved or copied
     *  
     *  addFile adds a version number one higher than existing version number 
     *  to dstFile before putting in storage area defined by category
     *  
     *  For testing purposes it may be convenient to copy, rather than move, the srcFile.
     *  If so set move to false.
     */
    public bool addFile(DirName category, FileName srcFile, FileName dstFile = "", bool move = true)
    {
      if (dstFile.Length == 0)
        dstFile = srcFile;

      FileName ver_dstFile = RepoEnvironment.version.addVersion(category, dstFile);

      FileSpec srcFileSpec = stagingFilePath(srcFile);
      FileSpec catPath = storagePath(category);

      if (!Directory.Exists(catPath))
        Directory.CreateDirectory(catPath);

      FileSpec dstFileSpec = storageFilePath(category, ver_dstFile);

      bool ok;
      if (move)
      {
        ok = TestUtilities.handleInvoke(
          () => { File.Move(srcFileSpec, dstFileSpec); return true; }
        );
      }
      else
      {
        ok = TestUtilities.handleInvoke(
          () => { File.Copy(srcFileSpec, dstFileSpec); return true; }
        );
      }

      return ok;
    }
    /*----< move or copy file from staging area to storage >-------*/
    /* 
     *  srcFile is the name of file in stagingPath
     *  dstFile is the name of the file to be moved to storagePath
     *  category is subdirectory of storagePath where dstFile will be moved or copied
     *  
     *  modifyFile adds a version number equal to the existing version number 
     *  to dstFile before putting in storage area defined by category
     *  
     *  For testing purposes it may be convenient to copy, rather than move, the srcFile.
     *  If so set move to false.
     */
    public bool modifyFile(DirName category, FileName srcFile, FileName dstFile = "", bool move = true)
    {
      move = false;
      if (dstFile.Length == 0)
        dstFile = srcFile;

      //FileName ver_dstFile = RepoEnvironment.version.addVersion(category, dstFile);
      int ver = RepoEnvironment.version.getLatestVersion(category, dstFile);
      FileName ver_dstFile = dstFile + "." + ver.ToString();

      FileSpec srcFileSpec = stagingFilePath(srcFile);
      FileSpec catPath = storagePath(category);

      if (!Directory.Exists(catPath))
        Directory.CreateDirectory(catPath);

      FileSpec dstFileSpec = storageFilePath(category, ver_dstFile);

      bool ok;
      if (move)
      {
        ok = TestUtilities.handleInvoke(
          () => { File.Move(srcFileSpec, dstFileSpec); return true; }
        );
      }
      else
      {
        ok = TestUtilities.handleInvoke(
          () => { File.Copy(srcFileSpec, dstFileSpec, true); return true; }
        );
      }

      return ok;
    }
    /*----< copy file from storage to staging area >---------------*/
    /*
     * - srcFile is expected to be a file name, e.g., has no path
     */
    public bool retrieveFile(DirName category, FileName srcFile, bool overwrite = true)
    {
      FileSpec srcFileSpec = storageFilePath(category, srcFile);
      FileSpec dstFileSpec = RepoEnvironment.version.removeVersion(srcFile);
      dstFileSpec = stagingFilePath(dstFileSpec);

      bool test = TestUtilities.handleInvoke(
        () =>
        {
          File.Copy(srcFileSpec, dstFileSpec, overwrite); return true;
        }
      );
      return test;
    }
    /*----< find file in Storage/Category >-----------------------*/

    public FileName findFile(DirName category, FileName fileName)
    {
      string foundName = "";

      // if not versioned, add latest version

      if (!RepoEnvironment.version.hasVersion(fileName))
      {
        int ver = RepoEnvironment.version.getLatestVersion(category, fileName);
        fileName = fileName + "." + ver.ToString();
      }
      List<FileName> files = RepoEnvironment.storage.files(category);
      foreach (FileSpec file in files)
      {
        if (file == fileName)
        {
          foundName = fileName;
          break;
        }
      }
      return foundName;
    }
    /*----< find all versions of file in category >----------------*/

    public List<FileName> findVersions(DirName category, FileName fileName)
    {
      if (RepoEnvironment.version.hasVersion(fileName))
        fileName = RepoEnvironment.version.removeVersion(fileName);

      FileName[] files = Directory.GetFiles(storagePath(category), fileName + "*");
      for(int i=0; i < files.Length; ++i)
      {
        files[i] = Path.GetFileName(files[i]);
      }
      return files.ToList<string>();
    }
    /*----< delete file from storage >-----------------------------*/

    public bool deleteFile(string category, string fileName)
    {
      return false;  // not implemented yet
    }
    /*----< retrieve category names from XML file >----------------*/
    /*
     *  Categories are the names of folders used to partition
     *  Repository contents into manageable chunks.
     */
    void loadCategories()
    {
      XDocument doc = null;
      try
      {
        doc = XDocument.Load(RepoEnvironment.categories);
        IEnumerable<XElement> cats = doc.Descendants("category");
        foreach (var cat in cats)
        {
          categories_.Add(cat.Value);
        }
      }
      catch
      {
        categories_.Add("test");
        categories_.Add("test_a");
        categories_.Add("test_b");
      }
    }
    /*----< save Categories to XML file >--------------------------*/

    void saveCategories()
    {
      XDocument doc = new XDocument();
      XElement root = new XElement("categories");
      doc.Add(root);
      foreach (var item in categories_)
      {
        XElement elem = new XElement("category");
        XText txt = new XText(item);
        elem.Add(txt);
        root.Add(elem);
      }
      doc.Save(RepoEnvironment.categories);
    }
    /*----< add new category >-------------------------------------*/

    public void addCategory(DirName categoryName)
    {
      categories_.Add(categoryName);
    }
    /*----< return list of staged files >--------------------------*/

    public List<FileName> stagedFiles(bool showPath = false)
    {
      PathName path = RepoEnvironment.stagingPath;
      FileSpec[] files = Directory.GetFiles(path);
      for (int i = 0; i < files.Length; ++i)
      {
        if (showPath)
          files[i] = Path.GetFullPath(files[i]);  // now an absolute FileSpec
        else
          files[i] = Path.GetFileName(files[i]);  // now a FileName
      }
      return files.ToList<FileName>();
    }
    /*----< delete all staged files >------------------------------*/

    public void deleteStagedFiles()
    {
      PathName path = RepoEnvironment.stagingPath;
      FileSpec[] files = Directory.GetFiles(path);
      int count = files.Length;
      for (int i = 0; i < count; ++i)
      {
        System.IO.File.Delete(files[i]);
      }
    }
    /*----< return list of files in storage/category >-------------*/

    public List<FileName> files(DirName category, bool showPath = false)
    {
      PathName path = Path.Combine(RepoEnvironment.storagePath, category);
      FileSpec[] files = Directory.GetFiles(path);
      for (int i = 0; i < files.Length; ++i)
      {
        if (showPath)
          files[i] = Path.GetFullPath(files[i]);  // now an absolute FileSpec
        else
          files[i] = Path.GetFileName(files[i]);  // now a FileName
      }
      return files.ToList<FileName>();
    }
    /*---< return list of unique fileNames in storage/category >---*/

    public List<FileName> uniqueFileNames(DirName category, bool showPath = false)
    {
      HashSet<FileName> set = new HashSet<FileSpec>();
      List<FileName> fileNames = files(category);
      for(int i = 0; i<fileNames.Count; ++i)
      {
        fileNames[i] = RepoEnvironment.version.removeVersion(fileNames[i]);
        if (!set.Contains(fileNames[i]))
          set.Add(fileNames[i]);
      }
      return set.ToList<FileName>();
    }
    /*----< return list of categories >----------------------------*/

    public List<DirName> categories(bool showPath = false)
    {
      PathName[] dirs = Directory.GetDirectories(RepoEnvironment.storagePath);
      List<DirName> cats = new List<DirName>();
      foreach (DirName dir in dirs)
      {
        DirectoryInfo di = new DirectoryInfo(dir);
        DirName name = di.Name;
        if (showPath)
        {
          name = Path.Combine(RepoEnvironment.storagePath, name);
          name = Path.GetFullPath(name);  // now a FullPath
        }
        cats.Add(name);
      }
      return cats;
    }
    /*----< delete all files and folders on path >-----------------*/
    /*
     * - will return false, doing nothing, if path is not a
     *   storage or staging path.
     */
    private static bool deleteFilesAndFolders(PathName path)
    {
      PathName storePath = RepoEnvironment.storagePath;
      PathName stagePath = RepoEnvironment.stagingPath;

      // make sure we only delete things in Storage or Staging
      if (path.Contains(storePath) || path.Contains(stagePath))
      {
        // delete all folders and their contents

        PathName[] dirs = Directory.GetDirectories(path);

        bool test1 = dirs.Length == 0;
        int count = dirs.Length;
        for (int i = 0; i < count; ++i)
        {
          test1 = TestUtilities.handleInvoke(
            () => { Directory.Delete(dirs[i], true); return true; }
          );
        }
        // delete all remaining files

        FileSpec[] files = Directory.GetFiles(path);

        bool test2 = false;
        count = files.Length;
        for (int i = 0; i < count; ++i)
        {
          test2 = TestUtilities.handleInvoke(
            () => { File.Delete(files[i]); return true; }
          );
        }
        RepoEnvironment.version.restoreVersionsFromFiles();
        return test1 && test2;
      }
      return false;
    }
    /*----< delete all contents in storage >-----------------------*/

    public static bool deleteStorage()
    {
      return deleteFilesAndFolders(RepoEnvironment.storagePath);
    }
    /*----< delete all contents in staging >-----------------------*/

    public static bool deleteStaging()
    {
      return deleteFilesAndFolders(RepoEnvironment.stagingPath);
    }
    /*----< test storage component >-------------------------------*/

    public bool testComponent()
    {
      TestStorage test = new TestStorage();
      return test.testStorage();
    }
  }

  class TestStorage
  {
    private Storage storage = new Storage();
    private static int fileIndex = 0;

    /*----< test creating subfolder in storage >-------------------*/

    public bool testCreateStorage()
    {
      TestUtilities.vbtitle("testing createStorage");
      bool test1, test2;
      DirName path = "test";

      test1 = TestUtilities.handleInvoke(() => { return storage.createStorage(path); });
      test2 = Directory.Exists(storage.storagePath(path));
      if (test1 && test2)
        TestUtilities.putLine(string.Format("Created storage \"{0}\"", path));
      TestUtilities.checkResult(test1 && test2, "Storage.createStorage");
      return test1 && test2;
    }
    /*----< test delete staging files and folders >----------------*/

    public bool testDeleteStaging()
    {
      // setup test
      bool test = TestUtilities.handleInvoke(
        () =>
        {
          DirectoryInfo di = Directory.CreateDirectory(storage.stagingPath("test"));
          FileStream fs = File.Create(Path.Combine(storage.stagingPath("test"), "testFile"));
          fs.Close();
          fs = File.Create(Path.Combine(storage.stagingPath(), "anotherTestfile"));
          fs.Close();
          return true;
        }
      );
      if (!test)
        return false;

      // test execution
      TestUtilities.vbtitle("testing deleteStaging");
      test = Storage.deleteStaging();
      TestUtilities.checkResult(test, "Storage.deleteStaging");
      return test;
    }
    /*----< test delete storage files and folders >----------------*/

    public bool testDeleteStorage()
    {
      // setup test
      bool test = TestUtilities.handleInvoke(
        () =>
        {
          DirectoryInfo di = Directory.CreateDirectory(storage.storagePath("test"));
          FileStream fs = File.Create(Path.Combine(storage.storagePath("test"), "testFile"));
          fs.Close();
          fs = File.Create(Path.Combine(storage.storagePath(), "anotherTestfile"));
          fs.Close();
          return true;
        }
      );
      if (!test)
        return false;

      // test execution
      TestUtilities.vbtitle("testing deleteStorage");
      test = Storage.deleteStorage();
      TestUtilities.checkResult(test, "Storage.deleteStorage");
      return test;
    }
    /*----< test moving file to category folder >------------------*/

    public bool testAddFile(FileName srcFile, DirName category)
    {
      TestUtilities.vbtitle("testing addFile");
      FileName dstFile = srcFile;
      FileSpec srcFileSpec = storage.stagingFilePath(srcFile);
      srcFileSpec = Path.GetFullPath(srcFileSpec);

      bool test;

      if (!File.Exists(srcFileSpec))
      {
        test = TestUtilities.handleInvoke(
          () => {
            var tempFile = File.Create(srcFileSpec);
            tempFile.Close();
            return true;
          }
        );
      }

      TestUtilities.putLine(string.Format("adding file \"{0}\" to category \"{1}\"", srcFile, category));

      test = TestUtilities.handleInvoke(
        () => { return storage.addFile(category, srcFile); }
      );

      TestUtilities.checkResult(test, "Storage.testAddFile");
      return test;
    }
    /*----< test copying file from category to staging folder >----*/

    public bool testRetrieveFile(DirName category)
    {
      TestUtilities.vbtitle("testing retrieveFile");

      PathName path = storage.storagePath(category);
      FileSpec[] files = Directory.GetFiles(path);
      if (files.Length == 0)
      {
        TestUtilities.putLine(string.Format("can't find file in \"{0}\"", category));
        return false;
      }

      FileName testFile1 = Path.GetFileName(files[fileIndex]);
      TestUtilities.putLine(string.Format("retrieving file \"{0}\"", testFile1));
      bool result1 = storage.retrieveFile(category, testFile1);
      fileIndex = (++fileIndex) % files.Length;
      TestUtilities.checkResult(result1, "Storage.retrieveFile");

      FileName testFile2 = Path.GetFileName(files[fileIndex]);
      TestUtilities.putLine(string.Format("retrieving file \"{0}\"", testFile2));
      bool result2 = storage.retrieveFile(category, testFile2);
      fileIndex = (++fileIndex) % files.Length;
      TestUtilities.checkResult(result2, "Storage.retrieveFile");

      return result1 && result2;
    }
    /*----< test find file >---------------------------------------*/

    public bool testFindFile()
    {
      TestUtilities.vbtitle("testing findFile");
      DirName category = "test2";
      bool t1 = false;
      bool t2 = false;
      bool t3 = false;
      List<FileName> allfiles = storage.files(category);
      FileName file;
      if (allfiles.Count > 0)
      {
        file = allfiles[0];
        TestUtilities.putLine(string.Format("searching for file \"{0}\" in category \"{1}\"", file, category));
        FileName found1 = storage.findFile(category, file);
        TestUtilities.putLine(string.Format("found file \"{0}\"", found1));
        t1 = (found1.Length > 0);
        TestUtilities.checkResult(t1, "testFindFile");
      }
      if (allfiles.Count > 1)
      {
        file = "doesNotExist.cs";
        TestUtilities.putLine(string.Format("searching for file \"{0}\" in category \"{1}\"", file, category));
        string found2 = storage.findFile(category, file);
        TestUtilities.putLine(string.Format("found file \"{0}\"", found2));
        t2 = (found2.Length == 0);
        TestUtilities.checkResult(t2, "testFindFile");
      }
      if (allfiles.Count > 0)
      {
        file = RepoEnvironment.version.removeVersion(allfiles[1]);
        TestUtilities.putLine(string.Format("searching for file \"{0}\" in category \"{1}\"", file, category));
        FileName found3 = storage.findFile(category, file);
        TestUtilities.putLine(string.Format("found file \"{0}\"", found3));
        t3 = (found3.Length > 0);
        TestUtilities.checkResult(t3, "testFindFile");
      }
      bool result = t1 && t2 && t3;
      TestUtilities.checkResult(result, "Storage.findFile");
      return result;
    }
    /*----< test find versions >-----------------------------------*/

    bool testFindVersions()
    {
      TestUtilities.vbtitle("testing findVersions");

      bool test = true;
      List<DirName> categories = storage.categories();
      foreach(DirName cat in categories)
      {
        TestUtilities.putLine(string.Format("in category \"{0}\":", cat));
        List<FileName> files = storage.uniqueFileNames(cat);

        int fileCount = 0;
        foreach (FileName file in files)
        {
          TestUtilities.putLine(string.Format("  fileName \"{0}\" has versions:", file));
          List<FileName> versions = storage.findVersions(cat, file);
          {
            foreach (FileName version in versions)
            {
              TestUtilities.putLine(string.Format("    \"{0}\"", version));
              ++fileCount;
            }
          }
          // found a version for every fileName in category and previous test passed?
          test = test && (fileCount == versions.Count);
          fileCount = 0;
        }
      }
      TestUtilities.checkResult(test, "findVersioons");
      return true;
    }
    /*----< test content views >-----------------------------------*/

    public bool testViews()
    {
      TestUtilities.vbtitle("testing category view");
      bool showPath = false;

      List<DirName> testCategories = storage.categories(showPath);
      foreach (DirName cat in testCategories)
      {
        TestUtilities.putLine(string.Format("  {0}", cat));
      }
      TestUtilities.checkResult(true, "Storage.categories");  // demo, not test
      TestUtilities.putLine();

      TestUtilities.vbtitle("testing file view");
      foreach(DirName cat in testCategories)
      {
        TestUtilities.putLine(string.Format("  {0}", cat));
        List<FileName> allfiles = storage.files(cat, showPath);
        foreach(FileName file in allfiles)
        {
          TestUtilities.putLine(string.Format("    {0}", file));
        }
        TestUtilities.checkResult(true, "Storage.files");       // demo, not test
      }
      return true;
    }
    /*----< Storage self test >------------------------------------*/

    public bool testStorage()
    {
      TestUtilities.vbtitle("Testing Storage Component", '=');

      //deleteStorageDirectoryContents();

      bool t1 = testDeleteStorage();
      TestUtilities.putLine();
      bool t2 = testDeleteStaging();
      TestUtilities.putLine();
      bool t3 = testCreateStorage();
      TestUtilities.putLine();
      bool t4 = testAddFile("file1.cs", "test1");
      bool t5 = testAddFile("file1.cs", "test1");
      bool t6 = testAddFile("file1.cs", "test2");
      bool t7 = testAddFile("file2.cs", "test2");
      TestUtilities.putLine();
      bool t8 = testRetrieveFile("test2");
      TestUtilities.putLine();
      bool t9 = testFindFile();
      TestUtilities.putLine();
      bool t10 = testFindVersions();
      TestUtilities.putLine();
      bool t11 = testViews();
      return t1 && t2 && t3 && t4 && t5 && t6 && t7 && t8 && t9 && t10 && t11;
    }
  }
  /*----< run self tests >-----------------------------------------*/

#if (TEST_STORAGE)
  class Program
  {
    static void Main(string[] args)
    {
      if (RepoEnvironment.version == null)
        RepoEnvironment.version = new Version();

      ClientEnvironment.verbose = true;

      TestUtilities.title("Testing Storage Component", '=');
      Storage storage = new Storage();
      storage.analyzeDependencies();
      storage.dependency.showChildren();
      storage.dependency.showParents();
      storage.versionChain.show();

      ////////////////////////////////////////////////////////
      // These test functions alter the contents of Storage
      ////////////////////////////////////////////////////////
      //bool result = storage.testComponent();
      //TestUtilities.putLine();
      //TestUtilities.checkResult(result, "Storage Component");

      TestUtilities.putLine("\n");
    }
  }
#endif
}
