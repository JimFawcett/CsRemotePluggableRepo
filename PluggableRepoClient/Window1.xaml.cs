///////////////////////////////////////////////////////////////////////
// Window1.xaml.cs - Client prototype GUI for Pluggable Repository   //
// Ver 1.1                                                           //
// Jim Fawcett, CSE681-OnLine, Summer 2017                           //
///////////////////////////////////////////////////////////////////////
/*  
 *  Purpose:
 *    Prototype for a secondary popup window for the Pluggable Repository Client,
 *    used to display text of source code and corresponding metadata
 *
 *  Required Files:
 *    MainWindow.xaml, MainWindow.xaml.cs - view into repository and checkin/checkout
 *    Window1.xaml, Window1.xaml.cs       - Code and MetaData view for individual packages
 *
 *  Maintenance History:
 *  --------------------
 *  ver 1.1 : 15 Jul 2017
 *  - modified prologue comments
 *  ver 1.0 : 15 Jun 2017
 *  - first release
 */
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace PluggableRepoClient
{
  ///////////////////////////////////////////////////////////////////
  // Window1 class
  // - popup window holding code and metadata text

  public partial class Window1 : Window
  {
    private static double leftOffset = 500.0;
    private static double topOffset = -20.0;

    public Window1()
    {
      InitializeComponent();
      double Left = Application.Current.MainWindow.Left;
      double Top = Application.Current.MainWindow.Top;
      this.Left = Left + leftOffset;
      this.Top = Top + topOffset;
      this.Width = 600.0;
      this.Height = 800.0;
      leftOffset += 20.0;
      topOffset += 20.0;
      if (leftOffset > 700.0)
        leftOffset = 500.0;
      if (topOffset > 180.0)
        topOffset = -20.0;
    }

    public void showCode(string codeText)
    {
      // deprecated - will remove in later version
    }
    private void exitButton_Click(object sender, RoutedEventArgs e)
    {
      this.Close();
    }
  }
}
