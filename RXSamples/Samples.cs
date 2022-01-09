using System.Collections.ObjectModel;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using NUnit.Framework;
using Serilog;


namespace RXSamples
{
    [TestFixture]
    public class Samples
    {
        private AutoResetEvent m_autoResetEvent;

        #region Public Methods

        [SetUp]
        public void Setup()
        {
            Log.Logger = new LoggerConfiguration()
                .WriteTo.Console()
                // .WriteTo.File("log-.txt", rollingInterval: RollingInterval.Day)
                .CreateLogger();
        }
        [TestCase]
        public void A01_Interval()
        {
            var o = Observable.Interval(TimeSpan.FromSeconds(0.5)).Take(20);
            o.Subscribe(num => TestContext.Progress.WriteLine(num));
            Task.Delay(TimeSpan.FromSeconds(10)).Wait();
        }

        [TestCase]
        public void A02_Range()
        {
            var o = Observable.Range(1, 10);
            //Task.Delay(TimeSpan.FromSeconds(2)).Wait();
            o.Subscribe(num => Console.WriteLine(num));
        }

        [TestCase]
        public void A03_Generate()
        {
            var o = Observable.Generate(0,
               i => i < 10,
               i => i + 1,
               i => i * i);

            o.Subscribe(num => Console.WriteLine(num));
        }

        [TestCase]
        public void A04_GenerateWithTime_FromMain()
        {
            TimeSpan iterationTime = TimeSpan.FromMilliseconds(500);

            var o = Observable.Generate(0,
               i => i < 10,
               i => i + 1,
               i => i * i,
               i => iterationTime);

            TimeSpan delayTime = TimeSpan.FromSeconds(5);
            var o2 = o.Delay(delayTime);

            o.Subscribe(num => Console.WriteLine($"o:{num}, {DateTime.Now.ToShortTimeString()}"));
            o2.Subscribe(num => Console.WriteLine($"o2:{num}, {DateTime.Now.ToShortTimeString()}"));
            
            Task.Delay(delayTime+TimeSpan.FromSeconds(1)).Wait();
        }

        [TestCase]
        public void A05_repeat()
        {
            var o1 = Observable.Range(1, 5);
            var o2 = o1.Repeat(5);

            o1.Subscribe(n => Console.Write($"{n};"));
            Console.WriteLine();
            o2.Subscribe(n => Console.Write($"{n};"));
        }

        [TestCase]
        public void A06_ToObservable()
        {
            int[] array = { 1, 3, 5, 7 };
            IObservable<int> o = array.ToObservable();
            var disposable = o.Subscribe(v => Console.Write(v + "; ")); // 1; 3; 5; 7; 
            disposable.Dispose();
        }

        [TestCase]
        public void A07_Disposable()
        {
            IDisposable disposable = Disposable.Create(() => Console.WriteLine("disposed!"));
            disposable.Dispose();
        }

        [TestCase]
        public void A10_Subject_FromMain()
        {
            ISubject<int> subject = new Subject<int>();
            IObservable<int> observable = subject;

            Action<int> onNextHandler = num => Console.WriteLine(num);
            Action<Exception> onErrorHandler = e => Console.WriteLine(e);
            Action onCompleted = () => Console.WriteLine("Finished");

            observable.Subscribe(onNextHandler, onErrorHandler, onCompleted);

            subject.OnNext(10);
            subject.OnNext(20);
            subject.OnNext(30);

            subject.OnError(new InvalidDataException("Bad number"));

            //observable.Subscribe(onNextHandler, onErrorHandler, onCompleted);

            // OnError & OnCompleted disposes the Subject!
            subject.OnNext(40);     // won't raise!!


            subject = new Subject<int>();
            observable = subject;
            observable.Subscribe(onNextHandler, onErrorHandler, onCompleted);
            subject.OnCompleted();
        }

