using S4CE.Calculations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace S4CE.Test
{
    public class SimplifiedModelTests
    {
        InputData input;

        [Fact]
        public void ConventionalGeothermalTest()
        {
            input = new InputData()
            {
                SimplifiedECO2 = 0.0209,
                SimplifiedWd = 2220.0,
                SimplifiedPne = 9.0,
                SimplifiedCH4 = 0.000035

            };
            

            SimplifiedModel model = new SimplifiedModel(input);
            model.Calculate();
            Assert.NotEmpty(model.Results.SimplifiedResultColumns);

            double[,] expected = new double[,]
            {
                { 0.000025133205, 0.000020720296 },
                { 0.026714757406,  0.025950801266},
                //{ 0.025426757406, 0.024662801266 },
                { 0.000000152560, 0.000000130016 },
                { 0.000008140774, 0.000006478008 },
                { 0.000087495216, 0.000069444092 },
                { 0.016778266496, 0.013288945932 },
                { 0.000000000645, 0.000000000530 },
                { 0.000000001790, 0.000000001480 },
                { 0.000114609699, 0.000094633294 },
                { 0.016030603667, 0.014130148921 },
                { 0.000000000367, 0.000000000302 },
                { 0.000000000193, 0.000000000167 },
                { 0.000026007012, 0.000020832334 },
                { 0.043899185426, 0.036862553243 },
                { 0.000000082801, 0.000000074881 },
                { 0.001063041883, 0.000927039296 }
            };

            for (int i = 0; i < expected.GetLength(0); i++)
            {
                for (int j = 0; j < expected.GetLength(1); j++)
                {
                    Assert.Equal(expected[i, j], model.Results.SimplifiedResultColumns[i, j], 8);
                }
            }
        }

        [Fact]
        public void EnhancedGeothermalTest()
        {
            input = new InputData()
            {
                EnhancedGeothermal = true,
                SimplifiedD = 7200.0,
                SimplifiedWd = 2220.0,
                SimplifiedPne = 1.0
                
            };


            SimplifiedModel model = new SimplifiedModel(input);
            model.Calculate();
            Assert.NotEmpty(model.Results.SimplifiedResultColumns);

            double[,] expected = new double[,]
            {
                { 0.001668646784, 0.001290817997 },
                { 0.141680200515, 0.108679301846 },
                { 0.000002700106, 0.000001981321 },
                { 0.000627338541, 0.000491300039 },
                { 0.006855535314, 0.005369915765 },
                { 0.371435801116, 0.266431376745 },
                { 0.000000012653, 0.000000009131 },
                { 0.000000035580, 0.000000025817 },
                { 0.007154806238, 0.005529014484 },
                { 0.277678163431, 0.207618077857 },
                { 0.000000026565, 0.000000020836 },
                { 0.000000004618, 0.000000003483 },
                { 0.002069453253, 0.001601205795 },
                { 1.906826585755, 1.461444175197 },
                { 0.000001327754, 0.000001025895 },
                { 0.017736818337, 0.013150387108 }
            };

            for (int i = 0; i < expected.GetLength(0); i++)
            {
                for (int j = 0; j < expected.GetLength(1); j++)
                {
                    Assert.Equal(expected[i, j], model.Results.SimplifiedResultColumns[i, j], 8);
                }
            }
        }
    }
}
