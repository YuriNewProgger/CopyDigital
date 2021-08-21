using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace CopyDigital
{
    class Helper
    {
        public TextBlock FileName { get; set; }
        public ProgressBar Bar { get; set; }
        public TextBlock AmountCopied { get; set; }
        public bool IsBusy { get; set; }
        public string Path { get; set; }

        public Helper() { }

        public Helper(TextBlock fileName, ProgressBar bar, TextBlock amountCopied, bool isBusy)
        {
            FileName = fileName;
            Bar = bar;
            AmountCopied = amountCopied;
            IsBusy = isBusy;
        }
    }
}
