﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace RTLSpectrumAnalyzerGUI
{
    public partial class UserAnalysisForm : Form
    {
        private Form1 mainForm;

        clsResize _form_resize;

        public UserAnalysisForm(Form1 mainForm)
        {
            InitializeComponent();

            _form_resize = new clsResize(this);

            _form_resize.SetFormsInitialSize(new Size(this.Width, this.Height));
            _form_resize.StoreControlsInitialSizes();

            this.mainForm = mainForm;
        }

        private void UserAnalysisForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            mainForm.ResumeAutomatedAnalysis();

            this.Hide();

            e.Cancel = true;
        }

        private void UserAnalysisForm_Load(object sender, EventArgs e)
        {
            WindowState = FormWindowState.Maximized;
        }

        private void StoreAndClearData()
        {            
            mainForm.bufferFramesArray.Flush(mainForm.series1BinData, mainForm.series2BinData, mainForm.series1BinData);

            mainForm.series1BinData.Store();
            mainForm.series2BinData.Store();

            ClearData();
        }

        private void ClearData()
        {
            /*////
            mainForm.currentBufferFramesObject.bufferFrames.Change(BinDataMode.Far, BinDataMode.NotUsed);
            mainForm.currentBufferFramesObject.bufferFrames.Change(BinDataMode.Near, BinDataMode.NotUsed);
            */            
            
            mainForm.ClearSeries1();
            mainForm.ClearSeries2();
        }

        public void AnalyzingCenterFrequency()
        {
            mainForm.ShowUserAnalysisDialog();

            label1.Visible = false;

            textBox1.Visible = false;

            button3.Visible = false;

            button2.Visible = false;

            StoreAndClearData();
        }

        public void AnalyzingLeaderboardFrequency()
        {
            mainForm.ShowUserAnalysisDialog();

            label1.Visible = true;

            textBox1.Visible = true;

            button3.Visible = true;

            button2.Visible = true;

            StoreAndClearData();
        }

        private void button18_Click(object sender, EventArgs e)
        {
            this.mainForm.button5.PerformClick();
        }


        public void ShowTransitionDialog()
        {
            if (Properties.Settings.Default.DontShowInfoBoxes[(int) UserInfoDialogs.TransitionDialog] != 1)
            {
                UserInfoDialog dialog = new UserInfoDialog((int)UserInfoDialogs.TransitionDialog);

                string userTransitionAnalysisText = Utilities.GetResourceText("RTLSpectrumAnalyzerGUI.Resources.UserTransitionAnalysis.txt");

                dialog.SetText(userTransitionAnalysisText);

                dialog.ShowDialog();
            }
        }

        private void button17_Click(object sender, EventArgs e)
        {
            ////if (!Properties.Settings.Default.DontShowInfoBox1)
            ShowTransitionDialog();            

            ////MessageBox.Show("Move away from the detector's antenna for more than 4 seconds\r\nand return to the computer moving the mouse or pressing a key to indicate that you're near.");
            

            this.mainForm.button3.PerformClick();

            button17.Enabled = false;
            button18.Enabled = false;

            button18.Text = "Recording Far";
            button18.Enabled = false;

            button2.Enabled = false;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            ////mainForm.AnalyzeLeaderboardFrequency();

            mainForm.button24.PerformClick();

            Hide();
        }

        private void button3_Click(object sender, EventArgs e)
        {
            textBox1.Text = "1";

            mainForm.currentLeaderboardSignalBeingAnalyzedIndex = -1;
            
            mainForm.AnalyzeLeaderboardFrequency();
        }

        private void button2_Click(object sender, EventArgs e)
        {
            textBox15.Text = "";
            textBox16.Text = "";
            textBox18.Text = "";

            chart5.Series[0].Points.Clear();
            chart8.Series[0].Points.Clear();

            ////mainForm.currentLeaderboardSignalBeingAnalyzedIndex++;

            mainForm.AnalyzeLeaderboardFrequency();
        }

        private void button4_Click(object sender, EventArgs e)
        {
            if (!mainForm.recordingSeries1 && !mainForm.recordingSeries2)
            {
                mainForm.ClearSeries1();
                mainForm.ClearSeries2();
            }
            else
            {
                mainForm.series1BinData.clearFrames = true;
                mainForm.series2BinData.clearFrames = true;
            }

            /*////mainForm.ClearSeries1();
            mainForm.ClearSeries2();
            */
            ////mainForm.bufferFramesArray.Flush(mainForm.series1BinData, mainForm.series2BinData, mainForm.series1BinData);

            ////ClearData();
        }

        private void checkBox5_CheckedChanged(object sender, EventArgs e)
        {
            mainForm.checkBox5.Checked = checkBox5.Checked;

            if (checkBox5.Checked)
            {
                mainForm.checkBox4.Checked = false;
                checkBox6.Checked = false;
            }
        }

        private void checkBox6_CheckedChanged(object sender, EventArgs e)
        {
            mainForm.checkBox6.Checked = checkBox6.Checked;

            if (checkBox6.Checked)
            {
                mainForm.checkBox4.Checked = false;
                checkBox5.Checked = false;
            }
        }

        private void button15_Click(object sender, EventArgs e)
        {
            mainForm.ShowIncreasingStrengthColorIndicator();            
        }

        private void button22_Click(object sender, EventArgs e)
        {
            mainForm.ShowSaveDataDialogAndSaveData();
        }

        private void UserAnalysisForm_Resize(object sender, EventArgs e)
        {
            if (_form_resize != null)
                _form_resize._resize();
        }

        private void chart8_Click(object sender, EventArgs e)
        {

        }

        private void chart8_AxisViewChanged(object sender, System.Windows.Forms.DataVisualization.Charting.ViewEventArgs e)
        {
            Utilities.AutoAdjustChartZoom(chart8, e, "Series2");
        }
    }
}