        [TestCase]
        public void A11_SubscribedProtected_FromMain()
        {
            ISubject<int> subject = new Subject<int>();
            IObservable<int> observable = subject;

            Task.Run(() =>
            {
                Action<int> onNextHandler = num => Console.WriteLine(num);

                observable.Subscribe(onNextHandler);

                subject.OnNext(10);
                subject.OnNext(20);
                subject.OnNext(30);

                subject.OnError(new InvalidDataException("Bad number"));

                // OnError & OnCompleted disposes the Subject!
                subject.OnNext(40);		// won't raise!!
            });
        }

        [TestCase]
        public void A12_SynchroniseSubject()
        {
            Console.WriteLine($"Method threadId = {Thread.CurrentThread.ManagedThreadId}");

            ISubject<int> subject = new Subject<int>();
            var eventLoopScheduler = new EventLoopScheduler();

            // -----> change remarks <------
            ISubject<int, int> syncedSubject = Subject.Synchronize(subject, eventLoopScheduler);
            //			var syncedSubject = Subject.Synchronize(subject, Scheduler.Immediate);	//immediately on the current thread
            //			var syncedSubject = Subject.Synchronize(subject, Scheduler.Default);	//	on the platform's default scheduler
            //			var syncedSubject = Subject.Synchronize(subject, Scheduler.CurrentThread);	  //as soon as possible on the current thread


            syncedSubject.Subscribe(num => Console.WriteLine($"received thread:{Thread.CurrentThread.ManagedThreadId}"));

            subject.Subscribe(num => Console.WriteLine($"received thread:{Thread.CurrentThread.ManagedThreadId}"));

            Console.WriteLine($"send thread:{Thread.CurrentThread.ManagedThreadId}");
            subject.OnNext(0);

            Task.Delay(TimeSpan.FromSeconds(1)).Wait();
        }


        [TestCase]
        public void A13_Do()
        {
            IList<int> numbers = new List<int> { 1, 4, 7 };
            IObservable<int> numbersObservable = numbers.ToObservable();

            numbersObservable
                .Do(number => Console.WriteLine($"do1={number}"))
                .Select(number => number * 2)
                .Do(number => Console.WriteLine($"do2={number}"))
                .Subscribe(number => Console.WriteLine(number));
        }

        [TestCase]
        public void A14_Select_Where()
        {
            var o = Observable.Generate(0,
                i => i < 10,
                i => i + 1,
                i => -i,
                i => TimeSpan.FromSeconds(0.1));

            o.Where(number => number % 2 == 0)
                .Select(number => number * 2)
                .Subscribe(number => Console.WriteLine(number));

            Task.Delay(TimeSpan.FromSeconds(1)).Wait();
        }

        [TestCase]
        public void A15_DistinctUntilChanged()
        {
            ISubject<KeyValuePair<int, string>> values = new Subject<KeyValuePair<int, string>>();
            values.DistinctUntilChanged().Subscribe(pair => Console.WriteLine($"any change: {pair}"));
            values.DistinctUntilChanged(pair => pair.Value).Subscribe(pair => Console.WriteLine($"second field changed: {pair}"));

            values.OnNext(new KeyValuePair<int, string>(1, "A"));
            values.OnNext(new KeyValuePair<int, string>(1, "A"));
            values.OnNext(new KeyValuePair<int, string>(1, "A"));
            values.OnNext(new KeyValuePair<int, string>(2, "A"));
            values.OnNext(new KeyValuePair<int, string>(2, "A"));
            values.OnNext(new KeyValuePair<int, string>(2, "A"));
            values.OnNext(new KeyValuePair<int, string>(1, "B"));
            values.OnNext(new KeyValuePair<int, string>(1, "B"));
            values.OnNext(new KeyValuePair<int, string>(1, "B"));
        }

        [TestCase]
        public void A16_BehaviorSubject()
        {
            var behaviorSubject = new BehaviorSubject<int>(2);
            IObservable<int> observable = behaviorSubject;

            observable.Subscribe(num => Console.WriteLine(num));

            Console.WriteLine(behaviorSubject.Value);

            behaviorSubject.OnNext(5);
            Console.WriteLine(behaviorSubject.Value);
        }

