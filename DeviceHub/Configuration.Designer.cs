namespace Acumatica.DeviceHub
{
    partial class Configuration
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Configuration));
            this.okButton = new System.Windows.Forms.Button();
            this.cancelButton = new System.Windows.Forms.Button();
            this.mainTab = new System.Windows.Forms.TabControl();
            this.tabPage1 = new System.Windows.Forms.TabPage();
            this.label5 = new System.Windows.Forms.Label();
            this.label4 = new System.Windows.Forms.Label();
            this.passwordTextBox = new System.Windows.Forms.TextBox();
            this.loginTextBox = new System.Windows.Forms.TextBox();
            this.label1 = new System.Windows.Forms.Label();
            this.acumaticaUrlTextBox = new System.Windows.Forms.TextBox();
            this.tabPage2 = new System.Windows.Forms.TabPage();
            this.rawModeCheckbox = new System.Windows.Forms.CheckBox();
            this.label9 = new System.Windows.Forms.Label();
            this.label6 = new System.Windows.Forms.Label();
            this.paperSourceCombo = new System.Windows.Forms.ComboBox();
            this.orientationGroupBox = new System.Windows.Forms.GroupBox();
            this.orientationLandscape = new System.Windows.Forms.RadioButton();
            this.orientationPortrait = new System.Windows.Forms.RadioButton();
            this.orientationAutomatic = new System.Windows.Forms.RadioButton();
            this.paperSizeCombo = new System.Windows.Forms.ComboBox();
            this.label3 = new System.Windows.Forms.Label();
            this.printerCombo = new System.Windows.Forms.ComboBox();
            this.label2 = new System.Windows.Forms.Label();
            this.queueName = new System.Windows.Forms.TextBox();
            this.removePrintQueue = new System.Windows.Forms.Button();
            this.addPrintQueue = new System.Windows.Forms.Button();
            this.queueList = new System.Windows.Forms.ListBox();
            this.tabPage3 = new System.Windows.Forms.TabPage();
            this.label8 = new System.Windows.Forms.Label();
            this.acumaticaScaleIDTextBox = new System.Windows.Forms.TextBox();
            this.label7 = new System.Windows.Forms.Label();
            this.scalesDropDown = new System.Windows.Forms.ComboBox();
            this.showAllDevicesCheckBox = new System.Windows.Forms.CheckBox();
            this.mainTab.SuspendLayout();
            this.tabPage1.SuspendLayout();
            this.tabPage2.SuspendLayout();
            this.orientationGroupBox.SuspendLayout();
            this.tabPage3.SuspendLayout();
            this.SuspendLayout();
            // 
            // okButton
            // 
            resources.ApplyResources(this.okButton, "okButton");
            this.okButton.Name = "okButton";
            this.okButton.UseVisualStyleBackColor = true;
            this.okButton.Click += new System.EventHandler(this.okButton_Click);
            // 
            // cancelButton
            // 
            this.cancelButton.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            resources.ApplyResources(this.cancelButton, "cancelButton");
            this.cancelButton.Name = "cancelButton";
            this.cancelButton.UseVisualStyleBackColor = true;
            // 
            // mainTab
            // 
            this.mainTab.Controls.Add(this.tabPage1);
            this.mainTab.Controls.Add(this.tabPage2);
            this.mainTab.Controls.Add(this.tabPage3);
            resources.ApplyResources(this.mainTab, "mainTab");
            this.mainTab.Name = "mainTab";
            this.mainTab.SelectedIndex = 0;
            // 
            // tabPage1
            // 
            this.tabPage1.Controls.Add(this.label5);
            this.tabPage1.Controls.Add(this.label4);
            this.tabPage1.Controls.Add(this.passwordTextBox);
            this.tabPage1.Controls.Add(this.loginTextBox);
            this.tabPage1.Controls.Add(this.label1);
            this.tabPage1.Controls.Add(this.acumaticaUrlTextBox);
            resources.ApplyResources(this.tabPage1, "tabPage1");
            this.tabPage1.Name = "tabPage1";
            this.tabPage1.UseVisualStyleBackColor = true;
            // 
            // label5
            // 
            resources.ApplyResources(this.label5, "label5");
            this.label5.Name = "label5";
            // 
            // label4
            // 
            resources.ApplyResources(this.label4, "label4");
            this.label4.Name = "label4";
            // 
            // passwordTextBox
            // 
            resources.ApplyResources(this.passwordTextBox, "passwordTextBox");
            this.passwordTextBox.Name = "passwordTextBox";
            // 
            // loginTextBox
            // 
            resources.ApplyResources(this.loginTextBox, "loginTextBox");
            this.loginTextBox.Name = "loginTextBox";
            // 
            // label1
            // 
            resources.ApplyResources(this.label1, "label1");
            this.label1.Name = "label1";
            // 
            // acumaticaUrlTextBox
            // 
            resources.ApplyResources(this.acumaticaUrlTextBox, "acumaticaUrlTextBox");
            this.acumaticaUrlTextBox.Name = "acumaticaUrlTextBox";
            // 
            // tabPage2
            // 
            this.tabPage2.Controls.Add(this.rawModeCheckbox);
            this.tabPage2.Controls.Add(this.label9);
            this.tabPage2.Controls.Add(this.label6);
            this.tabPage2.Controls.Add(this.paperSourceCombo);
            this.tabPage2.Controls.Add(this.orientationGroupBox);
            this.tabPage2.Controls.Add(this.paperSizeCombo);
            this.tabPage2.Controls.Add(this.label3);
            this.tabPage2.Controls.Add(this.printerCombo);
            this.tabPage2.Controls.Add(this.label2);
            this.tabPage2.Controls.Add(this.queueName);
            this.tabPage2.Controls.Add(this.removePrintQueue);
            this.tabPage2.Controls.Add(this.addPrintQueue);
            this.tabPage2.Controls.Add(this.queueList);
            resources.ApplyResources(this.tabPage2, "tabPage2");
            this.tabPage2.Name = "tabPage2";
            this.tabPage2.UseVisualStyleBackColor = true;
            // 
            // rawModeCheckbox
            // 
            resources.ApplyResources(this.rawModeCheckbox, "rawModeCheckbox");
            this.rawModeCheckbox.Name = "rawModeCheckbox";
            this.rawModeCheckbox.UseVisualStyleBackColor = true;
            this.rawModeCheckbox.CheckedChanged += new System.EventHandler(this.rawModeCheckbox_CheckedChanged);
            // 
            // label9
            // 
            resources.ApplyResources(this.label9, "label9");
            this.label9.Name = "label9";
            // 
            // label6
            // 
            resources.ApplyResources(this.label6, "label6");
            this.label6.Name = "label6";
            // 
            // paperSourceCombo
            // 
            this.paperSourceCombo.DisplayMember = "SourceName";
            this.paperSourceCombo.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.paperSourceCombo.FormattingEnabled = true;
            resources.ApplyResources(this.paperSourceCombo, "paperSourceCombo");
            this.paperSourceCombo.Name = "paperSourceCombo";
            this.paperSourceCombo.ValueMember = "RawKind";
            this.paperSourceCombo.SelectedIndexChanged += new System.EventHandler(this.paperSourceCombo_SelectedIndexChanged);
            // 
            // orientationGroupBox
            // 
            this.orientationGroupBox.Controls.Add(this.orientationLandscape);
            this.orientationGroupBox.Controls.Add(this.orientationPortrait);
            this.orientationGroupBox.Controls.Add(this.orientationAutomatic);
            resources.ApplyResources(this.orientationGroupBox, "orientationGroupBox");
            this.orientationGroupBox.Name = "orientationGroupBox";
            this.orientationGroupBox.TabStop = false;
            // 
            // orientationLandscape
            // 
            resources.ApplyResources(this.orientationLandscape, "orientationLandscape");
            this.orientationLandscape.Name = "orientationLandscape";
            this.orientationLandscape.TabStop = true;
            this.orientationLandscape.Tag = "2";
            this.orientationLandscape.UseVisualStyleBackColor = true;
            this.orientationLandscape.CheckedChanged += new System.EventHandler(this.orientationLandscape_CheckedChanged);
            // 
            // orientationPortrait
            // 
            resources.ApplyResources(this.orientationPortrait, "orientationPortrait");
            this.orientationPortrait.Name = "orientationPortrait";
            this.orientationPortrait.TabStop = true;
            this.orientationPortrait.Tag = "1";
            this.orientationPortrait.UseVisualStyleBackColor = true;
            this.orientationPortrait.CheckedChanged += new System.EventHandler(this.orientationPortrait_CheckedChanged);
            // 
            // orientationAutomatic
            // 
            resources.ApplyResources(this.orientationAutomatic, "orientationAutomatic");
            this.orientationAutomatic.Name = "orientationAutomatic";
            this.orientationAutomatic.TabStop = true;
            this.orientationAutomatic.Tag = "-1";
            this.orientationAutomatic.UseVisualStyleBackColor = true;
            this.orientationAutomatic.CheckedChanged += new System.EventHandler(this.orientationDefault_CheckedChanged);
            // 
            // paperSizeCombo
            // 
            this.paperSizeCombo.DisplayMember = "PaperName";
            this.paperSizeCombo.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.paperSizeCombo.FormattingEnabled = true;
            resources.ApplyResources(this.paperSizeCombo, "paperSizeCombo");
            this.paperSizeCombo.Name = "paperSizeCombo";
            this.paperSizeCombo.ValueMember = "RawKind";
            this.paperSizeCombo.SelectedIndexChanged += new System.EventHandler(this.paperSizeCombo_SelectedIndexChanged);
            // 
            // label3
            // 
            resources.ApplyResources(this.label3, "label3");
            this.label3.Name = "label3";
            // 
            // printerCombo
            // 
            this.printerCombo.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.printerCombo.FormattingEnabled = true;
            resources.ApplyResources(this.printerCombo, "printerCombo");
            this.printerCombo.Name = "printerCombo";
            this.printerCombo.SelectedIndexChanged += new System.EventHandler(this.printerCombo_SelectedIndexChanged);
            // 
            // label2
            // 
            resources.ApplyResources(this.label2, "label2");
            this.label2.Name = "label2";
            // 
            // queueName
            // 
            resources.ApplyResources(this.queueName, "queueName");
            this.queueName.Name = "queueName";
            this.queueName.TextChanged += new System.EventHandler(this.queueName_TextChanged);
            this.queueName.Validated += new System.EventHandler(this.queueName_Validated);
            // 
            // removePrintQueue
            // 
            resources.ApplyResources(this.removePrintQueue, "removePrintQueue");
            this.removePrintQueue.Name = "removePrintQueue";
            this.removePrintQueue.UseVisualStyleBackColor = true;
            this.removePrintQueue.Click += new System.EventHandler(this.removePrintQueue_Click);
            // 
            // addPrintQueue
            // 
            resources.ApplyResources(this.addPrintQueue, "addPrintQueue");
            this.addPrintQueue.Name = "addPrintQueue";
            this.addPrintQueue.UseVisualStyleBackColor = true;
            this.addPrintQueue.Click += new System.EventHandler(this.addPrintQueue_Click);
            // 
            // queueList
            // 
            this.queueList.DisplayMember = "QueueName";
            this.queueList.FormattingEnabled = true;
            resources.ApplyResources(this.queueList, "queueList");
            this.queueList.Name = "queueList";
            this.queueList.SelectedIndexChanged += new System.EventHandler(this.queueList_SelectedIndexChanged);
            // 
            // tabPage3
            // 
            this.tabPage3.Controls.Add(this.showAllDevicesCheckBox);
            this.tabPage3.Controls.Add(this.label8);
            this.tabPage3.Controls.Add(this.acumaticaScaleIDTextBox);
            this.tabPage3.Controls.Add(this.label7);
            this.tabPage3.Controls.Add(this.scalesDropDown);
            resources.ApplyResources(this.tabPage3, "tabPage3");
            this.tabPage3.Name = "tabPage3";
            this.tabPage3.UseVisualStyleBackColor = true;
            // 
            // label8
            // 
            resources.ApplyResources(this.label8, "label8");
            this.label8.Name = "label8";
            // 
            // acumaticaScaleIDTextBox
            // 
            resources.ApplyResources(this.acumaticaScaleIDTextBox, "acumaticaScaleIDTextBox");
            this.acumaticaScaleIDTextBox.Name = "acumaticaScaleIDTextBox";
            // 
            // label7
            // 
            resources.ApplyResources(this.label7, "label7");
            this.label7.Name = "label7";
            // 
            // scalesDropDown
            // 
            this.scalesDropDown.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.scalesDropDown.FormattingEnabled = true;
            resources.ApplyResources(this.scalesDropDown, "scalesDropDown");
            this.scalesDropDown.Name = "scalesDropDown";
            // 
            // showAllDevicesCheckBox
            // 
            resources.ApplyResources(this.showAllDevicesCheckBox, "showAllDevicesCheckBox");
            this.showAllDevicesCheckBox.Name = "showAllDevicesCheckBox";
            this.showAllDevicesCheckBox.UseVisualStyleBackColor = true;
            this.showAllDevicesCheckBox.CheckedChanged += new System.EventHandler(this.showAllDevicesCheckBox_CheckedChanged);
            // 
            // Configuration
            // 
            resources.ApplyResources(this, "$this");
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.CancelButton = this.cancelButton;
            this.Controls.Add(this.mainTab);
            this.Controls.Add(this.cancelButton);
            this.Controls.Add(this.okButton);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "Configuration";
            this.Load += new System.EventHandler(this.Configuration_Load);
            this.mainTab.ResumeLayout(false);
            this.tabPage1.ResumeLayout(false);
            this.tabPage1.PerformLayout();
            this.tabPage2.ResumeLayout(false);
            this.tabPage2.PerformLayout();
            this.orientationGroupBox.ResumeLayout(false);
            this.orientationGroupBox.PerformLayout();
            this.tabPage3.ResumeLayout(false);
            this.tabPage3.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion
        private System.Windows.Forms.Button okButton;
        private System.Windows.Forms.Button cancelButton;
        private System.Windows.Forms.TabControl mainTab;
        private System.Windows.Forms.TabPage tabPage1;
        private System.Windows.Forms.TabPage tabPage2;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.TextBox passwordTextBox;
        private System.Windows.Forms.TextBox loginTextBox;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.TextBox acumaticaUrlTextBox;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.ComboBox printerCombo;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.TextBox queueName;
        private System.Windows.Forms.Button removePrintQueue;
        private System.Windows.Forms.Button addPrintQueue;
        private System.Windows.Forms.ListBox queueList;
        private System.Windows.Forms.Label label9;
        private System.Windows.Forms.Label label6;
        private System.Windows.Forms.ComboBox paperSourceCombo;
        private System.Windows.Forms.GroupBox orientationGroupBox;
        private System.Windows.Forms.ComboBox paperSizeCombo;
        private System.Windows.Forms.RadioButton orientationLandscape;
        private System.Windows.Forms.RadioButton orientationPortrait;
        private System.Windows.Forms.RadioButton orientationAutomatic;
        private System.Windows.Forms.CheckBox rawModeCheckbox;
        private System.Windows.Forms.TabPage tabPage3;
        private System.Windows.Forms.Label label8;
        private System.Windows.Forms.TextBox acumaticaScaleIDTextBox;
        private System.Windows.Forms.Label label7;
        private System.Windows.Forms.ComboBox scalesDropDown;
        private System.Windows.Forms.CheckBox showAllDevicesCheckBox;
    }
}