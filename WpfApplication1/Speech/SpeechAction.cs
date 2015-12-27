using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace WpfApplication1.Speech
{
    public class SpeechAction
    {
        public string Name
        {
            get;
            set;
        }

        public event EventHandler Triggered;

        public IList<SpeechAction> Children
        {
            get;
            set;
        }

        public SpeechAction(string Name)
        {
            this.Name = Name;
        }

        public void TriggerAction()
        {
            if (Triggered != null)
            {
                Triggered(this, new EventArgs());
            }
        }
    }
}
