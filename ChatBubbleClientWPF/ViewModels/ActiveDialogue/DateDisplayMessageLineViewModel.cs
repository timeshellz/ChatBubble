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

            if (currentTime - date < TimeSpan.FromHours(currentTime.Hour))
                MessageShortTime = "Today";
            else if (currentTime - date < TimeSpan.FromHours(24 + currentTime.Hour))
                MessageShortTime = "Yesterday";
            else if (currentTime - date < TimeSpan.FromDays(14))
                MessageShortTime = (currentTime - date).Days.ToString() + " days ago";
            else if (currentTime - date < TimeSpan.FromDays(30))
                MessageShortTime = ((currentTime - date).Days / 7).ToString() + " weeks ago";
            else MessageShortTime = date.ToShortDateString();
        }
    }
}
