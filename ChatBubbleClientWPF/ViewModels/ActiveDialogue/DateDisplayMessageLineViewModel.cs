using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChatBubbleClientWPF.ViewModels.ActiveDialogue
{
    class DateDisplayMessageLineViewModel : MessageLineViewModel
    {
        public DateDisplayMessageLineViewModel(DateTime date)
        {
            DateTime currentTime = DateTime.UtcNow;
            MessageDateTime = date;

            if (date.Day == currentTime.Day)
                MessageShortTime = "Today";
            else if (currentTime.Day - date.Day == 1)
                MessageShortTime = "Yesterday";
            else if (currentTime.Day - date.Day < 7)
                MessageShortTime = (currentTime.Day - date.Day).ToString() + " days ago";
            else if (currentTime.Month == date.Month)
                MessageShortTime = (currentTime.Day - date.Day / 7).ToString() + " weeks ago";
            else MessageShortTime = date.ToShortDateString();
        }
    }
}