        [TestCase]
        public void A17_EventLoopScheduler()
        {
            Console.WriteLine($"Method threadId = {Thread.CurrentThread.ManagedThreadId}");

            var e = new EventLoopScheduler();
            ISubject<Unit> subject = new Subject<Unit>();

            subject.Subscribe(i => Console.WriteLine($"1: {Thread.CurrentThread.ManagedThreadId}")); // on default thread
            IObservable<Unit> observable = subject.ObserveOn(e);
            //			IObservable<Unit> observable2 = subject.ObserveOn(e);
            observable.Subscribe(i => Console.WriteLine($"2: {Thread.CurrentThread.ManagedThreadId}")); // on e thread

            subject.OnNext(Unit.Default);

            Thread.Sleep(500);
        }

        [TestCase]
        public void A18_EventLoopScheduler_FromMain()
        {
            Console.WriteLine($"Main Thread={Thread.CurrentThread.ManagedThreadId}");

            ISubject<int> subject = new Subject<int>();
            var eventLoopSchedulerSource = new EventLoopScheduler();
            var eventLoopSchedulerDest = new EventLoopScheduler();

            subject.ObserveOn(eventLoopSchedulerDest).Subscribe(
                sourceThreadId => Console.WriteLine($"source thread={sourceThreadId}, current thread={Thread.CurrentThread.ManagedThreadId}"));

            object state = new object();
            eventLoopSchedulerSource.SchedulePeriodic(state, TimeSpan.FromSeconds(1), a => subject.OnNext(Thread.CurrentThread.ManagedThreadId));

            //eventLoopSchedulerSource.Schedule<object>(null, TimeSpan.FromSeconds(1), (p_scheduler, p_action) => subject.OnNext(Thread.CurrentThread.ManagedThreadId));

            Thread.Sleep(5000);
        }

        [TestCase]
        public void A19_Merge_FromMain()
        {
            var o1 = Observable.Generate(0,
               i => i < 10,
               i => i + 1,
               i => i + 1,
               i => TimeSpan.FromMilliseconds(100));

            var o2 = Observable.Generate(100,
            i => i < 110,
            i => i + 1,
            i => i + 1,
            i => TimeSpan.FromMilliseconds(1500));

            var o3 = o1.Merge(o2);
            o3.Subscribe(num => Console.WriteLine(num));

            Task.Delay(TimeSpan.FromSeconds(5)).Wait();
        }

        /*
                [TestCase]
                public void A8_SubscribeOn()
                {
                    var e = new EventLoopScheduler();
                    var e2 = new EventLoopScheduler();

                    Console.WriteLine("Method threadId = {0}", Thread.CurrentThread.ManagedThreadId);

                    Observable.Return(42L) // Merge both enumerables into one, whichever the order of appearance   
        //			e.Schedule(() => Console.WriteLine("Do(1): " + Thread.CurrentThread.ManagedThreadId))
                        .Merge(Observable.Timer(TimeSpan.FromSeconds(1),eObservable.Timer(TimeSpan.FromSeconds(1),e))
                        .SubscribeOn(e2)
                        .Subscribe(_ => Console.WriteLine("Do(1): " + Thread.CurrentThread.ManagedThreadId));

                    Thread.Sleep(2000);
                }
        */

        [TestCase]
        public void A20_FromEventPattern()
        {
            ObservableCollection<int> oc = new ObservableCollection<int>();
            
            //oc.CollectionChanged += Oc_CollectionChanged;
            var ocChanged =
                Observable.FromEventPattern<System.Collections.Specialized.NotifyCollectionChangedEventArgs>(oc,
                    nameof(oc.CollectionChanged));
            
            ocChanged.Subscribe(eventArgs => Console.WriteLine(eventArgs?.EventArgs.NewItems?[0]));
            oc.Add(5);

            /*
            // works only in WPF application
            var moves = Observable.FromEventPattern<MouseEventArgs>(frm, nameof(frm.MouseMove));
			using (moves.Subscribe(evt => { lbl.Text = evt.EventArgs.Location.ToString(); }))
			{
				Application.Run(frm);
			}
            */

            /////////////////////////////////////////////////////////////////////////////////////////////////////
            // use FromEventPattern when event is of type EventHanlder, use FromEvent when using custom event
            /////////////////////////////////////////////////////////////////////////////////////////////////////
        }
        
