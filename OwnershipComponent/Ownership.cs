///////////////////////////////////////////////////////////////////////////
// Ownership.cs - supports authorizing actions in the repository         //
// Ver 1.0                                                               //
// Jim Fawcett, CSE681-OnLine Software Modeling & Analysis, Summer 2017  //
///////////////////////////////////////////////////////////////////////////
/*
 * Package Operations:
 * -------------------
 * This package contains a single class Ownership with public functions:
 * - saveCredentials : accept and store user credetials
 * - isAuthorized    : manages access to Repository features
 * - testComponent   : Ownership self test
  * Note:
 *  - Package operations are all true by default, e.g., open access
 *  - The functions are here to support the Ownership interface that
 *    will provide more functionality for later versions.
* 
 * Required Files:
 * ---------------
 * - Ownership.cs
 * - IPluggable     - Repository interfaces and shared data
 * - TestUtilities  - Helper class that is used mostly for testing
 * 
 * Maintenance History:
 * --------------------
 * 11 June 2017
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

  ///////////////////////////////////////////////////////////////////
  // Ownership class
  // - will eventually provide authorization and credential management

  public class Ownership : IOwnership
  {
    public string componentType { get; } = "Ownership";

    public bool saveCredentials(string cred)
    {
      return true;
    }
    public bool isAuthorized(DirName category, FileName fileName)
    {
      return true;
    }
    public bool testComponent()
    {
      TestUtilities.vbtitle("testing Ownership component", '=');
      TestUtilities.putLine("open ownership - nothing to do");
      return true;    // open ownership - nothing to fail
    }
  }
  public class TestOwnership
  {
    static void Main(string[] args)
    {
      ClientEnvironment.verbose = true;
      Ownership ownership = new Ownership();
      ownership.testComponent();
      TestUtilities.putLine("\n");
    }
  }
}
