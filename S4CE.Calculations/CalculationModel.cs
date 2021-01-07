using System;
using System.Data;
using System.Linq;

namespace S4CE.Calculations
{
    public class CalculationModel
    {
        protected InputData _data;
        public OutputData Results { get; set; }
        private static Factors _factors;
        public static Factors Factors {
            get {
                if (_factors == null)
                    _factors = Factors.FromFile(new System.IO.FileInfo(@"LCAParameters.json"));
                return _factors; 
            }
        }


        public CalculationModel(InputData data) : this()
        {
            _data = data;
        }
        public CalculationModel()
        {
            Results = new OutputData();
            //GetFactors();
        }
        //private void GetFactors()
        //{
        //    Factors = Factors.FromFile(new System.IO.FileInfo(@"LCAParameters.json"));
        //}

        public virtual void Calculate() { }

        public virtual String ForExcel { get; }
        public virtual DataSet ResultSet
        {
            get;
        }
    }
}