        [TestCase]
        // FromAsyncPattern is Obsolete!!
        public void A21_FromAsyncPattern()
        {
            FileStream fs = File.OpenRead(@"c:\wpm_log.txt");
#pragma warning disable 618
            Func<byte[], int, int, IObservable<int>> read = Observable.FromAsyncPattern<byte[], int, int, int>(fs.BeginRead, fs.EndRead);
#pragma warning restore 618
            byte[] bs = new byte[1024];
            read(bs, 0, bs.Length).Subscribe(bytesRead => Console.WriteLine(bytesRead));
        }

        [TestCase]
        public void A22_FromAsync()
        {
            Func<Task<int>> calcFileLength = () => Task.Delay(TimeSpan.FromSeconds(2)).ContinueWith(t => 1024 /*calc length*/);
            var readLengthObservable = Observable.FromAsync(calcFileLength);
            readLengthObservable.Subscribe(bytesRead => Console.WriteLine(bytesRead));
            Task.Delay(TimeSpan.FromSeconds(3)).Wait();
        }

        [TestCase]
        public void A23_OnErrorHandeling()
        {
            AutoResetEvent autoResetEvent = new AutoResetEvent(false);

            var observable = Observable.Generate(
                0,
                i => i < 3,
                i => i + 1,
                i => i + 1,
                t => TimeSpan.FromMilliseconds(1000));

            observable.Subscribe(num => Console.WriteLine($"first: num={num}"));

            observable.Timeout(TimeSpan.FromMilliseconds(100))
                .Subscribe(num => Console.WriteLine($"second: num={num}"), ex => Console.WriteLine($"Exception: {ex}"));      // onerror handeling

            observable.Where(i => i == 3).Subscribe(n => autoResetEvent.Set());

            autoResetEvent.WaitOne();
        }

        [TestCase]
        public void A30_Buffer()
        {
            var observable = Observable.Generate(
                            0,
                            i => i < 100,
                            i => i + 1,
                            i => i + 1,
                            t => TimeSpan.FromMilliseconds(200));

            var bufferO = observable.Buffer(TimeSpan.FromSeconds(1));

            bufferO.Subscribe(buffer =>
            {
                foreach (var b in buffer)
                {
                    Console.Write(b + ";");
                }
                Console.WriteLine();
            });

            Task.Delay(TimeSpan.FromSeconds(6)).Wait();
        }

        [TestCase]
        public void A31_GenerateWithTime_Group()
        {
            var o1 = Observable.Generate(0,
                i => i < 100,
                i => i + 1,
                i => i % 5,
                i => TimeSpan.FromSeconds(0.01));

            var ooo = o1
                .Select(num => new Tuple<int, DateTime>(num, DateTime.Now))
                .Buffer(TimeSpan.FromMilliseconds(250));

            ooo.Subscribe(list =>
            {
                IEnumerable<Tuple<int, DateTime>> groups =
                    list
                        .GroupBy(tuple => tuple.Item1)
                        .Select(group => @group.ToList())
                        .Select(dataList => dataList.Last());

                foreach (var tuple in groups)
                {
                    Console.WriteLine($"{tuple.Item1},{tuple.Item2.Millisecond}");
                }
            });


            Task.Delay(TimeSpan.FromSeconds(5)).Wait();
        }

        [TestCase]
        public async Task A32_CheckSample()
        {
            var numbersStream = Observable.Generate(0,
                i => i < 100,
                i => i + 1,
                i => i,
                i => TimeSpan.FromSeconds(0.01));

            EventLoopScheduler els = new EventLoopScheduler();

            numbersStream.Subscribe(num => Console.WriteLine(num));

            numbersStream
                .Sample(TimeSpan.FromMilliseconds(100))
                .ObserveOn(els)
                .Subscribe(num => Console.WriteLine($"With sampling {num}"));

            await Task.Delay(1000);
        }

