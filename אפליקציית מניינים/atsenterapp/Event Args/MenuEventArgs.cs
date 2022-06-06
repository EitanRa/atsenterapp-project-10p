using System;

namespace atsenterapp
{
    public class MenuEventArgs : EventArgs
    {
        public Menu menu;
        public Action[] actions;
        public int layL, layT, layR, layB;
        public MenuEventArgs(Menu menu, Action[] actions, int layL, int layT, int layR, int layB)
        {
            this.menu = menu;
            this.actions = actions;
            this.layL = layL;
            this.layT = layT;
            this.layR = layR;
            this.layB = layB;
        }
    }
}
