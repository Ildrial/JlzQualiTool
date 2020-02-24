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
            // TODO find more elegant way to achieve that
            string completeText = (sender as TextBox)?.Text + e.Text;
            e.Handled = !(new Regex("[0-9]").IsMatch(e.Text) && completeText.Length <= 2);
        }
    }
}