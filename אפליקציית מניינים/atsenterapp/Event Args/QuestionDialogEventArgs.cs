using System;

namespace atsenterapp
{
    public class QuestionDialogEventArgs : EventArgs
    {
        public string title, question, ok, cancel;
        public Action okAction;
        public QuestionDialogEventArgs(string titlep, string questionp, string okp, string cancelp, Action okActionp)
        {
            title = titlep;
            question = questionp;
            ok = okp;
            cancel = cancelp;
            okAction = okActionp;
        }
    }
}
