# C#并发流程控制框架，基于CSP模型构建，主要用于工控逻辑、运动控制开发。

**开发者联系591170887**
----

相对于传统多线程模型、状态机模型、类PLC模型，逻辑结构紧凑清晰，开发效率极高，易于维护升级；
在golang语言的编程模式上设计开发，且进行必要的功能扩展；
自定义单/多线程调度，亦可主UI线程调度，方便逻辑与UI的交互；
自带高精度定时器、调度优先级、逻辑停止、逻辑暂停功能；
树形多任务调度，提高逻辑的可靠性；
单线程调度每秒100万次以上，从容应对千级IO点数；
已在多个项目中使用，稳定可靠。

----

```C#
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Go;

namespace WorkerFlow
{
    class Program
    {
        static shared_strand strand;

        static void Log(string msg)
        {
            Console.WriteLine($"{DateTime.Now.ToString("HH:mm:ss.fff")} {msg}");
        }

        static async Task Worker(string name, int time = 1000)
        {
            await generator.sleep(time);
            Log(name);
        }

        //1 A、B、C依次串行
        //A->B->C
        static async Task Worker1()
        {
            await Worker("A");
            await Worker("B");
            await Worker("C");
        }

        //2 A、B、C全部并行，且依赖同一个strand(隐含参数，所有依赖同一个strand的任务都是线程安全的)
        //A
        //B
        //C
        static async Task Worker2()
        {
            generator.children children = new generator.children();
            children.go(() => Worker("A"));
            children.go(() => Worker("B"));
            children.go(() => Worker("C"));
            await children.wait_all();
        }

        //3 A执行完后，B、C再并行
        //  -->B
        //  |
        //A->
        //  |
        //  -->C
        static async Task Worker3()
        {
            await Worker("A");
            generator.children children = new generator.children();
            children.go(() => Worker("B"));
            children.go(() => Worker("C"));
            await children.wait_all();
        }

        //4 B、C都并行执行完后，再执行A
        //B--
        //  |
        //  -->A
        //  |
        //C--
        static async Task Worker4()
        {
            generator.children children = new generator.children();
            children.go(() => Worker("B"));
            children.go(() => Worker("C"));
            await children.wait_all();
            await Worker("A");
        }

        //5 B、C任意一个执行完后，再执行A
        //B--
        //  |
        //  >-->A
        //  |
        //C--
        static async Task Worker5()
        {
            generator.children children = new generator.children();
            var B = children.tgo(() => Worker("B", 1000));
            var C = children.tgo(() => Worker("C", 2000));
            var task = await children.wait_any();
            if (task == B)
            {
                Log("B成功");
            }
            else
            {
                Log("C成功");
            }
            await Worker("A");
        }

        //6 等待一个特定任务
        static async Task Worker6()
        {
            generator.children children = new generator.children();
            var A = children.tgo(() => Worker("A"));
            var B = children.tgo(() => Worker("B"));
            await children.wait(A);
        }

        //7 超时等待一个特定任务，然后中止所有任务
        static async Task Worker7()
        {
            generator.children children = new generator.children();
            var A = children.tgo(() => Worker("A", 1000));
            var B = children.tgo(() => Worker("B", 2000));
            if (await children.timed_wait(1500, A))
            {
                Log("成功");
            }
            else
            {
                Log("超时");
            }
            await children.stop();
        }

        //8 超时等待一组任务，然后中止所有任务
        static async Task Worker8()
        {
            generator.children children = new generator.children();
            children.go(() => Worker("A", 1000));
            children.go(() => Worker("B", 2000));
            var tasks = await children.timed_wait_all(1500);
            await children.stop();
            Log($"成功{tasks.Count}个");
        }

        //9 超时等待一组任务，然后中止所有任务，且在中止任务中就地善后处理
        static async Task Worker9()
        {
            generator.children children = new generator.children();
            children.go(() => Worker("A", 1000));
            children.go(async delegate ()
            {
                try
                {
                    await Worker("B", 2000);
                }
                catch (generator.stop_exception)
                {
                    Log("B被中止");
                    await generator.sleep(500);
                    throw;
                }
                catch (System.Exception)
                {
                }
            });
            var task = await children.timed_wait_all(1500);
            await children.stop();
            Log($"成功{task.Count}个");
        }

        //10 嵌套任务
        static async Task Worker10()
        {
            generator.children children = new generator.children();
            children.go(async delegate ()
            {
                generator.children children1 = new generator.children();
                children1.go(() => Worker("A"));
                children1.go(() => Worker("B"));
                await children1.wait_all();
            });
            children.go(async delegate ()
            {
                generator.children children1 = new generator.children();
                children1.go(() => Worker("C"));
                children1.go(() => Worker("D"));
                await children1.wait_all();
            });
            await children.wait_all();
        }

        //11 嵌套中止
        static async Task Worker11()
        {
            generator.children children = new generator.children();
            children.go(() => Worker("A", 1000));
            children.go(async delegate ()
            {
                try
                {
                    generator.children children1 = new generator.children();
                    children1.go(async delegate ()
                    {
                        try
                        {
                            await Worker("B", 2000);
                        }
                        catch (generator.stop_exception)
                        {
                            Log("B被中止1");
                            await generator.sleep(500);
                            throw;
                        }
                        catch (System.Exception)
                        {
                        }
                    });
                    await children1.wait_all();
                }
                catch (generator.stop_exception)
                {
                    Log("B被中止2");
                    throw;
                }
                catch (System.Exception)
                {
                }
            });
            await generator.sleep(1500);
            await children.stop();
        }

        //12 并行执行且等待一组耗时算法
        static async Task Worker12()
        {
            wait_group wg = new wait_group();
            for (int i = 0; i < 2; i++)
            {
                wg.add();
                int idx = i;
                var _ = Task.Run(delegate ()
                {
                    try
                    {
                        Log($"执行算法{idx}");
                    }
                    finally
                    {
                        wg.done();
                    }
                });
            }
            await wg.wait();
            Log("执行算法完成");
        }

        //13 串行执行耗时算法，耗时算法必需放在线程池中执行，否则依赖同一个strand的调度将不能及时
        static async Task Worker13()
        {
            for (int i = 0; i < 2; i++)
            {
                await generator.send_task(() => Log($"执行算法{i}"));
            }
        }

        static async Task MainWorker()
        {
            await Worker1();
            await Worker2();
            await Worker3();
            await Worker4();
            await Worker5();
            await Worker6();
            await Worker7();
            await Worker8();
            await Worker9();
            await Worker10();
            await Worker11();
            await Worker12();
            await Worker13();
        }

        static void Main(string[] args)
        {
            work_service work = new work_service();
            strand = new work_strand(work);
            generator.go(strand, MainWorker);
            work.run();
            Console.ReadKey();
        }
    }
}
```