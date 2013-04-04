/*
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;
using System.IO;
using System.IO.Pipes;
using System.Timers;
using System.Globalization;
using System.Xml.Serialization;


namespace dataloopers_reader
{
    public class Raw_IV_Curve
    {
        [XmlIgnoreAttribute]
        public const int MAX_POINTS_PER_RF_PACKET = 20;

        [XmlIgnoreAttribute]
        public List<short> v, c;

        public string stat_calc_error = "none";

        public DateTime SweepTime;


        public int last_frame_added = -1;

        public byte info_byte;

        public ushort total_points;

        public float LQI;
        // public byte sweep_info;
        public ushort sweep_time;

        public ushort v_gs_end;

        public byte sweep_time_scaler_per_point;

        public float board_temp, vbat;

        public short accel_x, accel_y, accel_z;

        public int skipped_frame_at = -1;

        public ushort id;

        public float irradiation_mv;

        public bool is_irradation_valid;

        public float irradiation_mv_stdev_lastmin;

        public float Vmpp, Impp;

        public bool isCharging;


        [XmlIgnoreAttribute]
        public int total_points_added_so_far;

        [XmlIgnoreAttribute]
        public int sum_LQI = 0;


        public volatile bool complete_curve;


        public volatile bool written_to_db;

        [XmlIgnoreAttribute]
        public long first_packet_ms;

        [XmlIgnoreAttribute]
        public long last_packet_ms;


        public byte hardware_version;
        public long digi_seconds;

        public ushort address;

        public ushort v_thresh = 0;

        public UInt32 power_dissipation = 0;

        public string ModuleName = "none";

        public float ActiveArea = 1.0f;

        public DateTime ModuleInstallDate;

        [XmlIgnoreAttribute]
        public string module_description;

        [XmlIgnoreAttribute]
        public int module_id;

        [XmlIgnoreAttribute]
        public System.Timers.Timer curve_incomplete_timer;

        public float sense_r;

        public float dV;

        public float dI;

        public float calibration;

        public float accuracy_scaler = hardware_definition.accuracy_scaler;

        public float Voc, Isc, PeakPower, Rsc, Rsh, FF;


        public float[] currents;
        public float[] voltages;

        public int frames_received = 0;

        public string sweep_direction = "";



        public byte[] get_curve_as_raw_bytes()
        {
            //using little endian
            int loc = 0;
            byte[] curve = new byte[4 * v.Count];
            for (int i = 0; i < v.Count; i++)
            {
                curve[loc++] = (byte)v[i];
                curve[loc++] = (byte)(v[i] >> 8);
            }
            for (int i = 0; i < c.Count; i++)
            {
                curve[loc++] = (byte)c[i];
                curve[loc++] = (byte)(c[i] >> 8);
            }
            return curve;
        }

        public void calc_curve_stats()
        {
            Voc = (v[0] * dV);
            Isc = c[c.Count - 1] * dI;
            long peakpower = 0;
            int ip = 0;
            long power;
            for (int i = 0; i < c.Count; i++)
            {
                power = v[i] * c[i];
                if (peakpower < power)
                {
                    ip = i;
                    peakpower = power;

                }
            }
            PeakPower = v[ip] * dV * c[ip] * dI;
            Vmpp = v[ip] * dV;
            Impp = c[ip] * dI;
            try
            {
                FF = PeakPower / (Isc * Voc);
            }
            catch
            {
                FF = 0;
            }

            try
            {
                int hi = 2;
                int lo = 0;
                Rsc = Math.Abs((float)(v[hi] - v[lo]) / (float)(c[hi] - c[lo]));
                Rsc = Rsc * (dV / dI);

            }
            catch
            {

            }

            try
            {
                int limit = v.Count - 1;
                int spread = 3;
                Rsh = Math.Abs((float)(v[limit] - v[limit - spread]) / (float)(c[limit] - c[limit - spread]));
                Rsh = Rsh * (dV / dI);
            }
            catch { }
            if (float.IsInfinity(Rsh))
            {
                Rsh = -1;
            }

            LQI = (float)(sum_LQI / frames_received);

        }
        public void WriteDataToExcel(string filename)
        {

            StreamWriter wr = new System.IO.StreamWriter(filename);
            wr.WriteLine("Current (A),Voltage (V)");
            try
            {
                for (int i = 0; i < total_points; i++)
                {
                    wr.WriteLine(string.Format("{0},{1}", currents[i].ToString("F4", CultureInfo.InvariantCulture), voltages[i].ToString("F4", CultureInfo.InvariantCulture)));
                }

            }
            catch (Exception ex)
            {
                Console.WriteLine("error writing csv " + ex.Message);
            }
            finally
            {
                wr.Close();
            }
            Console.WriteLine("wrote csv file " + filename);
        }

        [XmlIgnoreAttribute]
        public float average_LQI
        {
            get
            {
                float ave = 0;
                return ave;
            }
        }

        [XmlIgnoreAttribute]
        public long total_ms_receive_time
        {
            get
            {
                return (last_packet_ms - first_packet_ms) / 10000;
            }
        }
        public Raw_IV_Curve()
        {


        }
        //public Raw_IV_Curve(ushort id_in, int total_points_in, ushort address_in, byte hardware_version_in)
        public Raw_IV_Curve(ushort address_in)
        {
            SweepTime = DateTime.Now;

            first_packet_ms = DateTime.Now.Ticks;
            total_points_added_so_far = 0;
            complete_curve = false;
            written_to_db = false;
            address = address_in;
            curve_incomplete_timer = new System.Timers.Timer(10000);
            curve_incomplete_timer.Elapsed += new ElapsedEventHandler(curve_incomplete_timer_Elapsed);
            curve_incomplete_timer.Start();


            //frames = new List<Raw_IV_Curve_Frame>();


        }
        public void frame_zero(byte[] raw_data, ref int loc, int points_in_frame)
        {


            hardware_version = raw_data[loc];
            loc += 1;

            sweep_time_scaler_per_point = raw_data[loc];
            loc += 1;

            total_points = BitConverter.ToUInt16(raw_data, loc);
            loc += 2;

            sweep_time = raw_data[loc];
            loc += 1;

            info_byte = raw_data[loc];
            loc += 1;


            if (((byte)info_byte & (byte)0x01) == (byte)0x01)
            {
                sweep_direction = "VOC_TO_ISC";
            }
            else
            {
                sweep_direction = "ISC_TO_VOC";
            }

            if (((byte)info_byte & (byte)0x80) == (byte)0x80)
            {
                isCharging = true;
            }
            else
            {
                isCharging = false;
            }

            vbat = BitConverter.ToUInt16(raw_data, loc) * 3.3f / 1023f;
            loc += 2;

            board_temp = 100f * (BitConverter.ToUInt16(raw_data, loc) * 3.3f / 1023f - .5f);
            loc += 2; //(get_voltage(raw_data, 5) - 0.5f) * 100f;

            accel_x = BitConverter.ToInt16(raw_data, loc);
            loc += 2;

            accel_y = BitConverter.ToInt16(raw_data, loc);
            loc += 2;

            accel_z = BitConverter.ToInt16(raw_data, loc);
            loc += 2;

            v_thresh = BitConverter.ToUInt16(raw_data, loc);
            loc += 2;

            v_gs_end = BitConverter.ToUInt16(raw_data, loc);
            loc += 2;

            sweep_time = BitConverter.ToUInt16(raw_data, loc);
            loc += 2;

            power_dissipation = BitConverter.ToUInt32(raw_data, loc);
            loc += 4;

            v = new List<short>();
            c = new List<short>();

            for (int h = 0; h < points_in_frame; h++)
            {
                v.Add(BitConverter.ToInt16(raw_data, loc));
                loc += 2;

                c.Add(BitConverter.ToInt16(raw_data, loc));
                loc += 2;
            }

            //todo: add LQI averaging here
        }

        public void other_frames(byte[] raw_data, ref int loc, int points_in_frame)
        {
            for (int h = 0; h < points_in_frame; h++)
            {
                v.Add(BitConverter.ToInt16(raw_data, loc));
                loc += 2;

                c.Add(BitConverter.ToInt16(raw_data, loc));
                loc += 2;
            }
            //todo: add LQI averaging here
        }

        public bool AddFrame(byte[] raw_data)
        {
            int loc = 0;

            id = BitConverter.ToUInt16(raw_data, loc);
            loc += 2;



            int frame_number = raw_data[loc];
            loc += 1;

            int points_in_frame = raw_data[loc];
            loc += 1;

            if (frame_number == last_frame_added)
            {
                Console.WriteLine("***************DUPLICATE FRAME, IGNORING************");
                return true;
            }
            else if (frame_number > last_frame_added + 1)
            {
                skipped_frame_at = last_frame_added + 1;
                Console.WriteLine("***************SKIPPED A FRAME IN CURVE************");
                return false;
            }

            last_frame_added = frame_number;


            if (frame_number == 0)
            {

                frame_zero(raw_data, ref loc, points_in_frame);
            }
            else
            {
                other_frames(raw_data, ref loc, points_in_frame);
            }

            if (raw_data.Length - 1 == loc)
            {
                sum_LQI += raw_data[loc];
            }
            digi_seconds = -1;
            try
            {
                string digi_seconds_time_string = ASCIIEncoding.ASCII.GetString(raw_data, loc + 1, raw_data.Length - loc - 2);
                digi_seconds_time_string = digi_seconds_time_string.Trim('\"');
                digi_seconds = (long)float.Parse(digi_seconds_time_string);
            }
            catch (Exception ex)
            {
                digi_seconds = -2;
            }




            total_points_added_so_far += points_in_frame;

            frames_received++;



            if (total_points_added_so_far == total_points)
            {
                complete_curve = true;
                curve_incomplete_timer.Stop();
                last_packet_ms = DateTime.Now.Ticks;

                voltages = new float[total_points];
                currents = new float[total_points];

                hardware_definition def = Program.hardware_dict[hardware_version];

                dI = def.dI;
                dV = def.dV;

                sense_r = def.sense_r;


                for (int i = 0; i < total_points; i++)
                {
                    currents[i] = Program.GetCurrentFromADC(c[i], def);
                    voltages[i] = Program.GetVoltageFromADC(v[i], def);
                }

                if (Program.modules != null)
                {
                    try
                    {
                        ActiveArea = Program.modules[address].active_area;
                        ModuleName = Program.modules[address].name;
                        ModuleInstallDate = Program.modules[address].install_date_time;
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(string.Format("error looking up module in dictionary for address {0}.\r\n {1} ", address, ex.Message));
                    }

                }


                try
                {
                    calc_curve_stats();
                }
                catch (Exception ex)
                {
                    stat_calc_error = ex.Message + " : " + ex.StackTrace;
                }
            }

            return true;
        }
        void curve_incomplete_timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            curve_incomplete_timer.Stop();
            Console.WriteLine(string.Format("TIMER ELAPSED  with {0} frame received for unit with address {1}", frames_received, Program.hex(address)));
            lock (Program.curves)
            {
                Program.curves.Remove(this.id);
            }
        }
    }
}


*/