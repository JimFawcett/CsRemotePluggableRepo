﻿///////////////////////////////////////////////////////////////////////////
// Browse.cs - supports traversal of package dependency graphs           //
// Ver 1.0                                                               //
// Jim Fawcett, CSE681-OnLine Software Modeling & Analysis, Summer 2017  //
///////////////////////////////////////////////////////////////////////////
/*
 * Package Operations:
 * -------------------
 * This package contains a single class Version with public functions:
 * - testComponent   : Browse self test
  * Note:
 *  - In this version there are no operations.
 *  - This component will not be used for anticipated later versions.
 *  - It is here as a placeholder for variations that may need a browsing
 *    policy, e.g., based on credentials.
* 
 * Required Files:
 * ---------------
 * - Browse.cs
 * - IPluggable     - Repository interfaces and shared data
 * - TestUtilities  - Helper class that is used mostly for testing
 * 
 * Maintenance History:
 * --------------------
 * Ver 1.1 : 15 Jul 2017
 * - modified prologue comments
 * Ver 1.0 : 11 June 2017
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
  public class Browse : IBrowse
  {
    public string componentType { get; } = "Browse";

    public bool testComponent()
    {
      if(ClientEnvironment.verbose)
        TestUtilities.title("testing browse component", '=');

      TestUtilities.putLine("Browse class is intended to support remote operations.");
      TestUtilities.putLine("Nothing to do yet.");
      return false;
    }
  }
  public class TestBrowse
  {
    static void Main(string[] args)
    {
      Browse browse = new Browse();
      browse.testComponent();
    }
  }
}
