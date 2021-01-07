namespace S4CE.Calculations
{
	public class InputData
	{
		int _pwn, _mwn, _ewn;
		private double _pwd,_mwd,_ewd,_cpm, _cpp;

        public bool SimplifiedModel { get; set; }
        public bool SuccessRate { get; set; }
        public EntryModeEnum EntryMode {get;set;}
		PlantTypeEnum planttype;
		public PlantTypeEnum PlantType
		{
			get
			{
				return planttype;
			}
			set
			{
				if (planttype == value)
				{
					return;
				}

				planttype = value;
				if (planttype == PlantTypeEnum.DEG)
				{
					D = 7000;
                    Ap = 30;
                    OF = 300d;
                    ECO2 = 0d;
                    CH4 = 0d;
                    //Sw = 20000d;
                    //S = 0d;
                }
                else
				{
					//Sw = 0d;
					OF = 0d;
					D = 2000;
                    ECO2 = 0.077d;
                    CH4 = 0d;
                    Ap = 4;
                }
            }
		}
        public double ECO2 { get; set; }
        public double CH4 { get; set; }

        public int MWn
		{
			get { return _mwn; }
			set
			{
				if (_mwn == value)
				{
					return;
				}

				_mwn = value;
				if (EntryMode == EntryModeEnum.Basic)
				{
					_cpm = 500d * _mwn;
				}
			}
		}
		public int PWn
		{
			get { return _pwn; }
			set
			{
				if (_pwn == value)
				{
					return;
				}

				_pwn = value;
				if (EntryMode == EntryModeEnum.Basic)
				{
					_cpp = 500d * _pwn;
				}
			}
		}
		public int EWn {
			get {
				if (_ewn == 0 && EntryMode == EntryModeEnum.Basic)
					return 3;
				return _ewn;
			}
			set { _ewn = value; }
		}

        public double MWd
        {
            get
            {
                return _mwd;
            }
            set
            {
                if (_mwd == value || EntryMode == EntryModeEnum.Basic)
                    return;
                _mwd = value;
            }
        }

        public double PWd
		{
			get { return _pwd; }
			set
			{
				if (_pwd == value)
				{
					return;
				}

				_pwd = value;
				if (EntryMode == EntryModeEnum.Basic)
				{
					_mwd = _pwd;
                    _ewd = _pwd;
				}

			}
		}
        public double EWd
        {
            get
            {
                return _ewd;
            }
            set
            {
                if (_ewd == value || EntryMode == EntryModeEnum.Basic)
                    return;
                _ewd = value;
            }
        }
        public double SRm { get; set; }
        public double SRp { get; set; }
        public double SRe { get; set; }
        public double D { get; set; }

		public double Cs { get; set; }
		public double Cc { get; set; }
		public double DM { get; set; }
        public double DW { get; set; }
        public double CP { get; set; }

        public double Pne { get; set; }
		public double OF { get; set; }
		public double CTn { get; set; }
        public double CTel { get; set; }
        public double GF { get; set; }
        public double SWn { get; set; }
        public double Sw { get; set; }
        public double Sel { get; set; }
        public double LT { get; set; }
		public double CF { get; set; }

		public double Ap {get;set; }
		public double Pnh { get; set; }
		public double PCh { get; set; }
		public double Tenv { get; set; }
		public double Tf { get; set; }
		public double Ce { get; set; }
		public double Ch { get; set; }

		public InputData()
		{
			EWn = 3;
			Cs = 105;
			Cc = 65;
			DM = 0.65d;
			CP = 500d;
			OF = 300;
			CTn = 1d/40d;
            CTel = 864;
            //Sw = 20000;
			LT = 30;
			CF = 90;
			Tenv = 10d;
			Tf = 90d;
		    Ap = 4;
			D = 2000;
			Ce = 0.10475;
			Ch = 0.0181;
            SRm = 72;
            SRp = 77;
            SRe = 67;
            DW = 400.0;
            Sw = 20000;
            Sel = 20;
        }
		public bool ThermalEnergy { get; set; }
		public AllocationStrategyEnum AllocationStrategy { get; set; }
        public bool IncludeIntermediates { get; set; }
        public bool Stimulation { get; set; } = true;
        public double SimplifiedD { get; set; }
        public double SimplifiedECO2 { get; set; }
        public double SimplifiedCH4 { get; set; }
        public double SimplifiedPne { get; set; }
        public double SimplifiedWd { get; set; }
        public bool EnhancedGeothermal { get; set; }
        public bool SimplifiedSuccessRateEnabled { get; set; }

        public int[] SelectedImpactCategories { get; set; }
    }
}