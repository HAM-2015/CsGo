using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Go;

namespace GoTest
{
    class Program
    {
        static shared_strand _strand;
        static channel<long> _chan1;
        static channel<long> _chan2;
        static channel<long> _chan3;

        static async Task Producer1()
        {
            while (true)
            {
                await generator.chan_push(_chan1, system_tick.get_tick_us());
                await generator.sleep(300);
            }
        }

        static async Task Producer2()
        {
            while (true)
            {
                await generator.chan_pop(_chan2);
                await generator.sleep(500);
            }
        }

        static async Task Producer3()
        {
            while (true)
            {
                await generator.chan_push(_chan3, system_tick.get_tick_us());
                await generator.sleep(1000);
            }
        }

        static async Task Consumer()
        {
            Console.WriteLine("pop chan1 {0}", (await generator.chan_pop(_chan1)).result);
            Console.WriteLine("push chan2 {0}", await generator.chan_push(_chan2, system_tick.get_tick_us()));
            Console.WriteLine("pop chan3 {0}", (await generator.chan_pop(_chan3)).result);
            while (true)
            {
                await generator.select_chans_once(generator.case_read(_chan1, async delegate (long msg)
                {
                    Console.WriteLine("select read chan1 {0}", msg);
                    await generator.sleep(100);
                }), generator.case_write(_chan2, system_tick.get_tick_us(), async delegate ()
                {
                    Console.WriteLine("select write chan2");
                    await generator.sleep(100);
                }), generator.case_read(_chan3, async delegate (long msg)
                {
                    Console.WriteLine("select read chan3 {0}", msg);
                    await generator.sleep(100);
                }));
            }
        }

        static void Main(string[] args)
        {
            _strand = new shared_strand();
            _chan1 = channel<long>.make(_strand, 3);
            _chan2 = channel<long>.make(_strand, 0);
            _chan3 = channel<long>.make(_strand, -1);
            generator.go(_strand, Producer1);
            generator.go(_strand, Producer2);
            generator.go(_strand, Producer3);
            generator.go(_strand, Consumer).sync_wait();
        }
    }
}
