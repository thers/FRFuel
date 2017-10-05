using CitizenFX.Core;

namespace FRFuel
{
    public static class GasStations
    {
        public static Vector3[] positions = {
            new Vector3(49.41872f,   2778.793f,  58.04395f),
            new Vector3(263.8949f,   2606.463f,  44.98339f),
            new Vector3(1039.958f,   2671.134f,  39.55091f),
            new Vector3(1207.26f,    2660.175f,  37.89996f),
            new Vector3(2539.685f,   2594.192f,  37.94488f),
            new Vector3(2679.858f,   3263.946f,  55.24057f),
            new Vector3(2005.055f,   3773.887f,  32.40393f),
            new Vector3(1687.156f,   4929.392f,  42.07809f),
            new Vector3(1701.314f,   6416.028f,  32.76395f),
            new Vector3(179.8573f,   6602.839f,  31.86817f),
            new Vector3(-94.46199f,  6419.594f,  31.48952f),
            new Vector3(-2554.996f,  2334.402f,  33.07803f),
            new Vector3(-1800.375f,  803.6619f,  138.6512f),
            new Vector3(-1437.622f, -276.7476f,  46.20771f),
            new Vector3(-2096.243f, -320.2867f,  13.16857f),
            new Vector3(-724.6192f, -935.1631f,  19.21386f),
            new Vector3(-526.0198f, -1211.003f,  18.18483f),
            new Vector3(-70.21484f, -1761.792f,  29.53402f),
            new Vector3(265.6484f,  -1261.309f,  29.29294f),
            new Vector3(819.6538f,  -1028.846f,  26.40342f),
            new Vector3(1208.951f,  -1402.567f,  35.22419f),
            new Vector3(1181.381f,  -330.8471f,  69.31651f),
            new Vector3(620.8434f,   269.1009f,  103.0895f),
            new Vector3(2581.321f,   362.0393f,  108.4688f),
            new Vector3(1785.363f,   3330.372f,  41.38188f),
            new Vector3(-319.690f,   -1471.610f,  30.030f), /* Innocence Blvd / Alta St [SR-19] */
            new Vector3(174.880f,    -1562.450f,  28.740f), /* Davis Ave / Macdonald St */
            new Vector3(1246.480f, -1485.450f,  34.900f), /* Near El Rancho Blvd [SR-18] / Capital Blvd */
            new Vector3(-66.330f, -2532.570f,  6.140f) /* Near Miriam Turner Overpass */
        };

