using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;

namespace S4CE.Calculations
{
    public class ParametricModel:CalculationModel
	{
        public ParametricModel():base(){}
        public ParametricModel(InputData data) : base(data) { }

		void SupportingParameters()
		{
            double srm = _data.SRm;
            double sre = _data.SRe;
            double srp = _data.SRp;
            if (_data.EntryMode == EntryModeEnum.Basic || !_data.SuccessRate)
            {
                srm = 100.0;
                sre = 100.0;
                srp = 100.0;
            }


			Results.Wn = (_data.PWn/(srp/100.0)) + (_data.MWn / (srm / 100.0)) + (_data.EWn * 0.3 / (sre / 100.0));
			Results.Wd = (_data.PWn * _data.PWd / (srp / 100.0)) + (_data.MWn * _data.MWd / (srm / 100.0)) + (_data.EWn * _data.EWd * 0.3 / (sre / 100.0));
			Results.CP = _data.CP* ((_data.PWn / (srp / 100.0)) + (_data.MWn / (srm / 100.0)));

			int[] idx = new int[3];
			//if (_data.PlantType == PlantTypeEnum.SF || _data.PlantType == PlantTypeEnum.DEG)
            if (_data.PlantType == PlantTypeEnum.DEG)
                    idx[0] = Factors.ColumnNames.ToList().IndexOf("i4.1e SF");
			else if (_data.PlantType == PlantTypeEnum.Conventional)
				idx[0] = Factors.ColumnNames.ToList().IndexOf("i4.1e DF");
			idx[1] = Factors.ColumnNames.ToList().IndexOf("i4.2e");
			idx[2] = Factors.ColumnNames.ToList().IndexOf("i4.3e");

			Results.i4e = new double[Factors.ImpactCategories.Length];
			// "i4.1e SF", "i4.1e DF", "i4.2e", "i4.3e"

			for (int i = 0; i < Factors.ImpactCategories.Length; i++)
			{
				Results.i4e[i] = Factors.CoefficientMatrix[i, idx[0]] + (Factors.CoefficientMatrix[i, idx[1]] * _data.CTn) + (Factors.CoefficientMatrix[i, idx[2]] * _data.OF);
			}
			Results.WU = 1d / (4.195 * (_data.Tf - 40d));
		}

		public void AllocationFactors()
		{
            if (_data == null)
                throw new ArgumentException("No input data to process");

            Results.CarnotEfficiency = 1d - ((_data.Tenv + 273.15) / (_data.Tf + 273.15));
            Results.Economica = Results.Exergya = Results.Energya = (_data.Pne ) * (1d - (_data.Ap / 100d));
			Results.Economica *= _data.Ce / 3.6;

            Results.Energya /= (_data.Pne ) * (1d - _data.Ap / 100d) + _data.Pnh;
            Results.Exergya /= (_data.Pne ) * (1d - _data.Ap / 100d) + (_data.Pnh * Results.CarnotEfficiency);
            Results.Economica /= ((_data.Pne / 3.6d) ) * (1d - _data.Ap / 100d) * _data.Ce + (_data.Pnh * _data.Ch);

        }


