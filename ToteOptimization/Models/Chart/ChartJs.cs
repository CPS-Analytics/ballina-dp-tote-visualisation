namespace ToteOptimization.Models.Chart
{
    public class ChartJs
    {
        public string type { get; set; }
        public int duration { get; set; }
        public string easing { get; set; }
        public bool responsive { get; set; }
        public Title title { get; set; }
        public bool lazy { get; set; }
        public Data data { get; set; }
        public string drops { get; set; }
        public string secsToEmpty { get; set; }
        public string totes { get; set; }
        public Options options { get; set; }
    }
}