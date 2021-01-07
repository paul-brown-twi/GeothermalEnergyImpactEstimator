using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace S4CE.Calculations
{
    public class SimplifiedModel:CalculationModel
    {
        private DataSet _dset = new DataSet();
        public override DataSet ResultSet
        {
            get
            {
                return _dset;
            }
        }

        public SimplifiedModel(InputData data) : base(data) { }
        public SimplifiedModel() : base() { }
        public void Calculate(InputData data)
        {
            _data = data;
            Calculate();
        }


        private double coefficient(string impact, int i, string param)
        {
            return coeffs[impactCats.IndexOf(impact), cols.IndexOf($"{param}{i}")];

        }

        private double alpha(string  impact, int i )
        {
            return coefficient(impact, i, "alpha");
        }
        private double beta(string impact, int i)
        {
            return coefficient(impact, i, "beta");
        }
        private double chi(string impact, int i)
        {
            return coefficient(impact, i, "chi");
        }
        private double delta(string impact, int i)
        {
            return coefficient(impact, i, "delta");
        }

        private double betaEq(double Wd, double CWne, string impact)
        {
            return ((Wd * beta(impact, 1) + beta(impact, 2)) / CWne) + Wd * beta(impact, 3) + beta(impact, 4);
        }

        private double chiEq(double Wd, double Pne, string impact)
        {
            return (chi(impact, 1) / Pne) + chi(impact, 2);
            //return (Wd * chi(impact, 1) + Pne * chi(impact, 2) + chi(impact, 3)) / (Pne * chi(impact, 4) - chi(impact, 5));
        }
        private double deltaEq(double D, double Pne, string impact)
        {
            return (((D * delta(impact, 1) + delta(impact, 2) )) / Pne) + delta(impact, 3);
            //return (D * delta(impact, 1) + Pne * delta(impact, 2) + delta(impact, 3)) / (Pne * delta(impact, 4) - delta(impact, 5));
        }

        List<String> cols = null;
        double[,] coeffs = null;
        List<string> impactCats = null;

        public override void Calculate()
        {
            impactCats = new List<string>();
            for (int i = 0; i < Factors.ImpactCats.Length; i++)
            {
                impactCats.Add(Factors.ImpactCats[i]);
            }
            Results.ImpactElectricity = new double[1, Factors.ImpactCategories.Length];

            cols = Factors.SimplifiedColumnNames.ToList();
            double eco2 = _data.SimplifiedECO2;
                double CH4 = _data.SimplifiedCH4;
            double wd = _data.SimplifiedWd;
            double Pne = _data.SimplifiedPne;
            double d = _data.SimplifiedD;
            

            //conventional geothermal
            _dset = new DataSet();
            DataTable dt = new DataTable("Results");

            dt.Columns.Add(new DataColumn("Impact Category", typeof(string)));
            dt.Columns.Add(new DataColumn("Units", typeof(string)));
            dt.Columns.Add(new DataColumn("With Success", typeof(double)));
            dt.Columns.Add(new DataColumn("Without Success", typeof(double)));

            Results.SimplifiedResultColumns = new double[Factors.ImpactCategories.Length, 2];

            if (_data.SelectedImpactCategories == null)
            {
                _data.SelectedImpactCategories = new int[CalculationModel.Factors.ImpactCategories.Length];
                int i = 0;
                foreach (string impactCat in CalculationModel.Factors.ImpactCategories)
                {
                    _data.SelectedImpactCategories[i] = i++;
                }
            }
            if (!_data.EnhancedGeothermal)
            {
                //for (int i = 0; i < Factors.ImpactCategories.Length; i++)
                foreach (int i in _data.SelectedImpactCategories)
                {
                    
                    DataRow dr = dt.NewRow();
                    dr[0] = String.Format("{0} ({1})", Factors.ImpactCategories[i], Factors.ImpactCats[i]);
                    dr[1] = Factors.CategoryUnits[i];

                    coeffs = Factors.SimplifiedCoefficientsWithSuccess;
                    dr[2] = eco2 * alpha(Factors.ImpactCats[i], 1) + alpha(Factors.ImpactCats[i], 2) + CH4*alpha(Factors.ImpactCats[i], 3) + betaEq(wd, Pne, Factors.ImpactCats[i]); 
                    coeffs = Factors.SimplifiedCoefficientsWithoutSuccess;
                    dr[3] = eco2 * alpha(Factors.ImpactCats[i], 1) + alpha(Factors.ImpactCats[i], 2) + CH4 * alpha(Factors.ImpactCats[i], 3) + betaEq(wd, Pne, Factors.ImpactCats[i]);

                    Results.SimplifiedResultColumns[i, 0] = (double)dr[2];
                    Results.SimplifiedResultColumns[i, 1] = (double)dr[3];
                    dt.Rows.Add(dr);
                }
            }
            else
            {
                //List<String> chi = new List<string>() { "Ef", "ET", "HT-nc", "HT-c", "LU", "PM/RI", "RUm", "WS" };
                foreach (int i in _data.SelectedImpactCategories)// for (int i = 0; i < Factors.ImpactCategories.Length; i++)
                {
                    DataRow dr = dt.NewRow();
                    dr[0] = String.Format("{0} ({1})", Factors.ImpactCategories[i], Factors.ImpactCats[i]);
                    dr[1] = Factors.CategoryUnits[i];
                    coeffs = Factors.SimplifiedCoefficientsWithSuccess;
                    dr[2] = chiEq(wd, Pne, Factors.ImpactCats[i]) + deltaEq(d,Pne,Factors.ImpactCats[i]);
                    coeffs = Factors.SimplifiedCoefficientsWithoutSuccess;
                    dr[3] = chiEq(wd, Pne, Factors.ImpactCats[i]) + deltaEq(d, Pne, Factors.ImpactCats[i]); 

                    Results.SimplifiedResultColumns[i, 0] = (double)dr[2];
                    Results.SimplifiedResultColumns[i, 1] = (double)dr[3];

                    dt.Rows.Add(dr);
                }
            }
            _dset.Tables.Add(dt);

        }

        public override string ForExcel
        {
            get
            {
                StringBuilder sb = new StringBuilder();
                sb.AppendFormat("Results").AppendLine().AppendLine();
                foreach (DataTable dataTable in _dset.Tables)
                {
                    foreach (DataColumn dataColumn in dataTable.Columns)
                    {
                        sb.AppendFormat($"{dataColumn.Caption}\t");
                    }
                    sb.AppendLine();
                    foreach (DataRow dataRow in dataTable.Rows)
                    {
                        foreach (object item in dataRow.ItemArray)
                        {
                            sb.AppendFormat($"{item}\t");
                        }
                        sb.AppendLine();
                    }
                }

                return sb.ToString();
            }
        }
    }
}
