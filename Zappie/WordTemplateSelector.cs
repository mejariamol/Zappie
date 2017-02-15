using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace Zappie
{
    public class WordTemplateSelector : DataTemplateSelector
    {
        public DataTemplate ZappieTemplate { get; set; }
        public DataTemplate UserTemplate { get; set; }

        protected override DataTemplate SelectTemplateCore(object item, DependencyObject container)
        {
            Word word = item as Word;
            if (word.isZappie)
            {
                return ZappieTemplate;
            }
            else
            {
                return UserTemplate;
            }
        }
    }
}
