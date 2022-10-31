using S4CE.Calculations;
using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Windows.Forms;
using System.Windows.Forms.DataVisualization.Charting;
using System.Xml;

namespace s4ce
{
    public partial class S4CE : Form
    {
        #region Variables      
        private InputData _data = new InputData();
        private InputData _baseDataForChecksConventional = new InputData();
        private InputData _baseDataForChecksDEG = new InputData();
        private Color GREEN = Color.FromArgb(192, 255, 192);
        private Color ORANGE = Color.FromArgb(255, 224, 192);
        private Color YELLOW = Color.FromArgb(255, 255, 192);
        private CalculationModel _model;

        private ErrorState _errorState;
        private Control[] basicctl;
        private Control[] advancedctl;
        private Control[] yellowctl;
        private Control[] simplifiedctl;
        private int _currentTab = 0;

        private bool _selectAll = true;
        #endregion

        #region constructors...

        public S4CE()
        {
            _errorState = new ErrorState();
            _baseDataForChecksConventional = new InputData()
            {
                D = 2000,  //Conventional 2, DEG 7
                Cs = 100,
                Cc = 100,
                DM = 1,
                OF = 0, //300 DEG
                CTn = 0.0231023102310231,
                //SWn = 0, //20000 DEG 
                LT = 30,
                CF = 90,
                Ap = 4, //4 - conventional, 30 DEG
                Tenv = 10,
                Tf = 90,
                Ce = 5.87,
                Ch = 0.708,
                Sw = 20000,
                Sel = 20,
            };
            _baseDataForChecksDEG = new InputData()
            {
                D = 7000,
                Cs = 100,
                Cc = 100,
                DM = 1,
                OF = 300,
                CTn = 0.0231023102310231,
                Sw = 20000,
                Sel = 20,
                LT = 30,
                CF = 90,
                Ap = 30,
                Tenv = 10,
                Tf = 90,
                Ce = 5.87,
                Ch = 0.708
            };
            #region test data
#if DEBUG
            _data = new InputData()
            {
                SimplifiedModel = true,
                SuccessRate = false,
                EntryMode = EntryModeEnum.Advanced,
                PlantType = PlantTypeEnum.Conventional,
                AllocationStrategy = AllocationStrategyEnum.Energy,
                ThermalEnergy = false,
                MWn = 16,
                PWn = 64,
                EWn = 0,
                MWd = 2220,
                PWd = 2220,
                EWd = 0,
                D = 2262.06,
                Cs = 100,
                Cc = 40,
                DM = 1,
                DW = 450,
                CP = 500,
                Pne = 303.3,
                OF = 0,
                CTn = 0.0231023102310231,
                CTel = 864,
                GF = 1050,
                //SWn = 0,
                LT = 30,
                CF = 87,
                Ap = 4,
                Pnh = 133,
                PCh = 0.4016666667,
                Tenv = 10,
                Tf = 90,
                Ce = 5.87,
                Ch = 0.708,
                ECO2 = 20.9 / 1000.0,
            };
#endif

            #endregion
            InitializeComponent();

            advancedctl = new Control[]
            {
                txtEWn, txtEWd, txtMWd, txtDM, txtDW,
                txtCP, txtCP, txtOF, txtCTn, txtCTel, txtSw, txtSel, txtLT, txtCF,
                txtSRm, txtSRp, txtSRe,
                txtTenv, txtTf, txtCe, txtCh, tpAllocationParameters,
                txtECO2, txtCH4,
                cboAllocationStrategy,
                cbxSuccessRate
            };
            yellowctl = new Control[]
            {
                 txtD, txtCS, txtCC, txtAp
            };
            basicctl = new Control[]
            {
                txtMWn, txtPWn, txtPWd,txtPne, txtGF,
                txtPnh, txtPCh, txtSWn,
            };



            simplifiedctl = new Control[]
            {
                txtSimplifiedCH4, txtSimplifiedD, txtSimplifiedECO2, txtSimplifiedPne, txtSimplifiedWd
            };

            Type? versionInfo = Assembly.GetExecutingAssembly().GetType("GitVersionInformation");
            this.Text = String.Format("{0} {1}", GetAssemblyAttribute<AssemblyTitleAttribute>(a => a.Title), versionInfo.GetField("FullSemVer")?.GetValue(null).ToString());//, verThis);


            //Green
            lblSimplifiedInfo.Text = "*User must enter all parameters";
            lblBasic.Text = lblBasic1.Text = lblBasic2.Text = "Must be entered";
            

            //Orange
            lblAdvanced.Text = lblAdvanced1.Text = lblAdvanced2.Text = "Default values provided. \r\n(May not be inserted by user.)";

            //Red
            lblRecommended.Text = lblRecommended1.Text = lblRecommended2.Text = "Recommended user enters. \r\n(Default values are very uncertain.)";

            clbImpactsToBeConsidered.Items.Clear();
            foreach (string impactCat in CalculationModel.Factors.ImpactCategories)
            {
                clbImpactsToBeConsidered.Items.Add(impactCat, true);
            }

        }

        #endregion

        #region Form Events

