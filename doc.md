# 调度器 #

**work_service**

&ensp;&ensp;&ensp;&ensp;一级调度，使用post将任务提交给一个单事件队列，启用一个或多个线程执行run函数，事件队列中的任务将被并行执行。这个类可用来执行一些无序任务，类似于线程池。

**work_engine**

&ensp;&ensp;&ensp;&ensp;对work_service的多线程封装，设置好线程数和优先级，直接开启run执行即可。不用另外开启线程执行work_service.run()。

**shared_strand**

&ensp;&ensp;&ensp;&ensp;二级调度，使用post将任务提交给一个双缓冲事件队列，内部将事件一个接一个的按序抛给Task执行，只有上一个任务完成，才开始下一个任务，这样尽管任务可能被分派到不同的线程中，但所执行的任务彼此也将是线程安全的。

**work_strand**

&ensp;&ensp;&ensp;&ensp;继承自shared_strand，只不过将任务抛给work_service执行。

**control_strand**

&ensp;&ensp;&ensp;&ensp;继承自shared_strand，只不过将任务抛给Control控件，任务将在UI线程中执行。这个对象一个进程只能创建一个.


# generator #

&ensp;&ensp;&ensp;&ensp;任务执行实体，不同于Thread的抢占式调度，由shared_strand协作式调度运行。所有依赖同一个shared_strand的generator彼此将是线程安全的。

**go**

&ensp;&ensp;&ensp;&ensp;创建并立即开始运行一个generator。

**tgo**

&ensp;&ensp;&ensp;&ensp;创建并立即开始运行一个generator，并返回generator对象。

**make**

&ensp;&ensp;&ensp;&ensp;创建一个generator但不立即运行。

**run**

&ensp;&ensp;&ensp;&ensp;开始运行一个generator。


**stop**

&ensp;&ensp;&ensp;&ensp;给generator发送一个中止操作，generator内部流程收到后自行处理。

**suspend**

&ensp;&ensp;&ensp;&ensp;给generator发送一个挂起操作。

**resume**

&ensp;&ensp;&ensp;&ensp;给generator发送一个挂起恢复操作。

**lock_stop**

&ensp;&ensp;&ensp;&ensp;与unlock_stop成对出现，lock后generator将延时接收stop操作，只有unlock_stop后才能接收到stop。

**lock_suspend**

&ensp;&ensp;&ensp;&ensp;与unlock_suspend成对出现，lock后generator将延时接收suspend，只有unlock_suspend后才能接收到suspend。

**lock_shield**

&ensp;&ensp;&ensp;&ensp;与unlock_shield成对出现，lock后generator将延时接收suspend/stop操作，只有unlock_shield后才能接收到stop/suspend。

**chan_send**

&ensp;&ensp;&ensp;&ensp;执行chan.send()操作。

**chan_timed_send**

&ensp;&ensp;&ensp;&ensp;执行chan.timed_send()操作。

**chan_try_send**

&ensp;&ensp;&ensp;&ensp;执行chan.try_send()操作。

**chan_receive**

&ensp;&ensp;&ensp;&ensp;执行chan.receive()操作。

**chan_timed_receive**

&ensp;&ensp;&ensp;&ensp;执行chan.timed_receive()操作。

**chan_try_receive**

&ensp;&ensp;&ensp;&ensp;执行chan.try_receive()操作。

**select**

&ensp;&ensp;&ensp;&ensp;一次选择一个可立即操作的chan进行send/recv。

**sleep**

&ensp;&ensp;&ensp;&ensp;延时运行一小会。

**send_task**

&ensp;&ensp;&ensp;&ensp;发送一个任务到Task中，完成后返回，主要用来执行耗时算法。

**send_service**

&ensp;&ensp;&ensp;&ensp;发送一个任务到work_service中，完成后返回，主要用来执行耗时算法。

**send_strand**

&ensp;&ensp;&ensp;&ensp;发送一个任务到shared_strand中，完成后返回，主要用来串行耗时算法。

**send_control**

&ensp;&ensp;&ensp;&ensp;发送一个任务到shared_control中，完成后返回，主要用来在非control_strand调度的generator中操作UI，或弹出模态对话框。

# wait_group #

&ensp;&ensp;&ensp;&ensp;用来给任务计数，然后等待所有任务结束。

**add**

&ensp;&ensp;&ensp;&ensp;添加一个任务计数。

**done**

&ensp;&ensp;&ensp;&ensp;结束一个任务计数.

**wait**

&ensp;&ensp;&ensp;&ensp;等待所有任务计数被清零。

