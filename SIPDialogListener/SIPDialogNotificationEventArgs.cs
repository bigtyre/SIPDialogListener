using System;

namespace BigTyre.Phones
{
    public class SIPDialogNotificationEventArgs : EventArgs
    {
        public SIPDialogNotificationEventArgs(DialogInfo dialogInfo)
        {
            DialogInfo = dialogInfo;
        }

        public DialogInfo DialogInfo { get; }
    }

}
