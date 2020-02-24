using System.Text.RegularExpressions;
using System.Windows.Controls;

namespace JlzQualiTool.UserControls
{
    /// <summary>
    /// Interaction logic for Matchup.xaml
    /// </summary>
    public partial class Matchup : UserControl
    {
        public Matchup()
        {
            this.InitializeComponent();
        }

        private void NumbersOnlyTextBox(object sender, System.Windows.Input.TextCompositionEventArgs e)
        {
            e.Handled = new Regex("\\D").IsMatch(e.Text);
        }
    }
}