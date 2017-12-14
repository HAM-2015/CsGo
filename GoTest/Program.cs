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
        static csp_chan<long, long> _csp;

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

        static async Task Producer4()
        {
            while (true)
            {
                long res = await generator.csp_invoke(_csp, system_tick.get_tick_us());
                Console.WriteLine("csp return {0}", res);
                await generator.sleep(1000);
            }
        }

        static async Task Consumer()
        {
            Console.WriteLine("pop chan1 {0}", await generator.chan_pop(_chan1));
            Console.WriteLine("push chan2 {0}", await generator.chan_push(_chan2, system_tick.get_tick_us()));
            Console.WriteLine("pop chan3 {0}", await generator.chan_pop(_chan3));
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
                }), generator.case_read(_csp, async delegate (long msg)
                {
                    Console.WriteLine("select csp delay {0}", system_tick.get_tick_us() - msg);
                    await generator.sleep(100);
                    return system_tick.get_tick_us();
                }));
            }
        }

        static async Task Producer5(generator cons)
        {
            channel<int> intMb = await cons.get_mailbox<int>();
            channel<long> longMb = await cons.get_mailbox<long>();
            for (int i = 0; i< 10; i++)
            {
                await generator.chan_push(intMb, i);
                await generator.sleep(1000);
                await generator.chan_push(longMb, i);
                await generator.sleep(1000);
            }
            await generator.chan_close(intMb);
            await generator.chan_close(longMb);
        }

        static async Task Consumer2()
        {
            await generator.begin().receive(async delegate (int msg)
            {
                Console.WriteLine("                                   receive int {0}", msg);
                await generator.sleep(1);
            }).receive(async delegate (long msg)
            {
                Console.WriteLine("                                   receive long {0}", msg);
                await generator.sleep(1);
            }).end();
            Console.WriteLine("                                   receive end");
        }

        static void Main(string[] args)
        {
            work_service work = new work_service();
            _strand = new work_strand(work);
            _chan1 = channel<long>.make(_strand, 3);
            _chan2 = channel<long>.make(_strand, 0);
            _chan3 = channel<long>.make(_strand, -1);
            _csp = new csp_chan<long, long>(_strand);
            generator.go(_strand, Producer1);
            generator.go(_strand, Producer2);
            generator.go(_strand, Producer3);
            generator.go(_strand, Producer4);
            generator.go(_strand, Consumer);
            generator.go(_strand, () => Producer5(generator.tgo(_strand, Consumer2)));
            work.run();
        }
    }
}