        private void S4CE_Load(object sender, EventArgs e)
        {
            SuspendLayout();
            cboModelType.SelectedIndex = _data.SimplifiedModel ? 0 : 1;
            if (!_data.SimplifiedModel)
                cboLevel.SelectedIndex = (int)_data.EntryMode;
            rbBackgroundColours.Select();
            SetScreenData();
            this.AutoScroll = true;
            tcDataEntry.Size = new Size(0, 0);
            tableLayoutPanel1.Size = new Size(0, 0);
            ResumeLayout();
        }

        private void text_Validated(object sender, EventArgs e)
        {
            SuspendLayout();
            GetScreenData();
            SetScreenData();
            ValidateFields((Control)sender);
            ResumeLayout();
        }

        #endregion

        #region Validation

        void ValidateAll()
        {
            errorProvider1.Clear();
            _errorState.Clear();
            ValidateFields(tcDataEntry);
        }

        private String GetError(Control ctl)
        {
            Control[] errCtl = this.Controls.Find(ctl.Name.Replace("txt", "lbl"), true);
            String nstr = errCtl is null || errCtl.Length == 0 ? ctl.Name.Replace("txt", "") : errCtl[0].Text;
            return nstr;

        }

        void RequiredEntryCanBeZero(Control ctl)
        {
            if (ctl is TextBox && ctl.Enabled)
            {
                errorProvider1.SetError(ctl, "");
                if (GetDouble(ctl as TextBox) <= 0)
                {
                    errorProvider1.SetError(ctl, "Should be entered (0 is possible but please verify)");
                    _errorState.AddWarning(ctl, String.Format("{0} should be entered (0 is possible but please verify)", GetError(ctl)));
                }
            }
        }
        void RequiredEntry(Control ctl)
        {
            if (ctl is TextBox && ctl.Enabled)
            {
                errorProvider1.SetError(ctl, "");
                if (GetDouble(ctl as TextBox) <= 0)
                {
                    errorProvider1.SetError(ctl, "Must be entered");
                    _errorState.AddWarning(ctl, String.Format("{0} should be entered (0 is possible but please verify)", GetError(ctl)));
                }
            }
        }

        void Check10xBase(double val, double based, TextBox txt)
        {
            if (!txt.Enabled || based == 0)
                return;
            if (Math.Abs(val) > based * 10d)
            {
                _errorState.AddWarning(txt, String.Format("{1}: Typical value would be {0}, you have exceeded this by >10x, are you really sure ?", based, GetError(txt)));
                errorProvider1.SetError(txt, _errorState.Warnings[txt]);
            }
        }

        void ValidateFields(Control sender)
        {
            InputData check = (_data.PlantType == PlantTypeEnum.DEG) ? _baseDataForChecksDEG : _baseDataForChecksConventional;
            if (sender.HasChildren)
            {
                foreach (Control control in sender.Controls)
                {
                    ValidateFields(control);
                }
            }

            errorProvider1.SetError(sender, "");
            _errorState.Clear(sender);

            switch (sender.Name)
            {
                case "txtD":
                    Check10xBase(_data.D, check.D, txtD);
                    RequiredEntry(sender);
                    break;
                case "txtMWn":
                    RequiredEntryCanBeZero(sender); //Warning if 0.
                    break;
                case "txtSWn":
                    if (cbxStimulation.Checked && cbxStimulation.Enabled)
                    {
                        RequiredEntry(sender);
                        if (sender is TextBox && sender.Enabled)
                        {
                            if (GetDouble(sender as TextBox) >= _data.MWn + _data.PWn)
                            {
                                errorProvider1.SetError(sender, "SWn should be < MWn+PWn");
                                _errorState.AddWarning(sender, String.Format("{0} Should be between 0 and {1}", GetError(sender), _data.MWn + _data.PWn));
                            }
                        }
                    }
                    break;

                case "txtPNn":
                case "txtPWd":
                    RequiredEntry(sender);
                    break;
                case "txtMWd":
                    if (GetDouble(txtMWn) > 0)
                        RequiredEntry(sender);
                    break;
                case "txtEWn":
                case "txtEWd":
                    break;
                case "txtCS":
                    Check10xBase(_data.Cs, check.Cs, txtCS);
                    RequiredEntry(sender);
                    break;
                case "txtCC":
                    Check10xBase(_data.Cc, check.Cc, txtCC);
                    RequiredEntry(sender);
                    break;
                case "txtDM":
                    Check10xBase(_data.DM, check.DM, txtDM);
                    RequiredEntry(sender);
                    break;
                case "txtDW":
                    Check10xBase(_data.DW, check.DW, txtDW);
                    RequiredEntry(sender);
                    break;
                case "txtCP":
                    RequiredEntry(sender);
                    break;
                case "txtPne":
                    RequiredEntry(sender);
                    break;
                case "txtOF":
                    if ((PlantTypeEnum)cboPlantType.SelectedIndex == PlantTypeEnum.DEG)
                        RequiredEntry(sender);
                    break;
                case "txtCTn":
                    RequiredEntryCanBeZero(sender); //Warning if 0.
                    break;
                case "txtCTel":
                    RequiredEntry(sender); //Warning if 0.
                    break;
                //case "txtGF":
                    //if ((PlantTypeEnum)cboPlantType.SelectedIndex != PlantTypeEnum.DEG)
                    //    RequiredEntry(sender);
                    //break;
                case "txtS":
                    if ((PlantTypeEnum)cboPlantType.SelectedIndex == PlantTypeEnum.DEG && cbxStimulation.Checked)
                        RequiredEntryCanBeZero(sender);
                    break;
                case "txtLT":
                    RequiredEntry(sender);
                    break;
                case "txtCF":
                    RequiredEntry(sender);
                    break;
                case "txtAP":
                    RequiredEntry(sender);
                    break;
                case "txtSRm":
                case "txtSRp":
                case "txtSRe":
                    RequiredEntry(sender);
                    break;
                case "txtPCh":
                case "txtPnh":
                    if (cbxThermalEnergyProduction.Checked)
                    {
                        RequiredEntry(sender);
                    }
                    break;
                case "txtTenv":
                case "txtTf":
                case "txtCe":
                case "txtCh":
                    if ((AllocationStrategyEnum)cboAllocationStrategy.SelectedIndex == AllocationStrategyEnum.Exergy)
                        RequiredEntry(sender);
                    if ((AllocationStrategyEnum)cboAllocationStrategy.SelectedIndex == AllocationStrategyEnum.Economic)
                        RequiredEntry(sender);

                    SetAllocationStrategyUI();
                    break;
            }
        }