        public override void Calculate()
        {
			if (_data == null)
				throw new ArgumentException("No input data to process");
			SupportingParameters();
			AllocationFactors();

			List<string> cols = Factors.ColumnNames.ToList();

			double[] a = new double[] { Results.Energya, Results.Exergya, Results.Economica };

            //Denominators:
			double lifetimeE = _data.Pne * _data.CF / 100d * (1 - (_data.Ap / 100d)) * _data.LT * 8760000;
            //lifetimeE -= _data.Pne * _data.CTn * _data.CTel * _data.LT * 1000.0;

            double lifetimeh = _data.Pnh * (_data.CF / 100d) * _data.LT * 31536000;


			Results.ImpactElectricity = new double[3, Factors.ImpactCategories.Length];
			Results.ImpactHeat = new double[3, Factors.ImpactCategories.Length];

			Results.IntermediateEnergyColumns = new double[3, Factors.ImpactCategories.Length, 7];
			Results.IntermediateThermalColumns = new double[3, Factors.ImpactCategories.Length, 7];
			for (int ai = 0; ai < 3; ai++)
			{

				for (int j = 0; j < Factors.ImpactCategories.Length; j++)
				{
					double well = Results.Wn * Factors.CoefficientMatrix[j, cols.IndexOf("i1")] +
						 Results.Wd * (
                             _data.D * Factors.CoefficientMatrix[j, cols.IndexOf("i2.1")] +
						    _data.Cs * Factors.CoefficientMatrix[j, cols.IndexOf("i2.2")] +
						    _data.Cc  * Factors.CoefficientMatrix[j, cols.IndexOf("i2.3")] +
						    _data.DM * Factors.CoefficientMatrix[j, cols.IndexOf("i2.4")] +
						    _data.DW * Factors.CoefficientMatrix[j, cols.IndexOf("i2.5")] +
                            Factors.CoefficientMatrix[j, cols.IndexOf("i2.6")] 
                         );
					well *= a[ai];
					double collPipeline = Results.CP * Factors.CoefficientMatrix[j, cols.IndexOf("i3")] * a[ai];

					double powerPlant = _data.Pne * Results.i4e[j];

                    //double directEmissions = _data.GF * _data.LT * 31536000 * Factors.CoefficientMatrix[j, cols.IndexOf("i5")] * a[ai];
                    double directEmissions = _data.ECO2 * Factors.CoefficientMatrix[j, cols.IndexOf("i6.1")] + _data.CH4 * Factors.CoefficientMatrix[j, cols.IndexOf("i6.2")] ;// * a[ai];
                    double _S = _data.Stimulation ? _data.SWn*_data.Sw : 0d;

                    double stim = _S * (Factors.CoefficientMatrix[j, cols.IndexOf("i5.1")]  + _data.Sel*3.6* Factors.CoefficientMatrix[j, cols.IndexOf("i5.2")]) * a[ai];
                    
					Results.IntermediateEnergyColumns[ai, j, 0] = well / lifetimeE;
					Results.IntermediateEnergyColumns[ai, j, 1] = collPipeline / lifetimeE;
					Results.IntermediateEnergyColumns[ai, j, 2] = powerPlant / lifetimeE;
					Results.IntermediateEnergyColumns[ai, j, 3] = directEmissions/ lifetimeE;
                    Results.IntermediateEnergyColumns[ai, j, 4] = stim / lifetimeE;

					Results.ImpactElectricity[ai, j] = ((well + collPipeline + powerPlant + stim) / lifetimeE) + directEmissions;

					well *= (1d - a[ai]) / a[ai];
					collPipeline *= (1d - a[ai]) / a[ai];
					powerPlant = _data.Pnh * Factors.CoefficientMatrix[j, cols.IndexOf("i4h")];
					directEmissions *= (1d - a[ai]) / a[ai];
					stim *= (1d - a[ai]) / a[ai];

                    if (_data.ThermalEnergy)
                    {
                        double water = 0d;//Results.WU * _data.Pnh * _data.LT * 31536000 * Factors.CoefficientMatrix[j, cols.IndexOf("i7")];
                        double heating = _data.PCh * _data.LT * 31536000 * Results.ImpactElectricity[ai, j];

                        Results.IntermediateThermalColumns[ai, j, 0] = well / lifetimeh;
                        Results.IntermediateThermalColumns[ai, j, 1] = collPipeline / lifetimeh;
                        Results.IntermediateThermalColumns[ai, j, 2] = powerPlant / lifetimeh;
                        Results.IntermediateThermalColumns[ai, j, 3] = 0;// directEmissions lifetimeh;
                        Results.IntermediateThermalColumns[ai, j, 4] = stim / lifetimeh;

                        Results.IntermediateThermalColumns[ai, j, 5] = water/ lifetimeh;
                        Results.IntermediateThermalColumns[ai, j, 6] = heating / lifetimeh;

                        Results.ImpactHeat[ai, j] = ((well + collPipeline + powerPlant + stim + water + heating) / lifetimeh); //+ water + heating;// + directEmissions;
                    }
				}
			}
		}