        [TestCase]
        public void A33_CheckSample2()
        {
            ISubject<int> subject = new Subject<int>();

            EventLoopScheduler els = new EventLoopScheduler();

            subject.Sample(TimeSpan.FromSeconds(1)).Subscribe(num => Console.WriteLine(num));

            for (int i = 0; i < 100; i++)
            {
                subject.OnNext(1);
                Thread.Sleep(100);
            }
        }
        [TestCase]
        public void A34_GenerateWithTime_GroupBy()
        {
            var source = Observable.Interval(TimeSpan.FromSeconds(0.01)).Take(100);

            var groups = source.GroupBy(i => i % 3);

            groups.ForEachAsync(group => group.Sample(TimeSpan.FromMilliseconds(200))
                .Subscribe(num => Console.WriteLine($"num={num}, time={DateTime.Now.Millisecond}")));

            Task.Delay(TimeSpan.FromSeconds(2)).Wait();
        }

        [TestCase]
        public void A35_Throttle()
        {
            var numbersStream = Observable.Generate(0,
                i => i < 12,
                i => i + 1,
                i => i,
                i => TimeSpan.FromSeconds(i/2.0));

            numbersStream.Subscribe(num => Console.WriteLine($"{DateTime.Now.ToString("mm:ss.fff")}"));

            numbersStream.Throttle(TimeSpan.FromSeconds(2))
                .Subscribe(num => Console.WriteLine($"{DateTime.Now.ToString("mm:ss.fff")} - 2 seconds of quite"));

            Task.Delay(TimeSpan.FromSeconds(10)).Wait();
        }

        [TestCase]
        public void A36_Defer()
        {
            var obs = Observable.Return(DateTime.Now);
            var deferedObs = Observable.Defer(() => Observable.Return(DateTime.Now));

            obs.Subscribe(dt => Console.WriteLine($"{DateTime.Now.ToLocalTime()} not defered: {dt}"));
            deferedObs.Subscribe(dt => Console.WriteLine($"{DateTime.Now.ToLocalTime()} defered: {dt}"));
            
            Task.Delay(TimeSpan.FromSeconds(2)).Wait();
            
            obs.Subscribe(dt => Console.WriteLine($"{DateTime.Now.ToLocalTime()} not defered: {dt}"));
            deferedObs.Subscribe(dt => Console.WriteLine($"{DateTime.Now.ToLocalTime()} defered: {dt}"));

        }

        [TestCase]
        public void A37_asyncSubscription()
        {
            async Task printNumber(int p_number)
            {
                await Task.Delay(TimeSpan.FromMilliseconds(50)).ContinueWith(t => Console.WriteLine(p_number));
            }

            IObservable<int> generatedObservables = Observable.Generate(1,
                i => i < 10,
                i => i + 1,
                i => i*i,
                i => TimeSpan.FromSeconds(0.5));

            generatedObservables.Subscribe(async i => await printNumber(i));
            Task.Delay(TimeSpan.FromSeconds(5)).Wait();
        }

        [TestCase]
        public void A38_SwitchObservables()
        {
            IObservable<int> GenerateObservable(int i)
            {
                return Observable.Generate(i, j => j < 10, j => j, j => j , j => TimeSpan.FromSeconds(0.5));
            }

            IObservable<IObservable<int>> observablesOfObservables = Observable.Generate(1,
                i => i < 10,
                i => i + 1,
                i => GenerateObservable(i),
                i => TimeSpan.FromSeconds(0.5));

            // switch() - Transforms an observable sequence of observable sequences into an observable sequence producing values only from the most recent observable sequence
            var singleObs = observablesOfObservables.Switch();          
            singleObs.Subscribe(i => Console.WriteLine(i));

            Task.Delay(TimeSpan.FromSeconds(5)).Wait();
        }

        #endregion

        #region Fields

        // private static readonly ILog s_log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        #endregion
    }
}