        #endregion

        #region Helpers
        private static string GetAssemblyAttribute<T>(Func<T, string> value) where T : Attribute
        {
            T attribute = (T)Attribute.GetCustomAttribute(Assembly.GetExecutingAssembly(), typeof(T));
            return value.Invoke(attribute);
        }

        int GetInt(TextBox txt)
        {
            int output = 0;
            Int32.TryParse(txt.Text, out output);
            return output;
        }

        double GetDouble(TextBox txt)
        {
            double output = 0;
            double.TryParse(txt.Text, out output);
            return output;
        }

        void ChangeSettings(Control ctl)
        {
            if (ctl.HasChildren)
            {
                foreach (Control c in ctl.Controls)
                {
                    ChangeSettings(c);
                }
            }
            if (ctl is TextBox)
                ctl.BackColor = SystemColors.Window;
        }
        private void LevelsControlVisibility()
        {
            foreach (Control control in advancedctl)
            {
                control.Enabled = (cboLevel.SelectedIndex == (int)EntryModeEnum.Advanced && !_data.SimplifiedModel);
            }
            ChangeControlColours(rbBackgroundColours.Checked);
            txtPnh.Enabled = txtPCh.Enabled = (!_data.SimplifiedModel && cboLevel.SelectedIndex == (int)EntryModeEnum.Advanced && cbxThermalEnergyProduction.Checked);
            //txtSRm.Enabled = txtSRe.Enabled = txtSRp.Enabled = (cboLevel.SelectedIndex == (int)EntryModeEnum.Advanced && !_data.SimplifiedModel && cbxSuccessRate.Checked);
            SetSuccessRateControl(_data.SuccessRate, (cboLevel.SelectedIndex == (int)EntryModeEnum.Advanced && !_data.SimplifiedModel && _data.SuccessRate));
        }

