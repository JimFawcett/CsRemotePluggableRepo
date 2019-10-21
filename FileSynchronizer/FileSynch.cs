///////////////////////////////////////////////////////////////////////////
// FileSync.cs - supports synchronizing files in two directories         //
// Ver 1.0                                                               //
// Jim Fawcett, CSE681-OnLine Software Modeling & Analysis, Summer 2017  //
///////////////////////////////////////////////////////////////////////////
/*
 * Package Operations:
 * -------------------
 * This package contains a class FileSynch with public methods:
 * - isSynched            : evaluates exclusive union of file lists
 * - testComponent()      : self-test
 * and public properties:
 * - synchPath            : path to synchronized directory
 * - notInList            : files in synchDir, not in list
 * - notInSynchDir        : files in list, not in synchDir
 * - synchDirFiles        : all files in synchDir
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
 * Ver 1.0 : 20 Jul 2017
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
  using FileName = String;
  using FileSpec = String;
  using FileList = List<String>;
  using SynchPath = String;

  ///////////////////////////////////////////////////////////////////
  // FileSynch class
  // - given two lists of files, finds files in one, but not the other

  public class FileSynch
  {
    public SynchPath synchPath { get; set; }
    public FileList notInList { get; set; } = new FileList();
    public FileList notInSyncDir { get; set; } = new FileList();
    public FileList synchDirFiles { get; set; } = new FileList();

    /*----< save path to directory to be synchronized >------------*/

    public FileSynch(SynchPath path)
    {
      synchPath = path;
    }
    /*----< converts List of files to HashSet of files >-----------*/

    HashSet<FileName> toHashSet(List<FileName> list)
    {
      HashSet<FileName> fileSet = new HashSet<FileName>();
      foreach (FileName file in list)
      {
        fileSet.Add(file);
      }
      return fileSet;
    }
    /*----< converts array of files to HashSet of files >----------*/

    HashSet<FileName> toHashSet(FileName[] list)
    {
      HashSet<FileName> fileSet = new HashSet<FileName>();
      foreach (FileName file in list)
      {
        fileSet.Add(file);
      }
      return fileSet;
    }
    /*----< are lists synchronized, e.g., have same members? >-----*/
    /*
     *  Evaluates notInSynchDir and notInList collections
     */
    public bool isSynched(FileList list)
    {
      synchDirFiles = System.IO.Directory.GetFiles(synchPath).ToList<FileName>();
      for(int i=0; i<synchDirFiles.Count; ++i)
      {
        synchDirFiles[i] = System.IO.Path.GetFileName(synchDirFiles[i]);
      }

      HashSet<FileName> synchFiles = toHashSet(synchDirFiles);
      foreach(FileName file in list)
      {
        if (!synchFiles.Contains(file))
          notInSyncDir.Add(file);
      }
      HashSet<FileName> synchList = toHashSet(list);
      foreach(FileName file in synchFiles)
      {
        if (!synchList.Contains(file))
          notInList.Add(file);
      }
      return (notInList.Count == 0 && notInSyncDir.Count == 0);
    }
    /*----< creates empty file for testing >-----------------------*/

    public bool createFile(FileName name)
    {
      try
      {
        FileSpec fileSpec = System.IO.Path.Combine(synchPath, name);
        System.IO.FileStream fs = System.IO.File.Create(fileSpec);
        fs.Close();
        return true;
      }
      catch(Exception ex)
      {
        Console.Write("\n  {0}", ex.Message);
        return false;
      }
    }
    /*----< display list elements >--------------------------------*/

    void showList(FileList list, string listName)
    {
      Console.Write("\n  {0}:", listName);
      foreach(FileName file in list)
      {
        Console.Write("\n    {0}", file);
      }
      Console.Write("\n");
    }
    /*----< test synchronization >---------------------------------*/

    public bool testComponent()
    {
      TestUtilities.title("Testing FileSynchronizer", '=');

      createFile("testFile1");
      createFile("testFile2");
      createFile("testFile3");
      createFile("testFile4");
      FileList testList = new FileList();
      testList.Add("testFile2");
      testList.Add("testFile3");
      testList.Add("testFile4");
      testList.Add("testFile5");

      bool test = !isSynched(testList) && notInList.Contains("testFile1") && notInSyncDir.Contains("testFile5");
      showList(synchDirFiles, "Synch directory files");
      showList(testList, "Comparison list of files");
      showList(notInList, "files in synch dir but not in list");
      showList(notInSyncDir, "files in list but not in synch dir");

      return test;
    }
  }

  ///////////////////////////////////////////////////////////////////
  // TestFileSynch class

  class TestFileSynch
  {

    static void Main(string[] args)
    {
      ClientEnvironment.verbose = true;

      FileSynch fs = new FileSynch(".");
      bool test = fs.testComponent();
      TestUtilities.checkResult(test, "FileSynchronizer");
      TestUtilities.putLine("\n");
    }
  }
}
