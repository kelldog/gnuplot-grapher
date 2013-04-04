using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Diagnostics;
using System.Threading;
//using dataloopers_reader;
using System.Xml.Serialization;


namespace iv_curve_grapher
{
    class Program
    {
        static void Main(string[] args)
        {

            string dir = Directory.GetCurrentDirectory() +"\\iv_data";//

           // dir = @"C:\Users\cowdo\Documents\WirelessTracer\iv_catcher\iv_curve_catcher\iv_curve_catcher\bin\Debug\iv_data\";
            
            FileSystemWatcher watcher = new FileSystemWatcher(dir);

            string pgm = @"C:\Program Files\gnuplot\bin\pgnuplot.exe";
            if (args.Length > 0)
            {
                pgm = args[0];
            }

            Console.WriteLine("using gnu plot @ " + pgm);
            Thread.Sleep(400);
            Process extPro = new Process();
            extPro.StartInfo.FileName = pgm;
            extPro.StartInfo.UseShellExecute = false;
            extPro.StartInfo.RedirectStandardInput = true;
            extPro.Start();

           // XmlSerializer iv_xml = new XmlSerializer(typeof(Raw_IV_Curve));


            StreamWriter gnupStWr = extPro.StandardInput;
            while (true)
            {

                WaitForChangedResult res = watcher.WaitForChanged(WatcherChangeTypes.All,int.MaxValue);
                if (res.TimedOut)
                {
                    continue;
                }
                if (res.Name.Contains(".csv"))
                {
                    Thread.Sleep(1000);

                    try
                    {
                        string fn = Path.Combine(dir, res.Name);

                        string[] lines = File.ReadAllLines(fn);

                        DateTime filetime = File.GetCreationTime(fn);

                        List<float> V = new List<float>();
                        List<float> C = new List<float>();

                        float Vmpp = 0;
                        float Impp = 0;
                        float mpp = 0;

                        foreach (string l in lines)
                        {
                            try
                            {
                                string[] f = l.Split(',');
                                float v = float.Parse(f[1]);
                                float c = float.Parse(f[0]);
                                V.Add(v);
                                C.Add(c);
                                if (c * v > mpp)
                                {
                                    mpp = c * v;
                                    Vmpp = v;
                                    Impp = c;

                                }
                            }
                            catch (Exception ex)
                            {

                            }


                        }

                        //  TextReader reader = new StreamReader( fn );
                        //  Raw_IV_Curve curve = (Raw_IV_Curve)iv_xml.Deserialize(reader);

                        //  string csv_file = res.Name.Replace(".txt", ".csv");



                        int index = res.Name.LastIndexOf('a');
                        string address = res.Name.Substring(index + 1, res.Name.LastIndexOf('.') - index);
                        gnupStWr.WriteLine("reset");
                        gnupStWr.Flush();
                        gnupStWr.WriteLine("set term wxt " + address);
                        gnupStWr.Flush();

                        gnupStWr.WriteLine("set style data linespoints");
                        gnupStWr.Flush();
                        gnupStWr.WriteLine("set grid");
                        gnupStWr.Flush();
                        gnupStWr.WriteLine("set datafile separator \",\"");
                        gnupStWr.Flush();
                        gnupStWr.WriteLine("set output \"last_iv.png\"");
                        gnupStWr.Flush();
                        gnupStWr.WriteLine(string.Format("set title \"File : {0}\"", res.Name));
                        gnupStWr.Flush();

                        gnupStWr.WriteLine("set xlabel \"Voltage(V)\"");
                        gnupStWr.Flush();
                        gnupStWr.WriteLine("set ylabel \"Current(A)\"");
                        gnupStWr.Flush();
                        gnupStWr.WriteLine(string.Format("set label \"  Peak Power = {0} Watts \" at {1},{2}", mpp.ToString("F2"), Vmpp, Impp));

                        gnupStWr.Flush();
                        gnupStWr.WriteLine("set style line 1 lc rgb '#0060ad' lt 1 lw 2 pt 7 ps .4");
                        gnupStWr.Flush();
                        gnupStWr.WriteLine("set autoscale");
                        gnupStWr.Flush();
                        //string file = fn;// @"C:\Users\cowdo\Documents\WirelessTracer\iv_catcher\iv_curve_catcher\iv_curve_catcher\bin\Debug\iv_data\2013_77_47895_a13.csv";
                        gnupStWr.WriteLine(string.Format("plot '{0}' using 2:1 title \'{1}  {2}\' with linespoints ls 1", fn, filetime.ToShortDateString(), filetime.ToShortTimeString()));
                        gnupStWr.Flush();
                    }
                    catch (Exception ex)
                    {

                        Console.WriteLine(ex.Message);
                    }
                }
            }
        }

        static void watcher_Changed(object sender, FileSystemEventArgs e)
        {
            //throw new NotImplementedException();
        }
    }
}
