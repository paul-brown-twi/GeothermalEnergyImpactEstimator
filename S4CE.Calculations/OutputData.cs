namespace S4CE.Calculations
{
	public class OutputData
	{
		public double Wn { get; set; }
		public double Wd { get; set; }
		public double CP { get; set; }
		public double WU { get; set; }

		public double[] i4e { get; set; }

		public double Energya { get; set; }
		public double Exergya { get; set; }
		public double Economica { get; set; }
		public double CarnotEfficiency { get; set; }
		public double[,] ImpactElectricity { get; set; }
		public double[,] ImpactHeat { get; set; }

		public double[,,] IntermediateEnergyColumns { get; set; }
		public double[,,] IntermediateThermalColumns { get; set; }

        public double[,] SimplifiedResultColumns { get; set; }
	}
}