		public override String ForExcel
		{
			get
			{
				StringBuilder sb = new StringBuilder();
				sb.AppendFormat("Results").AppendLine().AppendLine() ;
				sb.AppendFormat("Electricity (impact per kWh)").AppendLine().AppendLine().AppendFormat("\t\t");

				for (int i = 0; i < Factors.IntermediateTableCoefficientNames.Length; i++)
				{
					sb.AppendFormat("{0}\t", Factors.IntermediateTableCoefficientNames[i]);
				}
				sb.Append("Total").AppendLine();

				//for (int i = 0; i < Factors.ImpactCategories.Length; i++)
                if (_data.SelectedImpactCategories == null)
                {
                    _data.SelectedImpactCategories = new int[CalculationModel.Factors.ImpactCategories.Length];
                    int i = 0;
                    foreach (string impactCat in CalculationModel.Factors.ImpactCategories)
                    {
                        _data.SelectedImpactCategories[i] = i++;
                    }
                }
                foreach (int i in _data.SelectedImpactCategories)
                {
					sb.AppendFormat("{0}\t{1}\t", Factors.ImpactCategories[i], Factors.CategoryUnits[i]);
					for (int j = 0; j < Results.IntermediateEnergyColumns.GetLength(2); j++)
					{
						sb.AppendFormat("{0}\t", Results.IntermediateEnergyColumns[(int)_data.AllocationStrategy, i, j]);
					}
					sb.AppendLine(Results.ImpactElectricity[(int)_data.AllocationStrategy, i].ToString());
				}

                if (_data.ThermalEnergy)
                {
                    sb.AppendLine().AppendLine();
                    sb.AppendFormat("Thermal energy (impact per MJ)").AppendLine().AppendLine().AppendFormat("\t\t"); ;

                    for (int i = 0; i < Factors.IntermediateTableCoefficientNames.Length; i++)
                    {
                        sb.AppendFormat("{0}\t", Factors.IntermediateTableCoefficientNames[i]);
                    }
                    sb.Append("Total").AppendLine();

                    foreach (int i in _data.SelectedImpactCategories)
                    {
                        sb.AppendFormat("{0}\t{1}\t", Factors.ImpactCategories[i], Factors.CategoryUnits[i]);
                        for (int j = 0; j < Results.IntermediateThermalColumns.GetLength(2); j++)
                        {
                            sb.AppendFormat("{0}\t", Results.IntermediateThermalColumns[(int)_data.AllocationStrategy, i, j]);
                        }
                        sb.AppendLine(Results.ImpactHeat[(int)_data.AllocationStrategy, i].ToString());
                    }
                }      

                return sb.ToString();
			}
		}

		public double [,] ElectricalResultsForGraph
		{
			get
			{
				int col = (int)_data.AllocationStrategy;
				double[,] results = new double[Results.IntermediateEnergyColumns.GetLength(1), Results.IntermediateEnergyColumns.GetLength(2)];

				for (int i = 0; i < Results.IntermediateEnergyColumns.GetLength(1); i++)
				{
					for (int j = 0; j < Results.IntermediateEnergyColumns.GetLength(2); j++)
					{
						results[i, j] = Results.IntermediateEnergyColumns[col, i, j];
					}
				}
				return results;
			}
		}

