/**
* Copyright (c)2011 Tracy Platt (te_platt@yahoo.com)
* 
* Dual licensed under the MIT and GPL licenses. 
**/

//Note: You will need to build IceWSS first and add it as a reference.
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using IceWSS;

namespace WSSTest
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        private string[] HandleMessage(string[] messageParameters)
        {
            if (messageParameters[0] == "expectedFunction")
            {
                //do what you wanted for 'expectedFunction'

                string[] returnParams = { "p1", "p2", "p3", "howdy" };
                return returnParams;
            }
            else
            {
                string[] returnParams = { "unHandledFunction" };
                return returnParams;
            }
        }

        private void Start_Click(object sender, EventArgs e)
        {
            int portNum = 1111; //1111 is purely for example. It just needs to match what you setup in your web page.
            WSS.MessageHandler mh = new WSS.MessageHandler(HandleMessage);
            WSS server = new WSS("iceWSTest", portNum, mh);
            server.Start();
            Start.Hide();
        }
    }
}
