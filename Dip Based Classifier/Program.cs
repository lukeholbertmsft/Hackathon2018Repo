using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Kepler
{
    class Dip
    {
        public Dip(double start, double duration, double depth)
        {
            Start = start;
            Duration = duration;
            Depth = depth;
            Planet = 0;
        }
        public Dip(double? start, double? duration, double depth)
        {
            Start = start ?? 0.0;
            Duration = duration ?? 0.0;
            Depth = depth;
            Planet = 0;
        }


        public double Start { get; }
        public double Duration { get; }
        public double Depth { get; }
        public int Planet { get; set; }

        public double Center() { return Start + Duration / 2; }
        public double End() { return Start + Duration; }
    }

    class Planet
    {
        public Planet(double period, double duration, int transits, double depth)
        {
            Period = period;
            Duration = duration;
            Transits = transits;
            Depth = depth;
        }
        public double Period { get; }
        public double Duration { get; }
        public int Transits { get; }
        public double Depth { get; }
    }

    class Program
    {
        static void LoadTBL(string fileName, List<double?> time, List<double?> flux)
        {
            StreamReader f = File.OpenText(fileName);

            string s;
            while ((s = f.ReadLine()) != null)
            {
                if (s.Length == 0 ||
                    s[0] == '\\' ||
                    s[0] == '|')
                    continue;

                s = Regex.Replace(s, "^ *", "");
                s = Regex.Replace(s, " *$", "");
                s = Regex.Replace(s, " +", " ");

                string[] part = s.Split(' ');

                if (part.Length >= 9)
                {
                    if (part[0] == "null")
                        time.Add(null);
                    else
                        time.Add(double.Parse(part[0]));

                    if (part[8] == "null")
                        flux.Add(null);
                    else
                        flux.Add(double.Parse(part[8]));
                }
            }
        }

        static void DumpTBL(string fileName, List<double?> time, List<double?> flux)
        {
            StreamWriter f = File.CreateText(fileName);

            for (int i = 0; i < time.Count; i++)
                f.WriteLine("{0}\t{1}", time[i], flux[i]);
        }

        static double FluxAverage(List<double?> flux, int start, int end)
        {
            double sum = 0.0;
            int count = 0;

            for (int i = start; i < end; i++)
            {
                if (i >= 0 && i < flux.Count && flux[i] != null)
                {
                    sum += flux[i] ?? 0.0;
                    count++;
                }
            }

            return sum / (double) count;
        }

        static void FluxRange(List<double?> flux, int start, int end, out double min, out double max)
        {
            min = double.MaxValue;
            max = double.MinValue;

            for (int i = start; i < end; i++)
            {
                if (i >= 0 && i < flux.Count && flux[i] != null)
                {
                    min = Math.Min(min, flux[i] ?? double.MaxValue);
                    max = Math.Max(max, flux[i] ?? double.MinValue);
                }
            }
        }

        static List<Dip> FindDips(List<double?> time, List<double?> flux)
        {
            int minDipDuration = 3;
            int minDipDepth = 1;

            double[] depths = new double[flux.Count];

            for (int i = 0; i < depths.Length; i++)
                depths[i] = 0.0;

            for (int duration = minDipDuration; duration <= 48; duration++)
            {
                for (int i = 0; i < flux.Count; i++)
                {
                    /*
                    double surroundingAvg =
                        (FluxAverage(flux, i - 2 * duration, i - duration) +
                            FluxAverage(flux, i + 2 * duration, i + 3 * duration)) /
                            2;
                    double dipAvg =
                        FluxAverage(flux, i, i + duration);

                    if (dipAvg <= surroundingAvg * (1.0 - 0.01))
                    {
                        for (int j = 0; j < duration; j++)
                        {
                            depths[i + j] = surroundingAvg - dipAvg;
                        }
                    }
                    */

                    double min, minBefore, minAfter, max, maxBefore, maxAfter;

                    FluxRange(flux, i - 2 * duration, i - duration, out minBefore, out maxBefore);
                    FluxRange(flux, i + 2 * duration, i + 3 * duration, out minAfter, out maxAfter);
                    FluxRange(flux, i, i + duration, out min, out max);

                    if (min != double.MaxValue && max != double.MinValue &&
                        minBefore != double.MaxValue && maxBefore != double.MinValue &&
                        minAfter != double.MaxValue && maxAfter != double.MinValue &&
                        max < minBefore - minDipDepth * (maxBefore - minBefore) &&
                        max < minAfter - minDipDepth * (maxAfter - minAfter))
                    {
                        double baseFlux =
                            (((maxBefore + minBefore) / 2) + ((maxAfter + minAfter) / 2)) / 2;
                        double depth = (baseFlux - min) / baseFlux;
                        for (int j = 0; j < duration; j++)
                        {
                            depths[i + j] = depth;
                        }
                    }
                }
            }

            List<Dip> dips = new List<Dip>();

            double lastDepth = 0.0;
            int dipStart = 0;
            for (int i = 0; i < depths.Length; i++)
            {
                if (depths[i] != 0.0)
                {
                    if (lastDepth == 0.0)
                    {
                        dipStart = i;
                    }
                }
                else
                {
                    if (lastDepth != 0.0)
                    {
                        dips.Add(new Dip(time[dipStart], time[i] - time[dipStart], lastDepth));
                    }
                }

                lastDepth = depths[i];
            }

            if (lastDepth != 0.0)
                dips.Add(new Dip(time[dipStart], time[depths.Length - 1] - time[dipStart], lastDepth));

            return dips;
        }

        static List<Planet> FindPlanets(List<Dip> dips)
        {
            List<Planet> planets = new List<Planet>();
            int planetNum = 1;

            for (int i = 0; i < dips.Count; i++)
            {
                if (dips[i].Planet == 0)
                {
                    double start = dips[i].Center();
                    for (int j = i + 1; j < dips.Count; j++)
                    {
                        if (dips[i].Planet == 0 && dips[j].Planet == 0)
                        {
                            double period = dips[j].Center() - start;
                            double probe = dips[i].Center() + 2 * period;
                            int transitCount = 0;
                            for (int k = j + 1; k < dips.Count; k++)
                            {
                                if (dips[i].Planet == 0 && dips[j].Planet == 0 && dips[k].Planet == 0)
                                {
                                    if (probe < dips[k].Start)
                                    {
                                        probe += period;
                                    }

                                    if (probe >= dips[k].Start && probe < dips[k].End())
                                    {
                                        dips[k].Planet = planetNum;
                                        transitCount = Math.Max(transitCount + 1, 3);
                                    }
                                }
                            }

                            if (transitCount > 0)
                            {
                                dips[i].Planet = planetNum;
                                dips[j].Planet = planetNum;
                                planetNum++;
                                planets.Add(new Planet(period, dips[i].Duration, transitCount, dips[i].Depth));
                            }
                        }
                    }
                }
            }

            return planets;
        }

        static void Main(string[] args)
        {
            if (args.Length != 1)
            {
                Console.WriteLine("Usage: FOO.EXE <Kepler .TBL file>");
                return;
            }

            List<double?> time = new List<double?>();
            List<double?> flux = new List<double?>();

            LoadTBL(args[0], time, flux);

            DumpTBL("foo.txt", time, flux);

            List<Dip> dips = FindDips(time, flux);

            List<Planet> planets = FindPlanets(dips);

            Console.WriteLine("Dips\t{0}", dips.Count);
            if (dips.Count > 0)
                Console.WriteLine("\nStart_d\tDur_h\tDepth\tPlanet");
            foreach (Dip d in dips)
                Console.WriteLine("{0:G6}\t{1:G6}\t{2:G6}\t{3:G6}",
                                  d.Start,
                                  d.Duration * 24.0,
                                  d.Depth,
                                  d.Planet);

            Console.WriteLine("\nPlanets\t{0}", planets.Count);
            if (planets.Count > 0)
                Console.WriteLine("\nPer_d\tDur_h\tTransits\tDepth");
            foreach (Planet p in planets)
                Console.WriteLine("{0:G6}\t{1:G6}\t{2}\t{3:G6}",
                                  p.Period,
                                  p.Duration * 24.0,
                                  p.Transits,
                                  p.Depth);
        }
    }
}
