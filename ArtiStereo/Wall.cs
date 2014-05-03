using System;
using System.Collections.Generic;
using System.Linq;

namespace Earlvik.ArtiStereo
{
    [Serializable]
    public class Wall:Line,IRoomObject
    {
        public MaterialPreset MatPreset;
        /// <summary>
        /// List of predefined material options
        /// </summary>
        public enum MaterialPreset { OakWood, Air, Glass, Granite, Brick, Rubber,OakWoodCarpeted, None}

        private static readonly Dictionary<MaterialPreset, Material> Materials = new Dictionary<MaterialPreset, Material>()
        {
            {MaterialPreset.OakWood, Material.OakWood},
            {MaterialPreset.Air, Material.Air},
            {MaterialPreset.Glass,Material.Glass},
            {MaterialPreset.Granite, Material.Granite},
            {MaterialPreset.Brick, Material.Brick},
            {MaterialPreset.OakWoodCarpeted, Material.OakWoodCarpeted},
            {MaterialPreset.Rubber,Material.Rubber},
            {MaterialPreset.None, Material.OakWood}
        }; 
        public Wall(Point start, Point end, Material material) : base(start, end)
        {
            WallMaterial = material;
            MatPreset = MaterialPreset.None;
        }

        public Wall(double xstart, double ystart, double xend, double yend, Material material)
            : base(xstart, ystart, xend, yend)
        {
            WallMaterial = material;
            MatPreset = MaterialPreset.None;
        }

        public Wall(Point start, Point end, MaterialPreset preset)
            : base(start, end)
        {
            MatPreset = preset;
            WallMaterial = Materials[preset];
        }

        static public Material GetMaterial(MaterialPreset preset)
        {
            Material result;
            if (!Materials.TryGetValue(preset, out result))
            {
                throw new ArgumentException("No such material");
            }
            return result;
        }

        static public MaterialPreset GetPreset(Material material)
        {
            if (Materials.ContainsValue(material))
            {
                return Materials.First(x => x.Value == material).Key;
            }
            return MaterialPreset.None;
        }
        public Wall(double xstart, double ystart, double xend, double yend, MaterialPreset preset) : this(new Point(xstart, ystart), new Point(xend, yend), preset) { }
        public Material WallMaterial { set; get; }
        /// <summary>
        /// Level of sound reduction after rflection from this wall
        /// </summary>
        /// <param name="angle">Angle of sound ray falling</param>
        /// <returns></returns>
        public double ReflectionCoefft(double angle)
        {
           
            double impedancepart = (WallMaterial.Density*WallMaterial.SoundSpeed)/
                                   (Material.Air.Density*Material.Air.SoundSpeed);
            double tangentPart =1 - ((WallMaterial.SoundSpeed/Material.Air.SoundSpeed)*
                                     (WallMaterial.SoundSpeed/Material.Air.SoundSpeed) - 1)*Math.Tan(angle);
            if (tangentPart < 0) return 1;
            tangentPart = Math.Sqrt(tangentPart);
            double result = (impedancepart - tangentPart)/(impedancepart + tangentPart);
            return result;
        }
        [Serializable]
        public class Material
        {
            public static readonly Material Air = new Material(1.20, 340.29);
            public static readonly Material Brick = new Material(1800,3480,0.01,0.02,0.02);
            public static readonly Material Glass = new Material(2500, 5000,0.035,0.037,0.2);
            public static readonly Material Granite = new Material(2800, 3950,0.01,0.02,0.03);
            public static readonly Material OakWood = new Material(720,4000,0.09,0.08,0.1);
            public static readonly Material OakWoodCarpeted = new Material(720,4000,0.01,0.1,0.4);
            public static readonly Material Rubber = new Material(1050, 54);

            Material(double dens, double sspeed, double low = 0, double medium = 0, double high = 0)
            {
                Density = dens;
                SoundSpeed = sspeed;
                Low = low;
                Medium = medium;
                High = high;
            }

            public double Density { set; get; }
            public double High { set; get; }
            public double Low { set; get; }
            public double Medium { set; get; }
            public double SoundSpeed { set; get; }
        }
    }
}