        bool MessageBoxErrors()
        {

            if (_errorState.HasErrors)
            {
                MessageBox.Show(_errorState.ErrorSTR, "Errors", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return true;
            }
            else if (_errorState.HasWarnings)
            {
                if (MessageBox.Show(_errorState.WarningSTR, "Warnings", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes)
                {
                    return true;
                }
            }
            return false;
        }

        #endregion

        #region Control Events

        private void btnCalculate_Click(object sender, EventArgs e)
        {
            if (tcDataEntry.SelectedTab == tpResults)
            {
                tcDataEntry_SelectedIndexChanged(tcDataEntry, EventArgs.Empty);
            }
            else
            {
                tcDataEntry.SelectedTab = tpResults;
            }
        }

        private void btnClear_Click(object sender, EventArgs e)
        {
            SuspendLayout();
            _data = new InputData();
            SetScreenData();
            ResumeLayout();
        }

        private void btnCopyData_Click(object sender, EventArgs e)
        {
            if (_model == null)
                return;
            Clipboard.SetText(_model.ForExcel);
        }

        private void btnMoveTab_Click(object sender, EventArgs e)
        {
            Button btn = sender as Button;
            if (btn == btnNext)
                tcDataEntry.SelectTab(_currentTab + 1);
            else if (btn == btnPrevious)
                tcDataEntry.SelectTab(_currentTab - 1);
            SetScreenData();
        }

        private void cboAllocationStrategy_SelectedIndexChanged(object sender, EventArgs e)
        {
            ComboBox cbo = sender as ComboBox;
            if (cbo == null)
                return;
            if (_data.AllocationStrategy == (AllocationStrategyEnum)cbo.SelectedIndex)
                return;
            _data.AllocationStrategy = (AllocationStrategyEnum)cbo.SelectedIndex;
            SetAllocationStrategyUI();
        }

        private void cboPlantType_SelectedIndexChanged(object sender, EventArgs e)
        {
            ComboBox cbx = sender as ComboBox;
            if (cbx == null || cbx != cboPlantType)
                return;

            SuspendLayout();
            if (_data.SimplifiedModel)
            {
                _data.EnhancedGeothermal = (cbx.SelectedIndex == 2);
                lblSimplifiedPne.Text = _data.EnhancedGeothermal ? "Installed electrical capacity (Pne, MW)" : "Producer capacity (CWn,e MW)";
                txtSimplifiedD.Visible = lblSimplifiedD.Visible = _data.EnhancedGeothermal;
                txtSimplifiedCH4.Visible = lblSimplifiedCH4.Visible = txtSimplifiedWd.Visible = lblSimplifiedWd.Visible = txtSimplifiedECO2.Visible = lblSimplifiedECO2.Visible = !_data.EnhancedGeothermal;
            }
            else
            {
                _data.PlantType = (PlantTypeEnum)cbx.SelectedIndex;
                btnCalculate.Enabled = btnNext.Enabled = btnPrevious.Enabled = tcDataEntry.Enabled = (_data.PlantType != PlantTypeEnum.Unset) || _data.SimplifiedModel;
                txtOF.Enabled = txtSw.Enabled = txtSel.Enabled = (_data.PlantType == PlantTypeEnum.DEG && _data.EntryMode == EntryModeEnum.Advanced);

                if (_data.PlantType == PlantTypeEnum.Conventional)
                {
                    advancedctl = new Control[]
                    {
                        txtEWn, txtEWd, txtMWd, txtDM, txtDW,
                        txtCP, txtCP, txtOF, txtCTn, txtCTel, txtSw, txtSel, txtLT, txtCF,
                        txtSRm, txtSRp, txtSRe,
                        txtTenv, txtTf, txtCe, txtCh, tpAllocationParameters,
                        cboAllocationStrategy,
                        cbxSuccessRate
                    };
                    yellowctl = new Control[]
                    {
                        txtD, txtCS, txtCC, txtAp,
                        txtECO2, txtCH4
                    };

                }
                else if (_data.PlantType == PlantTypeEnum.DEG)
                {
                    advancedctl = new Control[]
                    {
                        txtEWn, txtEWd, txtMWd, txtDM, txtDW,
                        txtCP, txtCP, txtOF, txtCTn, txtCTel, txtSw, txtSel, txtLT, txtCF,
                        txtSRm, txtSRp, txtSRe,
                        txtTenv, txtTf, txtCe, txtCh, tpAllocationParameters,
                        txtECO2,txtCH4,
                        cboAllocationStrategy,
                        cbxSuccessRate
                    };
                    yellowctl = new Control[]
                    {
                        txtD, txtCS, txtCC, txtAp
                    };

                }
                ChangeControlColours(rbBackgroundColours.Checked);

            }
            SetScreenData();
            ResumeLayout();
        }

        private void cbxIntermediates_CheckedChanged(object sender, EventArgs e)
        {

            if (_data.SimplifiedModel)
                return;
            CheckBox cbx = sender as CheckBox;
            if (cbx == null || _data.IncludeIntermediates == cbx.Checked)
                return;
            _data.IncludeIntermediates = cbx.Checked;
            SetScreenResults();
        }


        private void cboModelType_SelectedIndexChanged(object sender, EventArgs e)
        {
            ComboBox cbx = sender as ComboBox;
            _data.SimplifiedModel = (cbx.SelectedIndex == 0);
            LevelsControlVisibility();
            ControlsBasedOnModel();
        }

        private void cbxLevel_SelectedIndexChanged(object sender, EventArgs e)
        {

            ComboBox cbx = sender as ComboBox;
            _data.EntryMode = (EntryModeEnum)cbx.SelectedIndex;
            LevelsControlVisibility();
            ControlsBasedOnModel();
        }

        private void ControlsBasedOnModel()
        {
            cbxThermalEnergyProduction.Visible = cboLevel.Visible = lblEntryMode.Visible = (!_data.SimplifiedModel);
            //lblSimplifiedPlantType.Visible = cboSimplifiedPlantType.Visible = _data.SimplifiedModel;

            //lblPlantType.Visible = cboPlantType.Visible = (!_data.SimplifiedModel);
            tcDataEntry.SuspendLayout();
            tcDataEntry.TabPages.Clear();
            if (_data.SimplifiedModel)
            {
                tcDataEntry.TabPages.AddRange(new TabPage[] { tpSimplified, tpImpacts, tpResults });
            }
            else
            {
                tcDataEntry.TabPages.AddRange(new TabPage[] { tpWells, tpPipelines, tpAllocationParameters, tpImpacts, tpResults });
            }
            btnCalculate.Enabled = btnNext.Enabled = btnPrevious.Enabled = tcDataEntry.Enabled = (_data.PlantType != PlantTypeEnum.Unset) || _data.SimplifiedModel;
            tcDataEntry.ResumeLayout(true);
        }

        private void cbxStimulation_CheckedChanged(object sender, EventArgs e)
        {
            CheckBox cbx = sender as CheckBox;
            _data.Stimulation = cbx.Checked;
            if (cbx == null)
                return;
            lblSw.Visible = txtSw.Visible =
            lblSel.Visible = txtSel.Visible =
            lblSWn.Visible = txtSWn.Visible = (cbxStimulation.Checked && (PlantTypeEnum)cboPlantType.SelectedIndex == PlantTypeEnum.DEG);
        }

        private void cbxThermalEnergyProduction_CheckedChanged(object sender, EventArgs e)
        {
            CheckBox cbx = sender as CheckBox;
            if (cbx == null)
                return;
            _data.ThermalEnergy = cbx.Checked;

            txtPnh.Enabled = txtPCh.Enabled = cbx.Checked;
        }

        private void rbDisplayColours_CheckedChanged(object sender, EventArgs e)
        {
            RadioButton rb = sender as RadioButton;
            if (rb == null || !rb.Checked)
                return;
            SuspendLayout();
            bool colourful = (rb == rbBackgroundColours);
            ChangeControlColours(colourful);
        }

        private void ChangeControlColours(bool colourful)
        {
            ChangeSettings(tcDataEntry);
            lblSimplifiedInfo.Visible = true;
            lblRecommended.Visible = lblAdvanced.Visible = lblBasic.Visible =
                lblRecommended1.Visible = lblAdvanced1.Visible = lblBasic1.Visible =
                lblRecommended2.Visible = lblAdvanced2.Visible = lblBasic2.Visible = colourful;
            if (colourful)
            {
                SetBackgroundTextBoxColour(simplifiedctl, GREEN); // green
                SetBackgroundTextBoxColour(basicctl, GREEN); // green
                SetBackgroundTextBoxColour(advancedctl, ORANGE); //orange
                SetBackgroundTextBoxColour(yellowctl, YELLOW); //yellow
                lblSimplifiedInfo.BackColor = lblBasic.BackColor = lblBasic1.BackColor = lblBasic2.BackColor = GREEN;
                lblAdvanced.BackColor = lblAdvanced1.BackColor = lblAdvanced2.BackColor = ORANGE;
                lblRecommended.BackColor = lblRecommended1.BackColor = lblRecommended2.BackColor = YELLOW;
            }
            else
            {
                SetBackgroundTextBoxColour(basicctl, SystemColors.Window);
                SetBackgroundTextBoxColour(advancedctl, SystemColors.Window);
                SetBackgroundTextBoxColour(yellowctl, SystemColors.Window);
                SetBackgroundTextBoxColour(simplifiedctl, SystemColors.Window);
                lblSimplifiedInfo.BackColor = SystemColors.Window;
            }
            ResumeLayout();
        }

        private void tcDataEntry_SelectedIndexChanged(object sender, EventArgs e)
        {
            TabControl tc = sender as TabControl;
            if (tc == null)
                return;

            btnNext.Enabled = (tc.SelectedIndex != tc.TabPages.Count - 1) && (_data.PlantType != PlantTypeEnum.Unset || _data.SimplifiedModel);
            btnPrevious.Enabled = (tc.SelectedIndex != 0) && (_data.PlantType != PlantTypeEnum.Unset || _data.SimplifiedModel);
            btnCalculate.Enabled = (_data.PlantType != PlantTypeEnum.Unset || _data.SimplifiedModel);
            if (tc.SelectedTab == tpAllocationParameters)
            {
                CalculateAllocation();
            }
            if (tc.SelectedTab == tpResults)
                Calculate();
            _currentTab = tc.SelectedIndex;
        }

        private void tcDataEntry_Selecting(object sender, TabControlCancelEventArgs e)
        {
            TabControl tc = sender as TabControl;
            if (tc == null || tc.TabPages.Count <= 2 || _currentTab == -1)
                return;
            _errorState.Clear();
            if (tc.TabPages.Count > _currentTab)
                ValidateFields(tc.TabPages[_currentTab]);
            e.Cancel = MessageBoxErrors();
        }

        private void tpWells_Validated(object sender, EventArgs e)
        {
            ValidateFields(sender as TabPage);
        }


        #endregion

        private void GetScreenData() //Only green or amber boxes, not orange ones
        {
            _data.PlantType = (PlantTypeEnum)cboPlantType.SelectedIndex;
            _data.EntryMode = (EntryModeEnum)cboLevel.SelectedIndex;
            _data.AllocationStrategy = (AllocationStrategyEnum)cboAllocationStrategy.SelectedIndex;
            _data.ThermalEnergy = cbxThermalEnergyProduction.Checked;
            _data.Stimulation = cbxStimulation.Checked && _data.PlantType == PlantTypeEnum.DEG;
            //if (_data.SimplifiedModel)
            //    _data.EnhancedGeothermal = (cboSimplifiedPlantType.SelectedIndex == 1);
            //else
            _data.EnhancedGeothermal = (cboPlantType.SelectedIndex == 2);
            _data.MWn = GetInt(txtMWn);
            _data.Tf = GetDouble(txtTf);
            _data.Tenv = GetDouble(txtTenv);
            _data.SWn = GetDouble(txtSWn);
            _data.Sw = GetDouble(txtSw);
            _data.Sel = GetDouble(txtSel);
            _data.PWn = GetInt(txtPWn);
            _data.PWd = GetDouble(txtPWd);
            _data.Pnh = GetDouble(txtPnh);
            _data.Pne = GetDouble(txtPne);
            _data.PCh = GetDouble(txtPCh);
            _data.OF = GetDouble(txtOF);
            _data.MWn = GetInt(txtMWn);
            _data.MWd = GetDouble(txtMWd);
            _data.LT = GetDouble(txtLT);
            _data.GF = GetDouble(txtGF);
            _data.EWn = GetInt(txtEWn);
            _data.EWd = GetDouble(txtEWd);
            _data.DM = GetDouble(txtDM);
            _data.DW = GetDouble(txtDW);
            _data.D = GetDouble(txtD);
            _data.CTn = GetDouble(txtCTn);
            _data.CTel = GetDouble(txtCTel);
            _data.Cs = GetDouble(txtCS);
            _data.CP = GetDouble(txtCP);
            _data.Ch = GetDouble(txtCh);
            _data.CF = GetDouble(txtCF);
            _data.Ce = GetDouble(txtCe);
            _data.Cc = GetDouble(txtCC);
            _data.Ap = GetDouble(txtAp);
            _data.ECO2 = GetDouble(txtECO2);
            _data.CH4 = GetDouble(txtCH4);
            if (_data.EntryMode == EntryModeEnum.Advanced && cbxStimulation.Checked)
            {
                _data.SRm = GetDouble(txtSRm);
                _data.SRp = GetDouble(txtSRp);
                _data.SRe = GetDouble(txtSRe);
            }

            _data.SimplifiedD = GetDouble(txtSimplifiedD);
            _data.SimplifiedECO2 = GetDouble(txtSimplifiedECO2);
            _data.SimplifiedPne = GetDouble(txtSimplifiedPne);
            _data.SimplifiedWd = GetDouble(txtSimplifiedWd);
            _data.SimplifiedCH4 = GetDouble(txtSimplifiedCH4);
            _data.SimplifiedSuccessRateEnabled = cbxEnableSuccessRate.Checked;

            List<int> items = new List<int>();
            foreach (int selectedItem in clbImpactsToBeConsidered.CheckedIndices)
            {
                 items.Add(selectedItem);
            }
            _data.SelectedImpactCategories = items.ToArray();
        }
        private void SetScreenData()
        {
            if (_data.SimplifiedModel)
                cboPlantType.SelectedIndex = _data.EnhancedGeothermal ? 2 : 1;
            else
                cboPlantType.SelectedIndex = (int)_data.PlantType;
            cboLevel.SelectedIndex = (int)_data.EntryMode;
            lblEntryMode.Visible = cboLevel.Visible = !_data.SimplifiedModel;
            cboAllocationStrategy.SelectedIndex = (int)_data.AllocationStrategy;
            cbxThermalEnergyProduction.Checked = _data.ThermalEnergy;

            //cboSimplifiedPlantType.SelectedIndex = _data.EnhancedGeothermal?1:0;
            txtMWn.Text = _data.MWn.ToString();
            txtTf.Text = _data.Tf.ToString();
            txtTenv.Text = _data.Tenv.ToString();
            txtSWn.Text = _data.SWn.ToString();
            txtSw.Text = _data.Sw.ToString();
            txtSel.Text = _data.Sel.ToString();
            txtPWn.Text = _data.PWn.ToString();
            txtPWd.Text = _data.PWd.ToString();
            txtPnh.Text = _data.Pnh.ToString();
            txtPne.Text = _data.Pne.ToString();
            txtPCh.Text = _data.PCh.ToString();
            txtOF.Text = _data.OF.ToString();
            txtMWn.Text = _data.MWn.ToString();
            txtMWd.Text = _data.MWd.ToString();
            txtLT.Text = _data.LT.ToString();
            //txtGF.Text = _data.GF.ToString();
            txtEWn.Text = _data.EWn.ToString();
            txtEWd.Text = _data.EWd.ToString();
            txtDM.Text = _data.DM.ToString();
            txtDW.Text = _data.DW.ToString();
            txtD.Text = _data.D.ToString();
            txtCTn.Text = _data.CTn.ToString();
            txtCTel.Text = _data.CTel.ToString();
            txtCS.Text = _data.Cs.ToString();
            txtCP.Text = _data.CP.ToString();
            txtCh.Text = _data.Ch.ToString();
            txtCF.Text = _data.CF.ToString();
            txtCe.Text = _data.Ce.ToString();
            txtCC.Text = _data.Cc.ToString();
            txtAp.Text = _data.Ap.ToString();
            SetAllocationStrategyUI();
            btnNext.Enabled = (tcDataEntry.SelectedIndex != tcDataEntry.TabPages.Count - 1) && (_data.PlantType != PlantTypeEnum.Unset || _data.SimplifiedModel);
            btnPrevious.Enabled = (tcDataEntry.SelectedIndex != 0) && (_data.PlantType != PlantTypeEnum.Unset || _data.SimplifiedModel);
            btnCalculate.Enabled = (_data.PlantType != PlantTypeEnum.Unset) || _data.SimplifiedModel;

            txtMWd.Enabled = (txtMWn.Enabled && _data.EntryMode == EntryModeEnum.Advanced);
            txtEWd.Enabled = (txtEWn.Enabled && _data.EntryMode == EntryModeEnum.Advanced);
            //txtGF.Enabled = (_data.PlantType == PlantTypeEnum.DF || _data.PlantType == PlantTypeEnum.SF);
            txtGF.Enabled = (_data.PlantType == PlantTypeEnum.Conventional);

            cbxStimulation.Enabled = (_data.PlantType == PlantTypeEnum.DEG);
            lblSw.Visible = txtSw.Visible =
            lblSel.Visible = txtSel.Visible =
            lblSWn.Visible = txtSWn.Visible = cbxStimulation.Checked && cbxStimulation.Enabled;

            txtECO2.Text = _data.ECO2.ToString();
            txtCH4.Text = _data.CH4.ToString();
            cbxSuccessRate.Checked =  _data.SuccessRate;

            SetSuccessRateControl(_data.SuccessRate, _data.EntryMode == EntryModeEnum.Advanced && !_data.SimplifiedModel);

            //t
            txtSimplifiedD.Text = _data.SimplifiedD.ToString();
            txtSimplifiedECO2.Text = _data.SimplifiedECO2.ToString();
            txtSimplifiedPne.Text = _data.SimplifiedPne.ToString();
            txtSimplifiedWd.Text = _data.SimplifiedWd.ToString();
            txtSimplifiedCH4.Text = _data.SimplifiedCH4.ToString();
            cbxEnableSuccessRate.Checked = _data.SimplifiedSuccessRateEnabled;



            lblSimplifiedPne.Text = _data.EnhancedGeothermal ? "Installed electrical capacity (MW)" : "Producer capacity (MW)";
            txtSimplifiedD.Visible = lblSimplifiedD.Visible = _data.EnhancedGeothermal;
            txtSimplifiedECO2.Visible = lblSimplifiedECO2.Visible = !_data.EnhancedGeothermal;

        }

        void SetBackgroundTextBoxColour(Control[] control, Color color)
		{
			foreach (Control item in control)
			{
				if (item is TextBox || item is ComboBox)
					item.BackColor = color;
			}
		}

        void CalculateAllocation()
        {
            GetScreenData();
            if (!_data.SimplifiedModel)
            {
                _model = new ParametricModel(_data);
                ((ParametricModel)_model).AllocationFactors();
                SetAllocationStrategyUI();
            }
        }

        void Calculate()
		{
			GetScreenData();
            if (_data.SimplifiedModel)
                _model = new SimplifiedModel(_data);
            else
                _model = new ParametricModel(_data);
            ValidateAll();
            if (MessageBoxErrors())
                return;
            _model.Calculate();
            SetScreenResults();
        }

        private void SetScreenResults()
        {

            dataGridView1.Columns.Clear();
            if (_model == null)
                return;
            dataGridView1.DataSource = _model.ResultSet;
            dataGridView1.DataMember = "Results";
            dataGridView1.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;

            if (_data.SimplifiedModel)
            {
                dataGridView1.Columns["With Success"].Visible = _data.SimplifiedSuccessRateEnabled;
                dataGridView1.Columns["Without Success"].Visible = !_data.SimplifiedSuccessRateEnabled;
                dataGridView1.Columns["With Success"].HeaderText = "Impact";
                dataGridView1.Columns["Without Success"].HeaderText = "Impact";
            }

            if (!_data.SimplifiedModel)
            {
                // limit double result columns to G2
                for (int i = 0; i < _model.ResultSet.Tables["Results"].Columns.Count; i++)
                {
                    if (dataGridView1.Columns.Count >= i)
                    {
                        dataGridView1.Columns[i].HeaderText = _model.ResultSet.Tables["Results"].Columns[i].Caption;
                    }
                }

            }
            foreach (DataGridViewColumn dataGridViewColumn in dataGridView1.Columns)
            {
                if (dataGridViewColumn.ValueType == typeof(double) && dataGridViewColumn.HeaderText != "Rank")
                {
                    dataGridViewColumn.DefaultCellStyle.Format = "G4";
                }
            }
            cbxIntermediates.Visible = tlpGraphs.Visible = (!_data.SimplifiedModel);
            //Graph
            if (_data.SimplifiedModel)
            {

            }
            if (!_data.SimplifiedModel)
            {
                double[,] resultsToPlot = ((ParametricModel)_model).ElectricalResultsForGraph;

                PlotData(resultsToPlot, chart1);
                resultsToPlot = ((ParametricModel)_model).ThermalResultsForGraph;

                chart2.Visible = (resultsToPlot != null && cbxThermalEnergyProduction.Checked);
                if (resultsToPlot != null)
                {
                    PlotData(resultsToPlot, chart2);
                }

                if (chart2.Visible)
                {
                    tlpGraphs.RowStyles[0].SizeType = SizeType.Percent;
                    tlpGraphs.RowStyles[0].Height = 50F;
                    tlpGraphs.RowStyles[1].SizeType = SizeType.Percent;
                    tlpGraphs.RowStyles[1].Height = 50F;
                }

                else
                {
                    tlpGraphs.RowStyles[0].SizeType = SizeType.Percent;
                    tlpGraphs.RowStyles[0].Height = 100F;
                    tlpGraphs.RowStyles[1].SizeType = SizeType.AutoSize;
                }

            }

        }

        private void PlotData(double[,] resultsToPlot, Chart chart)
        {
            Axis xAxis = null;
            foreach (var chartArea in chart.ChartAreas)
            {
                xAxis = chartArea.AxisX;
            }
            chart.Series.Clear();

            int cnt = resultsToPlot.GetLength(0);
            xAxis.CustomLabels.Clear();
            int ij = _data.SelectedImpactCategories.Length - 1;
            foreach (int i in _data.SelectedImpactCategories)
            {
                xAxis.CustomLabels.Add(new CustomLabel(ij + 0.5, ij + 1.5, CalculationModel.Factors.ImpactCats[i], 0, LabelMarkStyle.None));
                ij--;
            }

            for (int j = 0; j < resultsToPlot.GetLength(1); j++)
            {
                String series = CalculationModel.Factors.IntermediateTableCoefficientNames[j];
                chart.Series.Add(series);
                chart.Series[series].ChartType = SeriesChartType.StackedBar100;
                foreach (int i in _data.SelectedImpactCategories)
                    //for (int i = 0; i < cnt; i++)
                {
                    if (double.IsNaN(resultsToPlot[cnt - i - 1, j]))
                    {
                        MessageBox.Show("Error calculating results, please check inputs", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return;
                    }
                    chart.Series[series].Points.Add(resultsToPlot[cnt - i - 1, j]);
                }
            }
        }

        private void SetAllocationStrategyUI()
        {
            lblEconomica.BackColor = lblEnergya.BackColor = lblExergya.BackColor = Color.Transparent;
            tlpAllocationFactors.Enabled = true;

            foreach (Control control in tlpAllocationFactors.Controls)
            {
                control.Enabled = true;
            }
            _model = new ParametricModel(_data);
            _data.Pne = GetDouble(txtPne);
            _data.Pnh = GetDouble(txtPnh);
            _data.Tenv = GetDouble(txtTenv);
            _data.Tf = GetDouble(txtTf);
            _data.Ap = GetDouble(txtAp);
            _data.Ce = GetDouble(txtCe);
            _data.Ch = GetDouble(txtCh);
            ((ParametricModel)_model).AllocationFactors();

            if (cbxThermalEnergyProduction.Checked)
            {
                lblExergya.Text = _model.Results.Exergya.ToString("G3");
                lblEconomica.Text = _model.Results.Economica.ToString("G3");
                lblEnergya.Text = _model.Results.Energya.ToString("G3");
            }
            else
            {
                lblEnergya.Text = lblEconomica.Text = lblExergya.Text = "1";
            }


            switch (_data.AllocationStrategy)
            {
                case AllocationStrategyEnum.Exergy:
                    lblExergya.BackColor = GREEN;
                    txtTenv.Enabled = txtTf.Enabled = true;
                    break;
                case AllocationStrategyEnum.Economic:
                    lblEconomica.BackColor = GREEN;
                    txtCe.Enabled = txtCh.Enabled = true;
                    break;
                default:
                    lblEnergya.BackColor = GREEN;
                    txtTenv.Enabled = txtTf.Enabled = txtCe.Enabled = txtCh.Enabled = false;
                    break;
            }
        }


        private void cbxSuccessRate_CheckedChanged(object sender, EventArgs e)
        {
            CheckBox cbx = sender as CheckBox;
            if (cbx == null)
                return;
            _data.SuccessRate = cbx.Checked;
            SetSuccessRateControl(_data.SuccessRate, _data.EntryMode == EntryModeEnum.Advanced && !_data.SimplifiedModel);
        }
        private void SetSuccessRateControl(bool ischecked, bool isselectable)
        {
            txtSRm.Enabled = txtSRe.Enabled = txtSRp.Enabled = isselectable && ischecked;
            if (ischecked && isselectable)
            {
                txtSRm.Text = _data.SRm.ToString();
                txtSRp.Text = _data.SRp.ToString();
                txtSRe.Text = _data.SRe.ToString();
            }
            else
            {
                txtSRm.Text = "100";
                txtSRp.Text = "100";
                txtSRe.Text = "100";
            }
        }

        private void btnImpactCategorySelection_Click(object sender, EventArgs e)
        {
            bool check = _selectAll;
            clbImpactsToBeConsidered.SuspendLayout();
            for (int i = 0; i < CalculationModel.Factors.ImpactCategories.Length; i++)
            {
                clbImpactsToBeConsidered.SetItemCheckState(i, check ? CheckState.Checked : CheckState.Unchecked);
            }
            if (_selectAll)
            {
                btnImpactCategorySelection.Text = "Unselect All";
            }
            else
                btnImpactCategorySelection.Text = "Select All";
            clbImpactsToBeConsidered.ResumeLayout();
            _selectAll = !_selectAll;
        }
    }
}

