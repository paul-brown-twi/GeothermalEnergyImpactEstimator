using System;
using System.IO;
using System.Reflection;

namespace S4CE.Calculations
{
	public class Factors
	{
		public String[] TableHeaderCoefficientName { get; set; }
		public String[] ColumnNames { get; set; }
		public String[] ImpactCategories { get; set; }
		public String[] ImpactCats { get; set; }
		public String[] CategoryUnits { get; set; }
		public String[] CoefficientNames { get; set; }
		public String[] IntermediateTableCoefficientNames { get; set; }
		public double[,] CoefficientMatrix { get; set; }


        public String[] SimplifiedColumnNames { get; set; }
        public double[,] SimplifiedCoefficientsWithSuccess { get; set; }
        public double[,] SimplifiedCoefficientsWithoutSuccess { get; set; }

        public Factors()
		{
		}
		public static Factors FromFile(FileInfo fi)
		{
			Factors f;
			if (fi.Exists)
				f = Newtonsoft.Json.JsonConvert.DeserializeObject<Factors>(File.ReadAllText(fi.FullName));
			else
				f = FromResource();
			return f;
		}
		public static Factors FromResource()
		{
			Assembly asm = Assembly.GetAssembly(typeof(Factors));
			Factors f;

			using (Stream stream = asm.GetManifestResourceStream("S4CE.Calculations.LCAParameters.json"))
			using (StreamReader reader = new StreamReader(stream))
			{
				f = Newtonsoft.Json.JsonConvert.DeserializeObject<Factors>(reader.ReadToEnd());
			}
			return f;
		}
	}


}