        public static Vector3[][] pumps =
        {
            // 0
            new Vector3[] {
                new Vector3(49.49921f,  2778.912f,  58.04399f),
            },
            // 1
            new Vector3[]
            {
                new Vector3(263.1732f,  2606.515f,  44.98524f),
                new Vector3(265.0739f,  2606.9f,    44.98524f),
            },
            // 2
            new Vector3[]
            {
                new Vector3(1043.286f,  2668.316f,  39.6953f),
                new Vector3(1035.779f,  2667.884f,  39.59842f),
                new Vector3(1035.363f,  2674.146f,  39.6953f),
                new Vector3(1043.228f,  2674.727f,  39.69259f),
            },
            // 3
            new Vector3[]
            {
                new Vector3(1208.803f,  2659.41f,   38.29295f),
                new Vector3(1209.382f,  2658.55f,   38.29296f),
                new Vector3(1206.164f,  2662.243f,  38.29296f),
            },
            // 4
            new Vector3[]
            {
                new Vector3(2540.046f,  2594.93f,   37.94114f),
            },
            // 5
            new Vector3[]
            {
                new Vector3(2680.892f,  3266.344f,  55.15651f),
                new Vector3(2678.446f,  3262.312f,  55.15682f),
            },
            // 6
            new Vector3[]
            {
                new Vector3(2009.208f,  3776.832f,  32.14758f),
                new Vector3(2006.241f,  3775.01f,   32.15149f),
                new Vector3(2003.921f,  3773.584f,  32.14502f),
                new Vector3(2001.484f,  3772.196f,  32.1467f),
            },
            // 7
            new Vector3[]
            {
                new Vector3(1684.636f,  4931.696f,  41.92953f),
                new Vector3(1690.169f,  4927.816f,  41.91949f),
            },
            // 8
            new Vector3[]
            {
                new Vector3(1701.729f,  6416.423f,  32.9883f),
                new Vector3(1697.702f,  6418.276f,  32.39661f),
                new Vector3(1705.75f,   6414.476f,  32.47131f),
            },
            // 9
            new Vector3[]
            {
                new Vector3(172.1167f,  6603.46f,   31.76759f),
                new Vector3(179.7492f,  6604.962f,  31.75048f),
                new Vector3(187.0439f,  6606.254f,  31.75101f),
            },
            // 10
            new Vector3[]
            {
                new Vector3(-97.03368f, 6416.826f,  31.3868f),
                new Vector3(-91.31594f, 6422.506f,  31.34267f),
            },
            // 11
            new Vector3[]
            {
                new Vector3(-2551.421f, 2327.216f,  33.01744f),
                new Vector3(-2558.018f, 2327.195f,  33.07804f),
                new Vector3(-2558.608f, 2334.411f,  32.96354f),
                new Vector3(-2552.72f,  2334.706f,  32.97265f),
                new Vector3(-2552.41f,  2341.949f,  33.0052f),
                new Vector3(-2558.843f, 2340.989f,  33.01099f),
            },
            // 12
            new Vector3[]
            {
                new Vector3(-1796.294f, 811.6018f,  138.5058f),
                new Vector3(-1790.871f, 806.3741f,  138.2029f),
                new Vector3(-1797.151f, 800.7207f,  138.3891f),
                new Vector3(-1802.28f,  806.3079f,  138.3751f),
                new Vector3(-1808.657f, 799.9904f,  138.427f),
                new Vector3(-1803.637f, 794.5114f,  138.4097f),
            },
            // 13
            new Vector3[]
            {
                new Vector3(-1444.34f,  -274.1886f, 46.11931f),
                new Vector3(-1435.39f,  -284.6255f, 46.12236f),
                new Vector3(-1428.981f, -278.9675f, 46.10809f),
                new Vector3(-1438.003f, -268.3987f, 46.07535f),
            },
            // 14
            new Vector3[]
            {
                new Vector3(-2089.24f,  -327.3728f, 13.02895f),
                new Vector3(-2088.456f, -320.8316f, 12.97422f),
                new Vector3(-2087.033f, -312.7974f, 12.90649f),
                new Vector3(-2095.933f, -311.9274f, 12.90725f),
                new Vector3(-2096.466f, -320.4183f, 13.02885f),
                new Vector3(-2097.336f, -326.3977f, 12.88916f),
                new Vector3(-2105.951f, -325.589f,  12.93521f),
                new Vector3(-2105.103f, -319.0184f, 12.8779f),
                new Vector3(-2104.42f,  -311.009f,  12.93345f),
            },
            // 15
            new Vector3[]
            {
                new Vector3(-715.0432f, -932.5638f, 19.07506f),
                new Vector3(-715.4774f, -939.2256f, 19.35049f),
                new Vector3(-723.86f,   -939.2936f, 18.86283f),
                new Vector3(-723.7555f, -932.4474f, 19.40245f),
                new Vector3(-732.3931f, -932.5628f, 19.41346f),
                new Vector3(-732.47f,   -939.5462f, 18.94506f),
            },
            // 16
            new Vector3[]
            {
                new Vector3(-518.4993f, -1209.443f, 18.07783f),
                new Vector3(-521.2747f, -1208.402f, 18.06198f),
                new Vector3(-526.1282f, -1206.402f, 18.06817f),
                new Vector3(-528.546f,  -1204.938f, 18.08993f),
                new Vector3(-532.3412f, -1212.774f, 18.07594f),
                new Vector3(-529.4606f, -1213.783f, 18.07589f),
                new Vector3(-524.9258f, -1216.442f, 18.03981f),
                new Vector3(-522.1807f, -1217.371f, 18.07601f),
            },
            // 17
            new Vector3[]
            {
                new Vector3(-63.78423f, -1767.807f, 29.58496f),
                new Vector3(-61.21217f, -1760.783f, 29.57397f),
                new Vector3(-69.46559f, -1758.157f, 29.25509f),
                new Vector3(-72.02878f, -1765.13f,  29.23874f),
                new Vector3(-80.31097f, -1762.165f, 29.50828f),
                new Vector3(-77.66983f, -1755.077f, 29.52769f),
            },
            // 18
            new Vector3[]
            {
                new Vector3(273.8892f,  -1268.606f, 29.50896f),
                new Vector3(273.9102f,  -1261.341f, 29.45841f),
                new Vector3(273.9552f,  -1253.555f, 29.00463f),
                new Vector3(265.0881f,  -1253.459f, 29.53489f),
                new Vector3(264.5976f,  -1261.261f, 29.44312f),
                new Vector3(265.1926f,  -1268.503f, 29.06948f),
                new Vector3(256.4616f,  -1268.626f, 29.55151f),
                new Vector3(256.5174f,  -1261.287f, 28.94805f),
                new Vector3(256.4725f,  -1253.449f, 29.55769f),
            },
            // 19
            new Vector3[]
            {
                new Vector3(826.7513f,  -1026.165f, 26.35728f),
                new Vector3(826.7982f,  -1030.967f, 26.42957f),
                new Vector3(819.1411f,  -1030.997f, 26.22982f),
                new Vector3(819.1501f,  -1026.369f, 26.18121f),
                new Vector3(810.8204f,  -1026.367f, 26.15119f),
                new Vector3(810.869f,   -1031.196f, 26.1582f),
            },
            // 20
            new Vector3[]
            {
                new Vector3(1210.227f,  -1407.065f, 35.11445f),
                new Vector3(1213.007f,  -1404.079f, 35.09584f),
                new Vector3(1207.081f,  -1398.296f, 35.15728f),
                new Vector3(1204.209f,  -1401.101f, 35.13186f),
            },
            // 21
            new Vector3[]
            {
                new Vector3(1186.456f,  -338.1484f, 69.52541f),
                new Vector3(1179.055f,  -339.3943f, 69.68567f),
                new Vector3(1177.467f,  -331.1775f, 68.97179f),
                new Vector3(1184.804f,  -329.9716f, 69.48991f),
                new Vector3(1183.224f,  -321.369f,  69.19594f),
                new Vector3(1175.643f,  -322.2696f, 68.98219f),
            },
            // 22
            new Vector3[]
            {
                new Vector3(629.5554f,  263.8569f,  103.0224f),
                new Vector3(629.3791f,  273.9546f,  102.9987f),
                new Vector3(620.7898f,  273.8886f,  102.9988f),
                new Vector3(612.3482f,  274.0847f,  103.0043f),
                new Vector3(612.2713f,  263.8885f,  102.9918f),
                new Vector3(620.9271f,  263.8311f,  103.0251f),
            },
            // 23
            new Vector3[]
            {
                new Vector3(2588.463f,  358.539f,   108.3958f),
                new Vector3(2589.129f,  363.9044f,  108.3995f),
                new Vector3(2581.266f,  364.2455f,  108.3998f),
                new Vector3(2581.088f,  358.8944f,  108.3724f),
                new Vector3(2573.717f,  359.0278f,  108.3615f),
                new Vector3(2573.844f,  364.6972f,  108.3958f),
            },
            // 24
            new Vector3[]
            {
                new Vector3(1785.895f,  3330.168f,  41.34562f),
                new Vector3(1785.145f,  3331.252f,  41.38123f),
            },
            // 25
            new Vector3[]
            {
                new Vector3(-310.370f,  -1472.030f,  30.720f),
                new Vector3(-315.460f,  -1463.270f,  30.720f),
                new Vector3(-321.800f,  -1467.030f,  30.720f),
                new Vector3(-316.680f,  -1475.940f,  30.720f),
                new Vector3(-324.220f,  -1480.170f,  30.720f),
                new Vector3(-329.310f,  -1471.350f,  30.720f),
            },
            // 26
            new Vector3[]
            {
                new Vector3(169.650f,  -1562.680f,  29.320f),
                new Vector3(176.420f,  -1556.280f,  29.320f),
                new Vector3(181.390f,  -1561.560f,  29.320f),
                new Vector3(174.640f,  -1567.690f,  29.320f),
            },
            // 27
            new Vector3[]
            {
                new Vector3(1246.160f,  -1488.150f,  34.900f),
                new Vector3(1246.480f,  -1482.760f,  34.900f),
            },
            // 28
            new Vector3[]
            {
                new Vector3(-64.250f, -2533.900f,  6.140f),
                new Vector3(-68.720f, -2530.710f,  6.140f),
            }
        };
    }
}