        public double[,] ThermalResultsForGraph
        {
            get
            {
                if (Results.IntermediateThermalColumns == null || Results.IntermediateThermalColumns.GetLength(1) < 1 || Results.IntermediateThermalColumns.GetLength(2) < 1)
                    return null;
                int col = (int)_data.AllocationStrategy;
                double[,] results = new double[Results.IntermediateThermalColumns.GetLength(1), Results.IntermediateThermalColumns.GetLength(2)];

                for (int i = 0; i < Results.IntermediateThermalColumns.GetLength(1); i++)
                {
                    for (int j = 0; j < Results.IntermediateThermalColumns.GetLength(2); j++)
                    {
                        results[i, j] = Results.IntermediateThermalColumns[col, i, j];
                    }

                }
                return results;
            }
        }



		public override DataSet ResultSet
		{
			get
			{
				DataSet ds = new DataSet();
				DataTable dt = new DataTable("Results");

				dt.Columns.Add(new DataColumn("Impact Category", typeof(string)));
				dt.Columns.Add(new DataColumn("Units", typeof(string)));

                if (_data.AllocationStrategy == AllocationStrategyEnum.Energy)
                {
                    if (_data.ThermalEnergy)
                    {
                        dt.Columns.Add(new DataColumn("Thermal energy (impact per MJ)", typeof(double)));
                        dt.Columns.Add(new DataColumn("Electricity (impact per kWh)", typeof(double)));
                    }
                    else
                    {
                        dt.Columns.Add(new DataColumn("Electricity (impact per kWh)", typeof(double)));
                    }                    
                }
                else if (_data.AllocationStrategy == AllocationStrategyEnum.Exergy)
                {
                    if (_data.ThermalEnergy)
                    {
                        dt.Columns.Add(new DataColumn("Thermal [Exergy]", typeof(double)));
                    }
                    else
                    {
                        dt.Columns.Add(new DataColumn("Electricity [Exergy]", typeof(double)));

                    }
                }
				else if (_data.AllocationStrategy == AllocationStrategyEnum.Economic)
				{
                    if (_data.ThermalEnergy)
                    {
                        dt.Columns.Add(new DataColumn("Thermal [Economy]", typeof(double)));
                    }
                    else
                    {
                        dt.Columns.Add(new DataColumn("Electricity [Economy]", typeof(double)));
                    }
                        
				}

                if (_data.IncludeIntermediates)
                {
                    for (int i = 0; i < Factors.IntermediateTableCoefficientNames.Length; i++)
                    {
                        dt.Columns.Add(
                            new DataColumn(Factors.IntermediateTableCoefficientNames[i], typeof(double))
                            {
                                Caption = String.Format("{0}...", Factors.IntermediateTableCoefficientNames[i].Substring(0, 5))
                            });
                    }
                }
                foreach (int i in _data.SelectedImpactCategories)
                {
					DataRow dr = dt.NewRow();
                    int col = 0;
                    dr[col++] = String.Format("{0} ({1})",Factors.ImpactCategories[i], Factors.ImpactCats[i]) ;
					dr[col++] = Factors.CategoryUnits[i];

                    if (_data.ThermalEnergy)
                    {
                        dr[col++] = Results.ImpactHeat[(int)_data.AllocationStrategy, i];
                    }
                    if (!_data.ThermalEnergy || (_data.ThermalEnergy && _data.AllocationStrategy == AllocationStrategyEnum.Energy))
                        dr[col++] = Results.ImpactElectricity[(int)_data.AllocationStrategy, i];

                    if (_data.IncludeIntermediates)
                    {
                        if (_data.ThermalEnergy)
                        {
                            for (int k = 0; k < Factors.IntermediateTableCoefficientNames.Length; k++)
                            {
                                dr[col++] = Results.IntermediateThermalColumns[(int)_data.AllocationStrategy, i, k];
                            }
                        }
                        else
                        {
                            for (int k = 0; k < Factors.IntermediateTableCoefficientNames.Length; k++)
                            {
                                dr[col++] = Results.IntermediateEnergyColumns[(int)_data.AllocationStrategy, i, k];
                            }
                        }
                    }
                    dt.Rows.Add(dr);
				}
				ds.Tables.Add(dt);
				return ds;
			}
		}


	}
}