**async_wait**

&ensp;&ensp;&ensp;&ensp;异步等待所有任务计数被清零。

**cancel_wait**

&ensp;&ensp;&ensp;&ensp;中止当前的async_wait操作。

# wait_gate #

&ensp;&ensp;&ensp;&ensp;设置一个信号关卡，当计数满足后执行一个操作，然后开始接下来的任务。

**enter**

&ensp;&ensp;&ensp;&ensp;开始一个计数等待，当计数满足且一个操作完成后，然后开始接下来的任务。

**async_enter**

&ensp;&ensp;&ensp;&ensp;开始一个异步计数等待。

**cancel_enter**

&ensp;&ensp;&ensp;&ensp;取消async_enter等待。

# children #

&ensp;&ensp;&ensp;&ensp;child任务组，务必在generator上下文中创建和执行操作，且有效的管理内部的child，否则将造成泄漏，不然将等到generator被stop时才能清空。

**stop**

&ensp;&ensp;&ensp;&ensp;中止一个或多个child任务。

**wait**

&ensp;&ensp;&ensp;&ensp;等待一个或多个child任务结束。

**timed_wait**

&ensp;&ensp;&ensp;&ensp;超时等待一个或多个child任务结束。

**wait_any**

&ensp;&ensp;&ensp;&ensp;等待任意一个child任务结束。

**timed_wait_any**

&ensp;&ensp;&ensp;&ensp;超时等待任意一个child任务结束。

**wait_all**

&ensp;&ensp;&ensp;&ensp;等待所有child任务结束。

**timed_wait_all**

&ensp;&ensp;&ensp;&ensp;超时等待所有child任务结束。

# child #

&ensp;&ensp;&ensp;&ensp;继承自generator，由children创建的子任务，生命周期将受上级generator管理。

# chan #

&ensp;&ensp;&ensp;&ensp;用于generator之间的消息通信，分无缓存(nil_chan)、有限缓存(limit_chan)、无限缓存(unlimit_chan)、过程调用(csp_chan)四种类型。

**send**

&ensp;&ensp;&ensp;&ensp;发送消息，阻塞等待，成功后返回。

**try_send**

&ensp;&ensp;&ensp;&ensp;尝试发送消息，不阻塞等待，立即返回。

**timed_send**

&ensp;&ensp;&ensp;&ensp;尝试发送消息，超时阻塞等待，成功或过期后返回。

**receive**

&ensp;&ensp;&ensp;&ensp;接收消息，阻塞等待，成功后返回。

**try_receive**

&ensp;&ensp;&ensp;&ensp;尝试接收消息，不阻塞等待，立即返回。

**timed_receive**

&ensp;&ensp;&ensp;&ensp;尝试接收消息，超时阻塞等待，成功或过期后返回。

**close**

&ensp;&ensp;&ensp;&ensp;关闭chan。

**cancel**

&ensp;&ensp;&ensp;&ensp;取消当前chan中等待的所有操作。

**invoke**

&ensp;&ensp;&ensp;&ensp;在csp_chan中执行一次过程调用，得到结果后返回。

**try_invoke**

&ensp;&ensp;&ensp;&ensp;在csp_chan中尝试执行一次过程调用，得到结果或失败后返回。

**timed_invoke**

&ensp;&ensp;&ensp;&ensp;在csp_chan中超时执行一次过程调用，得到结果或过期后返回。

**wait**

&ensp;&ensp;&ensp;&ensp;在csp_chan中等待一次过程调用，完成后返回。

**try_wait**

&ensp;&ensp;&ensp;&ensp;在csp_chan中尝试等待一次过程调用，完成或失败后返回。

**timed_wait**

&ensp;&ensp;&ensp;&ensp;在csp_chan中超时等待一次过程调用，完成或超时后返回。

# async_timer #

&ensp;&ensp;&ensp;&ensp;受shared_strand调度的定时器，所有操作只能在该shared_strand中进行。

**timeout**

&ensp;&ensp;&ensp;&ensp;开始一个计时操作。

**deadline**

&ensp;&ensp;&ensp;&ensp;使用终止时间，开始一个计时操作。

**interval**

&ensp;&ensp;&ensp;&ensp;开始一个周期性的定时操作。

**restart**

&ensp;&ensp;&ensp;&ensp;如果当前计时还未结束，将重新开始计时。

**advance**

&ensp;&ensp;&ensp;&ensp;如果当前计时还未结束，将提前完成计时。

**cancel**

&ensp;&ensp;&ensp;&ensp;如果当前计时还未结束，将中止计时。