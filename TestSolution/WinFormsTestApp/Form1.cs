﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace WinFormsTestApp
{
  public partial class Form1 : Form
  {
    public Form1()
    {
      InitializeComponent();
      // pl-PL - polski
      // en-US - angielski
      Languages.Resource.Culture = new System.Globalization.CultureInfo("pl-PL");

      label1.Text = Languages.Resource.TransWorldWelcome;
    }
  }
}
